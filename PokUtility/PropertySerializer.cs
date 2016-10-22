using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace PokUtility
{
    public class PropertySerializer
    {
        public static String ObjectToString(Object obj)
        {
            MemoryStream stream = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            XmlTextWriter writer = new XmlTextWriter(stream, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;

            serializer.Serialize(writer, obj);

            stream.Seek(0, SeekOrigin.Begin);
            StreamReader sr = new StreamReader(stream);
            String xmlString = sr.ReadToEnd();
            sr.Close();

            return xmlString;
        }

        public static Object StringToObject(String text, Type type)
        {
            // convert string to stream
            byte[] byteArray = Encoding.UTF8.GetBytes(text);
            MemoryStream stream = new MemoryStream(byteArray);

            XmlSerializer serializer = new XmlSerializer(type);
            return serializer.Deserialize(stream);
        }

        public static String ObjectToFile(Object obj, String path)
        {
            return ObjectToFile(obj, path, Encoding.UTF8);
        }

        public static String ObjectToFile(Object obj, String path, Encoding encoding)
        {
            String text = ObjectToString(obj);

            StreamWriter sw = new StreamWriter(path, false, encoding);
            sw.Write(text);
            sw.Close();

            return text;
        }

        public static Object ObjectFromFile(String path, Type type)
        {
            StreamReader sr = new StreamReader(path);
            String text = sr.ReadToEnd();
            sr.Close();

            return StringToObject(text, type);
        }
    }
}
