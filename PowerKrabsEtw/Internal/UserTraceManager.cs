using O365.Security.ETW;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace PowerKrabsEtw.Internal
{
    /// <summary>
    /// UserTraceManager coordinates enabling/disabling
    /// Providers on a UserTrace object. You can't enable
    /// providers once the UserTrace is started.
    /// </summary>
    public class UserTraceManager : IDisposable
    {
        readonly object _sync = new object();
        readonly UserTrace _trace;
        readonly CancellationTokenSource _cts;
        Task _task;

        bool _isRunning;

        internal UserTraceManager(string traceName)
        {
            _trace = new UserTrace(traceName);
            _cts = new CancellationTokenSource();
            ResetTask();
        }

        internal UserTraceManager() : this($"PowerKrabsEtw {Guid.NewGuid().ToString()}")
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_sync)
                {
                    Stop();
                }
            }
        }

        public void EnableProvider(Provider provider)
        {
            lock (_sync)
            {
                if (!_isRunning)
                {
                    _trace.Enable(provider);
                }
                else
                {
                    throw new TraceAlreadyRunningException();
                }
            }
        }

        public void Start()
        {
            lock (_sync)
            {
                _task.Start();
                _isRunning = true;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                _trace.Stop();
                _task.Wait(TimeSpan.FromSeconds(2));
                ResetTask();
                _isRunning = false;
            }
        }

        public bool IsRunning => _isRunning;

        private void ResetTask()
        {
            _task = new Task(() => { _trace.Start(); }, _cts.Token, TaskCreationOptions.LongRunning);
        }
    }
}
