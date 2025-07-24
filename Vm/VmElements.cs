using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Vrm.Cfg;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmElements : VmBase
    {
        public class SortDsc : IEquatable<SortDsc>
        {
            private readonly string _cond;
            private ListSortDirection _dir;

            public void ToggleDirection()
            {
                if (_dir == ListSortDirection.Descending)
                    _dir = ListSortDirection.Ascending;
                else if (_dir == ListSortDirection.Ascending)
                    _dir = ListSortDirection.Descending;
            }

            public bool Equals(SortDsc other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return _cond == other._cond && _dir == other._dir;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((SortDsc)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_cond != null ? _cond.GetHashCode() : 0) * 397) ^ (int)_dir;
                }
            }

            public static bool operator ==(SortDsc left, SortDsc right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(SortDsc left, SortDsc right)
            {
                return !Equals(left, right);
            }

            public SortDsc(string cond, ListSortDirection dir)
            {
                _cond = cond;
                _dir = dir;
            }

            public SortDescription ToDsc()
            {
                return new SortDescription(_cond, _dir);
            }
        }

        public List<VmElementBase> Items { get; } = new List<VmElementBase>();
        public ICollectionView GroupedItems { get; }

        private List<string> _groupDescriptors = new List<string>();
        public List<string> GroupDescriptors
        {
            get => _groupDescriptors;
            set
            {
                _groupDescriptors = value;

                GroupedItems.GroupDescriptions.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                        GroupedItems.GroupDescriptions.Add(new PropertyGroupDescription(item));
                }

                OnPropertyChanged();
            }
        }

        private List<SortDsc> _sortDescriptions = new List<SortDsc>();
        public List<SortDsc> SortDescriptions
        {
            get => _sortDescriptions;
            set
            {
                _sortDescriptions = value;

                GroupedItems.SortDescriptions.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                        GroupedItems.SortDescriptions.Add(item.ToDsc());
                }

                OnPropertyChanged();
            }
        }

        public ICommand CmdOnChecked {get;}
        public ICommand CmdOnUnchecked {get;}


        #region init

        protected VmElements()
        {
            var view = CollectionViewSource.GetDefaultView(Items);
            GroupedItems = view;

            CmdOnChecked = new RelayCommand(x=>
            {
                if (x is VmElementBase el)
                {
                    if(el.Var != null)
                        ParentTab?.ReceivedVarCheck(this, el.Var, el.IsChecked);
                    ParentTab?.ReceivedElementCheck(this, el, el.IsChecked);
                }
            });
            CmdOnUnchecked = new RelayCommand(x=>
            {
                if (x is VmElementBase el)
                {
                    if(el.Var != null)
                        ParentTab?.ReceivedVarCheck(this, el.Var, el.IsChecked);
                    ParentTab?.ReceivedElementCheck(this, el, el.IsChecked);
                }
            });
        }

        #endregion

        public override IEnumerable<VmCmdBtn> GetCmds()
        {
            yield break;
        }

        public override void OnReset()
        {
            Items.Clear();
            GroupedItems.Refresh();
            UpdateName();
        }

        public override void OnAddComplete()
        {
            GroupedItems.Refresh();
            UpdateName();
        }

        public override void UpdateName()
        {
            int count = GroupedItems.Cast<object>().Count();
            NameFull = $"{Name} ({count})";
            Count = count;
        }

        public override void OnApplyFilter(FilterMode mode, Predicate<object> f)
        {
            base.OnApplyFilter(mode, f);

            GroupedItems.Filter = f;
            GroupedItems.Refresh();

            UpdateName();
        }

        public override IEnumerable<VmElementBase> GetCheckedElements()
        {
            foreach (var item in Items)
            {
                if (item.IsChecked)
                {
                    yield return item;
                }
            }
        }

        public override void SetElementsChecked(HashSet<RefItemCfg> refs)
        {
            foreach (var item in Items)
            {
                var r = item.CreateRef();
                item.IsChecked = refs.Contains(r);
            }

            GroupedItems.Refresh();
        }

        public override void UpdateVarChecks(VmBase ignore, VarFile var, bool isChecked)
        {
            if (this == ignore)
                return;

            foreach (var item in Items)
            {
                if (item.Var == var)
                {
                    item.IsChecked = isChecked;
                }
            }
        }

        public override void SetChecks(bool onlyVisible, bool isChecked)
        {
            if (onlyVisible)
            {
                foreach (var item in GroupedItems.Cast<VmElementBase>())
                {
                    item.IsChecked = isChecked;
                }
            }
            else
            {
                foreach (var item in Items)
                    item.IsChecked = isChecked;
            }
        }

        public override void OnRemove(IList<VmElementBase> elements)
        {
            foreach(var element in elements)
                Items.Remove(element);
            GroupedItems.Refresh();
        }
    }
}