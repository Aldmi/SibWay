using System.Collections.Generic;
using SibWay.SibWayApi;

namespace SibWay.Application.EventHandlers
{
    public class InputDataEventItem
    {
        public string TableName { get; set; }
        public List<ItemSibWay> Datas { get; set; }
    }
}