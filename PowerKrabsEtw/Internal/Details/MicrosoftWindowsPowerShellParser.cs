// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using System.Collections.Generic;
using System.Linq;

namespace PowerKrabsEtw.Internal.Details
{
    internal class MicrosoftWindowsPowerShellParser : IPropertyParser
    {
        enum PropertyNames
        {
            ContextInfo
        }

        public IEnumerable<KeyValuePair<string, object>> ParseProperty(string propertyName, IEventRecord record)
        {
            switch (propertyName)
            {
                case nameof(PropertyNames.ContextInfo):
                    return ParseContextInfo(record);
                default:
                    return Enumerable.Empty<KeyValuePair<string, object>>();
            }
        }

        private IEnumerable<KeyValuePair<string, object>> ParseContextInfo(IEventRecord record)
        {
            const string HostAppKey = "Host Application = ";
            const string CmdNameKey = "Command Name = ";
            const string CmdTypeKey = "Command Type = ";
            const string UsrNameKey = "User = ";

            string data = string.Empty;
            var ret = new List<KeyValuePair<string, object>>();

            if (record.TryGetUnicodeString(nameof(PropertyNames.ContextInfo), out data))
            {
                var startIndex = 0;

                var index = data.IndexOf(HostAppKey, startIndex);
                var host = index != -1
                            ? data.ReadToNewline(index + HostAppKey.Length, out startIndex)
                            : string.Empty;
                ret.Add(new KeyValuePair<string, object>("HostProcess", host));

                index = data.IndexOf(CmdNameKey, startIndex);
                var name = index != -1
                            ? data.ReadToNewline(index + CmdNameKey.Length, out startIndex)
                            : string.Empty;
                ret.Add(new KeyValuePair<string, object>("CommandName", name));

                index = data.IndexOf(CmdTypeKey, startIndex);
                var type = index != -1
                            ? data.ReadToNewline(index + CmdTypeKey.Length, out startIndex)
                            : string.Empty;
                ret.Add(new KeyValuePair<string, object>("CommandType", type));

                index = data.IndexOf(UsrNameKey, startIndex);
                var user = index != -1
                            ? data.ReadToNewline(index + UsrNameKey.Length, out startIndex)
                            : string.Empty;
                ret.Add(new KeyValuePair<string, object>("UserName", user));

                return ret;
            }

            return Enumerable.Empty<KeyValuePair<string, object>>();
        }


    }
}
