using System;
using System.Collections.Generic;
using System.Linq;
using Vrm.Cfg;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Refs
{
    public class RefItemDepItem
    {
        public HashSet<string> ResourcesRelPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //public HashSet<VarName> VarsReferenceName = new HashSet<VarName>(VarName.Eq); //var name reference. examples: "cr.name.latest", "cr.name.min23", "cr.name.34"
        public Dictionary<VarName, string> VarsName = new Dictionary<VarName, string>(VarName.Eq); //real var instances. example "cr.name.34", "cr.nam1e.334"  value -relative path
        public HashSet<KeyCreatorAndName> VarsKey = new HashSet<KeyCreatorAndName>(); //fast search of dependent items

        public static RefItemDepItem Self(RefItemCfg r, bool includeSelf = true)
        {
            var di = new RefItemDepItem();

            if (r.IsVar) //Nested var dependencies will be found later during tree construction
            {
                if(DepsHelper.TryFindVar(FileHelper.GetOnlyFileNameWithoutExtension(r.Files.First()), out var v))
                    di.AddVar(v.Name, v.RelativePath);
            }
            else //Assume that user resources cannot contain nested dependencies; all user resources are populated here
            {
                //To transfer resource files
                foreach (var f in r.Files)
                    if(includeSelf)
                        di.AddRes(f);

                var relPath = r.Files.First();
                var userItem = Settings.FindByRelPath(relPath);
                if (userItem == null)
                    throw new InvalidOperationException($"Could`t find UserItem for: {relPath}");

                if (userItem.CanContainDependencies)
                {
                    // For presets to be included in scene dependencies, they need to be added to megaref.
                    // It's like presets depend on scenes, although in reality, it's the opposite.
                    foreach (var p in userItem.Presets)
                        di.AddRes(p);

                    //References to other user resources
                    foreach (var res in userItem.DependenciesUserRes)
                    {
                        var otherUserItem = Settings.FindByRelPath(res);
                        if (otherUserItem == null)
                            continue;

                        foreach (var f in otherUserItem.Files)
                            di.AddRes(FileHelper.GetRelativePath(Settings.Config, f.FullName, userItem.IsInArchive));
                    }

                    //References to vars. These will also be processed later in the tree.
                    foreach (var vname in userItem.Dependencies)
                    {
                        var var = Settings.FindByName(vname);
                        di.AddVar(var.Name, var.RelativePath);
                    }
                }
            }
            return di;
        }

        public static RefItemDepItem AllArchive()
        {
            var di = new RefItemDepItem();

            foreach (var item in Settings.ArchiveRd.ResourcesRelPath)
                di.AddRes(item);

            foreach (var kvp in Settings.ArchiveRd.Vars)
            {
                foreach (var item in kvp.Value.Vars)
                    di.AddVar(item.Value.Name, item.Value.RelativePath);
            }

            return di;
        }

        public void AddVar(VarName name, string relPath)
        {
            if(!ContainsVar(name))
                VarsName.Add(name, FileHelper.NormalizePath(relPath));

            VarsKey.Add(new KeyCreatorAndName(name));
        }

        public void RemoveVar(VarName name)
        {
            VarsName.Remove(name);
            VarsKey.Remove(new KeyCreatorAndName(name));
        }

        public bool ContainsVar(VarName name)
        {
            return VarsName.ContainsKey(name);
        }

        public bool ContainsVarKey(VarName name)
        {
            return VarsKey.Contains(new KeyCreatorAndName(name));
        }

        public void AddRes(string relPath)
        {
            ResourcesRelPath.Add(FileHelper.NormalizePath(relPath));
        }
    }
}
