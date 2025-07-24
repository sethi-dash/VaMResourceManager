using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Cfg
{
    public class RefItemCfg
    {
        public List<string> Files {get;set;} // - only relative paths. One element for var, or more for resources
        public bool IsInArchive {get;set;} //Where to look for elements
        public bool IsVar {get;set;}

        public static RefItemCfg Copy(RefItemCfg source)
        {
            var obj = new RefItemCfg();
            obj.Files = source.Files.ToList();
            obj.IsInArchive = source.IsInArchive;
            obj.IsVar = source.IsVar;
            return obj;
        }

        public RefItemCfg() { }

        public RefItemCfg(VarFile var)
        {
            bool inArchive = var.IsInArchive;
            Files = new List<string>() { FileHelper.NormalizePath(FileHelper.GetRelativePath(Settings.Config, var.Info.FullName, inArchive)) };
            IsInArchive = inArchive;
            IsVar = true;
        }

        public RefItemCfg(UserItem item)
        {
            bool inArchive = item.IsInArchive;
            Files = item.Files.Select(x => x.FullName).Select(x => FileHelper.NormalizePath(FileHelper.GetRelativePath(Settings.Config, x, inArchive))).ToList();
            IsInArchive = inArchive;
            IsVar = false;
        }

        public override string ToString()
        {
            var loc = IsInArchive ? "archive" : "loaded";
            return $"{Files.First()} [{loc}]";
        }

        public static IEqualityComparer<RefItemCfg> Eq { get; } = new RefItemCfgEqualityComparer();
    }

    public class RefCfg
    {
        public List<RefItemCfg> Items {get;set;}
        public int Index {get;set;}

        public static RefCfg CopyOnlyItems(RefCfg source)
        {
            var obj = new RefCfg();
            obj.Items = source.Items.Select(RefItemCfg.Copy).ToList();
            return obj;
        }

        public IEnumerable<string> GetItemNames()
        {
            foreach (var item in Items)
            {
                foreach (var f in item.Files)
                {
                    yield return f;
                }
            }
        }

        public HashSet<string> GetItemNamesHash()
        {
            var hash = new HashSet<string>();
            foreach (var item in Items)
            {
                foreach (var f in item.Files)
                {
                    hash.Add(f);
                }
            }

            return hash;
        }
    }

    public class RefNamedCfg : RefCfg
    {
        public string Name {get;set;}
    }

    public class RefItemCfgEqualityComparer : IEqualityComparer<RefItemCfg>
    {
        public bool Equals(RefItemCfg x, RefItemCfg y)
        {
            if (x == null || y == null)
                return false;

            // Checking that both Files lists are not empty
            if (x.Files == null || y.Files == null || x.Files.Count == 0 || y.Files.Count == 0)
                return false;

            return string.Equals(x.Files[0], y.Files[0], StringComparison.OrdinalIgnoreCase) && x.IsInArchive == y.IsInArchive;
        }

        public int GetHashCode(RefItemCfg obj)
        {
            if (obj == null || obj.Files == null || obj.Files.Count == 0)
                return 0;

            int fileHash = obj.Files[0]?.GetHashCode() ?? 0; //works because Files always sorted
            int archiveHash = obj.IsInArchive.GetHashCode();

            return fileHash ^ archiveHash;
        }
    }

    public class RefCfgComparer : IEqualityComparer<RefCfg>
    {
        private readonly RefItemCfgEqualityComparer _itemComparer = new RefItemCfgEqualityComparer();

        public bool Equals(RefCfg x, RefCfg y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
        
            if (x.Items == null && y.Items == null) return true;
            if (x.Items == null || y.Items == null) return false;
        
            return x.Items.UnorderedSequenceEqual(y.Items, _itemComparer);
        }

        public int GetHashCode(RefCfg obj)
        {
            if (obj == null || obj.Items == null) 
                return 0;
        
            unchecked
            {
                int hash = 17;
                foreach (var item in obj.Items.OrderBy(i => _itemComparer.GetHashCode(i)))
                {
                    hash = hash * 23 + _itemComparer.GetHashCode(item);
                }
                return hash;
            }
        }
    }
}