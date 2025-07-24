using System;

namespace Vrm.Vam
{
    public readonly struct KeyCreatorAndName : IEquatable<KeyCreatorAndName>
    {
        private readonly VarName _n;

        public static bool IgnoreCase = true;

        public KeyCreatorAndName(VarName n)
        {
            _n = n;
        }

        public bool Equals(KeyCreatorAndName other)
        {
            if(IgnoreCase)
                return _n.Creator.IndexOf(other._n.Creator, StringComparison.OrdinalIgnoreCase) == 0 &&
                   _n.Name.IndexOf(other._n.Name, StringComparison.OrdinalIgnoreCase) == 0;
            else
                return _n.Creator == other._n.Creator && _n.Name == other._n.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KeyCreatorAndName)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                if (IgnoreCase)
                {
                    hash = hash * 23 + (_n.Creator != null ? _n.Creator.ToLowerInvariant().GetHashCode() : 0);
                    hash = hash * 23 + (_n.Name != null ? _n.Name.ToLowerInvariant().GetHashCode() : 0);
                }
                else
                {
                    hash = hash * 23 + (_n.Creator != null ? _n.Creator.GetHashCode() : 0);
                    hash = hash * 23 + (_n.Name != null ? _n.Name.GetHashCode() : 0);
                }

                return hash;
            }
        }

        public static bool operator ==(KeyCreatorAndName left, KeyCreatorAndName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(KeyCreatorAndName left, KeyCreatorAndName right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{_n.Creator}.{_n.Name}";
        }
    }
}
