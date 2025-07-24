using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Vrm.Cfg;
using Vrm.Util;
using Vrm.Vam;
using Vrm.Vm;

namespace Vrm.Refs
{
    public static class DepsHelper
    {
        public static bool TryFindVar(string varName, out VarFile var)
        {
            var = null;
            VarName name;
            try
            {
                name = VarName.Parse(varName);
            }
            catch
            {
                return false;
            }
            return TryFindVar(name, out var);
        }

        public static bool TryFindVar(VarName name, out VarFile var)
        {
            var = null;
            try
            {
                var = Settings.FindByName(name);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<Node<VarName>> BuildVarDepsTree(IEnumerable<VarName> items)
        {
            return Node<VarName>.BuildVarTree(items, Settings.FindByName);
        }

        public static IEnumerable<Node<T>> Walk<T>(List<Node<T>> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                if (node.Children != null && node.Children.Count > 0)
                {
                    foreach (var child in Walk(node.Children))
                        yield return child;
                }
            }
        }

        public static (RefItemDepItem,List<Node<VarName>>) BuildMegaRef(List<RefCfg> refs, ConcurrentQueue<string> messages)
        {
            var mega = new RefItemDepItem(); //archived-loaded agnostic

            #region collect direct dependencies

            var refItems = new List<RefItemCfg>();
            foreach (var r in refs)
                refItems.AddRange(r.Items);

            #endregion

            #region build mega ref

            var bag = new List<RefItemDepItem>();
            foreach (var r in refItems)
            {
                try
                {
                    var dep = RefItemDepItem.Self(r);
                    bag.Add(dep);
                }
                catch (Exception ex)
                {
                    messages.Enqueue(ex.Message);
                }
            }
            foreach (var item in bag)
            {
                foreach (var r in item.ResourcesRelPath)
                    mega.AddRes(r);
                foreach (var v in item.VarsName)
                    mega.AddVar(v.Key, v.Value);
            }

            var tree = BuildVarDepsTree(mega.VarsName.Select(x=>x.Key));
            foreach (var node in Walk(tree))
            {
                var var = Settings.FindByName(node.Value);
                mega.AddVar(var.Name, var.RelativePath);
            }

            #endregion
            return (mega,tree);
        }

        public static (RefItemDepItem,List<Node<VarName>>) BuildMegaRefAllArchive()
        {
            var mega = RefItemDepItem.AllArchive(); //archived-loaded agnostic

            var tree = new List<Node<VarName>>();
            foreach (var item in mega.VarsName)
                tree.Add(new Node<VarName> { Value = item.Key });

            return (mega,tree);
        }

        public static List<Node<DepsTreeItem>> CalcNodes((RefItemDepItem,List<Node<VarName>>) dep)
        {
            var resultTree = new Node<DepsTreeItem>{Value = DepsTreeItem.FromString("root") };

            var nodeVar = resultTree.Add(DepsTreeItem.FromString("Vars:"));
            var varTree = new Node<VarName>{ Value = VarName.Null, Children = dep.Item2.ToList() };
            var varDepsItemTree = NodeHelper.ConvertTree(varTree, n =>
            {
                return DepsTreeItem.FromVarName(n.Value, n.IsMissing);
            });
            foreach (var n in varDepsItemTree.Children)
                nodeVar.Children.Add(n);
            if(nodeVar.Children.Count == 0)
                nodeVar.Add(DepsTreeItem.FromString("<No items>"));

            var nodeUser = resultTree.Add(DepsTreeItem.FromString("User resources:"));
            foreach (var item in dep.Item1.ResourcesRelPath)
                nodeUser.Add(DepsTreeItem.FromResRelPath(item));
            if (nodeUser.Children.Count == 0)
                nodeUser.Add(DepsTreeItem.FromString("<No items>"));

            return resultTree.Children;
        }

        public static List<Node<DepsTreeItem>> CalcDependentNodes(DepsProviderBase item)
        {
            var res = new List<Node<DepsTreeItem>> { new Node<DepsTreeItem>{ Value = DepsTreeItem.FromString("<No dependent items>") } };

            var processed = new HashSet<RefItemCfg>(RefItemCfg.Eq);
            var tree =  Node<RefItemCfg>.Build(item.GetRef(), x => GetRefs(x, processed));
            var treeStr = NodeHelper.ConvertTree(tree, n => DepsTreeItem.FromRefItemCfg(n.Value));
            if (treeStr.Children.Any())
                res = treeStr.Children;

            return res;
        }

        private static void AddRefToTarget(HashSet<string> target, UserItem userItem, bool isArchive)
        {
            foreach (var item in userItem.Files)
                target.Add(FileHelper.GetRelativePath(Settings.Config, item.FullName, isArchive));
        }

        private static IEnumerable<RefItemCfg> FindDependentItems(RefItemCfg item, ResourceDict rd, bool isArchive, HashSet<RefItemCfg> processed, bool dependentMayBeOnlyPreset = false)
        {
            var relPath = item.Files.First();
            VarFile v = null;
            UserItem u = null;

            if(item.IsVar && TryFindVar(FileHelper.GetOnlyFileNameWithoutExtension(relPath), out var v1))
                v = v1;
            else
                u = Settings.FindByRelPath(relPath);

            var dependentVars = new HashSet<VarFile>();
            var dependentPaths = new HashSet<string>();

            if (v != null)
            {
                foreach (var kvp in rd.Vars)
                {
                    if (kvp.Key == new KeyCreatorAndName(v.Name))
                        continue;

                    //var v = kvp.Value.GetMaxVersion(); //TODO maxVersion
                    foreach (var var in kvp.Value.Vars.Values)
                    {
                        if (var.KeyDependencies.Contains(new KeyCreatorAndName(v.Name)))
                            dependentVars.Add(var);
                    }
                }
            }

            // Among all user resources, find those that have a dependency on relPath
            foreach (var checkedRes in rd.UserResources)
            {
                if (FileHelper.ArePathsEqual(checkedRes.Info.RelativePath, relPath, true))
                    continue;

                // v - if the element is a var, search in direct dependencies of ur for a reference to this var
                if(v != null && checkedRes.Dependencies.Contains(v.Name))
                    dependentPaths.Add(FileHelper.GetRelativePath(Settings.Config, checkedRes.Item.FullName, isArchive));

                //user resource is in the dependency list of another user resource
                if (u != null && checkedRes.DependenciesUserResContainsWoExt(relPath))
                {
                    if (!dependentMayBeOnlyPreset || checkedRes.IsPreset)
                        AddRefToTarget(dependentPaths, checkedRes, isArchive);
                }

                // In a scene/preset, there can be multiple arrays with references that may match presets.
                // In this case, the preset should get a dependent item, and the scene — a dependency item.
                // item — scene — do nothing, scenes cannot be depended on.
                // item — preset — search for scenes that depend on the preset.
                if (u != null && u.IsPreset && u.CanContainDependencies && checkedRes.CanContainDependencies && !checkedRes.IsPreset)
                {
                    foreach (var kvp in u.PresetArrays)
                    {
                        var arrayName = kvp.Key;
                        var vars = FileHelper.GetArray(kvp.Value);
                        if (checkedRes.PresetArrays.ContainsKey(arrayName))
                        {
                            var vars2 = FileHelper.GetArray(checkedRes.PresetArrays[arrayName]);
                            if (vars.Any() && vars.SetEquals(vars2))
                            {
                                if (!dependentMayBeOnlyPreset || checkedRes.IsPreset)
                                    AddRefToTarget(dependentPaths, checkedRes, isArchive);
                                break;
                            }
                        }
                    }
                }
            }


            foreach (var var in dependentVars)
            {
                //var path = FileHelper.RemoveFirstFolder(var.RelativePath, Folders.Type2RelPath[FolderType.AddonPackages]);
                var path = var.RelativePath;
                var r = new RefItemCfg
                {
                    IsVar = true,
                    Files = new List<string> { path },
                    IsInArchive = isArchive
                };
                if (!processed.Contains(r))
                {
                    processed.Add(r);
                    yield return r;
                }
            }
            foreach (var rl in dependentPaths)
            {
                if (string.IsNullOrWhiteSpace(rl))
                    continue;
                var r = new RefItemCfg
                {
                    IsVar = false,
                    Files = new List<string> { rl  },
                    IsInArchive = isArchive
                };
                if (!processed.Contains(r))
                {
                    processed.Add(r);
                    yield return r;
                }
            }
        }

        public static IEnumerable<RefItemCfg> FindPresetsInScene(UserItem u, ResourceDict rd, bool isArchive)
        {
            var relPath = u.Info.RelativePath;

            if (!u.CanContainDependencies)
                yield break;

            if (u.Type != FolderType.Scene && u.Type != FolderType.SubScene)
                yield break;

            var dependentPaths = new HashSet<string>();

            //Among all user resources, find those that have a dependency on relPath
            foreach (var checkedRes in rd.UserResources)
            {
                if (FileHelper.ArePathsEqual(checkedRes.Info.RelativePath, relPath, true))
                    continue;

                // In a scene/preset, there can be multiple arrays with references that may match presets.
                // In this case, the preset should receive a dependent item, and the scene — a dependency item.
                if (checkedRes.IsPreset)
                {
                    foreach (var kvp in u.PresetArrays)
                    {
                        var arrayName = kvp.Key;
                        var vars = new HashSet<VarName>(VarName.Eq);
                        foreach (var str in kvp.Value)
                        {
                            foreach (var line in str.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (FileHelper.TryFindVarName(line, out var name))
                                    vars.Add(name);
                            }
                        }

                        var vars2 = new HashSet<VarName>(VarName.Eq);
                        if (checkedRes.PresetArrays.ContainsKey(arrayName))
                        {
                            foreach (var str2 in checkedRes.PresetArrays[arrayName])
                            {
                                foreach (var line2 in str2.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (FileHelper.TryFindVarName(line2, out var name2))
                                        vars2.Add(name2);
                                }

                            }
                        }

                        if (vars.Any() && vars.SetEquals(vars2))
                        {
                            AddRefToTarget(dependentPaths, checkedRes, isArchive);
                            break;
                        }
                    }
                }
            }

            foreach (var rl in dependentPaths)
            {
                if (string.IsNullOrWhiteSpace(rl))
                    continue;
                var r = new RefItemCfg
                {
                    IsVar = false,
                    Files = new List<string> { rl  },
                    IsInArchive = isArchive
                };
                yield return r;
            }
        }

        public static IEnumerable<RefItemCfg> GetRefs(RefItemCfg ri, HashSet<RefItemCfg> processed)
        {
            foreach(var item in FindDependentItems(ri, Settings.LoadedRd, false, processed))
                yield return item;

            foreach(var item in FindDependentItems(ri, Settings.ArchiveRd, true, processed))
                yield return item;
        }
    }
}
