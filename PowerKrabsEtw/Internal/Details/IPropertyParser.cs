using O365.Security.ETW;
using System.Collections.Generic;

namespace PowerKrabsEtw.Internal.Details
{
    internal interface IPropertyParser
    {
        IEnumerable<KeyValuePair<string, object>> ParseProperty(string propertyName, IEventRecord record);
    }
}
