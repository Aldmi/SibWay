using System;
using System.Threading.Tasks;
using Serilog;

namespace SibWay.Services
{
    public class BackgroundProcessService
    {
        private readonly Task[] _process;
        private readonly ILogger _logger;

        public BackgroundProcessService(ILogger logger, params Task[] process)
        {
            _process = process;
            _logger = logger;
        }

        
        public void WaitAll()
        {
            try
            {
                Task.WaitAll(_process);
            }
            catch (Exception e)
            {
                _logger.Fatal($"Background Process Exception: {e}");
            }
            _logger.Information("All Background Process Completed");
        }
    }
}