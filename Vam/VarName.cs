using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Vrm.Vam
{
    public class VarName
    {
        private static readonly VarName _null = new VarNameNull();
        private class VarNameNull : VarName
        {
            public override string ToString()
            {
                return "NULL var";
            }
        }

        private const string _latestSuffix = "latest";

        public string Creator;
        public string Name;
        public int Version;
        public bool IsLatest;

        public string RawName { get;set; }

        public string CreatorAndName => $"{Creator}.{Name}";
        public string FullName => ToString();

        public static VarName Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            input = input.Trim('"', '/', '\\', '\'');
            var parts = input.Split('.');
            if (parts.Length == 3)
            {
                var creator = parts[0];
                var name = parts[1];


                string strVer = parts[2];
                if(ParseVersion(strVer, out var isLatest, out var version))
                {
                    return new VarName
                    {
                        Creator = creator,
                        Name = name,
                        Version = version,
                        IsLatest = isLatest,
                        RawName = input
                    };
                }
            }
            throw new Exception($"Invalid format: must contain 'creator.name.version'");
        }

        public static bool ParseVersion(string strVer, out bool isLatest, out int version)
        {
            version = 0;
            isLatest = false;
            if (strVer.Contains(_latestSuffix))
            {
                version = 0;
                isLatest = true;
                return true;
            }
            else
            {
                var ver = int.TryParse(strVer, out var v) ? v : ExtractFirstNumber(strVer);
                if (ver != null)
                {
                    isLatest = false;
                    version = ver.Value;
                    return true;
                }
            }
            return false;
        }

        private static int? ExtractFirstNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var match = Regex.Match(input, @"\d+");
            if (match.Success)
                return int.Parse(match.Value);

            return null;
        }

        public override string ToString()
        {
            return IsLatest ? $"{Creator}.{Name}.{_latestSuffix}" : $"{Creator}.{Name}.{Version}";
        }

        public static VarName Null { get; } = _null;
        public bool IsNull => this == _null;

        public static IComparer<VarName> Comparer { get; } = new VarNameComparer();
        public static IEqualityComparer<VarName> Eq { get; } = new VarNameEqualityComparer();
        public static IEqualityComparer<VarName> EqWoVersion { get; } = new VarNameEqualityComparerWoVersion();
    }

    public class VarNameComparer : IComparer<VarName>
    {
        public int Compare(VarName x, VarName y)
        {
            if (x == null || y == null)
                return 0;

            int creatorComparison = string.Compare(x.Creator, y.Creator, StringComparison.OrdinalIgnoreCase);
            if (creatorComparison != 0)
                return creatorComparison;

            return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class VarNameEqualityComparer : IEqualityComparer<VarName>
    {
        public bool Equals(VarName x, VarName y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            return string.Equals(x.Creator, y.Creator)
                   && string.Equals(x.Name, y.Name)
                   && x.Version == y.Version
                   && x.IsLatest == y.IsLatest;
        }

        public int GetHashCode(VarName obj)
        {
            if (obj == null) return 0;

            int hash = 17;
            hash = hash * 23 + (obj.Creator?.GetHashCode() ?? 0);
            hash = hash * 23 + (obj.Name?.GetHashCode() ?? 0);
            hash = hash * 23 + obj.Version.GetHashCode();
            hash = hash * 23 + obj.IsLatest.GetHashCode();
            return hash;
        }
    }

    public class VarNameEqualityComparerWoVersion : IEqualityComparer<VarName>
    {
        public bool Equals(VarName x, VarName y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            return string.Equals(x.Creator, y.Creator) && string.Equals(x.Name, y.Name);
        }

        public int GetHashCode(VarName obj)
        {
            if (obj == null) return 0;

            int hash = 17;
            hash = hash * 23 + (obj.Creator?.GetHashCode() ?? 0);
            hash = hash * 23 + (obj.Name?.GetHashCode() ?? 0);
            return hash;
        }
    }
}