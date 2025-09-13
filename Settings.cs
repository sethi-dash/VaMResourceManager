using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vrm.Cfg;
using Vrm.Refs;
using Vrm.Util;
using Vrm.Vam;
using Vrm.Vm;

namespace Vrm
{
    public static class Settings
    {
        public static Version Version { get; } = new Version(1, 0, 1); //window title
        public static Config Config { get; set; } = new Config();
        public static VmToolLogger Logger {get;} = new VmToolLogger();
        public static string ExePath => AppDomain.CurrentDomain.BaseDirectory;
        public static string NoImagePath = "no_image.jpg";
        public static string QrBtc = "bc1q6rynxs54wqyee6fe3wds9z6gn2nhl9ej40w84w.jpg";
        public static string QrEth = "0x7ce9c1Da02A5621b74327a01Ae4Ac6FE5Af73E4F.jpg";
        public static string AppName = "VaM Resource Manager";
        public static string SyncFilePath => FileHelper.PathCombine(Config.ReferenceFolder, "sync.json");
        public static Dictionary<FolderType, string> Archive;
        public static Dictionary<FolderType, string> Loaded;

        public static Stopwatch Sw = new Stopwatch();
        public static void PrintElapsed(string mark)
        {
            Debug.WriteLine($"{mark}: {Sw.ElapsedMilliseconds}");
        }

        static Settings()
        {
            //Sw.Start();
        }

        public static void RefreshPath()
        {
            Archive = new Dictionary<FolderType, string>();
            Loaded = new Dictionary<FolderType, string>();
            foreach (FolderType type in Enum.GetValues(typeof(FolderType)))
            {
                if (type == FolderType.Unset)
                    continue;
                if (type == FolderType.UserFolders)
                    continue;
                Archive.Add(type, FileHelper.NormalizePath(FileHelper.PathCombine(Config.VamArchivePath, Folders.Type2RelPath[type])));
                Loaded.Add(type, FileHelper.NormalizePath(FileHelper.PathCombine(Config.VamPath, Folders.Type2RelPath[type])));
            }
        }

        public static void Save()
        {
            ConfigManager.SaveConfig(Settings.Config, Logger);
        }

        public static void Load()
        {
            Settings.Config = ConfigManager.LoadConfig(Logger);
            RefreshPath();
        }

        #region refs

        public static ResourceDict ArchiveRd;
        public static ResourceDict LoadedRd;

        public static UserItem FindByRelPath(string relPath)
        {
            var ui = ArchiveRd.FindByRelPath(relPath) ?? LoadedRd.FindByRelPath(relPath);
            return ui;
        }

        public static VarFile FindByName(VarName n)
        {
            var missing = new VarFile() { Name = n, IsMissing = true };
            Dictionary<int, VarFile> versions = null;

            var key = new KeyCreatorAndName(n);
            if (ArchiveRd.Vars.ContainsKey(key))
                versions = new Dictionary<int, VarFile>(ArchiveRd.Vars[key].Vars);
            if (LoadedRd.Vars.ContainsKey(key))
            {
                if(versions == null)
                    versions = new Dictionary<int, VarFile>(LoadedRd.Vars[key].Vars);
                else
                {
                    foreach(var kvp in LoadedRd.Vars[key].Vars)
                        if (!versions.ContainsKey(kvp.Key))
                            versions.Add(kvp.Key, kvp.Value);
                }
            }

            if (versions == null)
                return missing;
            else
            {
                if (Config.UseMaxAvailableVarVersion)
                {
                    var maxVersion = versions.Max(x => x.Key);
                    if(versions.ContainsKey(maxVersion))
                        return versions[maxVersion];
                }
                else
                {
                    if (versions.ContainsKey(n.Version))
                        return versions[n.Version];
                    else if(n.IsLatest || Config.UseMaxAvailableVarVersionOnMissingVersion)
                    {
                        var maxVersion = versions.Max(x => x.Key);
                        if(maxVersion > n.Version)
                            return versions[maxVersion];
                    }
                }
            }
            return missing;
        }

        #endregion
    }
}