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
                    _action.Invoke();
                    return;
                }

                Thread.Sleep(nextReset - DateTime.Now);
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
