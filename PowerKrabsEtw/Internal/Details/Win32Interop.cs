// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PowerKrabsEtw.Internal.Details
{
    internal static class Win32Interop
    {
        #region kernel32.dll
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        internal static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        internal static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        internal static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern uint SearchPath(string lpPath,
                         string lpFileName,
                         string lpExtension,
                         int nBufferLength,
                         [MarshalAs(UnmanagedType.LPTStr)]
                         StringBuilder lpBuffer,
                         out IntPtr lpFilePart);
        #endregion

        #region dnsapi.dll
        [DllImport("dnsapi", EntryPoint = "DnsQuery_W", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern uint DnsQuery(
            [MarshalAs(UnmanagedType.VBByRefStr)] ref string name,
            [MarshalAs(UnmanagedType.U2)] DnsRecordType type,
            [MarshalAs(UnmanagedType.U4)] DnsQueryType opts,
            IntPtr Servers,
            [In, Out] ref IntPtr queryResults,
            IntPtr reserved);

        [DllImport("dnsapi", EntryPoint = "DnsRecordListFree", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern void DnsRecordListFree(IntPtr records, DnsFreeType freeType);

        [DllImport("dnsapi", EntryPoint = "DnsFree", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern void DnsFree(IntPtr ptr, DnsFreeType freeType);

        // pre-decl for the DnsGetCacheDataTable function from dnsapi.dll
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool DnsGetCacheDataTable(out IntPtr entries);
        #endregion

    }

    #region Win32 Types
    [StructLayout(LayoutKind.Explicit)]
    internal struct DnsRecordFlags
    {
        [FieldOffset(0)]
        [MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
        internal uint DW;

        [FieldOffset(0)]
        [MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
        internal uint S;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DnsRecord
    {
        internal IntPtr Next;
        [MarshalAs(UnmanagedType.LPWStr)] internal string Name;
        [MarshalAs(UnmanagedType.U2)] internal DnsRecordType RecordType;
        [MarshalAs(UnmanagedType.U2)] internal ushort DataLength;
        internal DnsRecordFlags Flags;
        [MarshalAs(UnmanagedType.U4)] internal uint Ttl;
        [MarshalAs(UnmanagedType.U4)] internal uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DnsARecord
    {
        [MarshalAs(UnmanagedType.U4)] internal UInt32 IpAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DnsAAAARecord
    {
        public uint Ip6Address0;
        public uint Ip6Address1;
        public uint Ip6Address2;
        public uint Ip6Address3;
    }

    [Flags]
    internal enum DnsQueryType : uint
    {
        STANDARD = 0x00000000,
        ACCEPT_TRUNCATED_RESPONSE = 0x00000001,
        USE_TCP_ONLY = 0x00000002,
        NO_RECURSION = 0x00000004,
        BYPASS_CACHE = 0x00000008,
        NO_WIRE_QUERY = 0x00000010,
        NO_LOCAL_NAME = 0x00000020,
        NO_HOSTS_FILE = 0x00000040,
        NO_NETBT = 0x00000080,
        WIRE_ONLY = 0x00000100,
        TREAT_AS_FQDN = 0x00001000,
        ALLOW_EMPTY_AUTH_RESP = 0x00010000,
        DONT_RESET_TTL_VALUES = 0x00100000,
        RESERVED = 0xff000000,
        CACHE_ONLY = NO_WIRE_QUERY,
        RETURN_MESSAGE = 0x00000200
    }

    internal enum DnsQueryReturnCode : ulong
    {
        SUCCESS = 0L,
        UNSPECIFIED_ERROR = 9000,
        MASK = 0x00002328,
        FORMAT_ERROR = 9001L,
        SERVER_FAILURE = 9002L,
        NAME_ERROR = 9003L,
        NOT_IMPLEMENTED = 9004L,
        REFUSED = 9005L,
        YXDOMAIN = 9006L,
        YXRRSET = 9007L,
        NXRRSET = 9008L,
        NOTAUTH = 9009L,
        NOTZONE = 9010L,
        BADSIG = 9016L,
        BADKEY = 9017L,
        BADTIME = 9018L,
        PACKET_FMT_BASE = 9500,
        NO_RECORDS = 9501L,
        BAD_PACKET = 9502L,
        NO_PACKET = 9503L,
        RCODE = 9504L,
        UNSECURE_PACKET = 9505L
    }

    internal enum DnsFreeType : uint
    {
        FreeFlat = 0,
        FreeRecordList
    }

    // This is intentionally incomplete, we don't care
    // about the other types.
    [Flags]
    internal enum DnsRecordType : ushort
    {
        A = 0x0001,
        CNAME = 0x0005,
        AAAA = 0x001c,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DnsCacheEntry
    {
        [MarshalAs(UnmanagedType.SysUInt)] internal IntPtr pNext;
        [MarshalAs(UnmanagedType.SysUInt)] internal IntPtr pszName;         // DNS Record Name
        [MarshalAs(UnmanagedType.U2)] internal ushort wType;           // DNS Record Type
        [MarshalAs(UnmanagedType.U2)] internal ushort wDataLength;     // Not referenced
        [MarshalAs(UnmanagedType.U2)] internal ushort dwFlags;           // DNS Record Flags
    }


    [Flags]
    internal enum ProcessCreationFlags : uint
    {
        ZERO_FLAG = 0x00000000,
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        CREATE_NEW_CONSOLE = 0x00000010,
        CREATE_NEW_PROCESS_GROUP = 0x00000200,
        CREATE_NO_WINDOW = 0x08000000,
        CREATE_PROTECTED_PROCESS = 0x00040000,
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
        CREATE_SEPARATE_WOW_VDM = 0x00001000,
        CREATE_SHARED_WOW_VDM = 0x00001000,
        CREATE_SUSPENDED = 0x00000004,
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,
        DEBUG_PROCESS = 0x00000001,
        DETACHED_PROCESS = 0x00000008,
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
        INHERIT_PARENT_AFFINITY = 0x00010000
    }

    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    internal struct STARTUPINFO
    {
        public uint cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    #endregion
}
