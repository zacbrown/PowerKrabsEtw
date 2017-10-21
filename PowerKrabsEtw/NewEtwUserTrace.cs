using System;
using System.Management.Automation;

using O365.Security.ETW;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.New, "EtwUserTrace")]
    public class NewEtwUserTrace : PSCmdlet
    {
        [Parameter(Position = 0)]
        public string Name { get; set; } = Guid.NewGuid().ToString();

        [Parameter(Position = 1)]
        public bool IncludeVerboseProperties { get; set; } = false;

        protected override void ProcessRecord()
        {
            var traceMan = new PSEtwUserTrace(Name, IncludeVerboseProperties);
            WriteObject(traceMan);
        }
    }
}
