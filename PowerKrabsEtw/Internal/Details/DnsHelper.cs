// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace PowerKrabsEtw.Internal.Details
{
    internal class ManagedDnsCacheEntry
    {
        internal string DomainName { get; set; }
        internal IPAddress Address { get; set; }
    }

    internal class DnsHelper : IDisposable
    {
        readonly Win32Interop.DnsGetCacheDataTable _dnsGetCacheDataTable;
        readonly IntPtr _dnsapiLibHandle;

        internal DnsHelper()
        {
            _dnsapiLibHandle = Win32Interop.LoadLibrary(@"dnsapi.dll");
            if (_dnsapiLibHandle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());

            var procAddress = Win32Interop.GetProcAddress(_dnsapiLibHandle, nameof(Win32Interop.DnsGetCacheDataTable));
            _dnsGetCacheDataTable = (Win32Interop.DnsGetCacheDataTable)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(Win32Interop.DnsGetCacheDataTable));
        }

        public void Dispose()
        {
            if (_dnsapiLibHandle != IntPtr.Zero)
            {
                if (!Win32Interop.FreeLibrary(_dnsapiLibHandle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        // BUGBUG: This function is gross. Clean this up.
        internal IEnumerable<ManagedDnsCacheEntry> GetDnsCacheEntries()
        //internal IEnumerable<string> GetDnsCacheEntries()
        {
            var ret = new List<ManagedDnsCacheEntry>();

            IntPtr ptr = IntPtr.Zero;
            IntPtr table = IntPtr.Zero;
            if (_dnsGetCacheDataTable.Invoke(out table))
            {
                ptr = table;
                do
                {
                    var entry = (DnsCacheEntry)Marshal.PtrToStructure(ptr, typeof(DnsCacheEntry));
                    var asString = Marshal.PtrToStringAuto(entry.pszName);
                    IntPtr queryResults = IntPtr.Zero;

                    if (Win32Interop.DnsQuery(ref asString, DnsRecordType.A, (DnsQueryType)0x8010, IntPtr.Zero, ref queryResults, IntPtr.Zero) == 0)
                    {
                        var recordIndexPtr = queryResults;

                        do
                        {
                            var record = (DnsRecord)Marshal.PtrToStructure(recordIndexPtr, typeof(DnsRecord));
                            if (record.RecordType == DnsRecordType.A)
                            {
                                int size = Marshal.SizeOf(record);
                                recordIndexPtr += size;
                                var ipv4 = (DnsARecord)Marshal.PtrToStructure(recordIndexPtr, typeof(DnsARecord));

                                ret.Add(new ManagedDnsCacheEntry { DomainName = asString, Address = new IPAddress(ipv4.IpAddress) });
                                recordIndexPtr = record.Next;
                            }
                            else
                            {
                                recordIndexPtr = IntPtr.Zero;
                            }
                        } while (recordIndexPtr != IntPtr.Zero);

                        if (queryResults != IntPtr.Zero) Win32Interop.DnsFree(queryResults, DnsFreeType.FreeRecordList);
                    }

                    if (Win32Interop.DnsQuery(ref asString, DnsRecordType.AAAA, (DnsQueryType)0x8010, IntPtr.Zero, ref queryResults, IntPtr.Zero) == 0)
                    {
                        var recordIndexPtr = queryResults;

                        do
                        {
                            var record = (DnsRecord)Marshal.PtrToStructure(queryResults, typeof(DnsRecord));
                            Console.WriteLine($"Domain: {asString} - DnsRecordType: {record.RecordType}");
                            if (record.RecordType == DnsRecordType.AAAA)
                            {
                                int size = Marshal.SizeOf(record);
                                recordIndexPtr += size;
                                var ipv6 = (DnsAAAARecord)Marshal.PtrToStructure(recordIndexPtr, typeof(DnsAAAARecord));
                                var address = GetIPAddressFromDnsAAAARecord(ipv6);
                                ret.Add(new ManagedDnsCacheEntry { DomainName = asString, Address = address });
                                recordIndexPtr = record.Next;
                            }
                            else
                            {
                                recordIndexPtr = IntPtr.Zero;
                            }
                        }
                        while (recordIndexPtr != IntPtr.Zero);

                        if (queryResults != IntPtr.Zero) Win32Interop.DnsFree(queryResults, DnsFreeType.FreeRecordList);
                    }

                    var temp = ptr;
                    ptr = entry.pNext;

                    Win32Interop.DnsFree(entry.pszName, DnsFreeType.FreeFlat);
                    Win32Interop.DnsFree(temp, DnsFreeType.FreeFlat);
                }
                while (ptr != IntPtr.Zero);
            }
            return ret;
        }

        private IPAddress GetIPAddressFromDnsAAAARecord(DnsAAAARecord data)
        {
            var addressBytes = new byte[16];
            addressBytes[0] = (byte)(data.Ip6Address0 & 0x000000FF);
            addressBytes[1] = (byte)((data.Ip6Address0 & 0x0000FF00) >> 8);
            addressBytes[2] = (byte)((data.Ip6Address0 & 0x00FF0000) >> 16);
            addressBytes[3] = (byte)((data.Ip6Address0 & 0xFF000000) >> 24);
            addressBytes[4] = (byte)(data.Ip6Address1 & 0x000000FF);
            addressBytes[5] = (byte)((data.Ip6Address1 & 0x0000FF00) >> 8);
            addressBytes[6] = (byte)((data.Ip6Address1 & 0x00FF0000) >> 16);
            addressBytes[7] = (byte)((data.Ip6Address1 & 0xFF000000) >> 24);
            addressBytes[8] = (byte)(data.Ip6Address2 & 0x000000FF);
            addressBytes[9] = (byte)((data.Ip6Address2 & 0x0000FF00) >> 8);
            addressBytes[10] = (byte)((data.Ip6Address2 & 0x00FF0000) >> 16);
            addressBytes[11] = (byte)((data.Ip6Address2 & 0xFF000000) >> 24);
            addressBytes[12] = (byte)(data.Ip6Address3 & 0x000000FF);
            addressBytes[13] = (byte)((data.Ip6Address3 & 0x0000FF00) >> 8);
            addressBytes[14] = (byte)((data.Ip6Address3 & 0x00FF0000) >> 16);
            addressBytes[15] = (byte)((data.Ip6Address3 & 0xFF000000) >> 24);

            return new IPAddress(addressBytes);
        }

    }
}
