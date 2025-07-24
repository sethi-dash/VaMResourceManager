using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vrm.Cfg
{
    public class SortItemCfg
    {
        public string Name {get;set;}
        public bool Asc {get;set;}

        public SortItemCfg(string name, bool asc)
        {
            Name = name;
            Asc = asc;
        }
    }
}
