using O365.Security.ETW;
using PowerKrabsEtw.Internal.Details;
using System;
using System.Linq;
using System.Management.Automation;

namespace PowerKrabsEtw.Internal
{
    internal class PropertyExtractor
    {
        readonly bool _includeVerboseProperties;
        static ProviderDictionary providerDictionary = new ProviderDictionary();

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

        internal PSObject Extract(IEventRecord record)
        {
            var obj = new PSObject();

            obj.Properties.Add(new PSNoteProperty("EventId", record.Id));
            obj.Properties.Add(new PSNoteProperty(nameof(record.Timestamp), record.Timestamp));
            obj.Properties.Add(new PSNoteProperty(nameof(record.ProcessId), record.ProcessId));
            obj.Properties.Add(new PSNoteProperty(nameof(record.ThreadId), record.ThreadId));

            if (_includeVerboseProperties)
            {
                obj.Properties.Add(new PSNoteProperty("EventName", record.Name));
                obj.Properties.Add(new PSNoteProperty(nameof(record.ProviderName), record.ProviderName));
                obj.Properties.Add(new PSNoteProperty(nameof(record.ProviderId), record.ProviderId));
                obj.Properties.Add(new PSNoteProperty(nameof(record.Version), record.Version));
                obj.Properties.Add(new PSNoteProperty(nameof(record.Level), record.Level));
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
                        obj.Properties.Add(new PSNoteProperty(parsedProp.Key, parsedProp.Value));
                    }
                }
                else
                {
                    obj.Properties.Add(new PSNoteProperty(p.Name, ParseBasicProperty(p, record)));
                }
            }


            return obj;
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

                default:
                    propertyValue = "<Unknown type>";
                    break;
            }

            return propertyValue;
        }
    }
}
