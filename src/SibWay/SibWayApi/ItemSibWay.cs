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
            return new ItemSibWay
            {
                Command = "Clear",
                IsActive = true,
                Addition = String.Empty,
                Event = String.Empty,
                Note = String.Empty,
                DirectionStation = String.Empty,
                PathNumber = String.Empty,
                StationArrival = String.Empty,
                StationDeparture = String.Empty,
                TypeTrain = String.Empty,
                DaysFollowingAlias = String.Empty,
                NumberOfTrain = String.Empty,
                ExpectedTime = DateTime.MinValue,
                DelayTime = DateTime.MinValue,
                TimeArrival = DateTime.MinValue,
                TimeDeparture = DateTime.MinValue,
                StopTime = TimeSpan.Zero
            };
        }
    }
}