using System;
using System.Threading.Tasks;

namespace SibWay.Services
{
    public class BackgroundProcessService
    {
        private readonly Task[] _process;

        public BackgroundProcessService(params Task[] process)
        {
            _process = process;
        }


        public async Task WhenAll()
        {
            try
            {
                await Task.WhenAll(_process);
            }
            catch (Exception e)
            {
                Console.WriteLine($" Background Process Exception: {e}");
                throw;
            }
            Console.WriteLine("All Background Process Completed");
        }
    }
}