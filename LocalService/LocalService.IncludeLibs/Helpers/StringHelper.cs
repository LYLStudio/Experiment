using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalService.IncludeLibs.Helpers
{
    public static class StringHelper
    {
        public static string InsertSymbol(this string raw, int index, string symbol = "-")
        {
            return raw.Insert(index, symbol);
        }

    }
}
