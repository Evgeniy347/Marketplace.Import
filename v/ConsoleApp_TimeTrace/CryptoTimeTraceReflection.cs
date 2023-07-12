using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.UI;

namespace ConsoleApp_TimeTrace
{
    public class CryptoTimeTraceReflection
    {
        static CryptoTimeTraceReflection()
        {

            Type_TimeTrace = Type.GetType(Type_TimeTrace_Name);
            Type_TimeTraceEntry = Type.GetType(Type_TimeTraceEntry_Name);

            Enable = Type_TimeTrace != null &&
                Type_TimeTraceEntry != null;

            if (Enable)
            {
                _TimeTraceEntry_Timer = Type_TimeTraceEntry.GetProperty("Timer", _flags);
                _TimeTrace_Current = Type_TimeTrace.GetProperty("Current", _flags);
                _TimeTrace_Start = Type_TimeTrace.GetMethod("Start", _flags);
                _TimeTrace_End = Type_TimeTrace.GetMethod("End", _flags);
                _TimeTrace_RenderTrace = Type_TimeTrace.GetMethod("RenderTrace", _flags);
                _TimeTrace_CurrentKey = Type_TimeTrace.GetProperty("CurrentKey", _flags);

                Enable = _TimeTraceEntry_Timer != null &&
                    _TimeTrace_Current != null &&
                    _TimeTrace_Start != null &&
                    _TimeTrace_End != null &&
                    _TimeTrace_RenderTrace != null &&
                    _TimeTrace_CurrentKey != null;
            }
        }

        public static bool Enable { get; }

        private const BindingFlags _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const string Type_TimeTraceEntry_Name = "WSSC.V4.SYS.Lib.Base.TimeTraceEntry";
        private const string Type_TimeTrace_Name = "WSSC.V4.SYS.Lib.Base.TimeTrace";

        private static readonly Type Type_TimeTrace;
        private static readonly Type Type_TimeTraceEntry;

        private static readonly PropertyInfo _TimeTraceEntry_Timer;
        private static readonly PropertyInfo _TimeTrace_Current;
        private static readonly MethodBase _TimeTrace_Start;
        private static readonly MethodBase _TimeTrace_End;
        private static readonly MethodBase _TimeTrace_RenderTrace;
        private static readonly PropertyInfo _TimeTrace_CurrentKey;

        [ThreadStatic]
        private static IDisposable _currentTrace;

        public static IDisposable Start(string key)
        {
            object current = _TimeTrace_Current.GetValue(null, null);
            IDisposable root = _currentTrace = (IDisposable)_TimeTrace_Start.Invoke(current, new object[] { key });
            return root;
        }

        public static string GetTraceLog()
        {
            IDisposable entry = _currentTrace;

            if (entry == null)
                return null;

            int time = 1;
            if (time == 0)
                return null;

            Stopwatch stopwatch = (Stopwatch)_TimeTraceEntry_Timer.GetValue(entry, null);

            if (stopwatch.Elapsed.TotalSeconds < time)
                return null;

            object current = _TimeTrace_Current.GetValue(null, null);

            while (_TimeTrace_CurrentKey.GetValue(current, null) != null)
                _TimeTrace_End.Invoke(current, null);

            using (StringWriter sw = new StringWriter())
            using (HtmlTextWriter writer = new HtmlTextWriter(sw))
            {
                _TimeTrace_RenderTrace.Invoke(current, new object[] { writer }); 
                string value = sw.ToString(); 
                return value;
            }
        }
    }
}
