using System;
using System.Collections.Generic;

namespace Utility
{
    public static class Utils
    {
        public static void ForEachI<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in ie) action(e, i++);
        }
    }
}