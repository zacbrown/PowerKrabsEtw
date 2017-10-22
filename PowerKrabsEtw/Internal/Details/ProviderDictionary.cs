// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerKrabsEtw.Internal.Details
{
    internal class ProviderDictionary
    {
        readonly object _lock = new object();
        readonly Dictionary<string, Guid> _providerNameDictionary = new Dictionary<string, Guid>();
        readonly Dictionary<Guid, IPropertyParser> _providerGuidDictionary = new Dictionary<Guid, IPropertyParser>();

        public void AddValue(string friendly, Guid guid, IPropertyParser parser)
        {
            lock (_lock)
            {
                _providerNameDictionary[friendly] = guid;
                _providerGuidDictionary[guid] = parser;
            }
        }

        public bool Contains(string name) => _providerNameDictionary.ContainsKey(name);

        public bool Contains(Guid guid) => _providerGuidDictionary.ContainsKey(guid);

        public IPropertyParser GetByProviderName(string key)
        {
            lock (_lock) return _providerGuidDictionary[_providerNameDictionary[key]];
        }

        public IPropertyParser GetByProviderGuid(Guid key)
        {
            lock (_lock) return _providerGuidDictionary[key];
        }

        public void RemoveByProviderName(string key)
        {
            lock (_lock)
            {
                var guid = _providerNameDictionary[key];
                _providerNameDictionary.Remove(key);
                _providerGuidDictionary.Remove(guid);
            }
        }

        public void RemoveByProviderString(Guid guid)
        {
            lock (_lock)
            {
                var providerName = _providerNameDictionary.Where((kv) => kv.Value == guid).Select(kv => kv.Key).First();
                _providerNameDictionary.Remove(providerName);
                _providerGuidDictionary.Remove(guid);
            }
        }
    }
}
