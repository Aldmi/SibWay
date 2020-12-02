using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using SibWay.HttpApi;
using SibWay.SibWayApi;

namespace SibWay.Settings
{
    public static class SettingsLoader
    {
        public static async Task<Result<List<XmlSibWaySettings>>> LoadXmlSibWaySettings()
        {
            var (_, isFailure, xmlFile, error) = XmlHelpers.LoadXmlFile("Settings", "Setting.xml");
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
        
        
        public static async Task<Result<XmlHttpServerSettings>> LoadHttpServerSettings()
        {
            var (_, isFailure, xmlFile, error) = XmlHelpers.LoadXmlFile("Settings", "Setting.xml"); 
            if(isFailure)
                return Result.Failure<XmlHttpServerSettings>("xmlFile == null");
            
            try
            {
                var setting = XmlHttpServerSettings.ParseXmlSetting(xmlFile);
                return setting;
            }
            catch (Exception ex)
            {
                return Result.Failure<XmlHttpServerSettings>($"ОШИБКА в узлах дерева XML файла настроек: '{ex}'");
            }
        }
    }
}