using System;
using System.Collections.Generic;
using SibWay.SibWayApi;

namespace SibWay.Application.EventHandlers
{
    public class InputDataEventItem
    {
        public Guid Id { get; set; }
        public string TableName { get; set; }
        public List<ItemSibWay> Datas { get; set; }
    }
}