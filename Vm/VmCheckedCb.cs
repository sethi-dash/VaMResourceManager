using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Vrm.Vm
{
    public class VmCheckedCb : INotifyPropertyChanged
    {
        public ObservableCollection<CbOption> Items { get; set; } = new ObservableCollection<CbOption>();
        public ObservableCollection<CbOption> Items2 { get; } = new ObservableCollection<CbOption>();

        private bool _show;
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show)
                    return;
                _show = value;
                OnPropertyChanged();

                OnShowHide?.Invoke(value);
            }
        }

        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                if (value == _isPopupOpen) 
                    return;
                _isPopupOpen = value;
                OnPropertyChanged();
            }
        }

        public Action<bool> OnShowHide;


        public VmCheckedCb()
        {
            Items.CollectionChanged += OnItemsCollectionChanged;
            Items2.CollectionChanged += Items2_CollectionChanged;
            foreach (var item in Items)
                SubscribeToItem(item);
        }

        private void Items2_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //Debug.WriteLine("---" + DateTime.Now.ToString("o"));

            //if(e.OldItems != null)
            //    Debug.WriteLine("removed: " + (e.OldItems[0] as CbOption).Name);

            //if (e.NewItems != null)
            //{
            //    Debug.WriteLine("added: " + (e.NewItems[0] as CbOption).Name);
            //}

            //Debug.WriteLine("count: " + Items2.Count);
        }

        public Action<CbOption> OnChanged;
        public Action<CbOption> OnToggled;

        private void ProcessCheck(CbOption toggled, CbOption changed)
        {
            if (StaticItems)
            {
                OnPropertyChanged(nameof(Status));
                UpdateIsFilterEnabled();
                return;
            }
                

            CbOption added = null;
            CbOption removed = null;
            if (toggled != null)
            {
                removed = CbOption.Copy(toggled);
                removed.Asc = !removed.Asc;
                added = CbOption.Copy(toggled);
            }
            else if(changed.IsChecked)
                added = changed;
            else
                removed = changed;

            if (removed != null)
            {
                CbOption toDel = null;
                foreach (var item in Items2)
                {
                    if (item.GroupName == removed.GroupName && item.Name == removed.Name && item.Asc == removed.Asc)
                    {
                        toDel = item;
                        break;
                    }
                }
                if(toDel != null)
                    Items2.Remove(toDel);
            }

            if (added != null)
            {
                bool add = true;
                foreach (var item in Items2)
                {
                    if (item.GroupName == added.GroupName && item.Name == added.Name && item.Asc == added.Asc)
                    {
                        add = false;
                        break;
                    }
                }
                if(add)
                    Items2.Add(added);
            }

            OnPropertyChanged(nameof(Status));
            UpdateIsFilterEnabled();
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CbOption item in e.NewItems)
                    SubscribeToItem(item);
            }

            if (e.OldItems != null)
            {
                foreach (CbOption item in e.OldItems)
                    UnsubscribeFromItem(item);
            }
        }

        private void SubscribeToItem(CbOption item)
        {
            item.PropertyChanged += OnItemPropertyChanged;
            item.Toggled = x =>
            {
                ProcessCheck(x, null);
                OnToggled?.Invoke(x);
            };
            item.CheckedChanged = x =>
            {
                ProcessCheck(null, x);
                OnChanged?.Invoke(x);
            };
        }

        private void UnsubscribeFromItem(CbOption item)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            item.Toggled = null;
            item.CheckedChanged = null;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is CbOption changedItem && e.PropertyName == nameof(CbOption.IsChecked))
            {
                var group = FindOther(changedItem).Where(x=>x.IsChecked).ToList();
                if (!group.Any())
                {
                    changedItem.CheckedChanged?.Invoke(changedItem);
                }
                else
                {
                    foreach(var item in group)
                    {
                        item.PropertyChanged -= OnItemPropertyChanged;
                        item.IsChecked = false;
                        item.PropertyChanged += OnItemPropertyChanged;
                    }
                    changedItem.Toggled?.Invoke(changedItem);
                }
            }
        }

        public bool GetValue(string propertyName)
        {
            foreach (var item in Items)
            {
                if (item.Name == propertyName)
                    return item.IsChecked;
            }

            return false;
        }

        public void SetValue(string propertyName, bool value)
        {
            foreach (var item in Items)
            {
                if (item.Name == propertyName)
                    item.IsChecked = value;
            }
        }

        private Brush _textBrush = Brushes.Black;
        public Brush TextBrush
        {
            get => _textBrush;
            set
            {
                if (Equals(value, _textBrush))
                    return;
                _textBrush = value;
                OnPropertyChanged();
            }
        }

        public bool IsFilterCanBeEnabledOrDisabled { get; set; } = true;

        private bool _isFilterEnabled = true;
        public bool IsFilterEnabled
        {
            get => _isFilterEnabled;
            set
            {
                if(!IsFilterCanBeEnabledOrDisabled)
                    return;

                if (value == _isFilterEnabled)
                    return;
                _isFilterEnabled = value;
                OnPropertyChanged();

                if (!value)
                    TextBrush = Brushes.Gray;
                else
                    TextBrush = Brushes.Black;
            }
        }


        public void UpdateIsFilterEnabled()
        {
            var res = IsAllItemsChecked(Items) || NoItemsChecked(Items);
            IsFilterEnabled = !res;
        }

        private static bool IsAllItemsChecked(IList<CbOption> items)
        {
            var items2 = items.Where(x => x.IsChecked).ToList();
            return items.Count == items2.Count;
        }

        private bool NoItemsChecked(IList<CbOption> items)
        {
            return !items.Any(x => x.IsChecked);
        }

        public bool AllIsNothing;

        public string Status
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(StaticText))
                    return StaticText;

                var elements = StaticItems ? Items : Items2;
                var items = elements.Where(x => x.IsChecked).ToList();
                if (items.Count > 0)
                {
                    if (AllIsNothing && items.Count == Items.Count)
                        return NothingCheckedText;
                    string result = ByText + string.Join(", ", items.Select(x=>x.Display));
                    if(!StaticItems)
                        result += $" ({Items2.Count})";
                    return result;

                }
                else
                    return NothingCheckedText;
            }
        }

        public bool StaticItems { get; set; } = true;
        private string _staticText;
        public string StaticText
        {
            get => _staticText;
            set
            {
                if (value == _staticText) 
                    return;
                _staticText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Status));
            }
        }

        public string ByText { get; set; } = "Grouped by ";
        public string NothingCheckedText { get; set; } = "Group";
        public string FooterText {get;set;}


        private IEnumerable<CbOption> FindOther(CbOption selected)
        {
            var sameGroup = Items.Where(x => x != selected && x.GroupName != null && x.GroupName == selected.GroupName);
            foreach (var item in sameGroup)
            {
                yield return item;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class CbOption : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private bool? _asc;
        public bool? Asc
        {
            get => _asc;
            set
            {
                if (value == _asc) 
                    return;
                _asc = value;
                OnPropertyChanged();

                UpdateDisplay();
            }
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _groupName;
        public string GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChanged(nameof(GroupName));
                }
            }
        }

        private VmCmdBtn _action;
        public VmCmdBtn Action
        {
            get => _action;
            set
            {
                if (Equals(value, _action))
                    return;
                _action = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public Action<CbOption> CheckedChanged;

        [JsonIgnore]
        public Action<CbOption> Toggled;

        private string _display;
        [JsonIgnore]
        public string Display
        {
            get => _display;
            set
            {
                if (value == _display) 
                    return;
                _display = value;
                OnPropertyChanged();
            }
        }

        public object Tag {get;set;}

        public CbOption(string name, bool? asc, bool isChecked, string group)
        {
            Name = name;
            Asc = asc;
            IsChecked = isChecked;

            if (group != null)
            {
                GroupName = group;
            }

            UpdateDisplay();
        }

        public static CbOption Copy(CbOption source)
        {
            return new CbOption(source.Name, source.Asc, source.IsChecked, source.GroupName);
        }

        private void UpdateDisplay()
        {
            if (Asc.HasValue)
            {
                if (Asc.Value)
                    Display = $"{Name} ▲";
                else
                    Display = $"{Name} ▼";
            }
            else
            {
                Display = Name;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
