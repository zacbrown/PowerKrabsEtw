using System;
using System.Management.Automation;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsLifecycle.Start, "EtwUserTrace")]
    public class StartEtwUserTrace : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public PSEtwUserTrace Trace { get; set; }

        protected override void BeginProcessing()
        {
            while (!Stopping)
            {
                Trace.Start((obj) => WriteObject(obj));
            }
        }
    }
}
