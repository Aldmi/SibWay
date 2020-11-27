using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using SibWay.SibWayApi;

namespace SibWay.Settings
{
    public class SettingsLoader
    {
        public static async Task<Result<IList<XmlSibWaySettings>>> LoadXmlSibWaySettings()
        {
            var (_, isFailure, xmlFile, error) = XmlHelpers.LoadXmlFile("Settings", "Setting.xml"); //все настройки в одном файле
            if(isFailure)
                return Result.Failure<IList<XmlSibWaySettings>>("xmlFile == null");
            
            try
            {
                var setting = XmlSibWaySettings.ParseXmlSetting(xmlFile);
                return setting;
            }
            catch (Exception ex)
            {
                return Result.Failure<IList<XmlSibWaySettings>>($"ОШИБКА в узлах дерева XML файла настроек:  {ex}");
            }
        }
    }
}