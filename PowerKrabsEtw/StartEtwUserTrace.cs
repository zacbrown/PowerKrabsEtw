// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PowerKrabsEtw.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsLifecycle.Start, "EtwUserTrace")]
    public class StartEtwUserTrace : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateNotNull]
        public PSEtwUserTrace Trace { get; set; }

        [Parameter()]
        [ValidateNotNullOrEmpty]
        public TimeSpan TraceTimeLimit { get; set; } = TimeSpan.MaxValue;

        readonly object _lock = new object();
        readonly List<PSObject> _records = new List<PSObject>();

        protected override void BeginProcessing()
        {
            try
            {
                // BUGBUG: At times, it seemed this was necessary to deal with PSReadline messing with stuff?
                //while (Host.UI.RawUI.KeyAvailable) Host.UI.RawUI.ReadKey();

                Trace.Start((obj) =>
                {
                    lock (_lock) { _records.Add(obj); }
                });

                while (!Trace.HasPumpedEvents && !Stopping)
                {
                    WriteVerbose("Waiting for trace to start...");
                    Thread.Sleep(1000);
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                while (!Stopping && stopwatch.Elapsed < TraceTimeLimit)
                {
                    lock (_lock)
                    {
                        foreach (var r in _records)
                        {
                            WriteObject(r);
                        }
                        _records.Clear();
                    }

                    Thread.Sleep(100);
                }
            }
            catch (PipelineStoppedException) { }
            finally
            {
                Trace.Stop();
            }
        }
    }
}
