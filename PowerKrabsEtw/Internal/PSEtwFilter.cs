// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using System.Collections.Generic;

namespace PowerKrabsEtw.Internal
{
    public class PSEtwFilter
    {
        readonly EventFilter _filter;
        readonly List<IEventRecordDelegate> _onEventHandlers = new List<IEventRecordDelegate>();

        internal PSEtwFilter(EventFilter filter)
        {
            _filter = filter;
        }

        internal void AddOnEventHandler(IEventRecordDelegate handler)
        {
            _onEventHandlers.Add(handler);
            _filter.OnEvent += handler;
        }

        internal IEnumerable<IEventRecordDelegate> OnEventHandlers => _onEventHandlers.ToArray();

        internal EventFilter Filter => _filter;
    }
}
