using System;
using CSharpFunctionalExtensions;

namespace SibWay.Application.EventHandlers
{
    public class SibWayResponseItem
    {
        public SibWayResponseItem(Guid id, string tableName, Result result)
        {
            Id = id;
            TableName = tableName;
            Result = result;
        }
        
        /// <summary>
        /// Уникальный Id порции ответа (используем Id запроса)
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Имя табло
        /// </summary>
        public string TableName { get; }
        
        /// <summary>
        /// Результат отправки порции данных.
        /// </summary>
        public Result Result { get; }
    }
}