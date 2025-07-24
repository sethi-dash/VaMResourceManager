using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Vrm.Util;

namespace Vrm.Window
{
    public partial class FavHideTagWindow : System.Windows.Window
    {
        public FavHideTagWindow()
        {
            InitializeComponent();
            DataContext = new FavHideTagWindowVm(this);
        }

        public FavHideTagWindowVm Vm => DataContext as FavHideTagWindowVm;

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
        }
    }

    public class FavHideTagWindowVm : INotifyPropertyChanged
    {
        private readonly System.Windows.Window _window;

        public FavHideTagWindowVm(System.Windows.Window window)
        {
            _window = window;

            OkCommand = new RelayCommand(_ => _window.DialogResult = true, _ => true);
            CancelCommand = new RelayCommand(_ => _window.Close(), _ => true);
        }

        #region Fav

        private int _countFav;
        public int CountFav
        {
            get => _countFav;
            set
            {
                if (value == _countFav) return;
                _countFav = value;
                OnPropertyChanged(nameof(CountFav));
            }
        }

        private bool _changeFav;
        public bool ChangeFav
        {
            get => _changeFav;
            set
            {
                if (value == _changeFav)
                    return;
                _changeFav = value;
                OnPropertyChanged(nameof(ChangeFav));
            }
        }

        private bool _isFav;
        public bool IsFav
        {
            get => _isFav;
            set
            {
                if (value == _isFav)
                    return;
                _isFav = value;
                OnPropertyChanged(nameof(IsFav));
            }
        }

        private bool _canEditFav;
        public bool CanEditFav
        {
            get => _canEditFav;
            set
            {
                if (value == _canEditFav) return;
                _canEditFav = value;
                OnPropertyChanged(nameof(CanEditFav));
            }
        }

        #endregion

        #region Hide

        private int _countHide;
        public int CountHide
        {
            get => _countHide;
            set
            {
                if (value == _countHide) return;
                _countHide = value;
                OnPropertyChanged(nameof(CountHide));
            }
        }

        private bool _changeHide;
        public bool ChangeHide
        {
            get => _changeHide;
            set
            {
                if (value == _changeHide)
                    return;
                _changeHide = value;
                OnPropertyChanged(nameof(ChangeHide));
            }
        }

        private bool _isHide;
        public bool IsHide
        {
            get => _isHide;
            set
            {
                if (value == _isHide)
                    return;
                _isHide = value;
                OnPropertyChanged(nameof(IsHide));
            }
        }

        private bool _canEditHide;
        public bool CanEditHide
        {
            get => _canEditHide;
            set
            {
                if (value == _canEditHide) return;
                _canEditHide = value;
                OnPropertyChanged(nameof(CanEditHide));
            }
        }

        #endregion

        #region Tag

        private int _countTag;
        public int CountTag
        {
            get => _countTag;
            set
            {
                if (value == _countTag) return;
                _countTag = value;
                OnPropertyChanged(nameof(CountTag));
            }
        }


        private bool _changeTag;
        public bool ChangeTag
        {
            get => _changeTag;
            set
            {
                if (value == _changeTag)
                    return;
                _changeTag = value;
                OnPropertyChanged(nameof(ChangeTag));
            }
        }

        private string _tag;
        public string Tag
        {
            get => _tag;
            set
            {
                if (value == _tag)
                    return;
                _tag = value;
                OnPropertyChanged(nameof(Tag));
            }
        }

        private bool _canEditTag;
        public bool CanEditTag
        {
            get => _canEditTag;
            set
            {
                if (value == _canEditTag)
                    return;
                _canEditTag = value;
                OnPropertyChanged(nameof(CanEditTag));
            }
        }

        #endregion

        private string _title = "Fav/Hide/Tag Editor";
        public string Title
        {
            get => _title;
            set
            {
                if (value == _title)
                    return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public void Invalidate()
        {
            CanEditFav = CountFav > 0;
            CanEditHide = CountHide > 0;
            CanEditTag = CountTag > 0;
        }

        public bool HasChanges => IsFav || IsHide || !string.IsNullOrWhiteSpace(Tag);

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}
