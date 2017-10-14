using O365.Security.ETW;

using System;
using System.Management.Automation;
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
        public delegate void PSEventRecordCallback(PSObject obj);

        readonly object _sync = new object();
        readonly UserTrace _trace;
        readonly CancellationTokenSource _cts;
        readonly PropertyExtractor _propertyExtractor = new PropertyExtractor();
        PSEventRecordCallback _callback;
        bool _isRunning;

        internal PSEtwUserTrace(string traceName)
        {
            _trace = new UserTrace(traceName);
            _cts = new CancellationTokenSource();
        }

        internal PSEtwUserTrace()
            : this($"PowerKrabsEtw {Guid.NewGuid().ToString()}")
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

        internal void Start(PSEventRecordCallback callback)
        {
            lock (_sync)
            {
                _callback = callback;
                _isRunning = true;
                _trace.Start();
            }
        }

        internal void Stop()
        {
            lock (_sync)
            {
                if (!_isRunning) return;

                _trace.Stop();
                _isRunning = false;
            }
        }

        internal void DefaultEventRecordHandler(IEventRecord record)
        {
            var obj = _propertyExtractor.Extract(record);
            _callback.Invoke(obj);
        }

        public bool IsRunning => _isRunning;
    }
}
