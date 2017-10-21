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
        public SwitchParameter IncludeVerboseProperties
        {
            get { return _includeVerboseProperties; }
            set { _includeVerboseProperties = value; }
        }
        private bool _includeVerboseProperties;

        protected override void ProcessRecord()
        {
            var traceMan = new PSEtwUserTrace(Name, IncludeVerboseProperties);
            WriteObject(traceMan);
        }
    }
}
