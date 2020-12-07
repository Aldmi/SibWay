using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LedScreenLibNetWrapper;
using LedScreenLibNetWrapper.Impl;
using Serilog;
using SibWay.Application.EventHandlers;
using SibWay.Infrastructure;

namespace SibWay.SibWayApi
{
    // Коды ошибок
    public enum ErrorCode
    {
        ERROR_SUCCESS = 0,
        ERROR_GENERAL_ERROR = -1,
        ERROR_CONNECTION_FAILED = -2,
        ERROR_NOT_CONNECTED = -3,
        ERROR_TIMEOUT = -4,
        ERROR_WRONG_RESPONSE = -5,
        ERROR_ALREADY_CONNECTED = -6,
        ERROR_EMPTY_RESPONSE = -7,
        ERROR_WRONG_LENGTH = -8,
        ERROR_CRC_ERROR = -9,
        ERROR_RESPONSE_UNKNOWN = -10,
        ERROR_UNSUPPORTED_RESPONSE = -11,
        ERROR_FILE_NOT_FOUND = -12,
        ERROR_INVALID_XML_CONFIGURATION = -13
    }


    public class SibWayProxy : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;
        private byte _countTryingTakeData;               //счетчик попыток
        
        #region prop
        public DisplayDriver DisplayDriver { get;} = new DisplayDriver();
        public SettingSibWay SettingSibWay { get; }
        public string TableName => SettingSibWay.TableName; 

        public Dictionary<string, string> DictSendingStrings { get; } = new Dictionary<string, string>(); //Словарь отправленных строк на каждую колонку. Key= Название колонки.   Value= Строка


        private string _statusString;
        public string StatusString
        {
            get { return _statusString; }
            set
            {
                if (value == _statusString) return;
                _statusString = value;
            }
        }

        
        private bool _isConnect;
        public bool IsConnect
        {
            get { return _isConnect; }
            set
            {
                if (value == _isConnect) return;
                _isConnect = value;
                _eventBus.Publish(new ChangeConnectSibWayEvent(TableName, StatusString, _isConnect));
            }
        }

        
        private bool _isRunDataExchange;
        public bool IsRunDataExchange
        {
            get { return _isRunDataExchange; }
            set
            {
                if (value == _isRunDataExchange) return;
                _isRunDataExchange = value;
            }
        }
        #endregion


        
        #region ctor
        public SibWayProxy(SettingSibWay settingSibWay, EventBus eventBus, ILogger logger)
        {
            _eventBus = eventBus;
            _logger = logger;
            SettingSibWay = settingSibWay;
        }
        #endregion


        
        #region Method
        public Task<Result> ReConnect()
        {
            _countTryingTakeData = 0;
            IsConnect = false;
            Dispose();
            return Connect();
        }
        
        private async Task<Result> Connect()
        {
            while (!IsConnect)
            {
                try
                {
                    DisplayDriver.Initialize(SettingSibWay.Ip, SettingSibWay.Port);
                    StatusString = $"{TableName}  Conect to {SettingSibWay.Ip} : {SettingSibWay.Port} ...";
                    var errorCode = await OpenConnectionAsync();
                    IsConnect = (errorCode == ErrorCode.ERROR_SUCCESS);
                    IsConnect = true;//DEBUG!!!!!!!!!!!!!!!
                    if (!_isConnect)
                    {
                        _logger.Warning("{Connection2SibWay}   {errorCode}", StatusString, errorCode);
                        await Task.Delay(SettingSibWay.Time2Reconnect);
                    }
                }
                catch (Exception ex)
                {
                    IsConnect = false;
                    StatusString = $"Ошибка инициализации соединения: \"{ex.Message}\"";
                    _logger.Error("{Connection2SibWay}", StatusString);
                    Dispose();
                }
            }
            StatusString = $"{TableName} Conect to Sucsess !!!!: {SettingSibWay.Ip}:{SettingSibWay.Port} ...";
            _logger.Information("'{Connection2SibWay}'", StatusString);
            return Result.Success($" Task подключения к {TableName} Завершен.");
        }
        
        /// <summary>
        /// Не блокирующая операция открытия соедининия. 
        /// </summary>
        private async Task<ErrorCode> OpenConnectionAsync()
        {
            return await Task<ErrorCode>.Factory.StartNew(() =>
            (ErrorCode)DisplayDriver.OpenConection());
        }

        /// <summary>
        /// Очистка табло.
        /// </summary>
        public  Task<Result> SendDataClear()
        {
            var clearData = new List<ItemSibWay>{new ItemSibWay()}; //TODO: элемент для очитки создать
            return SendData(clearData);
        }

        
        /// <summary>
        /// Отправка данных на табло.
        /// </summary>
        public async Task<Result> SendData(IList<ItemSibWay> sibWayItems)
        {
            if (!IsConnect)
                return Result.Failure($"{TableName}.  Not connect ...");
            
            if (IsRunDataExchange)
                return Result.Failure($"{TableName}.  Предыдущий обмен не закончен, повторите попытку позже ...");
            
            IsRunDataExchange = true;
            try
            {
                //Debug.WriteLine($"--------------------------- {DateTime.Now}");
                //Отправка информации каждому окну---------------------------------------
                foreach (var winSett in SettingSibWay.WindowSett)
                {
                    //Ограничим кол-во строк для окна.
                    var maxWindowHeight = winSett.Height;
                    var fontSize = winSett.FontSize;
                    var nItems = maxWindowHeight / fontSize;
                    var items = sibWayItems.Take(nItems).ToList();

                    //Если пришла команда инициализации (очистки), то копируем нулевой элемент nItems раз. Для очистки всех строк табло.
                    if (items.Count == 1 && (items[0].Command == "None" || items[0].Command == "Clear"))
                    {
                        var copyItem = items[0];
                        for (int i = 0; i < nItems-1; i++)
                        {
                            items.Add(copyItem);
                        }
                    }

                    //Сформируем список строк и возьмем nItems еще раз, т.к. формат вывода может включать перенос строки. 
                    var sendingStrings = CreateListSendingStrings(winSett, items)?.Take(nItems).ToList();
           
                    //Отправим список строк.
                    if (sendingStrings != null && sendingStrings.Any())
                    {
                        var result = await SendMessageAsync(winSett, sendingStrings, fontSize);
                        if (result)
                        {
                            _countTryingTakeData= 0;
                        }
                        else //Если в результате отправки даных окну возникла ошибка, то уходим на цикл ReConnect и прерываем отправку данных.
                        {
                            if (++_countTryingTakeData > SettingSibWay.NumberTryingTakeData)
                            {
                                //Debug.WriteLine($"RECONNECT:  {DateTime.Now:mm:ss}");
                                IsConnect = false;
                                return Result.Failure($"{TableName}. Ошибок слишком много, ушли на РЕКОННЕКТ");
                            }
                        }
                        await Task.Delay(winSett.DelayBetweenSending);
                    }
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"SendData НЕ известная ошибка '{ex.Message}'");
            }
            finally
            {
                IsRunDataExchange = false;
            }
            return Result.Success();
        }

        
        private IEnumerable<string> CreateListSendingStrings(WindowSett winSett, IList<ItemSibWay> items)
        {
            //Создаем список строк отправки для каждого окна------------------------------
            var listString= new List<string>();
            foreach (var sh in items)
            {
                var path2FontFile = SettingSibWay.Path2FontFileDictionary[winSett.FontSize]; // Каждому размеру шрифта свой файл с размерами символов.
                string trimStr = null;
                switch (winSett.ColumnName)
                {                    
                    case nameof(sh.TypeTrain):
                        trimStr = TrimStrOnWindowWidth(sh.TypeTrain, winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.NumberOfTrain):
                        trimStr = TrimStrOnWindowWidth(sh.NumberOfTrain, winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.PathNumber):
                        trimStr = TrimStrOnWindowWidth(sh.PathNumber, winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.Event):
                        trimStr = TrimStrOnWindowWidth(sh.Event, winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.Addition):
                        trimStr = TrimStrOnWindowWidth(sh.Addition, winSett.Width, path2FontFile);
                        break;

                    case "Stations":
                        var stations = $"{sh.StationDeparture}-{sh.StationArrival}";
                        if (!string.IsNullOrEmpty(winSett.Format))
                        {
                            try
                            {
                                var replaceStr = winSett.Format.Replace("StartStation", "0").Replace("EndStation", "1").Replace("n", "2");
                                stations = string.Format(replaceStr, sh.StationDeparture, sh.StationArrival, "\n");
                                var stationsArr = stations.Split('\n');
                                foreach (var st in stationsArr)
                                {
                                    trimStr = TrimStrOnWindowWidth(st, winSett.Width, path2FontFile);
                                    listString.Add(string.IsNullOrEmpty(trimStr) ? " " : trimStr);
                                }
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                            continue;
                        }
                        break;

                    case nameof(sh.DirectionStation):
                        trimStr = TrimStrOnWindowWidth(sh.DirectionStation, winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.Note):
                        trimStr = TrimStrOnWindowWidth(sh.Note, winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.DaysFollowingAlias):
                        var daysFolowingAlias = sh.DaysFollowingAlias?.Replace("\r", string.Empty);
                        var dfaArr= daysFolowingAlias.Split('\n');
                        foreach (var dfa in dfaArr)
                        {
                            trimStr = TrimStrOnWindowWidth(dfa, winSett.Width, path2FontFile);
                            listString.Add(string.IsNullOrEmpty(trimStr) ? " " : trimStr);
                        }
                        continue;
                        

                    case nameof(sh.TimeDeparture):
                        trimStr = TrimStrOnWindowWidth(sh.TimeDeparture?.ToString("HH:mm") ?? " ", winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.TimeArrival):
                        trimStr = TrimStrOnWindowWidth(sh.TimeArrival?.ToString("HH:mm") ?? " ", winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.DelayTime):
                        trimStr = TrimStrOnWindowWidth(sh.DelayTime?.ToString("HH:mm") ?? " ", winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.ExpectedTime):
                        trimStr = TrimStrOnWindowWidth(sh.ExpectedTime.ToString("HH:mm"), winSett.Width, path2FontFile);
                        break;

                    case nameof(sh.StopTime):
                        trimStr = TrimStrOnWindowWidth(sh.StopTime?.ToString("HH:mm") ?? " ", winSett.Width, path2FontFile);
                        break;
                }
   
                listString.Add(trimStr ?? string.Empty);              
            }
            
            return listString;
        }

        
        private async Task<bool> SendMessageAsync(WindowSett winSett, IEnumerable<string> sendingStrings, int fontSize)
        {
            uint colorRgb = BitConverter.ToUInt32(winSett.ColorBytes, 0);
            string text = GetResultString(sendingStrings);

            if (!CheckColumnChange(winSett.ColumnName, text))   //Обновляем только измененные колонки (экраны)
                return true;

            var textHeight = DisplayTextHeight.px8;
            switch (fontSize)
            {
                case 8:
                    textHeight = DisplayTextHeight.px8;
                    break;
                case 12:
                    textHeight = DisplayTextHeight.px12;
                    break;
                case 16:
                    textHeight = DisplayTextHeight.px16;
                    break;
                case 24:
                    textHeight = DisplayTextHeight.px24;
                    break;
                case 32:
                    textHeight = DisplayTextHeight.px32;
                    break;
            }

            StatusString = "Отправка на экран " + winSett.Number + "\n" + text + "\n";
            //Log.log.Error($"{StatusString}");

            //Debug.WriteLine("   ");
            //Debug.WriteLine($">>>> {winSett.Number}:  {DateTime.Now:mm:ss}");
            var err = await Task<ErrorCode>.Factory.StartNew(() => (ErrorCode)DisplayDriver.SendMessage(
                    winSett.Number,
                    winSett.Effect,
                    winSett.TextHAlign,
                    winSett.TextVAlign,
                    winSett.DisplayTime,
                    textHeight,
                    colorRgb,
                    text));
            //Debug.WriteLine($"<<<< {winSett.Number}  err= {err}:  {DateTime.Now:mm:ss}");

            var tryResult = (err == ErrorCode.ERROR_SUCCESS);
            if (!tryResult)
            {
                RemoveColumnChange(winSett.ColumnName);
                //Debug.WriteLine($"error = {err}");
                _logger.Error("{SendMessage2SibWay}", $"SibWayProxy SendMessageAsync respown statys {err}");
            }

            StatusString = "Отправка на экран " + winSett.Number + "errorCode= " + err + "\n";
            return tryResult;
        }


        private bool CheckColumnChange(string columnName, string text)
        {
            if (DictSendingStrings.ContainsKey(columnName) &&
                DictSendingStrings[columnName] == text)
            {
                return false;
            }

            DictSendingStrings[columnName] = text;
            return true;
        }


        private void RemoveColumnChange(string columnName)
        {
            if (DictSendingStrings.ContainsKey(columnName))
            {
                DictSendingStrings.Remove(columnName);
            }
        }



        private string TrimStrOnWindowWidth(string str, int width, string path2FontFile)
        { 
            if (File.Exists(path2FontFile))
            {
                //Измерим в пикселях размер текста
                using (var tu = new TextUtility())
                {
                    tu.Initialize(path2FontFile);
                    //var sizeStr=tu.MeasureString(str);//DEBUG
                    while (tu.MeasureString(str) > width)
                    {
                        str = str.Remove(str.Length - 1);
                    }
                    return str;
                }
            }
            return str;
        }


        private string GetResultString(IEnumerable<string> list)
        {
            var strBuilder = new StringBuilder();
            foreach (var l in list)
            {
                strBuilder.Append(l);
                strBuilder.Append("\n");
            }

            return strBuilder.Remove(strBuilder.Length - 1, 1).ToString(); //удалить послдений символ \n
        }


        public bool SyncTime(DateTime dateTime)
        {
            if (!IsConnect)
                return false;

            var isSucsees = true;//DisplayDriver.SetTime(dateTime);

            //var res= DisplayDriver.SetTime(dateTime);
           // Thread.Sleep(1000);
          //  var cr= DisplayDriver.GetTime();//DEBUG

            return isSucsees;
        }
        #endregion

        
        #region DisposePattern
        public void Dispose()
        {
            DisplayDriver?.CloseConection();
            DisplayDriver?.Dispose();
        }
        #endregion
    }
}