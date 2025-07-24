using System;
using System.Collections.Generic;
using System.Linq;
using Vrm.Vam;

namespace Vrm.Json
{
    public class VarFileDto
    {
        public DateTime Modified {get;set;}
        public string RawName {get;set;}
        public VarMeta Meta {get;set;}
        public string MetaJson {get;set;}
        public List<string> Dependencies {get;set;} = new List<string>();
        public List<string> Entries {get;set;} = new List<string>();
        public Dictionary<FolderType, List<ElementInfo>> ElementsDict {get;set;} = new Dictionary<FolderType, List<ElementInfo>>();
        public FolderType Type {get;set;}
        public int MorphCount {get;set;}
        public bool IsPreloadMorphs {get;set;}
        public bool CorruptedMetaJson {get;set;}
        public DateTime ModifiedMin {get;set;}
        public DateTime ModifiedMax {get;set;}

        public static VarFileDto From(VarFile v)
        {
            return new VarFileDto
            {
                Modified = v.Info.LastWriteTime,
                RawName = v.Name.RawName,
                Meta = v.Meta,
                MetaJson = v.MetaJson,
                Dependencies = v.Dependencies.Select(x=>x.FullName).ToList(),
                Entries = v.Entries,
                ElementsDict = v.ElementsDict,
                Type = v.Type,
                MorphCount = v.MorphCount,
                IsPreloadMorphs = v.IsPreloadMorphs,
                CorruptedMetaJson = v.CorruptedMetaJson,
                ModifiedMin = v.ModifiedMin,
                ModifiedMax = v.ModifiedMax
            };
        }
    }
}
