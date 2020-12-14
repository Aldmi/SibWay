using System;

namespace SibWay.SibWayApi
{
    public class ItemSibWay
    {
        public bool IsActive { get; set; }
        public string TypeTrain { get; set; }
        public string NumberOfTrain { get; set; }
        public string PathNumber { get; set; }
        public string Event { get; set; }
        public string Addition { get; set; }
        public string Command { get; set; }

        public string StationArrival { get; set; }
        public string StationDeparture { get; set; }

        public string DirectionStation { get; set; }

        public string Note { get; set; }
        public string DaysFollowingAlias { get; set; }

        public DateTime? TimeArrival { get; set; }
        public DateTime? TimeDeparture { get; set; }

        public DateTime? DelayTime { get; set; }
        public DateTime ExpectedTime { get; set; }
        public TimeSpan? StopTime { get; set; }


        public static ItemSibWay CreateClearItem()
        {
            const string defaultStr = " ";
            return new ItemSibWay
            {
                Command = "Clear",
                IsActive = true,
                Addition = defaultStr,
                Event = defaultStr,
                Note = defaultStr,
                DirectionStation = defaultStr,
                PathNumber = defaultStr,
                StationArrival = defaultStr,
                StationDeparture = defaultStr,
                TypeTrain = defaultStr,
                DaysFollowingAlias = defaultStr,
                NumberOfTrain = defaultStr,
                ExpectedTime = DateTime.MinValue,
                DelayTime = null,
                TimeArrival = null,
                TimeDeparture = null,
                StopTime = null
            };
        }
    }
}