using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using SibWay.Application.Dto;
using SibWay.SibWayApi;

namespace SibWay.Services
{
    public static class Mapper
    {
        public static Result<List<ItemSibWay>> MapAdInputType4XmlDtoContainer2ListItemSibWay(AdInputType4XmlDtoContainer adInputType)
        {
            try
            {
                var res= adInputType.Trains.Select(ad => new ItemSibWay()
                {
                    TypeTrain = ad.TypeName,
                    Addition = ad.Addition,
                    Event = ad.Direction == "0" ? "Прибытие" :"Отправление",
                    Note = ad.Note,
                    StationDeparture = ad.StartStation,
                    StationArrival = ad.EndStation,
                    PathNumber = ad.TrackNumber,
                    NumberOfTrain = ad.TrackNumber,
                    TimeDeparture = DateTimeParser(ad.SndDateTime),
                    TimeArrival = DateTimeParser(ad.RecDateTime),
                    DelayTime = DateTimeParser(ad.RecDateTime),
                    StopTime = TimeSpanParser(ad.HereDateTime),
                    ExpectedTime = DateTimeParser(ad.ExpectedDateTime),
                    DaysFollowingAlias = ad.DaysOfGoingAlias,
                    DirectionStation = ad.DirectionStation,
                    IsActive = true,
                    Command = "NOT USE"
                }).ToList();
                return res;
            }
            catch (Exception ex)
            {
                return Result.Failure<List<ItemSibWay>>($"Ошибка маппинга AdInputType4Xml->ItemSibWay: '{ex}'");
            }
            
            
            DateTime DateTimeParser(string str)
            {
                return DateTime.TryParse(str, out var date) ? date : DateTime.MinValue;
            }
            
            TimeSpan TimeSpanParser(string str)
            {
                return TimeSpan.TryParse(str, out var time) ? time : TimeSpan.MinValue;
            }
        }
    }
}