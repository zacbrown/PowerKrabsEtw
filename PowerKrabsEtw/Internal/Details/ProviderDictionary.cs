// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PowerKrabsEtw.Internal.PropertyParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerKrabsEtw.Internal.Details
{
    internal class ProviderDictionary<T>
    {
        readonly object _lock = new object();
        readonly Dictionary<string, Guid> _providerNameDictionary = new Dictionary<string, Guid>();
        readonly Dictionary<Guid, T> _providerGuidDictionary = new Dictionary<Guid, T>();

        public void AddValue(string friendly, Guid guid, T value)
        {
            lock (_lock)
            {
                _providerNameDictionary[friendly] = guid;
                _providerGuidDictionary[guid] = value;
            }
        }

        public bool Contains(string name) => _providerNameDictionary.ContainsKey(name);

        public bool Contains(Guid guid) => _providerGuidDictionary.ContainsKey(guid);

        public T GetByProviderName(string key)
        {
            lock (_lock) return _providerGuidDictionary[_providerNameDictionary[key]];
        }

        public T GetByProviderGuid(Guid key)
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

        public void RemoveByProviderGuid(Guid guid)
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
