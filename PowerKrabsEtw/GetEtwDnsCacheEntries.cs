using PowerKrabsEtw.Internal.Details;
using System.Management.Automation;
using System.Linq;
using System.Net;

namespace PowerKrabsEtw
{
    [Cmdlet(VerbsCommon.Get, "EtwDnsCacheEntries")]
    public class GetEtwDnsCacheEntries : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByIP")]
        public string IpAddress { get; set; }

        [Parameter(ParameterSetName = "ByDomain")]
        public string DomainName { get; set; }

        protected override void BeginProcessing()
        {
            var obj = new PSObject();
            if (IpAddress != null)
            {
                if (!System.Net.IPAddress.TryParse(IpAddress, out IPAddress asIpAddressObj))
                {
                    var error = new ErrorRecord(new PSArgumentException($"{IpAddress} does not appear to be a valid IP"),
                        nameof(PSArgumentException), ErrorCategory.InvalidArgument, null);

                    WriteError(error);
                    return;
                }
                var domains = ReverseDnsCache.GetDomainsByIPAddress(asIpAddressObj);
                obj.Properties.Add(new PSNoteProperty(nameof(DomainName), domains.ToArray()));
                obj.Properties.Add(new PSNoteProperty(nameof(IpAddress), IpAddress.ToString()));
            }
            else if (!string.IsNullOrWhiteSpace(DomainName))
            {
                var addresses = ReverseDnsCache.GetIPAddressesByDomain(DomainName);
                obj.Properties.Add(new PSNoteProperty(nameof(DomainName), DomainName));
                obj.Properties.Add(new PSNoteProperty(nameof(IpAddress), addresses.ToArray()));
            }
            else // (IPAddress == null && string.IsNullOrEmpty(DomainName))
            {
                var error = new ErrorRecord(new PSArgumentException($"Please specify -{IpAddress} or -{DomainName}."),
                    nameof(PSArgumentException), ErrorCategory.InvalidArgument, null);

                WriteError(error);
                return;
            }

            WriteObject(obj);
        }
    }
}
