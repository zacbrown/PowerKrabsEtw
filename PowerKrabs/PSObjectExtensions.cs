using O365.Security.ETW;
using System;
using System.Management.Automation;

namespace PowerKrabs
{
    internal static class PSObjectExtensions
    {
        internal static void AddProperty(this PSObject @this, Property prop, IEventRecord record)
        {
            object value = null;

            try
            {
                switch (prop.Type)
                {
                    case (int)TDH_IN_TYPE.TDH_INTYPE_UNICODESTRING:
                        value = record.GetUnicodeString(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_ANSISTRING:
                        value = record.GetAnsiString(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_INT8:
                        value = record.GetInt8(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_UINT8:
                        value = record.GetUInt8(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_INT16:
                        value = record.GetInt16(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_UINT16:
                        value = record.GetUInt16(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_INT32:
                        value = record.GetInt32(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_UINT32:
                        value = record.GetUInt32(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_INT64:
                        value = record.GetInt64(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_UINT64:
                        value = record.GetUInt64(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_FLOAT:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_DOUBLE:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_BOOLEAN:
                        value = "<Unavailable>";
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_BINARY:
                        value = record.GetBinary(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_GUID:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_POINTER:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_FILETIME:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_SYSTEMTIME:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_SID:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_HEXINT32:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_HEXINT64:
                        value = "<Unavailable>";
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_COUNTEDSTRING:
                        value = record.GetCountedString(prop.Name);
                        break;

                    case (int)TDH_IN_TYPE.TDH_INTYPE_COUNTEDANSISTRING:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_REVERSEDCOUNTEDSTRING:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_REVERSEDCOUNTEDANSISTRING:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_NONNULLTERMINATEDSTRING:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_NONNULLTERMINATEDANSISTRING:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_UNICODECHAR:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_ANSICHAR:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_SIZET:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_HEXDUMP:
                    case (int)TDH_IN_TYPE.TDH_INTYPE_WBEMSID:
                        value = "<Unavailable>";
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine("Got an unsupported record field type for " + prop.Name + ": " + prop.Type);
                        value = "<Unavailable>";
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encountered an exception while extracting property - {ex.Message}\nStack:{ex.StackTrace}");
                value = "<Unavailable>";
            }

            @this.Properties.Add(new PSNoteProperty(prop.Name, value));
        }
    }
}
