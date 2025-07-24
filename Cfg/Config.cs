using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Cfg
{
    public class Config
    {
        public int Version { get; set; } = 1;
        public string VamPath { get; set; } = "undefined";
        public string VamArchivePath {get;set;} = "undefined";
        public string ReferenceFolder {get;set;} = "references";
        public string CachePath {get;set;} = "cache";
        public string ShortcutVam {get;set;} = "";
        public bool RunVamViaShortcut {get;set;}
        public bool IsWindowTopmost {get;set;}
        public bool AutoScan { get; set; } = false;
        public bool UseMaxAvailableVarVersion { get; set; } = false;
        public bool CleanMega { get; set; } = false;
        public bool UseMaxAvailableVarVersionOnMissingVersion { get; set; } = true;
        public bool EnableHideFavTagFeature { get; set; } = true;
        public double ImageSize { get; set; } = 100.0;
        public HashSet<string> UserFolders { get; set; } = new HashSet<string>();
        public List<CategoryCfg> VarTabs { get; set; } = new List<CategoryCfg>();
        public List<CategoryCfg> UserDataTabs { get; set; } = new List<CategoryCfg>();

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<SortItemCfg> Sort { get; set; } = new List<SortItemCfg>(){ new SortItemCfg(Sorter.Modified, false)};

        public static IEnumerable<CategoryCfg> GetDefaultVarTabs()
        {
            foreach (FolderType item in Enum.GetValues(typeof(FolderType)))
            {
                if (item == FolderType.Unset)
                    continue;
                if (item == FolderType.UIAssistByJJW)
                    continue;
                if (item == FolderType.UserFolders)
                    continue;
                yield return new CategoryCfg(item);
            }
            yield return new CategoryCfg(FolderType.Clothing) { WithoutPresets = true };
            yield return new CategoryCfg(FolderType.Hair) { WithoutPresets = true };
        }

        public static IEnumerable<CategoryCfg> GetDefaultUserDataTabs()
        {
            foreach (FolderType item in Enum.GetValues(typeof(FolderType)))
            {
                if (item == FolderType.Unset)
                    continue;
                if (item == FolderType.AddonPackages)
                    continue;
                yield return new CategoryCfg(item);
            }
        }
    }
}