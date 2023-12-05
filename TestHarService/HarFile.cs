using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestHarService
{
    internal static class HarFileExtensions
    {
        public static string GetLookupValue(this JObject jObj, string path, bool throwEx = true) =>
              GetLookupValue<string>(jObj, path, throwEx);

        public static T GetLookupValue<T>(this JObject jObj, string path, bool throwEx = true)
        {
            string[] names = path.Split('/');

            JObject temp = jObj;
            object value = null;

            foreach (string name in names)
            {
                value = temp.GetValue(name);
                temp = value as JObject;
            }

            if (value is JValue val)
                value = val.Value;
            try
            {
                if (value is null)
                    return default;

                if (typeof(T) != value.GetType())
                    return (T)Convert.ChangeType(value, typeof(T));

                return (T)value;
            }
            catch (Exception ex)
            {
                string r = ex.ToString();
                string m = $"{typeof(T).FullName} | {value?.GetType()?.FullName}";
                throw new Exception(m, ex);
            }
        }
    }

    internal class HarFile
    {
        public HarEntry[] Entries { get; private set; }

        public static HarFile Parce(string fileName)
        {
            string json = File.ReadAllText(fileName);
            JObject jobject = JObject.Parse(json);

            JArray entries = jobject.GetLookupValue<JArray>("log/entries");


            HarFile result = new HarFile()
            {
                Entries = entries.Cast<JObject>().Select(Convert).ToArray(),
            };

            return result;
        }

        public static HarEntry Convert(JObject jEntry)
        {
            string url = jEntry.GetLookupValue("request/url");
            string content = jEntry.GetLookupValue("response/content/text");
            string mimeType = jEntry.GetLookupValue("response/content/mimeType");
            int status = jEntry.GetLookupValue<int>("response/status");


            HarEntry result = new HarEntry()
            {
                Request = new HarRequest()
                {
                    Url = new Uri(url)
                },
                Responce = new HarResponce()
                {
                    MimeType = mimeType,
                    ContentText = content,
                    Status = status,
                },
            };

            JArray headers = jEntry.GetLookupValue<JArray>("response/headers");

            foreach (JObject obj in headers)
            {
                string name = obj.GetLookupValue("name");
                string value = obj.GetLookupValue("value");

                try
                {
                    result.Responce.Headers.Add(name, value);
                }
                catch (Exception ex)
                {
                    //throw;
                }
            }

            return result;
        }
    }

    internal class HarEntry
    {
        public HarRequest Request { get; set; }
        public HarResponce Responce { get; set; }
    }

    public class HarRequest
    {
        public Uri Url { get; set; }
    }

    public class HarResponce
    {
        public int Status { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public byte[] Content => Encoding.UTF8.GetBytes(ContentText);
        public string ContentText { get; set; }
        public string MimeType { get; set; }
    }
}
