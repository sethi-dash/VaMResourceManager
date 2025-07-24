using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Vrm.Vm;

namespace Vrm.Util
{
    public class Sorter
    {
        public const string Name = "Name";
        public const string Creator = "Creator";
        public const string Size = "Size";
        public const string Created = "Created";
        public const string Modified = "Modified";

        public static Sorter Default { get; } = new Sorter();

        private static VmElements.SortDsc Create(CbOption item)
        {
            return new VmElements.SortDsc(item.Name, item.Asc.Value ? ListSortDirection.Ascending : ListSortDirection.Descending);
        }


        public void Sort(IEnumerable<CbOption> items, IEnumerable<VmBase> tabs)
        {
            foreach (var t in tabs)
            {
                if (t is VmElements ve)
                {
                    ve.SortDescriptions = items.Select(Create).ToList();
                }
            }
        }
    }
}
