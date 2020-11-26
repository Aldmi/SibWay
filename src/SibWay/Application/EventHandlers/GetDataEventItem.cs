using System.Collections.Generic;
using SibWay.SibWayApi;

namespace SibWay.Application.EventHandlers
{
    public class GetDataEventItem
    {
        public string TableName { get; set; }
        public List<ItemSibWay> Datas { get; set; }
    }
}