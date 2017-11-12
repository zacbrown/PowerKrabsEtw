// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using PowerKrabsEtw.Internal;
using PowerKrabsEtw.Internal.Details;
using PowerKrabsEtw.Internal.PropertyParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsDiagnostic.Trace, "EtwProcess")]
    public class TraceEtwProcess : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string ProcessName { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string ProcessArguments { get; set; }

        // readonly instance
        readonly object _lock = new object();
        readonly PropertyExtractor _propertyExtractor = new PropertyExtractor(false);
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        readonly List<object> _records = new List<object>();

        // instance
        long _eventCounts = 0;
        IntPtr _processHandle = IntPtr.Zero;
        string _fullProcessPath = string.Empty;
        uint _processId = 0;

        protected override void BeginProcessing()
        {
            PSEtwUserTrace trace = null;
            try
            {
                _processHandle = ProcessHelper.LaunchProcessSuspended(ProcessName, ProcessArguments, out _processId, out _fullProcessPath);
                WriteVerbose($"{ProcessName} started suspended with PID {_processId}...");
                WriteProgress(new ProgressRecord(0, "Trace-EtwProcess", "Step 1 (launch process suspended)")
                {
                    PercentComplete = 25
                });

                WriteVerbose($"Setting up trace...");
                trace = SetupEtwTrace();

                // BUGBUG: At times, it seemed this was necessary to deal with PSReadline messing with stuff?
                //while (Host.UI.RawUI.KeyAvailable) Host.UI.RawUI.ReadKey();

                trace.Start((obj) =>
                {
                    Interlocked.Increment(ref _eventCounts);
                });
                WriteVerbose($"ETW trace setup, resuming {ProcessName} (PID {_processId})...");
                WriteProgress(new ProgressRecord(0, "Trace-EtwProcess", "Step 2 (trace setup)")
                {
                    PercentComplete = 50
                });

                ProcessHelper.ResumeProcess(_processHandle);
                WriteProgress(new ProgressRecord(0, "Trace-EtwProcess", "Step 3 (resume process)")
                {
                    PercentComplete = 75
                });

                while (_eventCounts == 0 && !Stopping)
                {
                    WriteVerbose("Waiting for trace to start...");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                var sleepTimeSpan = TimeSpan.FromSeconds(2);
                while (!Stopping && !_cts.IsCancellationRequested)
                {
                    WriteVerbose($"Processed {Interlocked.Exchange(ref _eventCounts, 0)} events in last {sleepTimeSpan.Seconds} seconds...");

                    object[] records = new object[0];
                    lock (_lock)
                    {
                        records = _records.ToArray();
                        _records.Clear();
                    }

                    foreach (var record in records) WriteObject(record);

                    Thread.Sleep(sleepTimeSpan);
                }
            }
            catch (Exception ex)
            {
                var error = new ErrorRecord(ex, ex.GetType().ToString(), ErrorCategory.InvalidOperation, null);
                WriteError(error);
            }
            finally
            {
                WriteVerbose($"{ProcessName} exited. Stopping trace.");
                if (trace.IsRunning) trace.Stop();
                WriteProgress(new ProgressRecord(0, "Trace-EtwProcess", "Step 4 (stop trace)")
                {
                    PercentComplete = 100
                });
            }
        }

        #region Provider Helpers
        private PSEtwUserTrace SetupEtwTrace()
        {
            var trace = new PSEtwUserTrace($"Trace-ProcessWithEtw-{Guid.NewGuid()}", false);
            var processProvider = CreateProcessProvider();
            var powershellProvider = CreatePowerShellProvider();
            var networkProvider = CreateNetworkProvider();
            var dnsProvider = CreateDnsProvider();
            var wmiProvider = CreateWmiActivityProvider();
            var registryProvider = CreateRegistryProvider();
            var fileProvider = CreateFileProvider();

            trace.EnableProvider(processProvider);
            trace.EnableProvider(powershellProvider);
            trace.EnableProvider(networkProvider);
            trace.EnableProvider(dnsProvider);
            trace.EnableProvider(wmiProvider);
            trace.EnableProvider(registryProvider);
            trace.EnableProvider(fileProvider);

            return trace;
        }

        private PSEtwUserProvider CreateProcessProvider()
        {
            const string providerName = "Microsoft-Windows-Kernel-Process";
            var processProvider = new Provider(providerName);

            // process start/stop
            var startStopFilter = new EventFilter(Filter.EventIdIs(1).Or(Filter.EventIdIs(2)));
            startStopFilter.OnEvent += (IEventRecord r) =>
            {
                try
                {
                    if (r.Id == 1)
                    {
                        var parentProcessId = r.GetUInt32("ParentProcessID");
                        if (parentProcessId == _processId)
                        {
                            var obj = _propertyExtractor.Extract(r);
                            lock (_lock) { _records.Add(obj.ToPSObject()); }
                        }
                    }
                    else if (r.Id == 2 && r.ProcessId == _processId)
                    {
                        var obj = _propertyExtractor.Extract(r);
                        lock (_lock) { _records.Add(obj.ToPSObject()); }

                        const int secondsToWait = 10;
                        // We wait ten seconds to give ourselves a chance to process
                        // any remaining items relevant to the process.
                        Task.Run(() => {
                            Thread.Sleep(TimeSpan.FromSeconds(secondsToWait));
                            _cts.Cancel();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    // TODO: log bad record parse
                }
            };
            processProvider.AddFilter(startStopFilter);

            // image load
            var imageLoadFilter = new EventFilter(Filter.ProcessIdIs((int)_processId).And(Filter.EventIdIs(5)));
            imageLoadFilter.OnEvent += (IEventRecord r) =>
            {
                try
                {
                    var obj = _propertyExtractor.Extract(r);
                    lock (_lock) { _records.Add(obj.ToPSObject()); }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };
            processProvider.AddFilter(imageLoadFilter);

            // thread injection
            var threadFilter = new EventFilter(Filter.ProcessIdIs((int)_processId).And(Filter.EventIdIs(3)));
            threadFilter.OnEvent += (IEventRecord r) =>
            {
                try
                {
                    var targetProcessId = (int)r.GetUInt32("ProcessID");

                    if (targetProcessId != r.ProcessId)
                    {
                        var targetProcess = Process.GetProcessById(targetProcessId);
                        var targetProcessName = targetProcess.MainModule.FileName;

                        // If the process start for the target process is more than
                        // 10 milliseconds after the thread creation time, then it
                        // is a good chance this is not the initial thread.
                        var diff = r.Timestamp.Subtract(targetProcess.StartTime);
                        if (diff.TotalMilliseconds > 10)
                        {
                            var obj = _propertyExtractor.Extract(r);
                            obj.Add("TargetProcessName", targetProcessName);
                            lock (_lock) { _records.Add(obj.ToPSObject()); }
                        }
                    }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };
            processProvider.AddFilter(threadFilter);

            return new PSEtwUserProvider(processProvider, providerName);
        }

        private PSEtwUserProvider CreatePowerShellProvider()
        {
            const string providerName = "Microsoft-Windows-PowerShell";
            var powershellProvider = new Provider(providerName);

            var filter = new EventFilter(Filter.ProcessIdIs((int)_processId)
                .And(Filter.EventIdIs(7937))
                .And(UnicodeString.Contains("Payload", "Started.")));
            filter.OnEvent += (IEventRecord r) =>
            {
                try
                {
                    var obj = _propertyExtractor.Extract(r);
                    lock (_lock) { _records.Add(obj.ToPSObject()); }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };
            powershellProvider.AddFilter(filter);

            return new PSEtwUserProvider(powershellProvider, providerName);
        }

        private PSEtwUserProvider CreateWmiActivityProvider()
        {
            const int WMIEventId = 11;
            const string providerName = "Microsoft-Windows-WMI-Activity";
            var wmiProvider = new Provider(providerName);

            IEventRecordDelegate callback = (IEventRecord r) =>
            {
                try
                {
                    var clientPid = r.GetInt32("ClientProcessId");
                    if (clientPid == _processId)
                    {
                        var obj = _propertyExtractor.Extract(r);
                        lock (_lock) { _records.Add(obj.ToPSObject()); }
                    }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };

            var filter = new EventFilter(Filter.EventIdIs(WMIEventId));
            filter.OnEvent += callback;
            wmiProvider.AddFilter(filter);
            
            return new PSEtwUserProvider(wmiProvider, providerName);
        }

        private PSEtwUserProvider CreateNetworkProvider()
        {
            const string providerName = "Microsoft-Windows-Kernel-Network";
            var networkProvider = new Provider(providerName);

            const int IPv4TcpSend = 10;
            const int IPv6TcpSend = 26;
            const int IPv4UdpSend = 42;
            const int IPv6UdpSend = 58;

            var processIdFilter = Filter.ProcessIdIs((int)_processId);
            var eventIdFilter = Filter.EventIdIs(IPv4TcpSend)
                    .Or(Filter.EventIdIs(IPv6TcpSend)
                    .Or(Filter.EventIdIs(IPv4UdpSend)
                    .Or(Filter.EventIdIs(IPv6UdpSend))));
            var filter = new EventFilter(processIdFilter.And(eventIdFilter));

            filter.OnEvent += (IEventRecord r) =>
            {
                try
                {
                    var obj = _propertyExtractor.Extract(r);
                    lock (_lock) { _records.Add(obj.ToPSObject()); }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };

            networkProvider.AddFilter(filter);
            return new PSEtwUserProvider(networkProvider, providerName);
        }

        private PSEtwUserProvider CreateDnsProvider()
        {
            const string providerName = "Microsoft-Windows-DNS-Client";
            var dnsProvider = new Provider(providerName);

            const int NXDomainEventId = 1016;
            const int CachedLookupEventId = 3018;
            const int LiveLookupEventId = 3020;

            var eventIdFilter = Filter.EventIdIs(NXDomainEventId)
                    .Or(Filter.EventIdIs(CachedLookupEventId)
                    .Or(Filter.EventIdIs(LiveLookupEventId)));
            var filter = new EventFilter(eventIdFilter);

            filter.OnEvent += (IEventRecord r) =>
            {
                try
                {
                    var obj = _propertyExtractor.Extract(r);
                    lock (_lock) { _records.Add(obj.ToPSObject()); }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };

            dnsProvider.AddFilter(filter);
            return new PSEtwUserProvider(dnsProvider, providerName);
        }

        private PSEtwUserProvider CreateRegistryProvider()
        {
            const string providerName = "Microsoft-Windows-Kernel-Registry";
            var registryProvider = new Provider(providerName);

            IEventRecordDelegate callback = (IEventRecord r) =>
            {
                try
                {
                    var obj = _propertyExtractor.Extract(r);
                    lock (_lock) { _records.Add(obj.ToPSObject()); }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };

            var filter = new EventFilter(Filter.ProcessIdIs((int)_processId));
            filter.OnEvent += callback;
            registryProvider.AddFilter(filter);

            return new PSEtwUserProvider(registryProvider, providerName);
        }

        private PSEtwUserProvider CreateFileProvider()
        {
            const string providerName = "Microsoft-Windows-Kernel-File";
            var fileProvider = new Provider(providerName);

            IEventRecordDelegate callback = (IEventRecord r) =>
            {
                try
                {
                    var obj = _propertyExtractor.Extract(r);
                    lock (_lock) { _records.Add(obj.ToPSObject()); }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };

            var pidFilter = Filter.ProcessIdIs((int)_processId);
            var eventIdFilter = Filter.EventIdIs(12).Or(Filter.EventIdIs(30));

            var filter = new EventFilter(pidFilter.And(eventIdFilter));
            filter.OnEvent += callback;
            fileProvider.AddFilter(filter);

            return new PSEtwUserProvider(fileProvider, providerName);
        }
        #endregion
    }
}
