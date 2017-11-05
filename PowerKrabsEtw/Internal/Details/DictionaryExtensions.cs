using System;
using System.Collections.Generic;
using System.Linq;
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
