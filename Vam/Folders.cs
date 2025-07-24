using System;
using System.Collections.Generic;
using System.Linq;
using Vrm.Util;

namespace Vrm.Vam
{
    public static class Folders
    {
        public static Dictionary<FolderType, string> Type2RelPath = new Dictionary<FolderType, string>()
        {
            { FolderType.AddonPackages, @"AddonPackages\" },
            { FolderType.Scene, @"Saves\scene\" },
            { FolderType.UIAssistByJJW, @"Saves\PluginData\JayJayWon\UIAssist\" },
            { FolderType.LegacyAppearance, @"Saves\person\appearance" },
            { FolderType.LegacyPose, @"Saves\person\pose" },
            { FolderType.LegacyPreset, @"Saves\person\full" },
            { FolderType.PresetAppearance, @"Custom\Atom\Person\Appearance\" },
            { FolderType.PresetClothing, @"Custom\Atom\Person\Clothing\" },
            { FolderType.PresetHair, @"Custom\Atom\Person\Hair\" },
            { FolderType.PresetPersonPlugin, @"Custom\Atom\Person\Plugins\" },
            { FolderType.PresetPose, @"Custom\Atom\Person\Pose\" },
            { FolderType.PresetMorphs, @"Custom\Atom\Person\Morphs\" },
            { FolderType.PresetSkin, @"Custom\Atom\Person\Skin\" },
            { FolderType.PresetFemaleBreast, @"Custom\Atom\Person\BreastPhysics\" },
            { FolderType.PresetFemaleGlute, @"Custom\Atom\Person\GlutePhysics\" },
            { FolderType.PresetAnimation, @"Custom\Atom\Person\AnimationPresets\" },
            { FolderType.PresetGeneral, @"Custom\Atom\Person\General\" },
            { FolderType.PresetSessionAndScenePlugin, @"Custom\PluginPresets\" },
            { FolderType.SubScene, @"Custom\SubScene\" },
            { FolderType.Sounds, @"Custom\Sounds\" },
            { FolderType.Clothing, @"Custom\Clothing\" },
            { FolderType.Hair, @"Custom\Hair\" },
            { FolderType.Textures, @"Custom\Atom\Person\Textures\" },
            { FolderType.Assets, @"Custom\Assets\" },
            { FolderType.Scripts, @"Custom\Scripts\" }
        };

        public static bool IsPreset(FolderType type)
        {
            switch (type)
            {
                case FolderType.LegacyPose:
                case FolderType.LegacyPreset:
                case FolderType.LegacyAppearance:
                case FolderType.PresetAppearance:
                case FolderType.PresetClothing:
                case FolderType.PresetHair:
                case FolderType.PresetPersonPlugin:
                case FolderType.PresetPose:
                case FolderType.PresetMorphs:
                case FolderType.PresetSkin:
                case FolderType.PresetFemaleBreast:
                case FolderType.PresetFemaleGlute:
                case FolderType.PresetAnimation:
                case FolderType.PresetGeneral:
                case FolderType.PresetSessionAndScenePlugin:
                    return true;
                default:
                    return false;
            }
        }

        public static bool HasPreviewImage(FolderType type)
        {
            switch (type)
            {
                case FolderType.Unset:
                case FolderType.AddonPackages:
                    return false;
                case FolderType.Scene:
                    return true;
                case FolderType.UIAssistByJJW:
                    return false;
                case FolderType.PresetSessionAndScenePlugin:
                    return true;
                case FolderType.SubScene:
                    return true;
                case FolderType.PresetAppearance:
                    return true;
                case FolderType.PresetClothing:
                    return true;
                case FolderType.PresetHair:
                    return true;
                case FolderType.PresetPersonPlugin:
                    return true;
                case FolderType.PresetPose:
                    return true;
                case FolderType.PresetMorphs:
                    return false;
                case FolderType.PresetSkin:
                    return false;
                case FolderType.PresetFemaleBreast:
                    return true;
                case FolderType.PresetFemaleGlute:
                    return true;
                case FolderType.PresetAnimation:
                    return true;
                case FolderType.PresetGeneral:
                    return true;
                case FolderType.Clothing:
                    return true;
                case FolderType.Hair:
                    return true;
                case FolderType.Sounds:
                    return false;
                case FolderType.Textures:
                    return false;
                case FolderType.Assets:
                    return true;
                case FolderType.Scripts:
                    return false;
                case FolderType.LegacyPose:
                    return true;
                case FolderType.LegacyAppearance:
                    return true;
                case FolderType.LegacyPreset:
                    return true;
            }

            return false;
        }

        public static SearchOpt GetSearchOption(FolderType type)
        {
            switch (type)
            {
                case FolderType.Unset:
                    return SearchOpt.Ignore;
                case FolderType.LegacyAppearance:
                case FolderType.LegacyPose:
                case FolderType.LegacyPreset:
                default:
                    return SearchOpt.All;
            }
        }

        public static FolderType Parse(string path)
        {
            var unset = FolderType.Unset;
            if (string.IsNullOrWhiteSpace(path))
                return unset;

            path = FileHelper.NormalizePath(path);
            foreach (var kvp in Type2RelPath)
            {
                if (path.IndexOf(kvp.Value, 0, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Key;
                }
            }

            if (Settings.Config.UserFolders.Any())
            {
                foreach (var uf in Settings.Config.UserFolders)
                {
                    if (path.IndexOf(uf, 0, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return FolderType.UserFolders;
                    }
                }
            }

            return unset;
        }

        public static bool TryParse(string path, out FolderType type)
        {
            type = Parse(path);
            return type != FolderType.Unset;
        }
    }
}