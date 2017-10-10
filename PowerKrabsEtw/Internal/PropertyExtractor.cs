using O365.Security.ETW;
using System.Management.Automation;

namespace PowerKrabsEtw.Internal
{
    internal class PropertyExtractor
    {
        internal PSObject Extract(IEventRecord record)
        {
            var obj = new PSObject();

            foreach (var p in record.Properties)
            {
                obj.AddProperty(p, record);
            }

            return obj;
        }
    }
}
