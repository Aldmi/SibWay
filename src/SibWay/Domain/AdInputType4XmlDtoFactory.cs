using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CSharpFunctionalExtensions;
using SibWay.Domain.Dto;
using SibWay.SibWayApi;

namespace SibWay.Domain
{
    public static class AdInputType4XmlDtoFactory
    {
        public static Result<AdInputType4XmlDtoContainer> DeserializeFromXml(string xml)
        {
            var serializer  = new XmlSerializer(typeof(AdInputType4XmlDtoContainer));
            using (TextReader reader = new StringReader(xml))
            {
                try
                {
                    var result = (AdInputType4XmlDtoContainer)serializer.Deserialize(reader);
                    return result;
                }
                catch (InvalidOperationException ex)
                {
                    return Result.Failure<AdInputType4XmlDtoContainer>($"DeserializeFromXml Exception: {ex}");
                }
            }
        }
    }
}