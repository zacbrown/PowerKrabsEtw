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
    public class PSEtwUserTrace : IDisposable
    {
        readonly object _sync = new object();
        readonly UserTrace _trace;
        readonly CancellationTokenSource _cts;
        readonly PropertyExtractor _propertyExtractor = new PropertyExtractor();
        readonly IEventRecordDelegate _recordHandler;
        Task _task;

        bool _isRunning;

        internal PSEtwUserTrace(IEventRecordDelegate handler, string traceName)
        {
            _trace = new UserTrace(traceName);
            _cts = new CancellationTokenSource();
            _recordHandler = handler;
            ResetTask();
        }

        internal PSEtwUserTrace(IEventRecordDelegate handler)
            : this(handler, $"PowerKrabsEtw {Guid.NewGuid().ToString()}")
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

        internal void EnableProvider(PSEtwUserProvider provider)
        {
            lock (_sync)
            {
                if (!_isRunning)
                {
                    provider.EnsureDefaultHandlerSetup(DefaultEventRecordHandler);
                    _trace.Enable(provider.Provider);
                }
                else
                {
                    throw new TraceAlreadyRunningException();
                }
            }
        }

        internal void Start()
        {
            lock (_sync)
            {
                _task.Start();
                _isRunning = true;
            }
        }

        internal void Stop()
        {
            lock (_sync)
            {
                _trace.Stop();
                _task.Wait(TimeSpan.FromSeconds(2));
                ResetTask();
                _isRunning = false;
            }
        }

        internal void DefaultEventRecordHandler(IEventRecord record)
        {
            var obj = _propertyExtractor.Extract(record);
            
        }

        public bool IsRunning => _isRunning;

        private void ResetTask()
        {
            _task = new Task(() => { _trace.Start(); }, _cts.Token, TaskCreationOptions.LongRunning);
        }
    }
}
