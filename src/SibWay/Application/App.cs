using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly EventBus _eventBus;


        public App(IReadOnlyList<SibWayProxy> sibWays, EventBus eventBus)
        {
            _sibWays = sibWays ?? throw new ArgumentNullException(nameof(sibWays));
            _eventBus = eventBus;
            eventBus.Subscrube<GetDataEventItem>(GetData);

        }

        
        private async void GetData(GetDataEventItem obj)
        {
            
        }


        /// <summary>
        /// Получает данные с шины, и вызывает этот метод.
        /// </summary>
        private async Task SendData()
        {
            //1. Выбрать нужное табло для отправки
           var table= _sibWays.FirstOrDefault();

           var res= table.SendData(null); //Возвращать Result
           
           //Оценить Result залогировать ошибку 
        }

        
        public void Dispose()
        {
            foreach (var sibWay in _sibWays)
            {
                sibWay.Dispose();
            }
        }

        
        

    }
}