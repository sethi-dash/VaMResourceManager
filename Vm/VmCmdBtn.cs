using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Vrm.Vm
{
    public class VmCmdBtn : INotifyPropertyChanged
    {
        private string _caption;
        public string Caption
        {
            get => _caption;
            set => SetField(ref _caption, value);
        }

        private string _tooltip;
        public string Tooltip
        {
            get => _tooltip;
            set => SetField(ref _tooltip, value);
        }


        private ICommand _command;
        public ICommand Command
        {
            get => _command;
            set => SetField(ref _command, value);
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
        }

        #region init

        public VmCmdBtn()
        {
        }

        public VmCmdBtn(string caption, ICommand command)
        {
            Caption = caption;
            Command = command;
        }

        public VmCmdBtn(ICommand command, string caption) : this(caption, command)
        {
        }

        #endregion

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
