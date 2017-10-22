﻿using PowerKrabsEtw.Internal;
using System;
using System.Management.Automation;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsLifecycle.Start, "ProcessSuspended")]
    public class StartProcessSuspended : PSCmdlet
    {
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string ExePath { get; set; }

        [Parameter(Position = 1)]
        [ValidateNotNull]
        public string CommandLine { get; set; } = string.Empty;

        protected override void BeginProcessing()
        {
            uint pid = 0;
            string actualProcessPath = ExePath;
            var threadHandle = NativeMethods.LaunchProcessSuspended(ExePath, CommandLine, out pid, out actualProcessPath);

            if (threadHandle == IntPtr.Zero)
            {
                var error = new ErrorRecord(new InvalidOperationException(),
                    $"Unable to start process at path '{ExePath}'", ErrorCategory.InvalidOperation, null);

                WriteError(error);
            }
            else
            {
                WriteObject(new PSProcessObject(actualProcessPath, CommandLine, pid, threadHandle));
            }
        }
    }
}