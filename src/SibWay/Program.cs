using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
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
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File("logs\\myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
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
            var sibWayProxies= xmlSibWaySett.Select(xmlSett => new SibWayProxy(xmlSett.SettingSibWay, Log.Logger)).ToList();
            var sibWayReconnectTasks = sibWayProxies.Select(sw => sw.ReConnect()).ToArray();
            var app= new App(sibWayProxies, eventBus, Log.Logger);
            
            //Создание HttpListener и запуск BG обработки запросов.
            
            eventBus.Publish(new GetDataEventItem{TableName = "dsdsd"});//DEBUG
            eventBus.Publish("dsds");//DEBUG
            var bg= new BackgroundProcessService(Log.Logger, sibWayReconnectTasks);
            
            Log.Information("SibWay Start !!!");
            await bg.WhenAll();
            Log.Information("SibWay Stop !!!");
            Log.CloseAndFlush();
        }
    }
}