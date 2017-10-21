﻿using O365.Security.ETW;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PowerKrabsEtw.Internal.Details
{
    internal class MicrosoftWindowsKernelNetworkParser : IPropertyParser
    {
        enum PropertyNames
        {
            daddr,
            saddr
        }

        public IEnumerable<KeyValuePair<string, object>> ParseProperty(string propertyName, IEventRecord record)
        {
            switch (propertyName)
            {
                case nameof(PropertyNames.daddr):
                    return new List<KeyValuePair<string, object>>()
                    {
                        new KeyValuePair<string, object>(propertyName, ParseDaddr(record))
                    };
                case nameof(PropertyNames.saddr):
                    return new List<KeyValuePair<string, object>>()
                    {
                        new KeyValuePair<string, object>(propertyName, ParseSaddr(record))
                    };
                default:
                    return Enumerable.Empty<KeyValuePair<string, object>>();
            }
        }

        private IPAddress ParseDaddr(IEventRecord record) => record.GetIPAddress(nameof(PropertyNames.saddr));
        private IPAddress ParseSaddr(IEventRecord record) => record.GetIPAddress(nameof(PropertyNames.daddr));
    }
}
