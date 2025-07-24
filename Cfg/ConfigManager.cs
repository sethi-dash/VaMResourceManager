using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vrm.Util;

namespace Vrm.Cfg
{
    public static class ConfigManager
    {
        private static readonly string ConfigFilePath = "settings.json";

        public static Config LoadConfig(ILogger log)
        {
            var cfg = new Config();
            if (FileHelper.FileExists(ConfigFilePath))
            {
                try
                {
                    string json = FileHelper.FileReadAllText(ConfigFilePath);
                    cfg = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
                }
                catch(Exception ex)
                {
                    log.LogEx(ex);
                    cfg = new Config();
                }
            }

            if(!cfg.VarTabs.Any())
                cfg.VarTabs = Config.GetDefaultVarTabs().ToList();
            foreach (var item in Config.GetDefaultVarTabs())
            {
                if (cfg.VarTabs.FirstOrDefault(x => x.Type == item.Type && x.WithoutPresets == item.WithoutPresets) == null)
                    cfg.VarTabs.Add(new CategoryCfg(item.Type){WithoutPresets = item.WithoutPresets});
            }

            if(!cfg.UserDataTabs.Any())
                cfg.UserDataTabs = Config.GetDefaultUserDataTabs().ToList();
            foreach (var item in Config.GetDefaultUserDataTabs())
            {
                if (cfg.UserDataTabs.FirstOrDefault(x => x.Type == item.Type) == null)
                    cfg.UserDataTabs.Add(new CategoryCfg(item.Type));
            }

            return cfg;
        }

        public static void SaveConfig(Config config, ILogger log)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                FileHelper.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                log.LogEx(ex);
            }
        }

        public static void SaveRefs(List<RefNamedCfg> items)
        {
            var dir = Settings.Config.ReferenceFolder;
            FileHelper.CreateDirectoryInNotExists(dir);
            FileHelper.ClearDirectory(dir, new HashSet<string>{Settings.SyncFilePath});
            foreach (var r in items)
            {
                try
                {
                    FileHelper.WriteAllText($"{FileHelper.PathCombine(dir, r.Name)}.json", JsonConvert.SerializeObject(r, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Settings.Logger.LogEx(ex);
                }
            }
        }

        public static List<RefNamedCfg> LoadRefs()
        {
            return FileHelper.LoadItems<RefNamedCfg>(Settings.Config.ReferenceFolder, "*.json", x=>x != Settings.SyncFilePath);
        }

        public static void SaveSyncedRefs(List<string> synced)
        {
            FileHelper.WriteAllText(Settings.SyncFilePath, JsonConvert.SerializeObject(synced));
        }

        public static List<string> LoadSyncedRefs()
        {
            if (!FileHelper.FileExists(Settings.SyncFilePath))
                return new List<string>();
            try
            {
                var txt = FileHelper.FileReadAllText(Settings.SyncFilePath);
                var items = JsonConvert.DeserializeObject<List<string>>(txt) ?? new List<string>();
                return items;
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
