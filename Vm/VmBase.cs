using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Vrm.Cfg;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public abstract class VmBase : INotifyPropertyChanged
    {
        public ObservableCollection<VmBase> Tabs { get; } = new ObservableCollection<VmBase>();
        public ObservableCollection<VmCmdBtn> TabCmds { get; } = new ObservableCollection<VmCmdBtn>();

        private VmBase _selectedTab;
        public VmBase SelectedTab
        {
            get => _selectedTab;
            set
            {
                var prev = _selectedTab;
                if (SetField(ref _selectedTab, value))
                {
                    if (prev != null)
                    {
                        prev.PropertyChanged -= selectedTab_PropertyChanged;
                        prev.OnHide();
                    }
                    if (_selectedTab != null)
                    {
                        _selectedTab.PropertyChanged += selectedTab_PropertyChanged;
                        _selectedTab.OnShow();
                    }

                    TabCmds.Clear();
                    if (value != null)
                    {
                        foreach (var item in value.GetCmds())
                            TabCmds.Add(item);
                    }
                    OnPropertyChanged();

                    UpdateStatus();
                }
            }
        }

        private bool _invokeScroll;
        public bool InvokeScroll
        {
            get => _invokeScroll;
            set
            {
                if (_invokeScroll != value)
                {
                    _invokeScroll = value;
                    OnPropertyChanged();
                }
            }
        }

        public void RequestScroll()
        {
            InvokeScroll = true;
        }

        protected virtual void UpdateStatus() {}

        #region event handlers

        private void selectedTab_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VmBase.SelectedItem) || e.PropertyName == nameof(VmBase.SelectedTab))
            {
                UpdateStatus();
            }
        }

        #endregion


        private string _name = "vm base name";
        public string Name
        {
            get => _name;
            set
            {
                if (SetField(ref _name, value))
                {
                    NameFull = value;
                }
            }
        }

        private string _nameFull = "vm base name full";
        public string NameFull
        {
            get => _nameFull;
            set => SetField(ref _nameFull, value);
        }

        public VmBase ParentTab {get;set;}

        protected VmMain FindVmMain()
        {
            return ParentTab.FindMainVm();
        }

        public virtual IEnumerable<VmCmdBtn> GetCmds()
        {
            yield break;
        }
        public virtual void OnUpdateTools(ShowTools tools){}

        public virtual void OnReset() { }

        public virtual bool OnAdd(FolderType type, ElementInfo el, VarFile var) { return false; }
        public virtual void OnAdd(UserItem item) { }
        public virtual void OnAdd(VarFile var) { }
        public virtual void OnAddComplete() { }
        public virtual void OnRemove(IList<VmElementBase> elements){}

        public virtual void OnShow(){}
        public virtual void OnHide(){}

        private VmElementBase _selectedItem;
        public VmElementBase SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();

                    OnSelectedItemChanged();
                }
            }
        }

        private FolderType _type;
        public FolderType Type
        {
            get => _type;
            set
            {
                if (Equals(value, _type))
                    return;
                _type = value;
                OnPropertyChanged();

                UpdateName();
            }
        }

        public bool ShowWithoutPresets = false;

        public virtual void UpdateName(){}

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        private int _count;
        public int Count
        {
            get => _count;
            set
            {
                if (SetField(ref _count, value))
                {

                }
                Opacity = value > 0 ? 1 : 0.15;
            }
        }

        private double _opacity = 1;
        public double Opacity
        {
            get => _opacity;
            set => SetField(ref _opacity, value);
        }

        protected virtual void OnSelectedItemChanged(){}

        public virtual void OnApplyFilter(FilterMode mode, Predicate<object> f){}
        public virtual void OnApplyVarFilter(FilterMode mode, Predicate<object> f){}

        public ILogger Logger{get;set;}

        private string _statusLine;
        public string StatusLine
        {
            get => _statusLine;
            set => SetField(ref _statusLine, value);
        }

        public virtual IEnumerable<VmElementBase> GetCheckedElements()
        {
            yield break;
        }

        public virtual void SetElementsChecked(HashSet<RefItemCfg> refs){}
        public virtual void UpdateVarChecks(VmBase ignore, VarFile var, bool isChecked){}
        public virtual void ReceivedVarCheck(VmBase source, VarFile var, bool isChecked){}
        public virtual void ReceivedElementCheck(VmBase source, VmElementBase el, bool isChecked){}
        public virtual void SetChecks(bool onlyVisible, bool isChecked){}

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    public static class VmBaseExt
    {
        public static T FindVm<T>(this VmBase tab)
        {
            if (tab == null)
                return default;
            if(tab is T m)
                return m;
            return FindVm<T>(tab.ParentTab);
        }

        public static VmMain FindMainVm(this VmBase tab)
        {
            return FindVm<VmMain>(tab);
        }
    }
}
