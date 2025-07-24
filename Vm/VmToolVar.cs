using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Vrm.Cfg;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmToolVar : VmBase
    {
        public VmPropertyTabs PropertyView { get; } = new VmPropertyTabs();
        private readonly VmBase _tabVar;
        private readonly VmTextRaw _propertyTabUserPrefs;

        private bool _browsingMode;
        public bool BrowsingMode
        {
            get => _browsingMode;
            set
            {
                if (value == _browsingMode)
                    return;
                _browsingMode = value;
                OnPropertyChanged();

                var mainVm = FindVmMain();
                if (mainVm != null)
                {
                    OnUpdateTools(mainVm.ShowTools);
                    mainVm.BrowsedVar = value ? BrowsedVar : null;
                }
            }
        }

        private VarFile _browsedVar;
        public VarFile BrowsedVar
        {
            get => _browsedVar;
            set
            {
                if (Equals(value, _browsedVar))
                    return;
                _browsedVar = value;
                OnPropertyChanged();

                if(BrowsingMode)
                    FindVmMain().BrowsedVar = value;
            }
        }

        #region init

        public VmToolVar()
        {
            Name = "var";

            PropertyView.Tabs.Add(new VmJsonTree());
            PropertyView.Tabs.Add(new VmMetaRaw());
            PropertyView.Tabs.Add(new VmTextRaw(){Name = "Entries", ShowEntries = true});
            PropertyView.Tabs.Add(_propertyTabUserPrefs = new VmTextRaw(){Name = "User Prefs", ShowUserPrefs = true});
            PropertyView.Tabs.Add(new VmDepsTree(){Name = "Dependency Tree", ShowDependencyTree = true});
            PropertyView.Tabs.Add(new VmDepsTree(){Name = "Dependent Items", ShowDependentItems = true});
            PropertyView.Tabs.Add(new VmActions(){OwnerTab = this});
            PropertyView.SelectedTab = PropertyView.Tabs.First();

            foreach (var c in Settings.Config.VarTabs)
            {
                var tab = Folders.HasPreviewImage(c.Type) ? (VmBase)new VmImages() {Type = c.Type} : new VmTexts(){Type = c.Type};
                tab.ShowWithoutPresets = c.WithoutPresets;
                tab.Name = c.Name;
                tab.IsVisible = c.IsEnabled;
                tab.ParentTab = this;
                if(c.Type == FolderType.AddonPackages)
                    _tabVar = tab;

                Tabs.Add(tab);
            }
            SelectedTab = Tabs.First(x=>x.IsVisible);
        }

        #endregion

        public void BrowseVar(VarName name)
        {
            if (_tabVar is VmElements tab)
            {
                foreach (var item in tab.GroupedItems.Cast<VmElementBase>())
                {
                    if (item.Var.Name.FullName == name.FullName)
                    {

                        SelectedTab = _tabVar;
                        UiHelper.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _tabVar.SelectedItem = item;
                            BrowsingMode = true;
                            BrowsedVar = item.Var;
                        }), DispatcherPriority.Loaded);
                        break;
                    }
                }
            }
        }

        public override void OnUpdateTools(ShowTools tools)
        {
            base.OnUpdateTools(tools);
            tools.UpdateAll(false);
            tools.ShowLaFilter = true;
            tools.ShowFavHideFilter = Settings.Config.EnableHideFavTagFeature;
            tools.GenderFilter = true;
            tools.TagFilter = Settings.Config.EnableHideFavTagFeature;
            tools.ShowDates = true;
            tools.ShowSort = true;
            tools.ShowVarCats = true;
            tools.ShowGrouping = true;
            tools.ShowCreatorFilter = true;
            tools.ShowNameFilter = true;
            tools.ShowVersionFilter = true;
            tools.ShowExtras = true;
            tools.Invoke();
        }

        public override void UpdateName()
        {
            Count = Tabs.Where(x=>x.IsVisible).Sum(x => x.Count);
            NameFull = $"{Name} ({Count})";
        }

        protected override void UpdateStatus()
        {
            var status = "";

            _propertyTabUserPrefs.ModeShowInsideVarItems = SelectedTab != _tabVar;

            var vm = SelectedTab?.SelectedItem;
            if (vm != null)
            {
                status += $"{vm.Name} | Path: '{vm.FullName}' | Creator: {vm.Creator} | Var: {vm.Var.Name.Name}.{vm.Var.Name.Version} | Size: {vm.Size/1024.0/1024.0:F2} Mb | Created: {vm.Created.ToShortDateString()} Modified: {vm.Modified.ToShortDateString()}";

                if (vm.IsVarSelf)
                    BrowsedVar = vm.Var;
            }

            StatusLine = status;
            PropertyView.Item = vm;

            UpdateName();
            SelectedTab?.RequestScroll();
        }

        public override IEnumerable<VmCmdBtn> GetCmds()
        {
            yield break;
        }

        public override void OnAddComplete()
        {
            Count = _countItem;

            base.OnAddComplete();
            foreach (var t in Tabs)
                t.OnAddComplete();

            UpdateStatus();
        }

        private int _countItem = 0;
        public override bool OnAdd(FolderType type, ElementInfo el, VarFile var)
        {
            foreach (var t in Tabs)
            {
                if (!t.IsVisible)
                    continue;
                if (t == _tabVar)
                    continue;

                var success = t.OnAdd(type, el, var);
                if(success)
                    _countItem++;
            }
            return _countItem > 0;
        }

        public override void OnAdd(VarFile var)
        {
            _tabVar.OnAdd(var);
        }

        public override void OnReset()
        {
            _countItem = 0;
            Count = 0;
            PropertyView.Item = null;
            foreach (var t in Tabs)
                t.OnReset();

            UpdateStatus();
        }

        public override void OnRemove(IList<VmElementBase> elements)
        {
            foreach (var t in Tabs)
                t.OnRemove(elements);
        }

        public override void OnApplyFilter(FilterMode mode, Predicate<object> f)
        {
            foreach (var t in Tabs)
            {
                if (t == _tabVar)
                    continue;
                t.OnApplyFilter(mode, f);
            }

            UpdateName();
        }

        public override void OnApplyVarFilter(FilterMode mode, Predicate<object> f)
        {
            _tabVar.OnApplyFilter(mode, f);

            UpdateName();
        }

        public override IEnumerable<VmElementBase> GetCheckedElements()
        {
            foreach (var t in Tabs)
            {
                foreach (var item in t.GetCheckedElements())
                    yield return item;
            }
        }

        public override void SetElementsChecked(HashSet<RefItemCfg> refs)
        {
            foreach (var t in Tabs)
                t.SetElementsChecked(refs);
        }

        public override void ReceivedVarCheck(VmBase source, VarFile var, bool isChecked)
        {
            ParentTab?.ReceivedVarCheck(source, var, isChecked);
        }

        public override void ReceivedElementCheck(VmBase source, VmElementBase el, bool isChecked)
        {
            ParentTab?.ReceivedElementCheck(source, el, isChecked);
        }

        public override void UpdateVarChecks(VmBase ignore, VarFile var, bool isChecked)
        {
            base.UpdateVarChecks(ignore, var, isChecked);

            foreach (var t in Tabs)
            {
                t.UpdateVarChecks(ignore, var, isChecked);
            }
        }

        public override void SetChecks(bool onlyVisible, bool isChecked)
        {
            foreach (var t in Tabs)
            {
                t.SetChecks(onlyVisible, isChecked);
            }
        }
    }
}