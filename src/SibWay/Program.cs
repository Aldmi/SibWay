using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Serilog;
using Serilog.Events;
using SibWay.Application;
using SibWay.HttpApi;
using SibWay.Infrastructure;
using SibWay.Services;
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
            var httpServer= new HttpServer("http://localhost:44888/api/InputData/SendDataXmlMultipart4Devices/",eventBus, Log.Logger);
            StartHttpListenerCommand(httpServer);
            //DEBUG-------------------
             //await Task.Delay(3000);
             //StopHttpListenerCommand(httpServer);
            // await Task.Delay(8000);
            // StartHttpListenerCommand(httpServer);
            //DEBUG------------------
            
            
            var bg= new BackgroundProcessService(Log.Logger, sibWayReconnectTasks); //TODO: Поместить httpListenTask ???
            Log.Information("SibWay Start !!!");
            await bg.WhenAll();
            Log.Information("SibWay Stop !!!");
            Log.CloseAndFlush();
        }


        public static void StartHttpListenerCommand(HttpServer httpServer)
        {
            var (_, isFailure, httpListenTask, startHttpListenError) = httpServer.StartListen();
            if (isFailure)
            {
                Log.Logger.Error("Ошибка запуска HttpListener '{HttpListener}'", startHttpListenError);
            }
            else
            {
                Log.Logger.Information("HttpListener '{HttpListener}'", "Успешно запущен");
            }
        }
        
        public static void StopHttpListenerCommand(HttpServer httpServer)
        {
            var (_, isFailure, error) = httpServer.StopListen();
            if (isFailure)
            {
                Log.Logger.Error("Ошибка останова HttpListener '{HttpListener}'", error);
            }
            else
            {
                Log.Logger.Information("HttpListener '{HttpListener}'", "Успешно остановлен");
            }
        }
    }
}