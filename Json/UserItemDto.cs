using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vrm.Vam;

namespace Vrm.Json
{
    public class UserItemDto
    {
        public DateTime Modified {get;set;}
        public FolderType Type {get;set;}
        public bool CanContainDependencies {get;set;}
        public List<string> Dependencies { get;set; }
        public List<string> DependenciesUserRes {get;set;}
        public Dictionary<string, List<string>> PresetArrays {get;set;}
        public List<string> Presets {get;set;}

        public static UserItemDto From(UserItem obj)
        {
            return new UserItemDto
            {
                Modified = obj.Info.LastWriteTime,
                Type = obj.Type,
                CanContainDependencies = obj.CanContainDependencies,
                Dependencies = obj.Dependencies.Select(x=>x.FullName).ToList(),
                DependenciesUserRes = obj.DependenciesUserRes,
                PresetArrays = obj.PresetArrays,
                Presets = obj.Presets
            };
        }
    }
}
