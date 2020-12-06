using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Serilog;

namespace SibWay.Services
{
    public class BackgroundProcessService
    {
        private readonly Task<Result>[] _process;
        private readonly ILogger _logger;

        public BackgroundProcessService(ILogger logger, params Task<Result>[] process)
        {
            _process = process;
            _logger = logger;
        }

        
        public async Task WaitAll()
        {
            try
            {
                //просто перечисляем список задач, а они поступают в порядке выполнения а не по порядку списка.
                foreach (var bucket in Interleaved(_process)) 
                {
                    var t = await bucket;
                    var result = await t;
                    _logger.Debug("Task завершился с результатом: '{BackgroundProcess}'", result.ToString() );
                }
            }
            catch (Exception e)
            {
                _logger.Fatal($"Background Process Exception: {e}");
            }
            _logger.Information("All Background Process Completed");
        }
        
        
        
        public static Task<Task<T>>[] Interleaved<T>(IEnumerable<Task<T>> tasks)
        {
            var inputTasks = tasks.ToList();

            var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
            var results = new Task<Task<T>>[buckets.Length];
            for (int i = 0; i < buckets.Length; i++) 
            {
                buckets[i] = new TaskCompletionSource<Task<T>>();
                results[i] = buckets[i].Task;
            }

            int nextTaskIndex = -1;
            Action<Task<T>> continuation = completed =>
            {
                var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
                bucket.TrySetResult(completed);
            };

            foreach (var inputTask in inputTasks)
                inputTask.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return results;
        }
    }
}