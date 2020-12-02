using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SibWay.SibWayApi;

namespace SibWay.HttpApi
{
    public class XmlHttpServerSettings
    {
        #region prop
        public string EndpointAddress { get; set; }
        #endregion


        
        #region Methode
        /// <summary>
        /// Обязательно вызывать в блоке try{}
        /// </summary>
        public static XmlHttpServerSettings ParseXmlSetting(XElement xml)
        {
            var httpServ = xml?.Element("HttpServer");
            if(httpServ == null)
                throw new XmlException("Элемент 'HttpServer' не задан");
            
            var endPoint= (string) httpServ.Element("EndpointAddress");
            if(httpServ == null)
                throw new XmlException("Элемент 'endPoint' не задан");

            var sett = new XmlHttpServerSettings
            {
                EndpointAddress = endPoint
            };
            return sett;
    }
        #endregion
    }
}