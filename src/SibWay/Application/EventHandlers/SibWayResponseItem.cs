using CSharpFunctionalExtensions;

namespace SibWay.Application.EventHandlers
{
    public class SibWayResponseItem
    {
        public SibWayResponseItem(string tableName, Result result)
        {
            Result = result;
            TableName = tableName;
        }
        
        public string TableName { get; }
        public Result Result { get; }
    }
}