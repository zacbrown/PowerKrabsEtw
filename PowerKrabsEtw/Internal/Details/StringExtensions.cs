using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerKrabsEtw.Internal.Details
{
    static class StringExtensions
    {
        public static string ReadToNewline(this string data, int index, out int newIndex)
        {
            // don't want to throw, just return empty
            if (index >= data.Length)
            {
                newIndex = index;
                return string.Empty;
            }

            if (index < 0) index = 0;

            var start = index;

            while (index < data.Length && data[index] != '\r') index++;

            newIndex = index;
            return data.Substring(start, index - start);
        }
    }
}
