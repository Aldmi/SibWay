using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using SibWay.SibWayApi;

namespace SibWay.Settings
{
    public static class SettingsLoader
    {
        public static async Task<Result<List<XmlSibWaySettings>>> LoadXmlSibWaySettings()
        {
            var (_, isFailure, xmlFile, error) = XmlHelpers.LoadXmlFile("Settings", "Setting.xml"); //все настройки в одном файле
            if(isFailure)
                return Result.Failure<List<XmlSibWaySettings>>("xmlFile == null");
            
            try
            {
                var setting = XmlSibWaySettings.ParseXmlSetting(xmlFile);
                return setting;
            }
            catch (Exception ex)
            {
                return Result.Failure<List<XmlSibWaySettings>>($"ОШИБКА в узлах дерева XML файла настроек: '{ex}'");
            }
        }
    }
}