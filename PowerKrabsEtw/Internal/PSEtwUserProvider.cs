// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using System.Collections.Generic;
using System.Linq;

namespace PowerKrabsEtw.Internal
{
    public class PSEtwUserProvider
    {
        readonly Provider _provider;
        readonly List<IEventRecordDelegate> _onEventHandlers = new List<IEventRecordDelegate>();
        readonly List<PSEtwFilter> _filters = new List<PSEtwFilter>();

        internal PSEtwUserProvider(Provider provider)
        {
            _provider = provider;
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
