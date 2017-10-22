// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace PowerKrabsEtw.Internal
{
    internal enum TDH_IN_TYPE
    {
        TDH_INTYPE_NULL,
        TDH_INTYPE_UNICODESTRING,
        TDH_INTYPE_ANSISTRING,
        TDH_INTYPE_INT8,
        TDH_INTYPE_UINT8,
        TDH_INTYPE_INT16,
        TDH_INTYPE_UINT16,
        TDH_INTYPE_INT32,
        TDH_INTYPE_UINT32,
        TDH_INTYPE_INT64,
        TDH_INTYPE_UINT64,
        TDH_INTYPE_FLOAT,
        TDH_INTYPE_DOUBLE,
        TDH_INTYPE_BOOLEAN,
        TDH_INTYPE_BINARY,
        TDH_INTYPE_GUID,
        TDH_INTYPE_POINTER,
        TDH_INTYPE_FILETIME,
        TDH_INTYPE_SYSTEMTIME,
        TDH_INTYPE_SID,
        TDH_INTYPE_HEXINT32,
        TDH_INTYPE_HEXINT64, // End of winmeta intypes.
        TDH_INTYPE_COUNTEDSTRING = 300, // Start of TDH intypes for WBEM.
        TDH_INTYPE_COUNTEDANSISTRING,
        TDH_INTYPE_REVERSEDCOUNTEDSTRING,
        TDH_INTYPE_REVERSEDCOUNTEDANSISTRING,
        TDH_INTYPE_NONNULLTERMINATEDSTRING,
        TDH_INTYPE_NONNULLTERMINATEDANSISTRING,
        TDH_INTYPE_UNICODECHAR,
        TDH_INTYPE_ANSICHAR,
        TDH_INTYPE_SIZET,
        TDH_INTYPE_HEXDUMP,
        TDH_INTYPE_WBEMSID
    }
}
