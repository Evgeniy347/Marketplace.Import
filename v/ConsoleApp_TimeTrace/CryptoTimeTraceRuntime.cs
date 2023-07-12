using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.UI;
using WSSC.V4.SYS.Lib.Base;

namespace ConsoleApp_TimeTrace
{
    public class CryptoTimeTraceRuntime
    {
        private static readonly PropertyInfo _timeProp = typeof(TimeTraceEntry).GetProperty("Timer", BindingFlags.Instance | BindingFlags.NonPublic);

        [ThreadStatic]
        private static TimeTraceEntry _current;

        public static IDisposable Start(string key)
        {
            TimeTraceEntry root = _current = WSSC.V4.SYS.Lib.Base.TimeTrace.Current.Start(key);
            return root;
        }

        public static string GetTraceLog()
        {
            TimeTraceEntry entry = _current;

            if (entry == null)
                return null;

            int time = 1;
            if (time == 0)
                return null;

            Stopwatch stopwatch = (Stopwatch)_timeProp.GetValue(entry, null);

            if (stopwatch.Elapsed.TotalSeconds < time)
                return null;

            while (TimeTrace.Current.CurrentKey != null)
                TimeTrace.Current.End();

            using (StringWriter sw = new StringWriter())
            using (HtmlTextWriter writer = new HtmlTextWriter(sw))
            {
                TimeTrace.Current.RenderTrace(writer);
                string value = sw.ToString();
                return value;
            }
        }
    }
}
