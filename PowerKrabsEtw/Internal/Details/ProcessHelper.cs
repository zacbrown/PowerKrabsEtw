// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PowerKrabsEtw.Internal.Details;
using System;
using System.IO;
using System.Text;

namespace PowerKrabsEtw.Internal
{
    internal static class ProcessHelper
    {
        internal static IntPtr LaunchProcessSuspended(string processPath, string commandLine, out uint PID, out string actualProcessPath)
        {
            var fullProcessPath = GetFullProcessPath(processPath);

            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            bool success = Win32Interop.CreateProcess(fullProcessPath, commandLine,
                IntPtr.Zero, IntPtr.Zero, false, ProcessCreationFlags.CREATE_SUSPENDED | ProcessCreationFlags.CREATE_NEW_CONSOLE,
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

                if (0 == Win32Interop.SearchPath(null, processPath, null, sb.Capacity, sb, out ptr))
                {
                    throw new FileNotFoundException(processPath);
                }

                return sb.ToString();
            }

            return processPath;
        }

        public static void ResumeProcess(IntPtr threadHandle)
        {
            Win32Interop.ResumeThread(threadHandle);
        }
    }

}
