using PowerKrabsEtw.Internal;

using System;
using System.Management.Automation;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsCommon.New, "EtwUserTrace")]
    public class NewEtwUserTrace : PSCmdlet
    {
        [Parameter()]
        public string Name { get; set; } = Guid.NewGuid().ToString();

        protected override void ProcessRecord()
        {
            var traceMan = new UserTraceManager(Name);
            WriteObject(traceMan);
        }
    }
}
