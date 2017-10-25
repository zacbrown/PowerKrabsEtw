// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace PowerKrabsEtw.Internal.Details
{
    internal class ParsedDnsRecord
    {
        internal DnsRecordType Type { get; private set; }
        internal IPAddress Address { get; private set; }

        // TODO: Is CNAME, TXT, or MX ever interesting?
        public static ParsedDnsRecord Parse(string recordString)
        {
            var record = new ParsedDnsRecord();

            if (IPAddress.TryParse(recordString, out IPAddress parsed))
            {
                record.Address = parsed;
                if (parsed.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    record.Type = DnsRecordType.AAAA;
                }
                else
                {
                    record.Type = DnsRecordType.A;
                }

                return record;
            }

            return null;
        }
    }
}
