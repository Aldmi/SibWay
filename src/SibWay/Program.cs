using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SibWay.Application;
using SibWay.Application.EventHandlers;
using SibWay.Infrastructure;
using SibWay.Services;
using SibWay.Settings;
using SibWay.SibWayApi;


namespace SibWay
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var eventBus= new EventBus();
            eventBus.Subscrube<GetDataEventItem>(async o =>  //DEBUG
            {
                await Task.Delay(1000);
            });

            //Создание App на базе sibWayProxies и запуск BG ReConnect.
            //var xmlSibWaySett = await SettingsLoader.LoadXmlSibWaySettings();
            var xmlSibWaySett = new List<XmlSibWaySettings>
            {
                new XmlSibWaySettings {
                    SettingSibWay = new SettingSibWay("192.168.1.100", "12345", null, "2000", "2000", "5")
                 }
            };
            var sibWayProxies= xmlSibWaySett.Select(xmlSett => new SibWayProxy(xmlSett.SettingSibWay)).ToList();
            var sibWayReconnectTasks = sibWayProxies.Select(sw => sw.ReConnect()).ToArray();
            var app= new App(sibWayProxies, eventBus);
            
            //Создание HttpListener и запуск BG обработки запросов.
            
            eventBus.Publish(new GetDataEventItem{TableName = "dsdsd"});//DEBUG
            eventBus.Publish("dsds");//DEBUG
            var bg= new BackgroundProcessService(sibWayReconnectTasks);
            await bg.WhenAll();
        }
    }
}