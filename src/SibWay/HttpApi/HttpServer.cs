﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Serilog;
using SibWay.Application.EventHandlers;
using SibWay.Infrastructure;
using SibWay.Services;
using SibWay.Settings;

namespace SibWay.HttpApi
{
    public class HttpServer
    {
        #region fields
        private readonly HttpListener _listener;
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;
        private CancellationTokenSource _cts;
        private readonly  HashSet<Task<Result>> _httpContextHandlers = new HashSet<Task<Result>>(); //TODO: сделать контроль этих ЗАДАЧ. Т.Е. выполненные задачи убирать из списка и писать в лог результат ввыполнения.
        #endregion


        #region prop
        public bool IsStart => _cts != null && !_cts.IsCancellationRequested;
        #endregion
        
        
        #region ctor
        public HttpServer(string uriListen, EventBus eventBus, ILogger logger)
        {
            if(string.IsNullOrEmpty(uriListen))
                throw new ArgumentException(nameof(uriListen));
            
            _listener = new HttpListener();
            _listener.Prefixes.Add(uriListen);
            
            _eventBus = eventBus;
            _logger = logger;
        }
        #endregion
        
        
        
        public Result<Task> StartListen()
        {
            if(IsStart) //Токен создан и НЕ Остановлен
            {
                return Result.Failure<Task>("Задача уже запущена и не была остановленна");
            }
            _cts =  new CancellationTokenSource();
            return ListenHttpAsync(_cts.Token);
        }
        
        
        public Result StopListen()
        {
            if(!IsStart) //Токен НЕ созданн или Остановленн 
            {
                return Result.Failure<Task>("Задача НЕ запущена или была остановленна!!!");
            }
            _cts.Cancel();
            return Result.Success();
        }


        private async Task ListenHttpAsync(CancellationToken ct)
        {
            _listener.Start();
            _logger.Information("{HttpServer}", "Ожидание запросов ...");
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var handler = HttpListenerContextHandlerAsync(context, ct);
                    _httpContextHandlers.Add(handler);
                }
                catch (TaskCanceledException) { }
            }
        }


        private async Task<Result> HttpListenerContextHandlerAsync(HttpListenerContext context, CancellationToken ct)  //TODO: заменить на ValueTask и возможно возвращать Result<HttpListenerContext>, чтобы после отработки Task закрыть conext вручную
        {
           var contextRes= await RequestHandler(context.Request)
                .Bind(inDate =>
                {
                    //Публикуем входные данные на шину.
                    _eventBus.Publish(inDate);
                    return Result.Success();
                })
                .Bind(async () =>
                {
                    //Ждем ответа от SibWay об отправки входных данных. 
                    var sibWayResponse= await _eventBus.Subscrube<SibWayResponseItem>().FirstAsync();
                    return sibWayResponse.Result;
                })
                .Bind(async () =>
                {
                    //Формируем ответ клиенту.
                    return await ResponseHandler(context.Response);
                })              //TODO: возможно передавать ответ
                .Finally(result => result);
           
           return contextRes;
        }


        private async Task<Result<InputDataEventItem>> RequestHandler(HttpListenerRequest request) //TODO: пременить Result.Combine
        {
            _logger.Information("{HttpServer}", "Получили запрос");
            var tableNameRes = ParseUrl(request);
            var xmlStrRes= await ParsePostBody(request);
            var postDataRes= Result.Combine(tableNameRes, xmlStrRes)
                .Bind(() => XmlHelpers.DeserializeFromXml(xmlStrRes.Value))
                .Bind(deserializedXml => Mapper.MapAdInputType4XmlDtoContainer2ListItemSibWay(deserializedXml))
                .Bind(listItemSibWay =>
                {
                    var inType = new InputDataEventItem
                    {
                        TableName = tableNameRes.Value,
                        Datas = listItemSibWay
                    };
                    return Result.Success(inType);
                })
                .Finally(result => result);
            return postDataRes;
        }

        
        private Result<string> ParseUrl(HttpListenerRequest request)
        {
            if (request.HttpMethod != "POST")
                return Result.Failure<string>("HttpMethod != POST");
            
            var tableName = request.RawUrl.Split('/').Last();
            if (tableName == "SendDataXmlMultipart4Devices")
                return Result.Failure<string>("Имя табло не указано в адрессе!!");

            return tableName;
        }
        
        private async Task<Result<string>> ParsePostBody(HttpListenerRequest request)
        {
            if (request.HttpMethod != "POST")
                return Result.Failure<string>("HttpMethod != POST");
            
            using (var input = request.InputStream)
            {
                var streamReader = new StreamReader(input);
                var body = await streamReader.ReadToEndAsync();
                var clearBody = body
                    .Replace("\r", String.Empty)
                    .Replace("\n", String.Empty)
                    .Replace("\t", String.Empty);
                const string pattern = "(<tlist>.*?</tlist>)";
                var match = Regex.Match(clearBody, pattern);
                if (!match.Success)
                    return Result.Failure<string>("Формат XML не верен");
                    
                var xmlString = match.Groups[1].Value;
                input.Close();
                return Result.Success(xmlString);
            }
        }
        
        
        //TODO: вынести в отдельный класс
        private async Task<Result<InputDataEventItem>> ParseInpudDateFromHttpRequest(HttpListenerRequest request)
        {
            if (request.HttpMethod != "POST")
                return Result.Failure<InputDataEventItem>("HttpMethod != POST");
            
            var tableName = request.RawUrl.Split('/').Last();
            if (tableName == "SendDataXmlMultipart4Devices")
                return Result.Failure<InputDataEventItem>("Имя табло не указано в адрессе!!");
            
            using (var input = request.InputStream)
            {
                var streamReader = new StreamReader(input);
                var body = await streamReader.ReadToEndAsync();
                var clearBody = body
                    .Replace("\r", String.Empty)
                    .Replace("\n", String.Empty)
                    .Replace("\t", String.Empty);
                const string pattern = "(<tlist>.*?</tlist>)";
                var match = Regex.Match(clearBody, pattern);
                if (!match.Success)
                    return Result.Failure<InputDataEventItem>("Формат XML не верен");
                    
                var xmlString = match.Groups[1].Value;
                input.Close();
            }

            
            return Result.Success(new InputDataEventItem{TableName = $"TableName_{DateTime.Now:T}"});
        }
        
        
        private async Task<Result> ResponseHandler(HttpListenerResponse response)
        {
            _logger.Information("{HttpServer}", "Готовим ответ ...");
            
            
     
            
            return Result.Success();
        }

        
        
        // private  Task ListenHttpAsync(CancellationToken ct)
        // {
        //     _listener.Start();
        //     _logger.Information("{HttpServer}", "Ожидание запросов ...");
        //     
        //     var listenTask= Task.Factory.StartNew(async () =>
        //     {
        //         while (!ct.IsCancellationRequested)
        //         {
        //             try
        //             {
        //                 await Task.Delay(1000, ct);
        //                 _eventBus.Publish(new GetDataEventItem{TableName = $"TableName_{DateTime.Now:T}"});//DEBUG
        //             }
        //             catch (TaskCanceledException ex) { }
        //         }
        //         _eventBus.OnCompleted();
        //     }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        //     return listenTask;
        // }
    }
}