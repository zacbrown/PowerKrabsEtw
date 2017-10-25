// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;

namespace PowerKrabsEtw.Internal.ProviderSpecificHandler
{
    internal interface IProviderSpecificHandler
    {
        IEventRecordDelegate GetHandler();
    }
}
