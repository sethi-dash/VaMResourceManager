using System;
using System.Collections.Generic;
using System.Linq;
using Vrm.Cfg;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmToolUserData : VmBase
    {
        public VmPropertyTabs PropertyView { get; } = new VmPropertyTabs();

        #region init

        public VmToolUserData()
        {
            Name = "user data";

            foreach (var c in Settings.Config.UserDataTabs)
            {
                var tab = Folders.HasPreviewImage(c.Type) ? (VmBase)new VmImages() {Type = c.Type} : new VmTexts(){Type = c.Type};
                tab.Name = c.Name;
                tab.ParentTab = this;
                tab.ShowWithoutPresets = c.WithoutPresets;
                tab.IsVisible = c.IsEnabled;
                Tabs.Add(tab);
            }
            SelectedTab = Tabs.First(x=>x.IsVisible);

            PropertyView.Tabs.Add(new VmFileInfos());
            PropertyView.Tabs.Add(new VmTextRaw(){Name = "User Prefs", ShowUserPrefs = true});
            PropertyView.Tabs.Add(new VmDepsTree(){Name = "Dependency Tree", ShowDependencyTree = true});
            PropertyView.Tabs.Add(new VmDepsTree(){Name = "Dependent Items", ShowDependentItems = true});
            PropertyView.Tabs.Add(new VmActions(){OwnerTab = this});
            PropertyView.SelectedTab = PropertyView.Tabs.First();
        }

        #endregion

        public override void UpdateName()
        {
            Count = Tabs.Where(x=>x.IsVisible).Sum(x => x.Count);
            NameFull = $"{Name} ({Count})";
        }

        protected override void UpdateStatus()
        {
            var status = "";

            var vm = SelectedTab?.SelectedItem;
            if (vm != null)
            {
                status += $"{vm.Name} | Path: '{vm.FullName}' | Size: {vm.Size/1024.0/1024.0:F2} Mb | Created: {vm.Created.ToShortDateString()} Modified: {vm.Modified.ToShortDateString()}";
            }
            StatusLine = status;
            try
            {
                PropertyView.Item = vm;
            }
            catch{/**/}

            UpdateName();
        }

        public override IEnumerable<VmCmdBtn> GetCmds()
        {
            yield break;
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
            tools.ShowNameFilter = true;
            tools.ShowUserDataCats = true;
            tools.ShowExtras = true;
            tools.Invoke();
        }

        public override void OnApplyFilter(FilterMode mode, Predicate<object> f)
        {
            base.OnApplyFilter(mode, f);
            foreach (var t in Tabs)
                t.OnApplyFilter(mode, f);

            UpdateStatus();
        }

        public override IEnumerable<VmElementBase> GetCheckedElements()
        {
            foreach (var t in Tabs)
            foreach(var item in t.GetCheckedElements())
                yield return item;
        }

        public override void ReceivedElementCheck(VmBase source, VmElementBase el, bool isChecked)
        {
            ParentTab?.ReceivedElementCheck(source, el, isChecked);
        }

        public override void SetElementsChecked(HashSet<RefItemCfg> refs)
        {
            foreach (var t in Tabs)
                t.SetElementsChecked(refs);
        }

        public override void OnReset()
        {
            Count = 0;
            PropertyView.Item = null;
            foreach (var t in Tabs)
                t.OnReset();

            UpdateStatus();
        }

        public override void OnRemove(IList<VmElementBase> elements)
        {
            base.OnRemove(elements);
            foreach (var t in Tabs)
                t.OnRemove(elements);
        }

        public override void OnAdd(UserItem item)
        {
            foreach (var t in Tabs)
            {
                if (!t.IsVisible)
                    continue;
                t.OnAdd(item);
            }
        }

        public override void OnAddComplete()
        {
            foreach (var t in Tabs)
                t.OnAddComplete();

            UpdateStatus();
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