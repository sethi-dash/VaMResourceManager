using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vrm.Cfg;
using Vrm.Json;
using Vrm.Refs;
using Vrm.Util;

namespace Vrm.Vam
{
    public class UserItem : DepsProviderBase
    {
        public ElementInfo Info; //General data about the item
        public bool IsInArchive;
        public List<FileInfo> Files; //All files, including jpg
        public FileInfo Item; //Dependency file if CanContainDependencies == true
        public bool CanContainDependencies;


        private HashSet<VarName> _dependencies;
        public HashSet<VarName> Dependencies
        {
            get
            {
                if (_dependencies == null)
                {
                    if (CanContainDependencies)
                    {
                        _dependencies = FileHelper.FindVarNames(Item.FullName).ToHashSet(VarName.Eq);
                    }
                    else
                        _dependencies = new HashSet<VarName>(VarName.Eq);
                }
                return _dependencies;
            }
        }

        private List<string> _dependenciesUserRes;
        public List<string> DependenciesUserRes
        {
            get
            {
                if (_dependenciesUserRes == null)
                {
                    if (CanContainDependencies)
                        _dependenciesUserRes = FileHelper.FindUserResources(Item.FullName).Distinct().ToList();
                    else
                        _dependenciesUserRes = new List<string>();
                }

                return _dependenciesUserRes;
            }
        }

        private Dictionary<string, List<string>> _presetArrays;
        public Dictionary<string, List<string>> PresetArrays
        {
            get
            {
                if (_presetArrays == null)
                {
                    if (CanContainDependencies)
                        _presetArrays = ArrayExtractor.ExtractJsonArraysAll(Item.FullName, "clothing", "hair", "morphs");
                    else
                        _presetArrays = new Dictionary<string, List<string>>();
                }
                return _presetArrays;
            }
        }

        private List<string> _presets;
        /// <summary>
        /// Only for scene & subscene
        /// </summary>
        public List<string> Presets
        {
            get
            {
                if (_presets == null)
                {
                    if (Type == FolderType.Scene || Type == FolderType.SubScene)
                    {
                        _presets = new List<string>();
                        foreach (var item in DepsHelper.FindPresetsInScene(this, Settings.ArchiveRd, true)
                                     .Concat(DepsHelper.FindPresetsInScene(this, Settings.LoadedRd, false)))
                            _presets.Add(item.Files.First());
                    }
                    else
                        _presets = new List<string>();
                }

                return _presets;
            }
        }

        public bool IsPreset => Folders.IsPreset(Type);
        public FolderType Type => Info.Type;
        public bool IsDependenciesResolved;

        #region init

        public UserItem(ElementInfo info, bool isArchive, List<FileInfo> allFiles, FileInfo main, bool canContainDependencies)
        {
            Info = info;
            IsInArchive = isArchive;
            Files = allFiles.ToList();
            Item = main;
            CanContainDependencies = canContainDependencies;
        }

        public UserItem(UserItemDto obj, ElementInfo ei, bool isArchive, List<FileInfo> files, FileInfo main, bool canContainDependencies)
            : this(ei, isArchive, files, main, canContainDependencies)
        {
            _dependencies = obj.Dependencies.Select(VarName.Parse).ToHashSet(VarName.Eq);
            _dependenciesUserRes = obj.DependenciesUserRes;
            _presetArrays = obj.PresetArrays;
            _presets = obj.Presets;
        }

        #endregion

        public void WriteAsDto()
        {
            if (!CanContainDependencies)
                return;

            if (!IsDependenciesResolved)
                throw new InvalidOperationException();

            var path = FileHelper.PathCombine(Settings.Config.CachePath, FileHelper.ChangeExt(Info.RelativePath, "meta"));
            FileHelper.EnsureDirectoryStructureExists(path);
            var dto = UserItemDto.From(this);
            File.WriteAllText(path, JsonConvert.SerializeObject(dto, Formatting.Indented));
        }

        public bool DependenciesUserResContainsWoExt(string path)
        {
            foreach (var item in DependenciesUserRes)
            {
                if (FileHelper.ArePathsEqual(item, path, true))
                    return true;
            }

            return false;
        }

        public void ResolveDependencies()
        {
            if (IsDependenciesResolved)
                return;

            var c1 = Dependencies.Count;
            var c2 = DependenciesUserRes.Count;
            var c3 = PresetArrays.Count;
            var c4 = Presets.Count;

            GC.KeepAlive(c1);
            GC.KeepAlive(c2);
            GC.KeepAlive(c3);
            GC.KeepAlive(c4);
            IsDependenciesResolved = true;
        }

        private static HashSet<string> _mainExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Ext.Json, Ext.Preset, Ext.Vam, Ext.UiAssistCfg
        };

        public static (FileInfo,bool) SelectMain(List<FileInfo> files)
        {
            var canContainDependencies = false;
            var main = files.FirstOrDefault();
            
            foreach (var item in files)
            {
                var ext = FileHelper.GetExtension(item.Name);
                if (_mainExts.Contains(ext))
                {
                    canContainDependencies = true;
                    main = item;
                    break;
                }
            }

            return (main, canContainDependencies);
        }

        public static IComparer<UserItem> Comparer { get; } = new UserItemComparer();
        public static IEqualityComparer<string> RelPathComparer { get; } = new RelPathComparer();
        
        public override RefItemCfg GetRef()
        {
            return new RefItemCfg(this);
        }

        public override string GetTitle()
        {
            return ToString();
        }

        public override ElementInfo GetElementInfo()
        {
            return Info;
        }

        public override string ToString()
        {
            return Info.RelativePath;
        }
    }

    public class UserItemComparer : IComparer<UserItem>
    {
        public int Compare(UserItem x, UserItem y)
        {
            if (x == null || y == null)
                return 0;

            return string.Compare(x.Item.FullName, y.Item.FullName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class RelPathComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return FileHelper.ArePathsEqual(x, y, true);
        }

        public int GetHashCode(string obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(FileHelper.NormalizePath(obj, true));
        }
    }
}
