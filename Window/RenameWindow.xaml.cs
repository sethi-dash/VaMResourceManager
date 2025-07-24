using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Vrm.Util;

namespace Vrm.Window
{
    public partial class RenameWindow : System.Windows.Window
    {
        public RenameWindow(string currentName, IEnumerable<string> existingNames)
        {
            InitializeComponent();
            DataContext = new RenameVm(currentName, existingNames, this);
        }

        public string NewName => ((RenameVm)DataContext).NewName;

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }
    }

    public class RenameVm : INotifyPropertyChanged
    {
        private readonly System.Windows.Window _window;
        private readonly HashSet<string> _existingNames;

        public RenameVm(string currentName, IEnumerable<string> existingNames, System.Windows.Window window)
        {
            _window = window;
            _existingNames = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
            NewName = currentName;
            Title = $"Rename '{currentName}'";

            OkCommand = new RelayCommand(_ => _window.DialogResult = true, _ => IsValid);
            CancelCommand = new RelayCommand(_ => _window.Close(), _ => true);
        }

        private string _title = "Rename";
        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        private string _newName;
        public string NewName
        {
            get => _newName;
            set
            {
                if (_newName != value)
                {
                    _newName = value;
                    OnPropertyChanged(nameof(NewName));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(NewName) &&
            Regex.IsMatch(NewName, @"^[\p{L}\p{Nd} _.]+$") &&
            !_existingNames.Contains(NewName.Trim());

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
