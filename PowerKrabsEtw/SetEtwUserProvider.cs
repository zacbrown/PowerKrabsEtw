using System.Management.Automation;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.Set, "EtwUserProvider")]
    public class SetEtwUserProvider : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public PSEtwUserTrace Trace { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public PSEtwUserProvider Provider { get; set; }

        protected override void BeginProcessing()
        {
            Trace.EnableProvider(Provider);
        }
    }
}
