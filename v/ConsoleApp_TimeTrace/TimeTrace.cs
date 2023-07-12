using System;
using System.Web.UI;

namespace WSSC.V4.SYS.Lib.Base
{
    public class TimeTrace
    {
        public static TimeTrace Current { get; } = new TimeTrace();

        public object CurrentKey { get; internal set; }

        public void End()
        {

        }

        public void RenderTrace(HtmlTextWriter writer)
        {
            writer.Write("ok");
        }

        internal TimeTraceEntry Start(string key)
        {
            return new TimeTraceEntry();
        }
    }
}
