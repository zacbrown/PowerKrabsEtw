// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PowerKrabsEtw.Internal
{

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

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,
                                 bool bInheritHandles, ProcessCreationFlags dwCreationFlags, IntPtr lpEnvironment,
                                string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint SearchPath(string lpPath,
                         string lpFileName,
                         string lpExtension,
                         int nBufferLength,
                         [MarshalAs(UnmanagedType.LPTStr)]
                         StringBuilder lpBuffer,
                         out IntPtr lpFilePart);

        internal static IntPtr LaunchProcessSuspended(string processPath, string commandLine, out uint PID, out string actualProcessPath)
        {
            var fullProcessPath = GetFullProcessPath(processPath);

            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            bool success = CreateProcess(fullProcessPath, commandLine,
                IntPtr.Zero, IntPtr.Zero, false, ProcessCreationFlags.CREATE_SUSPENDED,
                IntPtr.Zero, null, ref si, out pi);
            var threadHandle = pi.hThread;
            PID = pi.dwProcessId;

            actualProcessPath = fullProcessPath;

            return success ? threadHandle : IntPtr.Zero;
        }

        private static string GetFullProcessPath(string processPath)
        {
            if (!File.Exists(processPath))
            {
                var sb = new StringBuilder(260);
                IntPtr ptr = new IntPtr();

                if (0 == SearchPath(null, processPath, null, sb.Capacity, sb, out ptr))
                {
                    throw new FileNotFoundException(processPath);
                }

                return sb.ToString();
            }

            return processPath;
        }

        public static void ResumeProcess(IntPtr threadHandle)
        {
            ResumeThread(threadHandle);
        }
    }

}
