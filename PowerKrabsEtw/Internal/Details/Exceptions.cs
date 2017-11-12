// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace PowerKrabsEtw.Internal.Details
{
    public class TraceAlreadyRunningException : Exception
    {
        public TraceAlreadyRunningException() : base() { }
        public TraceAlreadyRunningException(string msg) : base(msg) { }
    }
}
