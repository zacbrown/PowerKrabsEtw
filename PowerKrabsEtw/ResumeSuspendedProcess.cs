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
            NativeMethods.ResumeProcess(ProcessObject.ThreadHandle);
        }
    }
}
