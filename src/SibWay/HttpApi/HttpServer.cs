using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Serilog;
using SibWay.Application.Dto;
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



        #region Methode
        public Task<Result> StartListen()
        {
            if(IsStart)
            {
                return Task.FromResult<Result>(Result.Failure<Task>("Задача уже запущена и не была остановленна"));
            }
            _cts =  new CancellationTokenSource();
            try
            {
                _listener.Start();
            }
            catch (Exception e)
            {
                return Task.FromResult(Result.Failure(e.Message));
            }
            return ListenHttpAsync(_cts.Token);
        }
        
        
        public Result StopListen()
        {
            if(!IsStart)
            {
                return Result.Failure<Task>("Задача НЕ запущена или была остановленна!!!");
            }
            _cts.Cancel();
            _listener.Stop();
            return Result.Success();
        }

        
        private async Task<Result> ListenHttpAsync(CancellationToken ct)
        {
            _logger.Information("{HttpServer}", "Ожидание запросов ...");
            while (true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var handler = HttpListenerContextHandlerAsync(context, ct);
                    _ = handler.ContinueWith(t =>
                      {
                          if (t.IsCompleted)
                          {
                              var res = t.Result;
                              if (res.IsSuccess)
                              {
                                  _logger.Information("{HttpServer}", "ЗАПРОС ОБРАБОТАН УСПЕШНО");
                              }
                              else
                              {
                                  _logger.Error("{HttpServer} '{responseResult}'", "ЗАПРОС ОБРАБОТАН С ОШИБКОЙ", res.Error);
                              }
                          }
                          else
                          {
                              _logger.Error("{HttpServer}", "Task ОБРАБОТКИ запроса завершилась Не удачей.");
                          }
                      }, ct);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.Warning("{HttpServer}","Отмена ожидания запросов", ex.Message);
                    return Result.Success("Отмена ожидания запросов");
                }
            }
        }


        private async Task<Result> HttpListenerContextHandlerAsync(HttpListenerContext context, CancellationToken ct)  //TODO: заменить на ValueTask и возможно возвращать Result<HttpListenerContext>, чтобы после отработки Task закрыть conext вручную
        {
            //Формируем Id запроса, чтобы потом выделить Id ответа с шины данных.
            var contextId = Guid.NewGuid();
            var responseTask= _eventBus
               .Subscrube<SibWayResponseItem>()
               .FirstAsync(item => item.Id == contextId)
               .ToTask(ct);
           
           var contextRes= await RequestHandler(contextId, context.Request)
                .Bind(inDate =>
                {
                    //Публикуем полученные входные данные на шину.
                    _eventBus.Publish(inDate);
                    return Result.Success();
                })
                .Bind(async () =>
                {
                    //Ждем ответа от SibWay об отправки входных данных. 
                    var sibWayResponse = await responseTask;
                    if (sibWayResponse.Result.IsSuccess)
                    {
                        _logger.Information("{HttpServer} {ResponseResult}", "ОТВЕТ:", "OK");
                    }
                    else
                    {
                        _logger.Error("{HttpServer} {ResponseResult}", "ОТВЕТ:", sibWayResponse.Result.Error);
                    }
                    return sibWayResponse.Result;
                })
                .Finally(async result =>
                {
                    //Формируем ответ клиенту в Зависимости от результата.
                    return await ResponseHandler(context.Response, result);
                });
           
           return contextRes;
        }


        private async Task<Result<InputDataEventItem>> RequestHandler(Guid contextId, HttpListenerRequest request)
        {
            _logger.Information("{HttpServer}  {Methode}   {ContentType}", "Получили запрос", request.HttpMethod, request.ContentType );
            var tableNameRes = ParseUrl(request);
            var xmlStrRes= await ParsePostBody(request);
            var postDataRes= Result.Combine(tableNameRes, xmlStrRes)
                .Bind(() => XmlHelpers.DeserializeFromXml(xmlStrRes.Value))
                .Bind(deserializedXml => Mapper.MapAdInputType4XmlDtoContainer2ListItemSibWay(deserializedXml))
                .Bind(listItemSibWay =>
                
                {
                    var inType = new InputDataEventItem
                    {
                        Id = contextId,
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
            return tableName == "SendDataXmlMultipart4Devices" ?
                Result.Failure<string>("Имя табло не указано в адрессе!!") :
                tableName;
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
        
        
        private async Task<Result> ResponseHandler(HttpListenerResponse response, Result responseRes)
        {
            var indigoRespDto= responseRes.IsFailure ? new IndigoResponseDto(0, responseRes.Error) : new IndigoResponseDto(1,  "Ok");
            try
            {
                string responseString = indigoRespDto.ToString();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "Application/json";
                response.ContentLength64 = buffer.Length;
                using (var output = response.OutputStream)
                {
                    await output.WriteAsync(buffer, 0, buffer.Length);
                    output.Close();
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"Ошибка подготовки ответа. Exception: '{ex}'");
            }
            return Result.Success();
        }
        #endregion
    }
}