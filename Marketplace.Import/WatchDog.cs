using System;
using System.Threading;

namespace Marketplace.Import
{
    public class WatchDog : IDisposable
    {
        private readonly Thread _thread;
        private readonly Action _action;
        private readonly int _timeout;
        private DateTime _lastReset;
        private bool _disposed;

        public WatchDog(Action action, int timeout)
        {
            _action = action;
            Reset();
            _timeout = timeout;
            _thread = new Thread(Work)
            {
                Name = "WatchDog",
                IsBackground = true
            };
            _thread.Start();
        }

        private void Work()
        {
            while (!_disposed)
            {
                DateTime nextReset = _lastReset.AddMilliseconds(_timeout);

                if (nextReset < DateTime.Now)
                {
                    try
                    {
                        _action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        BrowserForm.Instance.FileWriter.WriteLogAsynk(ex.ToString());
                    }
                    return;
                }

                TimeSpan interval = nextReset - DateTime.Now;
                if (interval < TimeSpan.FromSeconds(1))
                    interval = TimeSpan.FromSeconds(1);

                Thread.Sleep(interval);
            }
        }

        public void Reset()
        {
            _lastReset = DateTime.Now;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
