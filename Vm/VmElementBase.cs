using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Vrm.Cfg;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmElementBase : INotifyPropertyChanged
    {
        public VarFile Var {get;set;}        
        public UserItem UserItem { get; set; } 
        public ElementInfo ElementInfo {get;set;}

        public string FullName => ElementInfo.FullName;

        public long Size
        {
            get
            {
                if (Var != null)
                    return Var.Info.Length;
                else
                    return UserItem.Item.Length;
            }
        }

        public DateTime Created
        {
            get
            {
                if (Var != null)
                    return Var.Info.CreationTime;
                else
                    return UserItem.Item.CreationTime;
            }
        }

        public DateTime Modified
        {
            get
            {
                return ElementInfo.LastWriteTime;
            }
        }

        public string Name
        {
            get
            {
                if (IsVar)
                {
                    if(IsVarSelf)
                        return Var.Name.Name;
                    else
                        return ElementInfo.Name;
                }
                else
                    return UserItem.Item.Name;
            }
        }

        public string Package
        {
            get
            {
                if (IsVar)
                {
                    return Var.Name.Name;
                }
                else
                    return UserItem.Item.Name;
            }
        }

        public string Creator
        {
            get
            {
                if (Var != null)
                    return Var.Name.Creator;
                else
                    return "";
            }
        }

        public bool IsInArchive
        {
            get
            {
                if (UserItem != null)
                {
                    return UserItem.IsInArchive;
                }
                else
                {
                    return Var.IsInArchive;
                }
            }
        }

        public bool IsLoaded
        {
            get
            {
                return !IsInArchive;
            }
        }

        public string RelativePath
        {
            get
            {
                if (IsVar)
                {
                    if (IsVarSelf)
                        return Var.RelativePath;
                    else
                    {
                        return ElementInfo.RelativePath;
                    }
                }
                else
                    return UserItem.Info.RelativePath;
            }
        }

        public bool HasType(FolderType singleFlag)
        {
            return ElementInfo.Type.HasFlag(singleFlag);
        }

        public bool HasClothing => HasType(FolderType.Clothing);
        public bool HasHair => HasType(FolderType.Hair);
        public bool HasScene => HasType(FolderType.Scene);
        public bool HasSubscene => HasType(FolderType.SubScene);
        public bool IsSceneOrSubscene => HasScene || HasSubscene;
        public bool IsClothingOrHair => HasClothing || HasHair;
        public bool IsVarSelf => IsVar && ElementInfo == Var.Info;

        protected VmElementBase(ElementInfo ei, VarFile var, UserItem userItem)
        {
            Var = var;
            UserItem = userItem;
            ElementInfo = ei;
        }

        public bool IsVar => Var != null;
        public bool IsUserItem => UserItem != null;
        public DepsProviderBase DepsProvider => IsVar ? (DepsProviderBase)Var : UserItem;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (SetField(ref _isChecked, value))
                {
                    //check/uncheck var affects other tabs via xaml
                }
            }
        }

        public RefItemCfg CreateRef()
        {
            if (Var != null)
                return new RefItemCfg(Var);
            else if (UserItem != null)
                return new RefItemCfg(UserItem);
            return null;
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
