using System;
using System.Collections.Generic;
using SibWay.SibWayApi;

namespace SibWay.Application.EventHandlers
{
    public class InputDataEventItem
    {
        /// <summary>
        /// Уникальный Id порции данных (используем Id запроса)
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Имя табло
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// Данные для табло
        /// </summary>
        public List<ItemSibWay> Datas { get; set; }
    }
}