using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vrm.Cfg;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class DepsTreeItem
    {
        public bool IsResourceRelPath;
        public bool IsVarName;
        public bool IsVarMissing;
        public bool IsRefItemCfg;
        public bool IsString;

        public string Display { get; private set; }
        public object Value { get; private set; }

        private DepsTreeItem(object value)
        {
            Value = value;
        }

        public static DepsTreeItem FromVarName(VarName var, bool isMissing)
        {
            var obj = new DepsTreeItem(var);
            obj.IsVarName = true;
            obj.IsVarMissing = isMissing;
            obj.UpdateDisplay();
            return obj;
        }  

        public static DepsTreeItem FromResRelPath(string resRelPath)
        {
            var obj = new DepsTreeItem(resRelPath);
            obj.IsResourceRelPath = true;
            obj.UpdateDisplay();
            return obj;
        }  

        public static DepsTreeItem FromRefItemCfg(RefItemCfg r)
        {
            var obj = new DepsTreeItem(r);
            obj.IsRefItemCfg = true;
            obj.UpdateDisplay();
            return obj;
        }  

        
        public static DepsTreeItem FromString(string str)
        {
            var obj = new DepsTreeItem(str);
            obj.IsString = true;
            obj.UpdateDisplay();
            return obj;
        }  

        public void UpdateDisplay()
        {
            if (IsResourceRelPath)
                Display = Value.ToString();
            else if (IsVarName)
            {
                Display = ((VarName)Value).FullName;
                if (IsVarMissing)
                    Display += " - Missing";
            }
            else if (IsRefItemCfg)
                Display = ((RefItemCfg)Value).ToString();
            else if (IsString)
                Display = Value.ToString();
            else
            {
                Display = "Invalid value type";
            }
            Display = Display.Replace(@"AddonPackages\", "");
        }

        public override string ToString()
        {
            return Display;
        }
    }

    public class VmDepsTree : VmBase
    {
        private List<Node<DepsTreeItem>> _nodes;
        public List<Node<DepsTreeItem>> Nodes
        {
            get => _nodes;
            set
            {
                if (Equals(value, _nodes))
                    return;
                _nodes = value;
                OnPropertyChanged();
            }
        }

        private Node<DepsTreeItem> _selectedTreeItem;
        public Node<DepsTreeItem> SelectedTreeItem
        {
            get => _selectedTreeItem;
            set
            {
                _selectedTreeItem = value;
                OnPropertyChanged(nameof(SelectedTreeItem));
            }
        }

        private async void Update()
        {
            if (!IsSelected)
                return;
            if (ShowEntries && SelectedItem != null && SelectedItem.IsVar)
            {
                var tree = PathNode.BuildTree(SelectedItem.Var.Entries.ToHashSet());
                var treeStr = PathNode.ConvertTree(tree, n => DepsTreeItem.FromString(n.Name));
                Nodes = treeStr.Children;
            }
            else if (ShowDependencyTree && SelectedItem != null)
            {
                Nodes = SelectedItem.DepsProvider.Nodes;
            }
            else if (ShowDependentItems && SelectedItem != null)
            {
                Nodes = new List<Node<DepsTreeItem>> { new Node<DepsTreeItem>{ Value = DepsTreeItem.FromString("Calculating...") }};
                Nodes = await Task.Run(() =>
                {
                    try
                    {
                        return SelectedItem.DepsProvider.NodesDependent;
                    }
                    catch
                    {
                        return new List<Node<DepsTreeItem>>{ new Node<DepsTreeItem>{ Value = DepsTreeItem.FromString("Operation cancelled") }};
                    }

                });
            }
            else
            {
                Nodes = new List<Node<DepsTreeItem>> { new Node<DepsTreeItem>{ Value = DepsTreeItem.FromString("<No item selected>") }};
            }
        }

        public bool ShowEntries;
        public bool ShowDependencyTree;
        public bool ShowDependentItems;

        public override void OnShow()
        {
            base.OnShow();

            Update();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        protected override void OnSelectedItemChanged()
        {
            Update();
        }

        public VmDepsTree()
        {
            Name = "Dependency Tree";
        }
    }
}
