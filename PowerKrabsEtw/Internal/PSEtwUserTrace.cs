// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using PowerKrabsEtw.Internal.Details;
using PowerKrabsEtw.Internal.PropertyParser;
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
        readonly PropertyExtractor _propertyExtractor;
        PSEventRecordCallback _callback;
        bool _isRunning;
        Task _task;

        internal PSEtwUserTrace(string traceName, bool includeVerboseEtwProperties)
        {
            _propertyExtractor = new PropertyExtractor(includeVerboseEtwProperties);
            _trace = new UserTrace(traceName);
            _cts = new CancellationTokenSource();
            Reset();
        }

        internal PSEtwUserTrace()
            : this($"PowerKrabsEtw {Guid.NewGuid().ToString()}", false)
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
                if (_isRunning) return;

                _callback = callback;
                _isRunning = true;
                _task.Start();
            }
        }

        internal void Stop()
        {
            lock (_sync)
            {
                if (!_isRunning) return;

                _trace.Stop();
                Reset();
                _isRunning = false;
            }
        }

        internal void Reset()
        {
            _task = new Task(() => { _trace.Start(); }, _cts.Token, TaskCreationOptions.LongRunning);
        }

        internal void DefaultEventRecordHandler(IEventRecord record)
        {
            var obj = _propertyExtractor.Extract(record);
            _callback.Invoke(obj);
        }

        public bool HasPumpedEvents => _trace.QueryStats().EventsHandled > 0;

        public bool IsRunning => _isRunning;
    }
}
