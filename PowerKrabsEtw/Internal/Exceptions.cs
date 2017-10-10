using System;

namespace PowerKrabsEtw.Internal
{
    public class TraceAlreadyRunningException : Exception
    {
        public TraceAlreadyRunningException() : base() { }
        public TraceAlreadyRunningException(string msg) : base(msg) { }
    }
}
