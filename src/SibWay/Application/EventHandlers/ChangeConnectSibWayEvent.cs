namespace SibWay.Application.EventHandlers
{
    public class ChangeConnectSibWayEvent
    {
        /// <summary>
        /// Имя табло
        /// </summary>
        public string TableName { get; }

        public string StatusString { get; }
        public bool IsConnect{ get; }
        
        
        public ChangeConnectSibWayEvent(string tableName, string statusString, bool isConnect)
        {
            TableName = tableName;
            StatusString = statusString;
            IsConnect = isConnect;
        }
    }
}