using O365.Security.ETW;

using System;
using System.Management.Automation;
using System.Threading;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.New, "EtwUserProvider")]
    public class NewEtwUserProvider : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByProviderName")]
        public string ProviderName { get; set; }

        [Parameter(ParameterSetName = "ByProviderGuid")]
        public string ProviderGuid { get; set; }

        [Parameter()]
        public ulong AnyFlags { get; set; }

        [Parameter()]
        public ulong AllFlags { get; set; }

        protected override void BeginProcessing()
        {
            Provider provider;

            if (ProviderName != null)
            {
                // In order to create the provider object using its friendly name
                // rather than the Guid, we need to use COM with an MTA apartment.
                // This is a weird special case and it's not necessary anywhere else.
                if (CheckIfInvalidApartmentState()) return;

                provider = new Provider(ProviderName);
            }
            else if (ProviderGuid != null)
            {
                provider = new Provider(Guid.Parse(ProviderGuid));
            }
            else
            {
                var ex = new ArgumentException($"Please provide a valid -{nameof(ProviderName)} or -{nameof(ProviderGuid)}");
                var errorRecord = new ErrorRecord(ex, nameof(PSArgumentException), ErrorCategory.InvalidArgument, null);
                WriteObject(errorRecord);
                return;
            }

            provider.All = AllFlags;
            provider.Any = AnyFlags;
            WriteObject(new PSEtwUserProvider(provider));
        }

        private bool CheckIfInvalidApartmentState()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
            {
                var ex = new ThreadStateException("New-EtwUserProvider with the -ProviderName flag requires powershell.exe to be started with the -MTA flag.");
                var er = new ErrorRecord(ex, nameof(ThreadStateException), ErrorCategory.InvalidOperation, null);
                WriteError(er);

                return true;
            }

            return false;
        }
    }
}
