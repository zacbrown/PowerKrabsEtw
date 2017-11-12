// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PowerKrabsEtw.Internal;
using System;
using System.Management.Automation;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsLifecycle.Start, "EtwProcessSuspended")]
    public class StartEtwProcessSuspended : PSCmdlet
    {
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string ExePath { get; set; }

        [Parameter(Position = 1)]
        [ValidateNotNull]
        public string CommandLine { get; set; } = string.Empty;

        protected override void BeginProcessing()
        {
            string actualProcessPath = ExePath;
            var threadHandle = ProcessHelper.LaunchProcessSuspended(ExePath, CommandLine, out uint pid, out actualProcessPath);

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
