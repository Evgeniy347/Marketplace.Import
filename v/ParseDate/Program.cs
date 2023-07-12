using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseDate
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            string[] values = new string[] { "BackUP 01.11.2023 10.34.08.json", "BackUP 01.11.2023 09.35.52.json", "BackUP 01.11.2023 09.56.27.json" };

            string[] newValues = values.OrderBy(GetDateFromName).ToArray();


            string resultValue = values.Select((x, y) => $"{y + 1}|{x}|{newValues[y]}|{Path.GetFileName(x)}").JoinString(Environment.NewLine);

            Console.WriteLine(resultValue);
            Console.ReadLine();
        }


        public static DateTime GetDateFromName(string name)
        {
            name = name.Replace("BackUP ", "").Replace(".json", "");
            DateTime result = DateTime.ParseExact(name, "MM.dd.yyyy HH.mm.ss", CultureInfo.InvariantCulture);
            return result;
        }


        public static string JoinString<TSourse>(this IEnumerable<TSourse> sourses, string separator = ",") =>
            sourses.Select(x => x.ToString()).JoinString(separator);

        public static string JoinString(this IEnumerable<string> sourses, string separator = ",") =>
             sourses.ToArray().JoinString(separator);

        public static string JoinString(this string[] sourses, string separator = ",") =>
            string.Join(separator, sourses);
    }
}
