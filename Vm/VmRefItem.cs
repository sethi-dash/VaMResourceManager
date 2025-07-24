using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Vrm.Cfg;

namespace Vrm.Vm
{
    public class VmRefItem : INotifyPropertyChanged
    {
        private string _name = "reference";
        public string Name
        {
            get => _name;
            set
            {
                if (SetField(ref _name, value))
                {
                    Display = value;
                }
            }
        }

        private string _display;
        public string Display
        {
            get => _display;
            set => SetField(ref _display, value);
        }

        private bool _isEnabled = false; //start/stop reference Items
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        private bool _isChanged;
        public bool IsChanged
        {
            get => _isChanged;
            set => SetField(ref _isChanged, value);
        }

        private int _count;
        public int Count
        {
            get => _count;
            set => SetField(ref _count, value);
        }

        private int _fileCount;
        public int FileCount
        {
            get => _fileCount;
            set => SetField(ref _fileCount, value);
        }

        private double _sizeMb;
        public double SizeMb
        {
            get => _sizeMb;
            set => SetField(ref _sizeMb, value);
        }

        private string _status; //loaded, archived
        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        private bool _isStatusValid; //only for loaded
        public bool IsStatusValid
        {
            get => _isStatusValid;
            set => SetField(ref _isStatusValid, value);
        }

        private VmRefItemOps _ops = VmRefItemOps.All;
        public VmRefItemOps Ops
        {
            get => _ops;
            set => SetField(ref _ops, value);
        }

        private List<RefItemCfg> _items = new List<RefItemCfg>();
        public List<RefItemCfg> Items
        {
            get => _items;
            set => SetField(ref _items, value);
        }

        public RefNamedCfg Item { get; private set; }

        #region init

        private VmRefItem(){}

        public VmRefItem(RefNamedCfg item)
        {
            Item = item;
            Name = item.Name;
            Items = item.Items;

        }

        public static VmRefItem New(string name) => new VmRefItem(){Name = name};

        #endregion


        public void UpdateItemFromView()
        {
            if (Item == null)
                Item = new RefNamedCfg();
            Item.Name = Name;
            Item.Items = Items;
        }

        public void UpdateViewFromItem(RefCfg cfg)
        {
            if (Item == null)
                Item = new RefNamedCfg();
            Item.Name = Name;
            Item.Items = cfg.Items;
            Items.Clear();
            Items = Item.Items;
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

    public enum VmRefItemOps
    {
        None,
        All,
        End
    }
}
