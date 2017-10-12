using System.Management.Automation;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsLifecycle.Start, "EtwUserTrace")]
    public class StartEtwUserTrace : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public PSEtwUserTrace Trace { get; set; }

        protected override void BeginProcessing()
        {
            Trace.Start();
        }
    }
}
