using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Vrm.Control
{
    public partial class UserPrefsEditor : UserControl, INotifyPropertyChanged
    {
        public UserPrefsEditor()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private bool _isOkEnabled = true;
        public bool IsOkEnabled
        {
            get => _isOkEnabled;
            set
            {
                if (value == _isOkEnabled)
                    return;
                _isOkEnabled = value;
                OnPropertyChanged(nameof(IsOkEnabled));
            }
        }

        public Action<UserPrefsEditor> OnOk {get;set;}
        public string Header { get; set; }
        public UserPrefsVm ViewModel { get; } = new UserPrefsVm(){ IgnoreMissingDependencyErrors = false, PluginsAlwaysDisabled = false, PluginsAlwaysEnabled = true};

        public string Footer { get; set; }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OnOk?.Invoke(this);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        #endregion
    }

    public class UserPrefsVm : INotifyPropertyChanged
    {
        private bool _pluginsAlwaysEnabled;
        public bool PluginsAlwaysEnabled
        {
            get => _pluginsAlwaysEnabled;
            set
            {
                if (_pluginsAlwaysEnabled != value)
                {
                    _pluginsAlwaysEnabled = value;
                    OnPropertyChanged(nameof(PluginsAlwaysEnabled));

                    if (value && PluginsAlwaysDisabled)
                        PluginsAlwaysDisabled = false;
                }
            }
        }

        private bool _pluginsAlwaysDisabled;
        public bool PluginsAlwaysDisabled
        {
            get => _pluginsAlwaysDisabled;
            set
            {
                if (_pluginsAlwaysDisabled != value)
                {
                    _pluginsAlwaysDisabled = value;
                    OnPropertyChanged(nameof(PluginsAlwaysDisabled));

                    if (value && PluginsAlwaysEnabled)
                        PluginsAlwaysEnabled = false;
                }
            }
        }

        private bool _ignoreMissingDependencyErrors;
        public bool IgnoreMissingDependencyErrors
        {
            get => _ignoreMissingDependencyErrors;
            set
            {
                if (_ignoreMissingDependencyErrors != value)
                {
                    _ignoreMissingDependencyErrors = value;
                    OnPropertyChanged(nameof(IgnoreMissingDependencyErrors));
                }
            }
        }

        private bool _preloadMorphs;
        public bool PreloadMorphs
        {
            get => _preloadMorphs;
            set
            {
                if (value == _preloadMorphs) return;
                _preloadMorphs = value;
                OnPropertyChanged(nameof(PreloadMorphs));
            }
        }

        private bool _processOnlyLatestVars = false;
        public bool ProcessOnlyLatestVars
        {
            get => _processOnlyLatestVars;
            set
            {
                if (value == _processOnlyLatestVars)
                    return;
                _processOnlyLatestVars = value;
                OnPropertyChanged(nameof(ProcessOnlyLatestVars));
            }
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        #endregion
    }
}