using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
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
        private readonly IDisposable _getDataEventItemRxLifeTime;


        #region ctor
        public App(IReadOnlyList<SibWayProxy> sibWays, EventBus eventBus, ILogger logger)
        {
            _sibWays = sibWays ?? throw new ArgumentNullException(nameof(sibWays));
            _eventBus = eventBus;
            _logger = logger;
            _getDataEventItemRxLifeTime= eventBus.Subscrube<InputDataEventItem>(
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
        #endregion
        

        #region Methods
        /// <summary>
        /// Получение данных с EventBus.
        /// И выбор табло для отправки.
        /// </summary>
        private async void GetDataRxHandler(InputDataEventItem data)
        {
            _logger.Information("Полученны данные: {@App}", data);
            //1. Выбрать нужное табло для отправки.
            var table= _sibWays.FirstOrDefault(sw => sw.SettingSibWay.TableName == data.TableName);
            if (table == null)
            {
                var error = $"Табло не найденно по имени: '{data.TableName}'";
                _eventBus.Publish(new SibWayResponseItem(data.Id, "", Result.Failure(error)));
            }
            else
            {
                await SendData(table, data);
            }
        }


        /// <summary>
        /// Отправка данных на табло.
        /// </summary>
        private async Task SendData(SibWayProxy table, InputDataEventItem data)
        {
            var res= await table.SendData(data.Datas);
            //await Task.Delay(500);//DEBUG
            _eventBus.Publish(new SibWayResponseItem(data.Id, table.SettingSibWay.TableName, res));
        }
        #endregion


        #region DisposePattern
        public void Dispose()
        {
            _getDataEventItemRxLifeTime.Dispose();
            foreach (var sibWay in _sibWays)
            {
                sibWay.Dispose();
            }
        }
        #endregion
    }
}