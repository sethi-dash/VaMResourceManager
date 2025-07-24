using System;
using System.Collections.Generic;
using System.Linq;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Refs
{
    public class ResourceDictItem
    {
        public Dictionary<int, VarFile> Vars = new Dictionary<int, VarFile>();

        public void Add(VarFile v)
        {
            if (!Vars.ContainsKey(v.Name.Version))
                Vars.Add(v.Name.Version, v);
        }

        public VarFile GetMaxVersion()
        {
            var maxVersion = Vars.Max(x => x.Key); 
            return Vars[maxVersion];
        }

        public List<VarFile> GetOldVersions()
        {
            if (Vars.Count <= 1)
                return new List<VarFile>();

            return Vars
                .OrderBy(kvp => kvp.Key)
                .Take(Vars.Count - 1)
                .Select(kvp => kvp.Value)
                .ToList();
        }

        public bool TryFind(VarName name, out KeyValuePair<int,VarFile> res)
        {
            res = new KeyValuePair<int, VarFile>();
            foreach (var kvp in Vars)
            {
                if (VarName.Eq.Equals(kvp.Value.Name, name))
                {
                    res = kvp;
                    return true;
                }
            }
            return false;
        }

    }

    public class ResourceDict
    {
        public DateTime StartModified = DateTime.MaxValue;
        public DateTime EndModified = DateTime.MinValue;
        public DateTime StartCreated = DateTime.MaxValue;
        public DateTime EndCreated = DateTime.MinValue;

        public HashSet<string> ResourcesRelPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<UserItem> UserResources = new HashSet<UserItem>();
        public Dictionary<string, UserItem> RelName2UserItem = new Dictionary<string, UserItem>(UserItem.RelPathComparer);

        public Dictionary<KeyCreatorAndName, ResourceDictItem> Vars = new Dictionary<KeyCreatorAndName, ResourceDictItem>();

        public void AddRes(UserItem ui)
        {
            UserResources.Add(ui);
            if (!RelName2UserItem.TryGetValue(ui.Info.RelativePath, out _))
            {
                RelName2UserItem.Add(ui.Info.RelativePath, ui);
            }

            var modified = ui.Info.LastWriteTime;
            if (modified < StartModified)
                StartModified = modified;
            if (modified > EndModified)
                EndModified = modified;

            var created = ui.Item.CreationTime;
            if (created < StartCreated)
                StartCreated = created;
            if (created > EndCreated)
                EndCreated = created;
        }

        public void AddRes(string relPath)
        {
            ResourcesRelPath.Add(FileHelper.NormalizePath(relPath));
        }

        public void AddVar(VarFile v)
        {
            var key = new KeyCreatorAndName(v.Name);
            if (!Vars.TryGetValue(key, out var item))
            {
                item = new ResourceDictItem();
                Vars.Add(key, item);
            }
            item.Add(v);

            if (v.ModifiedMin < StartModified)
                StartModified = v.ModifiedMin;
            if (v.ModifiedMax > EndModified)
                EndModified = v.ModifiedMax;

            var created = v.Info.CreationTime;
            if (created < StartCreated)
                StartCreated = created;
            if (created > EndCreated)
                EndCreated = created;
        }

        public UserItem FindByRelPath(string relPath)
        {
            if (RelName2UserItem.TryGetValue(relPath, out var value))
                return value;

            return null;
        }

        public bool TryGetMaxVersion(KeyCreatorAndName key, out VarFile vf)
        {
            vf = null;
            if (Vars.ContainsKey(key))
            {
                vf = Vars[key].GetMaxVersion();
                return true;
            }

            return false;
        }

        public void Clear()
        {
            UserResources.Clear();
            ResourcesRelPath.Clear();
            RelName2UserItem.Clear();
            Vars.Clear();
        }

        public void Delete(KeyCreatorAndName key, int ver)
        {
            if (Vars.ContainsKey(key))
            {
                int version = -1;
                var vars = Vars[key];
                foreach (var v in vars.Vars)
                {
                    if (v.Key == ver)
                    {
                        version = v.Key;
                        break;
                    }
                }

                if (version != -1)
                    vars.Vars.Remove(version);

                if (vars.Vars.Count == 0)
                    Vars.Remove(key);
            }
        }
    }
}
