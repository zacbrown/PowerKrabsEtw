using O365.Security.ETW;
using System;
using System.Management.Automation;

namespace PowerKrabs
{
    [Cmdlet("Create", "UserTrace")]
    public class CreateUserTrace : PSCmdlet
    {
        [Parameter()]
        public string Name { get; set; } = Guid.NewGuid().ToString();

        protected override void BeginProcessing()
        {
            var trace = new UserTrace(Name);
            WriteObject(trace);
        }
    }
}
