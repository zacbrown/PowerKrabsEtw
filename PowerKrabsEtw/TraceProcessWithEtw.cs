// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using O365.Security.ETW;
using PowerKrabsEtw.Internal;
using PowerKrabsEtw.Internal.PropertyParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Threading;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsDiagnostic.Trace, "ProcessWithEtw")]
    public class TraceProcessWithEtw : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string ProcessName { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string ProcessArguments { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public string OutputFile { get; set; }

        // readonly instance
        readonly object _lock = new object();
        readonly List<PSObject> _records = new List<PSObject>();
        readonly JsonSerializerSettings _serialiazerSettings = null;
        readonly PropertyExtractor _propertyExtractor = new PropertyExtractor(false);
        readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // instance
        long _eventCounts = 0;
        IntPtr _processHandle = IntPtr.Zero;
        string _fullProcessPath = string.Empty;
        uint _processId = 0;

        public TraceProcessWithEtw()
        {
            _serialiazerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
        }

        protected override void BeginProcessing()
        {
            using (var writer = new StreamWriter(OutputFile))
            {
                PSEtwUserTrace trace = null;
                try
                {
                    _processHandle = ProcessHelper.LaunchProcessSuspended(ProcessName, ProcessArguments, out _processId, out _fullProcessPath);
                    Console.WriteLine($"{ProcessName} started suspended with PID {_processId}...");

                    Console.WriteLine($"Setting up trace...");
                    trace = SetupEtwTrace(writer);

                    Console.WriteLine($"ETW trace setup, resuming {ProcessName} (PID {_processId})");
                    ProcessHelper.ResumeProcess(_processHandle);

                    Console.WriteLine("Hit enter to start...");
                    while (Host.UI.RawUI.KeyAvailable) Host.UI.RawUI.ReadKey();
                    trace.Start((obj) =>
                    {
                        Interlocked.Increment(ref _eventCounts);
                    });

                    while (!trace.HasPumpedEvents)
                    {
                        Console.WriteLine("Waiting for trace to start...");
                        Thread.Sleep(1000);
                    }

                    TimeSpan sleepTimeSpan = TimeSpan.FromSeconds(2);
                    while (!Stopping && !_cts.IsCancellationRequested)
                    {
                        lock (_lock)
                        {
                            Console.WriteLine($"Processed {_eventCounts} events in last {sleepTimeSpan.Seconds} seconds");
                            _eventCounts = 0;
                        }
                        
                        Thread.Sleep(sleepTimeSpan);
                    }
                }
                finally
                {
                    trace.Stop();
                }
            }
        }

        #region Provider Helpers
        private PSEtwUserTrace SetupEtwTrace(StreamWriter writer)
        {
            var trace = new PSEtwUserTrace($"Trace-ProcessWithEtw-{Guid.NewGuid()}", false);
            var processProvider = CreateProcessProvider(writer);
            var powershellProvider = CreatePowerShellProvider(writer);
            var networkProvider = CreateNetworkProvider(writer);
            var dnsProvider = CreateDnsProvider(writer);
            var wmiProvider = CreateWmiActivityProvider(writer);

            trace.EnableProvider(processProvider);
            trace.EnableProvider(powershellProvider);
            trace.EnableProvider(networkProvider);
            trace.EnableProvider(dnsProvider);
            trace.EnableProvider(wmiProvider);

            return trace;
        }

        private PSEtwUserProvider CreateProcessProvider(StreamWriter writer)
        {
            const string providerName = "Microsoft-Windows-Kernel-Process";
            var processProvider = new Provider(providerName);

            // process start/stop
            var startStopFilter = new EventFilter(Filter.EventIdIs(1).Or(Filter.EventIdIs(2)));
            startStopFilter.OnEvent += (IEventRecord r) =>
            {
                if (r.Id == 1)
                {
                    var parentProcessId = r.GetUInt32("ParentProcessID");
                    if (parentProcessId == _processId)
                    {
                        var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                        writer.WriteLine(obj);
                    }
                }
                else if (r.Id == 2 && r.ProcessId == _processId)
                {
                    var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                    writer.WriteLine(obj);
                    _cts.Cancel();
                }
            };
            processProvider.AddFilter(startStopFilter);

            // image load
            var imageLoadFilter = new EventFilter(Filter.ProcessIdIs((int)_processId).And(Filter.EventIdIs(5)));
            imageLoadFilter.OnEvent += (IEventRecord r) =>
            {
                var psObj = _propertyExtractor.Extract(r);
                var obj = JsonConvert.SerializeObject(psObj, _serialiazerSettings);
                writer.WriteLine(obj);
            };
            processProvider.AddFilter(imageLoadFilter);

            // thread injection
            var threadFilter = new EventFilter(Filter.ProcessIdIs((int)_processId).And(Filter.EventIdIs(3)));
            threadFilter.OnEvent += (IEventRecord r) =>
            {
                var targetProcessId = (int)r.GetUInt32("ProcessID");
                var targetProcess = Process.GetProcessById(targetProcessId);
                var targetProcessName = targetProcess.ProcessName;

                // If the process start for the target process is more than
                // 10 milliseconds after the thread creation time, then it
                // is a good chance this is not the initial thread.
                var diff = r.Timestamp.Subtract(targetProcess.StartTime);
                if (diff.TotalMilliseconds > 10)
                {
                    var obj = _propertyExtractor.Extract(r);
                    obj.Add("TargetProcessName", targetProcessName);

                    var serialized = JsonConvert.SerializeObject(obj, _serialiazerSettings);
                    writer.WriteLine(obj);
                }
            };
            processProvider.AddFilter(threadFilter);

            return new PSEtwUserProvider(processProvider, providerName);
        }

        private PSEtwUserProvider CreatePowerShellProvider(StreamWriter writer)
        {
            const string providerName = "Microsoft-Windows-PowerShell";
            var powershellProvider = new Provider(providerName);

            var filter = new EventFilter(Filter.ProcessIdIs((int)_processId).And(Filter.EventIdIs(7937)));
            filter.OnEvent += (IEventRecord r) =>
            {
                var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                writer.WriteLine(obj);
            };

            return new PSEtwUserProvider(powershellProvider, providerName);
        }

        private PSEtwUserProvider CreateWmiActivityProvider(StreamWriter writer)
        {
            const int WMIEventId = 11;
            const string providerName = "Microsoft-Windows-WMI-Activity";
            var wmiProvider = new Provider(providerName);

            IEventRecordDelegate callback = (IEventRecord r) =>
            {
                var psObj = _propertyExtractor.Extract(r);
                var obj = JsonConvert.SerializeObject(psObj, _serialiazerSettings);
                writer.WriteLine(obj);
            };

            var filterProcessIdAndEventId = Filter.ProcessIdIs((int)_processId)
                .And(Filter.EventIdIs(WMIEventId));

            // A suspicious instance creation/modification
            var suspiciousInstanceCreationFilter = UnicodeString.IContains("Operation", "::PutInstance").And
                (UnicodeString.IContains("NamespaceName", "root\\subscription").Or
                (UnicodeString.IContains("Operation", "__Provider")).Or
                (UnicodeString.IContains("Operation", "Win32_Service")).Or
                (UnicodeString.IContains("Operation", "Win32_StartupCommand")).Or
                (UnicodeString.IContains("Operation", "__NAMESPACE")).Or
                (UnicodeString.IContains("Operation", "__EventFilter")).Or
                (UnicodeString.IContains("Operation", "__FilterToConsumerBinding")).Or
                (UnicodeString.IContains("Operation", "ActiveScriptEventConsumer")).Or
                (UnicodeString.IContains("Operation", "CommandLineEventConsumer")).Or
                (UnicodeString.IContains("Operation", "LogFileEventConsumer")).Or
                (UnicodeString.IContains("Operation", "NTEventLogEventConsumer")).Or
                (UnicodeString.IContains("Operation", "SMTPEventConsumer")));

            var wmiSuspiciousInstanceCreationFilter = new EventFilter(filterProcessIdAndEventId
                .And(suspiciousInstanceCreationFilter));
            wmiSuspiciousInstanceCreationFilter.OnEvent += callback;

            // A suspicious class creation/modification
            var suspiciousClassCreationFilter = UnicodeString.IContains("Operation", "::PutClass").And
                (Filter.Not(UnicodeString.IContains("Operation", "HWINV")));

            var wmiSuspiciousClassCreationFilter = new EventFilter(filterProcessIdAndEventId
                .And(suspiciousClassCreationFilter));
            wmiSuspiciousClassCreationFilter.OnEvent += callback;

            // A suspicious method invocation
            var suspiciousMethodExecutionFilter = UnicodeString.IContains("Operation", "::ExecMethod").And
                (UnicodeString.IContains("Operation", "Win32_Process::Create")).Or
                (UnicodeString.IContains("Operation", "StdRegProv::Set").And(Filter.Not(UnicodeString.IContains("Operation", "SetBinaryValue")))).Or
                (UnicodeString.IContains("Operation", "StdRegProv::Create")).Or
                (UnicodeString.IContains("Operation", "Win32_ShadowCopy::Create")).Or
                (UnicodeString.IContains("Operation", "CIM_datafile::Copy"));
            var wmiSuspiciousMethodExecutionFilter = new EventFilter(filterProcessIdAndEventId
                .And(suspiciousMethodExecutionFilter));
            wmiSuspiciousMethodExecutionFilter.OnEvent += callback;

            wmiProvider.AddFilter(wmiSuspiciousInstanceCreationFilter);
            wmiProvider.AddFilter(wmiSuspiciousClassCreationFilter);
            wmiProvider.AddFilter(wmiSuspiciousMethodExecutionFilter);

            return new PSEtwUserProvider(wmiProvider, providerName);
        }

        private PSEtwUserProvider CreateNetworkProvider(StreamWriter writer)
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
                var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                writer.WriteLine(obj);
            };

            networkProvider.AddFilter(filter);
            return new PSEtwUserProvider(networkProvider, providerName);
        }

        private PSEtwUserProvider CreateDnsProvider(StreamWriter writer)
        {
            const string providerName = "Microsoft-Windows-DNS-Client";
            var dnsProvider = new Provider(providerName);

            const int NXDomainEventId = 1016;
            const int CachedLookupEventId = 3018;
            const int LiveLookupEventId = 3020;

            var processIdFilter = Filter.ProcessIdIs((int)_processId);
            var eventIdFilter = Filter.EventIdIs(NXDomainEventId)
                    .Or(Filter.EventIdIs(CachedLookupEventId)
                    .Or(Filter.EventIdIs(LiveLookupEventId)));
            var filter = new EventFilter(processIdFilter.And(eventIdFilter));

            filter.OnEvent += (IEventRecord r) =>
            {
                var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                writer.WriteLine(obj);
            };

            dnsProvider.AddFilter(filter);
            return new PSEtwUserProvider(dnsProvider, providerName);
        }

        #endregion
    }
}
