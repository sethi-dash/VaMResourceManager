using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vrm.Cfg;
using Vrm.Json;
using Vrm.Util;

namespace Vrm.Vam
{
    public class VarFile : DepsProviderBase
    {
        public VarName Name;
        public ElementInfo Info;
        public VarMeta Meta;
        public string MetaJson;
        public List<VarName> Dependencies = new List<VarName>(); //direct deps
        public HashSet<KeyCreatorAndName> KeyDependencies = new HashSet<KeyCreatorAndName>();
        public List<string> Entries = new List<string>();
        public Dictionary<FolderType, List<ElementInfo>> ElementsDict = new Dictionary<FolderType, List<ElementInfo>>();
        public int MorphCount;
        public bool IsPreloadMorphs;
        public bool IsPreloadMorhpsDisabledByPrefs;
        public bool CorruptedMetaJson;
        public bool IsLoaded;
        public bool IsMissing;
        public bool IsInArchive;
        public DateTime ModifiedMin = DateTime.MaxValue;
        public DateTime ModifiedMax = DateTime.MinValue;

        public string RelativePath => Info?.RelativePath;
        public FolderType Type => Info.Type;

        public void Add(FolderType type, ElementInfo e)
        {
            if (!ElementsDict.TryGetValue(type, out var list))
            {
                list = new List<ElementInfo>();
                ElementsDict[type] = list;
            }
            list.Add(e);
            if (e.LastWriteTime < ModifiedMin)
                ModifiedMin = e.LastWriteTime;
            if (e.LastWriteTime > ModifiedMax)
                ModifiedMax = e.LastWriteTime;
        }

        public override RefItemCfg GetRef()
        {
            return new RefItemCfg(this);
        }

        public override string GetTitle()
        {
            return ToString();
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override ElementInfo GetElementInfo()
        {
            return Info;
        }

        public static IComparer<VarFile> Comparer { get; } = new VarFileComparer();

        public VarFile()
        {

        }

        public static VarFile From(VarFileDto dto, FileInfo fi, bool isInArchive)
        {
            var v = new VarFile();
            v.Name = VarName.Parse(dto.RawName);
            v.Info = FileHelper.CreateVarInfo(fi, isInArchive, dto.Type);
            v.Meta = dto.Meta;
            v.MetaJson = dto.MetaJson;
            v.Dependencies = dto.Dependencies.Select(VarName.Parse).ToList();
            foreach (var item in v.Dependencies)
                v.KeyDependencies.Add(new KeyCreatorAndName(item));
            v.Entries = dto.Entries;
            v.ElementsDict = dto.ElementsDict;
            v.MorphCount = dto.MorphCount;
            v.IsPreloadMorphs = dto.IsPreloadMorphs;
            v.IsLoaded = true;
            v.IsMissing = false;
            v.IsInArchive = isInArchive;
            v.ModifiedMin = dto.ModifiedMin;
            v.ModifiedMax = dto.ModifiedMax;
            return v;
        }
    }

    public class ElementInfo
    {
        public string FullName;
        public string ImgLocalPath;
        public string Name;
        public string RelativePath;
        public DateTime LastWriteTime;
        public DateTime CreationTime;
        public long Length;
        public FolderType Type;
        public List<string> Exts;

        public bool IsImage => ImgLocalPath != null;
        public bool IsText => !IsImage;
        [JsonIgnore]
        public string UserPrefs;
        [JsonIgnore]
        public string UserTags;
        [JsonIgnore]
        public bool IsHide;
        [JsonIgnore]
        public bool IsFav;

        public ElementInfo(string fullName, string imgLocalPath, string name, string relativePath, DateTime modified, DateTime created, long length, FolderType type)
        {
            FullName = fullName;
            ImgLocalPath = imgLocalPath;
            Name = name;
            RelativePath = relativePath;
            LastWriteTime = modified;
            CreationTime = created;
            Length = length;
            Type = type;
        }

        public override string ToString()
        {
            return FullName;
        }
    }

    public class VarFileComparer : IComparer<VarFile>
    {
        public int Compare(VarFile x, VarFile y)
        {
            if (x == null || y == null)
                return 0;

            int nameResult = VarName.Comparer.Compare(x.Name, y.Name);
            if (nameResult != 0)
                return nameResult;

            return x.IsInArchive.CompareTo(y.IsInArchive);
        }
    }
}
