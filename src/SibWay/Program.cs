using System;
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
using SibWay.Settings;
using SibWay.SibWayApi;


namespace SibWay
{
    internal static class Program
    {
        private static ILogger _logger;
        private static EventBus _eventBus;
        private static App _app;

        public static async Task Main(string[] args)
        {
            Configure();
            
            //Создание табло SibWay----------------------------------------------------------
            var (_, isFailureSettLoad, xmlSibWaySett, error) = await SettingsLoader.LoadXmlSibWaySettings();
            if (isFailureSettLoad)
            {
                _logger.Fatal("{xmlSettings} {error}", "Ошибка загрузки XML", error);
                Console.ReadKey();
                return;
            }
            var sibWayProxies= xmlSibWaySett.Select(xmlSett => new SibWayProxy(xmlSett.SettingSibWay, _eventBus, Log.Logger)).ToList();
             _app= new App(sibWayProxies, _eventBus, Log.Logger);
            
            //Создание httpServer.-------------------------------------------------------------------
            var (_, isFailureHttpSettLoad, xmlHttpServSett, errorHttpSettLoad) = await SettingsLoader.LoadHttpServerSettings();
            if (isFailureHttpSettLoad)
            {
                _logger.Fatal("{xmlSettings} {error}", "Ошибка загрузки XML", errorHttpSettLoad);
                Console.ReadKey();
                return;
            }
            var httpServer= new HttpServer(xmlHttpServSett.EndpointAddress, _eventBus, Log.Logger);
            
            //Запуск фоновых задач.-------------------------------------------------------------------
            //1. Задачи коннекта всех табло SibWay.
            _app.Init();
            //var sibWayReconnectTasks = sibWayProxies.Select(sw => sw.ReConnect()).ToList();
            //2. Создание HttpListener и запуск BG обработки запросов.
            var listenHttpTask= httpServer.StartListen();
            
            //DEBUG-------------------
             //await Task.Delay(3000);
             //StopHttpListenerCommand(httpServer);
            // await Task.Delay(8000);
            // StartHttpListenerCommand(httpServer);
            //DEBUG------------------
            
            var allTasks = new List<Task<Result>> {listenHttpTask};
            //allTasks.AddRange(sibWayReconnectTasks);
            var bg= new BackgroundProcessService(Log.Logger, allTasks.ToArray());
            Log.Information("Allpication Loaded ...");
            await bg.WaitAll();
            Log.Information("Allpication Stoped");
            Log.CloseAndFlush();
        }


        private static void Configure()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File("logs\\SibWay.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            _logger = Log.Logger;
            
            _eventBus= new EventBus();
        }
        
        
        // public static void StartHttpListenerCommand(HttpServer httpServer)
        // {
        //     var (_, isFailure, httpListenTask, startHttpListenError) = httpServer.StartListen();
        //     if (isFailure)
        //     {
        //         Log.Logger.Error("Ошибка запуска HttpListener '{HttpListener}'", startHttpListenError);
        //     }
        //     else
        //     {
        //         Log.Logger.Information("HttpListener '{HttpListener}'", "Успешно запущен");
        //     }
        // }
        //
        // public static void StopHttpListenerCommand(HttpServer httpServer)
        // {
        //     var (_, isFailure, error) = httpServer.StopListen();
        //     if (isFailure)
        //     {
        //         Log.Logger.Error("Ошибка останова HttpListener '{HttpListener}'", error);
        //     }
        //     else
        //     {
        //         Log.Logger.Information("HttpListener '{HttpListener}'", "Успешно остановлен");
        //     }
        // }
    }
}