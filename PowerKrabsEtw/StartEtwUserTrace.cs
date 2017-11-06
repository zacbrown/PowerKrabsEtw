﻿using PowerKrabsEtw.Internal;
using System;
using System.Collections.Generic;
using System.Management.Automation;
// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsLifecycle.Start, "EtwUserTrace")]
    public class StartEtwUserTrace : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateNotNull]
        public PSEtwUserTrace Trace { get; set; }

        readonly object _lock = new object();
        readonly List<PSObject> _records = new List<PSObject>();

        protected override void BeginProcessing()
        {
            try
            {
                while (Host.UI.RawUI.KeyAvailable) Host.UI.RawUI.ReadKey();
                Trace.Start((obj) =>
                {
                    lock (_lock) { _records.Add(obj); }
                });

                while (!Trace.HasPumpedEvents && !Stopping)
                {
                    Console.WriteLine("Waiting for trace to start...");
                    Thread.Sleep(1000);
                }

                while (!Stopping)
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
            finally
            {
                Trace.Stop();
            }
        }
    }
}
