using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Vrm.Vm
{
    public class StatRow
    {
        public string Name { get; set; }
        public int Loaded { get; set; }
        public int Archived { get; set; }
        public int Total { get; set; }

        public StatRow(string name, int loaded, int archived, int total)
        {
            Name = name;
            Loaded = loaded;
            Archived = archived;
            Total = total;
        }

        public StatRow()
        {
        }
    }

    public class VmStatTable : INotifyPropertyChanged
    {
        public ObservableCollection<StatRow> Items { get; }

        public void Add(StatRow item)
        {
            Items.Add(item);
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
        }


        public VmStatTable()
        {
            Items = new ObservableCollection<StatRow>();
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
