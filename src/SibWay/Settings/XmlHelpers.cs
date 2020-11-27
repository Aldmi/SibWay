using System;
using System.IO;
using System.Xml.Linq;
using CSharpFunctionalExtensions;

namespace SibWay.Settings
{
    public static class XmlHelpers
    {
        public static Result<XElement> LoadXmlFile(string folderName, string fileName)
        {
            var path = string.IsNullOrEmpty(folderName) ?
                       Path.Combine(Directory.GetCurrentDirectory(), $"{fileName}") :
                       Path.Combine(Directory.GetCurrentDirectory(), $"{folderName}\\{fileName}");
            if (!File.Exists(path))
            {
                return Result.Failure<XElement>($"XML файл не НАЙДЕНН!!!   \"{path} \"");
            }
            try
            {
                return Result.Success(XElement.Load(path));
            }
            catch (Exception ex)
            {
                return Result.Failure<XElement>($"Ошибка при загрузке файла \"{path} \"  '{ex}'");
            }
        }
    }
}