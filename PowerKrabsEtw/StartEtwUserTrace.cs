using PowerKrabsEtw.Internal;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsLifecycle.Start, "EtwUserTrace")]
    public class StartEtwUserTrace : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public PSEtwUserTrace Trace { get; set; }

        readonly List<PSObject> records = new List<PSObject>();

        protected override void BeginProcessing()
        {
            try
            {
                while (Host.UI.RawUI.KeyAvailable) Host.UI.RawUI.ReadKey();
                while (!Stopping)
                {
                    Trace.Start((obj) =>
                    {
                        records.Add(obj);
                    });

                    lock (records)
                    {
                        foreach (var r in records)
                        {
                            WriteObject(r);
                        }
                        records.Clear();
                    }

                    Thread.Sleep(100);
                }
            }
            finally
            {
                Trace.Stop();
            }
        }
    }
}
