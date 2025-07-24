using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmFileInfos : VmBase
    {
        private List<System.IO.FileInfo> _items;
        public List<System.IO.FileInfo> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items))
                    return;
                _items = value;
                OnPropertyChanged();
            }
        }

        private System.IO.FileInfo _selectedFile;
        public System.IO.FileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (value == _selectedFile)
                    return;
                _selectedFile = value;
                OnPropertyChanged();
            }
        }

        private bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetField(ref _isEmpty, value);
        }


        public ICommand CmdLocate {get;}
        public ICommand CmdOpen {get;}


        private void Update()
        {
            Items = null;
            if (SelectedItem != null && SelectedItem.IsUserItem)
            {
                var items = SelectedItem.UserItem.Files;
                if (items.Count > 0)
                    Items = items;
                SelectedFile = Items?.FirstOrDefault();
            }

            IsEmpty = Items == null || !Items.Any();
        }

        public override void OnShow()
        {
            base.OnShow();

            Update();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        protected override void OnSelectedItemChanged()
        {
            Update();
        }

        public VmFileInfos()
        {
            Name = "Files";

            CmdLocate = new RelayCommand(x =>
            {
                if(x is System.IO.FileInfo fi)
                    FileHelper.ShowInExplorer(fi.FullName);

            });
            CmdOpen = new RelayCommand(x =>
            {
                if(x is System.IO.FileInfo fi)
                    FileHelper.Run(fi.FullName);
            });
        }
    }
}
