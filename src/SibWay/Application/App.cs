using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SibWay.Application.EventHandlers;
using SibWay.Infrastructure;
using SibWay.SibWayApi;


namespace SibWay.Application
{
    /// <summary>
    /// Управляет отправкой данных на все табло SibWay.
    /// Также контроллирует статус табел.
    /// </summary>
    public class App : IDisposable
    {
        private readonly IReadOnlyList<SibWayProxy> _sibWays;
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;
        private readonly IDisposable GetDataEventItemRxLifeTime;


        public App(IReadOnlyList<SibWayProxy> sibWays, EventBus eventBus, ILogger logger)
        {
            _sibWays = sibWays ?? throw new ArgumentNullException(nameof(sibWays));
            _eventBus = eventBus;
            _logger = logger;
            GetDataEventItemRxLifeTime= eventBus.Subscrube<InputDataEventItem>(
                GetDataRxHandler,
                ex =>
                {
                    _logger.Error($"Задача получения данных завершилась с ОШИБКОЙ {ex}");
                },
                () =>
                {
                    _logger.Warning("Задача получения данных Была остановленна");
                });
        }

        
        private async void GetDataRxHandler(InputDataEventItem data)
        {
            _logger.Information("Полученны данные: {@App}", data);
            await SendData(data);
        }


        /// <summary>
        /// Получает данные с шины, и вызывает этот метод.
        /// </summary>
        private async Task SendData(InputDataEventItem data)
        {
            //1. Выбрать нужное табло для отправки
           var table= _sibWays.FirstOrDefault();

           //Отправить данные на табло и результат отпарвки опубликовать на шину данных
           var res= await table.SendData(data.Datas);
           _eventBus.Publish(new SibWayResponseItem(res));
           
           //Оценить Result залогировать ошибку 
        }

        
        public void Dispose()
        {
            GetDataEventItemRxLifeTime.Dispose();
            foreach (var sibWay in _sibWays)
            {
                sibWay.Dispose();
            }
        }
    }
}