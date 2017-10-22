﻿// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Management.Automation;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.Set, "EtwUserProvider")]
    public class SetEtwUserProvider : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public PSEtwUserTrace Trace { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNull]
        public PSEtwUserProvider Provider { get; set; }

        protected override void BeginProcessing()
        {
            Trace.EnableProvider(Provider);
        }
    }
}
