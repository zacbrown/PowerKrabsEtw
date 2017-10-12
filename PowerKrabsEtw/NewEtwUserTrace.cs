using System;
using System.Management.Automation;

using O365.Security.ETW;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.New, "EtwUserTrace")]
    public class NewEtwUserTrace : PSCmdlet
    {
        [Parameter()]
        public string Name { get; set; } = Guid.NewGuid().ToString();

        protected override void ProcessRecord()
        {
            var traceMan = new PSEtwUserTrace(PSEventRecordDelegate, Name);
            WriteObject(traceMan);
        }

        private PropertyExtractor _extractor = new PropertyExtractor();
        private void PSEventRecordDelegate(IEventRecord evt)
        {
            WriteObject(_extractor.Extract(evt));
        }
    }
}
