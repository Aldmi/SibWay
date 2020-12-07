using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Serilog;
using SibWay.Application.EventHandlers;
using SibWay.Infrastructure;
using SibWay.Services;
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
        private readonly BackgroundControlTasks _backgroundControlTasks; 
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;
        private readonly IDisposable _getDataEventItemRxLifeTime;
        private readonly IDisposable _changeConnectRxLifeTime;

        
        #region ctor
        public App(IReadOnlyList<SibWayProxy> sibWays, EventBus eventBus, ILogger logger)
        {
            _sibWays = sibWays ?? throw new ArgumentNullException(nameof(sibWays));
            _eventBus = eventBus;
            _logger = logger;
            _backgroundControlTasks = new BackgroundControlTasks(_logger);
            _getDataEventItemRxLifeTime= eventBus.Subscrube<InputDataEventItem>(
                GetDataRxHandler,
                ex =>
                {
                    _logger.Error($"Обработка события получения данных завершилась с ОШИБКОЙ {ex}");
                },
                () =>
                {
                    _logger.Warning("Обработка события получения данных Была остановленна");
                });
            _changeConnectRxLifeTime = eventBus.Subscrube<ChangeConnectSibWayEvent>(
                ChangeConnectRxHandler, 
                ex =>
                {
                    _logger.Error($"Обработка события ChangeConnect завершилась с ОШИБКОЙ {ex}");
                },
                () =>
                {
                    _logger.Warning("Обработка события ChangeConnect Была остановленна");
                });
        }
        #endregion



        #region RxEventHandlers
        /// <summary>
        /// Получение события изменения IsConnect с EventBus.
        /// Если IsConnect == false, то выполним реконнект и отправк данных очистки
        /// </summary>
        private void ChangeConnectRxHandler(ChangeConnectSibWayEvent connChangeEvent)
        {
            if (connChangeEvent.IsConnect) return;
            var t= ReconnectAndCommandClear(GetByName(connChangeEvent.TableName));
            _backgroundControlTasks.AddTask(t);
        }

        
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
        #endregion
        
        
        #region Methods
        
        /// <summary>
        /// Инициализация приложения.
        /// Выполнение комманды ReconnectAndCommandClear для всех табло
        /// </summary>
        public Task<Result> Init(CancellationToken ct)
        {
            var sibWayReconnectTaskList = _sibWays.Select(ReconnectAndCommandClear).ToList();
            _backgroundControlTasks.AddTask(sibWayReconnectTaskList);
            var controlTask = _backgroundControlTasks.StartControll(ct);
            return controlTask;
        }
        
        
        /// <summary>
        /// последовательно выполнить ReConnect и затем SendDataClear для табло.
        /// </summary>
        private async Task<Result> ReconnectAndCommandClear(SibWayProxy sibWay)
        {
            var connectRes= await sibWay.ReConnect();
            var clearRes= await sibWay.SendDataClear(); //Долгая задача
            var res= Result.Combine(connectRes, clearRes);
            return res;
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
        
        private SibWayProxy GetByName(string tableName)=>  _sibWays.FirstOrDefault(sw => sw.SettingSibWay.TableName == tableName);
        #endregion


        #region DisposePattern
        public void Dispose()
        {
            _getDataEventItemRxLifeTime.Dispose();
            _changeConnectRxLifeTime.Dispose();
            foreach (var sibWay in _sibWays)
            {
                sibWay.Dispose();
            }
        }
        #endregion
    }
}