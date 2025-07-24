using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Vrm.Util;
using Vrm.Window;

namespace Vrm.Vm
{
    public class VmUserFolders : VmBase
    {
        private string _text;
        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        public ICommand CmdSave { get; }

        public VmUserFolders()
        {
            CmdSave = new RelayCommand(x =>
            {
                var items = GetValidDirectoryPaths(Text);
                if (items.Any())
                {
                    TextBoxDialog.ShowDialog("Valid paths found", "These folder paths will be included in synchronization between Loaded and Archived on 'user data' tab:", items);
                }
                else
                {
                    TextBoxDialog.ShowDialog("No valid paths found", "User folders will not be considered.");
                }

                Settings.Config.UserFolders = items.ToHashSet();
            });
        }

        private static List<string> GetValidDirectoryPaths(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            // Splitting by spaces, commas, line breaks, tabs, semicolons
            var parts = Regex.Split(input, @"[\s,;]+");

            var validPaths = new List<string>();

            foreach (var raw in parts)
            {
                var trimmed = raw.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                try
                {
                    var dir = FileHelper.GetFullPath(trimmed, false, Settings.Config, true);
                    if (FileHelper.DirectoryExists(dir))
                        validPaths.Add(trimmed);

                }
                catch
                {
                    //The path was invalid (e.g., too long, incorrect format) — skipping it
                }
            }

            return validPaths;
        }
    }
}