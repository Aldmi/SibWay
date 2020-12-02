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
            var sibWayProxies= xmlSibWaySett.Select(xmlSett => new SibWayProxy(xmlSett.SettingSibWay, Log.Logger)).ToList();
            var app= new App(sibWayProxies, _eventBus, Log.Logger);
            
            //Создание httpServer.-------------------------------------------------------------------
            var httpServer= new HttpServer("http://localhost:44888/api/InputData/SendDataXmlMultipart4Devices/", _eventBus, Log.Logger);
            
            //Запуск фоновых задач.-------------------------------------------------------------------
            //1. Задачи коннекта всех табло SibWay.
            var sibWayReconnectTasks = sibWayProxies.Select(sw => sw.ReConnect()).ToList();
            //2. Создание HttpListener и запуск BG обработки запросов.
            var listenHttpTask= httpServer.StartListen().Value;
            
            
            //DEBUG-------------------
             //await Task.Delay(3000);
             //StopHttpListenerCommand(httpServer);
            // await Task.Delay(8000);
            // StartHttpListenerCommand(httpServer);
            //DEBUG------------------

            
            var allTasks = new List<Task> {listenHttpTask};
            allTasks.AddRange(sibWayReconnectTasks);
            var bg= new BackgroundProcessService(Log.Logger, allTasks.ToArray());
            Log.Information("SibWay Start !!!");
            bg.WaitAll();
            Log.Information("SibWay Stop !!!");
            Log.CloseAndFlush();
        }


        private static void Configure()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File("logs\\myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            _logger = Log.Logger;
            
            _eventBus= new EventBus();
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