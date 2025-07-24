using Vrm.Vam;

namespace Vrm.Cfg
{
    public class CategoryCfg
    {
        public bool IsEnabled {get;set;} = true;
        public FolderType Type {get; set; }
        public bool WithoutPresets { get; set; } = false;

        public CategoryCfg()
        {
        }

        public CategoryCfg(FolderType type)
        {
            Type = type;
        }

        public string Name
        {
            get
            {
                if (WithoutPresets)
                    return $"{Type.GetDisplayName()} (w/o presets)";
                else
                    return $"{Type.GetDisplayName()}";
            }
        }

        public override string ToString()
        {
            if(WithoutPresets)
                return $"{Type} (w/o presets)";
            return $"{Type}";
        }
    }
}