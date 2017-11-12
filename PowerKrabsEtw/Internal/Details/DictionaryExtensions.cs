// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Management.Automation;

namespace PowerKrabsEtw.Internal.Details
{
    internal static class DictionaryExtensions
    {
        internal static PSObject ToPSObject(this IDictionary<string, object> @this)
        {
            var obj = new PSObject();

            foreach (var kv in @this)
            {
                obj.Properties.Add(new PSNoteProperty(kv.Key, kv.Value));
            }

            return obj;
        }
    }
}
