using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmTextRaw : VmBase
    {
        private IEnumerable<string> _lines;
        public IEnumerable<string> Lines
        {
            get => _lines;
            set => SetField(ref _lines, value);
        }

        public bool ShowEntries;
        public bool ShowUserPrefs;
        public bool ModeShowInsideVarItems;

        public ICommand CmdOpen {get;}

        public VmTextRaw()
        {
            Name = "Raw";
            CmdOpen = new RelayCommand(x =>
            {
                var f = FileHelper.CreateTempFile(Lines);
                FileHelper.Run(f);
            });
        }

        private string GetPrefsString(ElementInfo ei)
        {
            var str = ei.UserPrefs;
            if(ei.IsHide)
                str += Environment.NewLine + @"Hidden";
            if(ei.IsFav)
                str += Environment.NewLine + @"Favorited";

            if (string.IsNullOrWhiteSpace(str))
            {
                return ".prefs/.hide/.fav file not found";
            }
            else 
                return str;
        }

        private async void Update()
        {
            if (SelectedItem != null && SelectedItem.IsVar)
            {
                var var = SelectedItem.Var;
                if (ShowEntries)
                {
                    Lines = new List<string> { "Calculating..." };
                    Lines = await Task.Run(() =>
                    {
                        var nodes = new List<string>();
                        foreach (var e in var.Entries)
                            nodes.Add(e);
                        return nodes;
                    });
                }

                if (ShowUserPrefs)
                {
                    if (!Settings.Config.EnableHideFavTagFeature)
                    {
                        Lines = new List<string> { "No information to display.\nActivate 'Enables filtering of items by Hide, Fav, and Tags'" };
                    }
                    else
                    {
                        if (ModeShowInsideVarItems)
                            Lines = new List<string> { GetPrefsString(SelectedItem.ElementInfo) };
                        else
                            Lines = new List<string> { GetPrefsString(var.Info) };
                    }
                }
            }
            else if (SelectedItem != null && SelectedItem.IsUserItem)
            {
                var item = SelectedItem.UserItem;
                if (ShowUserPrefs)
                {
                    if (!Settings.Config.EnableHideFavTagFeature)
                    {
                        Lines = new List<string> { "No information to display.\nActivate 'Enables filtering of items by Hide, Fav, and Tags'" };
                    }
                    else
                        Lines = new List<string> { GetPrefsString(item.Info) };
                }
            }
            else
            {
                Lines = new []{ $"No item selected"};
            }
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
    }
}
