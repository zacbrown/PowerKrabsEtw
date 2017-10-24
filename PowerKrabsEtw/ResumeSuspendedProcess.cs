// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PowerKrabsEtw.Internal;
using System.Management.Automation;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsLifecycle.Resume, "SuspendedProcess")]
    public class ResumeSuspendedProcess : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public PSProcessObject ProcessObject { get; set; }

        protected override void BeginProcessing()
        {
            ProcessHelper.ResumeProcess(ProcessObject.ThreadHandle);
        }
    }
}
