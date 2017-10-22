using System;

namespace PowerKrabsEtw.Internal
{
    public class PSProcessObject
    {
        internal PSProcessObject(string exePath, string commandLine, uint pid, IntPtr threadHandle)
        {
            ProcessId = pid;
            ExePath = exePath;
            CommandLine = commandLine;
            ThreadHandle = threadHandle;
        }

        public uint ProcessId { get; }
        public string ExePath { get; }
        public string CommandLine { get; }
        internal IntPtr ThreadHandle { get; }
    }
}
