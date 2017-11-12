// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Management.Automation;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.New, "EtwUserTrace")]
    public class NewEtwUserTrace : PSCmdlet
    {
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = Guid.NewGuid().ToString();

        [Parameter(Position = 1)]
        public SwitchParameter IncludeVerboseProperties
        {
            get { return _includeVerboseProperties; }
            set { _includeVerboseProperties = value; }
        }
        private bool _includeVerboseProperties;

        protected override void BeginProcessing()
        {
            var traceMan = new PSEtwUserTrace(Name, IncludeVerboseProperties);
            WriteObject(traceMan);
        }
    }
}
