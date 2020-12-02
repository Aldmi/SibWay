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
        
        public Guid Id { get; }
        public string TableName { get; }
        public Result Result { get; }
    }
}