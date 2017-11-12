﻿// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using System.Collections.Generic;

namespace PowerKrabsEtw.Internal.PropertyParser
{
    internal interface IPropertyParser
    {
        IEnumerable<KeyValuePair<string, object>> ParseProperty(string propertyName, IEventRecord record);
    }
}
