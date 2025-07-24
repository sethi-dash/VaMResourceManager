using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Vrm.Vm
{
    public class VmPropertyTabs : INotifyPropertyChanged
    {
        public ObservableCollection<VmBase> Tabs { get; } = new ObservableCollection<VmBase>();
        public ObservableCollection<VmCmdBtn> TabCmds { get; } = new ObservableCollection<VmCmdBtn>();

        private VmBase _selectedTab;
        public VmBase SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (value != _selectedTab)
                {
                    if (_selectedTab != null)
                    {
                        _selectedTab.OnHide();
                        _selectedTab.IsSelected = false;
                    }
                    _selectedTab = value;
                    if (_selectedTab != null)
                    {
                        _selectedTab.IsSelected = true;
                        _selectedTab.OnShow();
                    }

                    TabCmds.Clear();
                    if(value != null)
                        foreach (var item in value.GetCmds())
                            TabCmds.Add(item);

                    OnPropertyChanged();
                }
            }
        }

        private VmElementBase _item;
        public VmElementBase Item
        {
            get => _item;
            set
            {
                if (Equals(value, _item))
                    return;
                _item = value;
                foreach (var t in Tabs)
                    t.SelectedItem = value;
                OnPropertyChanged();
            }
        }

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
}
