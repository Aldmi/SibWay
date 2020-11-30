using System.Collections.Generic;
using System.Linq;
using SibWay.Domain.Dto;
using SibWay.SibWayApi;

namespace SibWay.Domain
{
    public static class Mapper
    {
        public static IList<ItemSibWay> MapAdInputType4XmlDtoContainer2ListItemSibWay(AdInputType4XmlDtoContainer adInputType)
        {
            var res= adInputType.Trains.Select(ad =>
            {
                var sibWay = new ItemSibWay()
                {
                     
                };
                return sibWay;
            }).ToList();

            return res;
        }
    }
}