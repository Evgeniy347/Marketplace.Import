using System;
using System.Diagnostics;

namespace WSSC.V4.SYS.Lib.Base
{
    public class TimeTraceEntry : IDisposable
    {
        private Stopwatch Timer { get; } = Stopwatch.StartNew();

        public void Dispose()
        {
        }
    }
}
