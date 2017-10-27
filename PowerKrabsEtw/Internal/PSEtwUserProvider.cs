// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using PowerKrabsEtw.Internal.Details;
using PowerKrabsEtw.Internal.ProviderSpecificHandler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerKrabsEtw.Internal
{
    public class PSEtwUserProvider
    {
        readonly Provider _provider;
        readonly List<IEventRecordDelegate> _onEventHandlers = new List<IEventRecordDelegate>();
        readonly List<PSEtwFilter> _filters = new List<PSEtwFilter>();

        static readonly ProviderDictionary<IProviderSpecificHandler> _providerSpecificHandlers
            = new ProviderDictionary<IProviderSpecificHandler>();

        static PSEtwUserProvider()
        {
            _providerSpecificHandlers.AddValue("Microsoft-Windows-DNS-Client",
                Guid.Parse("1c95126e-7eea-49a9-a3fe-a378b03ddb4d"),
                new MicrosoftWindowsDNSClientHandler());
        }

        internal PSEtwUserProvider(Provider provider, string providerName)
        {
            _provider = provider;
            if (_providerSpecificHandlers.Contains(providerName))
            {
                var customHandlerProvider = _providerSpecificHandlers.GetByProviderName(providerName);
                AddOnEventHandler(customHandlerProvider.GetHandler());
            }
        }

        internal PSEtwUserProvider(Provider provider, Guid providerGuid)
        {
            _provider = provider;
            if (_providerSpecificHandlers.Contains(providerGuid))
            {
                var customHandlerProvider = _providerSpecificHandlers.GetByProviderGuid(providerGuid);
                AddOnEventHandler(customHandlerProvider.GetHandler());
            }
        }

        internal Provider Provider => _provider;
        internal IEnumerable<IEventRecordDelegate> OnEventHandlers => _onEventHandlers.ToArray();
        internal IEnumerable<EventFilter> Filters => _filters.Select(f => f.Filter).ToArray();

        internal void AddOnEventHandler(IEventRecordDelegate handler)
        {
            _onEventHandlers.Add(handler);
            _provider.OnEvent += handler;
        }

        internal void AddFilter(PSEtwFilter filter)
        {
            _filters.Add(filter);
            _provider.AddFilter(filter.Filter);
        }

        internal void EnsureDefaultHandlerSetup(IEventRecordDelegate handler)
        {
            if (_filters.Any())
            {
                foreach (var filter in _filters)
                {
                    if (filter.OnEventHandlers.Any()) continue;
                    filter.AddOnEventHandler(handler);
                }
            }
            else
            {
                if (!_onEventHandlers.Any())
                {
                    AddOnEventHandler(handler);
                }
            }
        }
    }
}
