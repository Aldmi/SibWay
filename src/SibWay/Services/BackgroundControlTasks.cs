using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Serilog;

namespace SibWay.Services
{
    public class BackgroundControlTasks
    {
        private readonly ILogger _logger;
        private readonly BlockingCollection<Task<Result>> _buffer= new BlockingCollection<Task<Result>>(50); //ограничить до 50 поступление данных Продюссером.


        public BackgroundControlTasks(ILogger logger)
        {
            _logger = logger;
        }
        
        
        /// <summary>
        /// контроль за задачей обработки запроса.
        /// По завершениию обработки удалить задачу из очереди.
        /// </summary>
        public Task<Result> StartControll(CancellationToken ct)
        {
            return Task.Run(async () =>
            {
                foreach (var task in _buffer.GetConsumingEnumerable())
                {
                    var res = await task;
                    var strResult = res.ToString();
                    _logger.Information("{BackgroundControlTasks}  Результат: '{res}'","Фоновая задача завершена", strResult);
                }
                _logger.Information("{BackgroundControlTasks}","Цикл ожидания задач завершен");
                return Result.Success();
            }, ct);
        }

        
        /// <summary>
        /// Добавить задачу в коллекцию
        /// </summary>
        public void AddTask(Task<Result> task)
        {
            _buffer.Add(task);
        }
        
        
        public void AddTask(IEnumerable<Task<Result>> taskList)
        {
            foreach (var task in taskList)
            {
                AddTask(task);
            }
        }

        
        /// <summary>
        /// Команда о прекрашении ожидания добавления элементов в Буфер
        /// </summary>
        public void CompleteAdding()
        {
            _buffer.CompleteAdding();
        }
    }
}