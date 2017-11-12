// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using PowerKrabsEtw.Internal.Details;
using System.Linq;

namespace PowerKrabsEtw.Internal.ProviderSpecificHandler
{
    internal class MicrosoftWindowsDNSClientHandler : IProviderSpecificHandler
    {
        public IEventRecordDelegate GetHandler()
        {
            return HandleRecord;
        }

        internal void HandleRecord(IEventRecord record)
        {
            if (record.Id == 3018 || record.Id == 3020)
            {
                if (!record.TryGetUnicodeString("QueryName", out string domainName)) return;

                if (!record.TryGetUnicodeString("QueryResults", out string queryResult)) return;

                if (string.IsNullOrWhiteSpace(queryResult)) return;

                var tokens = queryResult.Trim().Split(';');

                var parsed = tokens
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s.Trim())
                        .Distinct()
                        .Select(ParsedDnsRecord.Parse)
                        .Where(r => r != null);

                var dnsRecords = parsed.ToArray();
                foreach (var dnsRecord in dnsRecords)
                {
                    ReverseDnsCache.AddOrUpdate(dnsRecord.Address, domainName);
                }
            }
        }
    }
}
