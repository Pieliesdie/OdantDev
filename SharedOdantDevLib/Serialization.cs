using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Newtonsoft.Json;

namespace OdantDev;
public static class SerializationHelper
{
    public static T DeserializeBinary<T>(this string toDeserialize)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        var array = Convert.FromBase64String(toDeserialize);
        using var memoryStream = new MemoryStream(array);
        return (T)formatter.Deserialize(memoryStream);
    }

    public static string SerializeBinary<T>(this T toSerialize)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using var memoryStream = new MemoryStream();
        formatter.Serialize(memoryStream, toSerialize);
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public static T DeserializeXml<T>(this string toDeserialize)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
        using (StringReader textReader = new StringReader(toDeserialize))
        {
            return (T)xmlSerializer.Deserialize(textReader);
        }
    }

    public static string SerializeXml<T>(this T toSerialize)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
        using (StringWriter textWriter = new StringWriter())
        {
            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }
    }

    public static T DeserializeJson<T>(this string toDeserialize)
    {
        return JsonConvert.DeserializeObject<T>(toDeserialize);
    }

    public static string SerializeJson<T>(this T toSerialize)
    {
        return JsonConvert.SerializeObject(toSerialize);
    }
}
