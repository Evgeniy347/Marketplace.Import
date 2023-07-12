using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp_TimeTrace
{
    internal class Program
    {
        static void Main(string[] args)
        { 
            using (CryptoTimeTrace.Start("asd"))
            {
                Thread.Sleep(2000);
                CryptoTimeTrace.CurrentSaveLongTrace("test");
            }
        }
    }

    public class CryptoTimeTrace
    {
        public static IDisposable Start(string key)
        {
            if (CryptoTimeTraceReflection.Enable)
                return CryptoTimeTraceReflection.Start(key);

            return CryptoTimeTraceRuntime.Start(key);
        }

        public static void CurrentSaveLongTrace(string prefix)
        {
            string value = CryptoTimeTraceReflection.Enable ?
                CryptoTimeTraceReflection.GetTraceLog() :
                CryptoTimeTraceRuntime.GetTraceLog();

            if (string.IsNullOrEmpty(value))
                return;

            DateTime now = DateTime.Now;
            string fileName = $"{now:HH.mm.ss} {prefix} UserID_{10}.html";

            //string fullPath = $@"C:\Program Files\WSS\Logs\{now:yyyy.MM.dd}";
            string fullPath = $@"C:\Users\GTR\source\repos\Marketplace.Import\v\ConsoleApp_TimeTrace\bin\Debug\{now:yyyy.MM.dd}";

            string fullName = Path.Combine(fullPath, fileName);

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            File.WriteAllText(fullName, value);
        }
    }
}
