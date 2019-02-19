using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace of
{
    public static class Extensions
    {
        public static string ConcatenateWithSeparator(this IEnumerable<string> list, char separator)
        {
            return list.Aggregate((sum, value) =>
            {
                return $"{sum}{separator}\"{ value}\"";
            });
        }

        public static void AppendSemiColon(this string s)
        {
            if (s.Length > 0 && !s.EndsWith(";"))
                s += ";";            
        }
    }
}
