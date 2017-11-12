// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using O365.Security.ETW;
using PowerKrabsEtw.Internal.Details;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerKrabsEtw.Internal.PropertyParser
{
    internal class PropertyExtractor
    {
        readonly bool _includeVerboseProperties;
        static ProviderDictionary<IPropertyParser> providerDictionary = new ProviderDictionary<IPropertyParser>();

        static PropertyExtractor()
        {
            providerDictionary.AddValue(
                "Microsoft-Windows-PowerShell",
                Guid.Parse("A0C1853B-5C40-4B15-8766-3CF1C58F985A"),
                new MicrosoftWindowsPowerShellParser());

            providerDictionary.AddValue(
                "Microsoft-Windows-Kernel-Network",
                Guid.Parse("7dd42a49-5329-4832-8dfd-43d979153a88"),
                new MicrosoftWindowsKernelNetworkParser());
        }

        internal PropertyExtractor(bool includeVerboseProperties)
        {
            _includeVerboseProperties = includeVerboseProperties;
        }

        internal IDictionary<string, object> Extract(IEventRecord record)
        {
            var dict = new Dictionary<string, object>();

            dict.Add("EtwEventId", record.Id);
            dict.Add("Etw" + nameof(record.Timestamp), record.Timestamp);
            dict.Add("Etw" + nameof(record.ProcessId), record.ProcessId);
            dict.Add("Etw" + nameof(record.ThreadId), record.ThreadId);
            dict.Add("Etw" + nameof(record.ProviderName), record.ProviderName);

            if (_includeVerboseProperties)
            {
                dict.Add("EtwEventName", record.Name);
                dict.Add("Etw" + nameof(record.ProviderId), record.ProviderId);
                dict.Add("Etw" + nameof(record.Version), record.Version);
                dict.Add("Etw" + nameof(record.Level), record.Level);
            }

            IPropertyParser parser = null;

            if (providerDictionary.Contains(record.ProviderId))
            {
                parser = providerDictionary.GetByProviderGuid(record.ProviderId);
            }

            foreach (var p in record.Properties)
            {
                var parsed = parser?.ParseProperty(p.Name, record);
                if (parsed != null && parsed.Any())
                {
                    foreach (var parsedProp in parsed)
                    {
                        dict.Add(parsedProp.Key, parsedProp.Value);
                    }
                }
                else
                {
                    dict.Add(p.Name, ParseBasicProperty(p, record));
                }
            }


            return dict;
        }

        private object ParseBasicProperty(Property prop, IEventRecord record)
        {
            object propertyValue = null;
            switch (prop.Type)
            {
                case (int)TDH_IN_TYPE.TDH_INTYPE_ANSISTRING:
                    propertyValue = record.GetAnsiString(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_BINARY:
                    propertyValue = record.GetBinary(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_COUNTEDSTRING:
                    propertyValue = record.GetCountedString(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_INT8:
                    propertyValue = record.GetInt8(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_INT16:
                    propertyValue = record.GetInt16(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_INT32:
                    propertyValue = record.GetInt32(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_INT64:
                    propertyValue = record.GetInt64(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_UINT8:
                    propertyValue = record.GetUInt8(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_UINT16:
                    propertyValue = record.GetUInt16(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_UINT32:
                    propertyValue = record.GetUInt32(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_UINT64:
                    propertyValue = record.GetUInt64(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_UNICODESTRING:
                    propertyValue = record.GetUnicodeString(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_FILETIME:
                    propertyValue = record.GetDateTime(prop.Name);
                    break;

                case (int)TDH_IN_TYPE.TDH_INTYPE_POINTER:
                    propertyValue = record.GetUInt64(prop.Name);
                    break;

                default:
                    propertyValue = "<Unknown type>";
                    break;
            }

            return propertyValue;
        }
    }
}
