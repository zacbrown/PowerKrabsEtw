using PowerKrabsEtw.Internal.Details;
using System.Management.Automation;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsCommon.Get, "DnsCacheEntries")]
    public class GetDnsCacheEntries : PSCmdlet
    {
        protected override void BeginProcessing()
        {
            using (var dns = new DnsCacheHelper())
            {
                var ret = dns.GetDnsCacheEntries();

                foreach (var r in ret)
                {
                    var obj = new PSObject();
                    obj.Properties.Add(new PSNoteProperty("Domain", r.DomainName));
                    obj.Properties.Add(new PSNoteProperty("Address", r.Address));
                    WriteObject(obj);
                }
            }
        }
    }
}
