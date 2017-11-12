// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PowerKrabsEtw.Internal.Details
{
    internal static class ReverseDnsCache
    {
        static readonly Dictionary<IPAddress, HashSet<string>> _cache = new Dictionary<IPAddress, HashSet<string>>();

        static ReverseDnsCache()
        {
            var helper = new DnsCacheHelper();
            var cacheEntries = helper.GetDnsCacheEntries();
            foreach (var entry in cacheEntries) AddOrUpdate(entry.Address, entry.DomainName);
        }

        internal static void AddOrUpdate(IPAddress addr, string domain)
        {
            if (_cache.ContainsKey(addr))
            {
                _cache[addr].Add(domain);
            }
            else
            {
                _cache.Add(addr, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { domain });
            }
        }

        internal static IEnumerable<string> GetDomainsByIPAddress(IPAddress addr)
        {
            if (_cache.ContainsKey(addr)) return _cache[addr];

            return Enumerable.Empty<string>();
        }

        internal static IEnumerable<IPAddress> GetIPAddressesByDomain(string domain)
        {
            return _cache
                .Where(kv => kv.Value.Contains(domain))
                .Select(kv => kv.Key)
                .ToArray();
        }
    }
}
