﻿// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public TraceEtwProcess()
        {
            _serialiazerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            _serialiazerSettings.Converters.Add(new JsonIPAddressConverter());
        }

        protected override void BeginProcessing()
        {
            if (!Path.IsPathRooted(OutputFile))
            {
                OutputFile = Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, OutputFile);
            }

            using (var writer = new StreamWriter(OutputFile))
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
                    trace = SetupEtwTrace(writer);

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
                        lock (_lock)
                        {
                            WriteVerbose($"Processed {_eventCounts} events in last {sleepTimeSpan.Seconds} seconds...");
                            _eventCounts = 0;
                        }

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
                    WriteVerbose($"OutputFile full path is {OutputFile}");
                }
            }

            try
            {
                WriteObject(BuildSummaryFromOutputFile(OutputFile));
            }
            catch
            {
                // TODO: log error?
            }
        }

        #region Summary Helpers
        private PSObject BuildSummaryFromOutputFile(string filename)
        {
            var ret = new PSObject();
            var dict = new Dictionary<string, List<JObject>>();
            ret.Properties.Add(new PSNoteProperty(nameof(OutputFile), OutputFile));

            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var obj = JObject.Parse(line);
                    var provider = (string)obj["EtwHeader"][nameof(IEventRecord.ProviderName)];

                    if (!dict.ContainsKey(provider))
                    {
                        dict[provider] = new List<JObject>();
                    }

                    dict[provider].Add(obj);
                }
            }

            foreach (var kv in dict)
            {
                switch (kv.Key)
                {
                    case "Microsoft-Windows-Kernel-Process":
                        SummarizeProcessActivity(ret, kv.Value);
                        break;
                    case "Microsoft-Windows-Kernel-Network":
                        SummarizeNetworkActivity(ret, kv.Value);
                        break;
                    case "Microsoft-Windows-PowerShell":
                        SummarizePowerShellActivity(ret, kv.Value);
                        break;
                    default:
                        break;
                }
            }

            return ret;
        }

        private void SummarizeProcessActivity(PSObject output, List<JObject> records)
        {
            var dllsLoaded = new HashSet<string>(
                records
                    .Where(r => (int)r["EtwHeader"]["EventId"] == 5)
                    .Select(r => (string)r["ImageName"]),
                StringComparer.OrdinalIgnoreCase);

            output.Properties.Add(new PSNoteProperty("DllsLoaded", dllsLoaded));

            var processesInjectedInto = new HashSet<string>(
                records
                    .Where(r => (int)r["EtwHeader"]["EventId"] == 3)
                    .Select(r => (string)r["TargetProcessName"]),
                StringComparer.OrdinalIgnoreCase);

            output.Properties.Add(new PSNoteProperty("PossibleInjectedProcesses", processesInjectedInto));
        }

        private void SummarizeNetworkActivity(PSObject output, List<JObject> records)
        {
            var networkEndpoints = new HashSet<IPAddress>(records
                .Where(r => r["daddr"] != null && !string.IsNullOrEmpty(r["daddr"].ToString()))
                .Select(r => IPAddress.Parse(r["daddr"].ToString())));

            var networkEndpointsWithDomains = networkEndpoints
                .Select(ip => {
                    var obj = new PSObject();
                    obj.Properties.Add(new PSNoteProperty(nameof(IPAddress), ip));
                    obj.Properties.Add(new PSNoteProperty("HostName", ReverseDnsCache.GetDomainsByIPAddress(ip).ToArray()));
                    return obj;
                })
                .ToArray();

            output.Properties.Add(new PSNoteProperty("NetworkEndpoints", networkEndpointsWithDomains));
        }

        private void SummarizePowerShellActivity(PSObject output, List<JObject> records)
        {
            var commands = new HashSet<string>(
                records
                    .Where(r => !string.IsNullOrEmpty(r["CommandName"].ToString()))
                    .Select(r => r["CommandName"].ToString()),
                StringComparer.OrdinalIgnoreCase);

            output.Properties.Add(new PSNoteProperty("PowerShellCommands", commands.ToArray()));
        }

        #endregion

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
                try
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
                    var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                    writer.WriteLine(obj);
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

                            writer.WriteLine(JsonConvert.SerializeObject(obj, _serialiazerSettings));
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

        private PSEtwUserProvider CreatePowerShellProvider(StreamWriter writer)
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
                    var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                    writer.WriteLine(obj);
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };
            powershellProvider.AddFilter(filter);

            return new PSEtwUserProvider(powershellProvider, providerName);
        }

        private PSEtwUserProvider CreateWmiActivityProvider(StreamWriter writer)
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
                        var psObj = _propertyExtractor.Extract(r);
                        var obj = JsonConvert.SerializeObject(psObj, _serialiazerSettings);
                        writer.WriteLine(obj);
                    }
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };

            var filterEventId = Filter.EventIdIs(WMIEventId);

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

            var wmiSuspiciousInstanceCreationFilter = new EventFilter(filterEventId
                .And(suspiciousInstanceCreationFilter));
            wmiSuspiciousInstanceCreationFilter.OnEvent += callback;

            // A suspicious class creation/modification
            var suspiciousClassCreationFilter = UnicodeString.IContains("Operation", "::PutClass").And
                (Filter.Not(UnicodeString.IContains("Operation", "HWINV")));

            var wmiSuspiciousClassCreationFilter = new EventFilter(filterEventId
                .And(suspiciousClassCreationFilter));
            wmiSuspiciousClassCreationFilter.OnEvent += callback;

            // A suspicious method invocation
            var suspiciousMethodExecutionFilter = UnicodeString.IContains("Operation", "::ExecMethod").And
                (UnicodeString.IContains("Operation", "Win32_Process::Create")).Or
                (UnicodeString.IContains("Operation", "StdRegProv::Set").And(Filter.Not(UnicodeString.IContains("Operation", "SetBinaryValue")))).Or
                (UnicodeString.IContains("Operation", "StdRegProv::Create")).Or
                (UnicodeString.IContains("Operation", "Win32_ShadowCopy::Create")).Or
                (UnicodeString.IContains("Operation", "CIM_datafile::Copy"));
            var wmiSuspiciousMethodExecutionFilter = new EventFilter(filterEventId
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
                try
                {
                    var extracted = _propertyExtractor.Extract(r);
                    var obj = JsonConvert.SerializeObject(extracted, _serialiazerSettings);
                    writer.WriteLine(obj);
                }
                catch
                {
                    // TODO: log bad record parse
                }
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

            var eventIdFilter = Filter.EventIdIs(NXDomainEventId)
                    .Or(Filter.EventIdIs(CachedLookupEventId)
                    .Or(Filter.EventIdIs(LiveLookupEventId)));
            var filter = new EventFilter(eventIdFilter);

            filter.OnEvent += (IEventRecord r) =>
            {
                try
                {
                    var obj = JsonConvert.SerializeObject(_propertyExtractor.Extract(r), _serialiazerSettings);
                    writer.WriteLine(obj);
                }
                catch
                {
                    // TODO: log bad record parse
                }
            };

            dnsProvider.AddFilter(filter);
            return new PSEtwUserProvider(dnsProvider, providerName);
        }

        #endregion
    }
}