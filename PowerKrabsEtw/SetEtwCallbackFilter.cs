using System.Management.Automation;

using O365.Security.ETW;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.Set, "EtwCallbackFilter")]
    public class SetEtwCallbackFilter : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public PSEtwUserProvider UserProvider { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNull]
        public PSEtwFilter Filter { get; set; }

        protected override void BeginProcessing()
        {
            UserProvider.AddFilter(Filter);
        }
    }
}
