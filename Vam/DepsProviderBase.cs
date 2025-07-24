using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Vrm.Cfg;
using Vrm.Refs;
using Vrm.Util;
using Vrm.Vm;

namespace Vrm.Vam
{
    public abstract class DepsProviderBase
    {
        public abstract RefItemCfg GetRef();
        public abstract string GetTitle();
        public abstract ElementInfo GetElementInfo();

        private List<Node<DepsTreeItem>> _nodes;
        public List<Node<DepsTreeItem>> Nodes => _nodes ?? (_nodes = DepsHelper.CalcNodes(CalcDependencies()));
        private (RefItemDepItem, List<Node<VarName>>) CalcDependencies()
        {
            var errors = new ConcurrentQueue<string>();
            var @ref = new RefCfg { Items = new List<RefItemCfg> { GetRef() } };
            var res = DepsHelper.BuildMegaRef(new[] { @ref }.ToList(), errors);
            return res;
        }

        private List<Node<DepsTreeItem>> _nodesDependent;
        public List<Node<DepsTreeItem>> NodesDependent => _nodesDependent ?? (_nodesDependent = DepsHelper.CalcDependentNodes(this));
    }
}
