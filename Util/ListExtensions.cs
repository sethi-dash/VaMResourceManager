using System.Collections.Generic;

namespace Vrm.Util
{
    public static class ListExtensions
    {
        public static bool UnorderedSequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer = null)
        {
            if (first == null && second == null) return true;
            if (first == null || second == null) return false;
        
            var counts = new Dictionary<T, int>(comparer);
        
            foreach (var item in first)
            {
                if (counts.ContainsKey(item))
                    counts[item]++;
                else
                    counts[item] = 1;
            }
        
            foreach (var item in second)
            {
                if (!counts.ContainsKey(item))
                    return false;
            
                counts[item]--;
                if (counts[item] == 0)
                    counts.Remove(item);
            }
        
            return counts.Count == 0;
        }
    }
}
