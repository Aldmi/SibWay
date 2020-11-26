using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SibWay.SibWayApi;

namespace SibWay.Settings
{
    public class SettingsLoader
    {
        //TODO: Возвращать Result<T>
        public static async Task<IList<XmlSibWaySettings>> LoadXmlSibWaySettings()
        {
            try
            {
                var xmlFile = XmlHelpers.LoadXmlFile("Settings", "Setting.xml"); //все настройки в одном файле
                if (xmlFile == null)
                    return null;


                var setting = XmlSibWaySettings.LoadXmlSetting(xmlFile);
                return setting;
            }
            catch (FileNotFoundException)
            {
                //ErrorString = "Файл Setting.xml не найденн";
                //Log.log.Error(ErrorString);
                return null;
            }
            catch (Exception ex)
            {
                //ErrorString = "ОШИБКА в узлах дерева XML файла настроек:  " + ex;
                //Log.log.Error(ErrorString);
                return null;
            }
        }
    }
}