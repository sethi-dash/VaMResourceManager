using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Vrm.Vam
{
    [Flags]
    public enum FolderType
    {
        [Display(Name = "None")]
        Unset = 0,

        [Display(Name = "Var")]
        AddonPackages = 1 << 0,

        [Display(Name = "Scene")]
        Scene = 1 << 1,

        [Display(Name = "Ui Assist (JJW)")]
        UIAssistByJJW = 1 << 2,

        [Display(Name = @"Session\Scene plugin")]
        PresetSessionAndScenePlugin = 1 << 3,

        [Display(Name = "Subscene")]
        SubScene = 1 << 4,

        [Display(Name = "Appearance")]
        PresetAppearance = 1 << 5,

        [Display(Name = "Clothing preset")]
        PresetClothing = 1 << 6,

        [Display(Name = "Hair preset")]
        PresetHair = 1 << 7,

        [Display(Name = "Person plugin")]
        PresetPersonPlugin = 1 << 8,

        [Display(Name = "Pose")]
        PresetPose = 1 << 9,

        [Display(Name = "Morphs")]
        PresetMorphs = 1 << 10,

        [Display(Name = "Skin")]
        PresetSkin = 1 << 11,

        [Display(Name = "Breast")]
        PresetFemaleBreast = 1 << 12,

        [Display(Name = "Glute")]
        PresetFemaleGlute = 1 << 13,

        [Display(Name = "Animation")]
        PresetAnimation = 1 << 14,

        [Display(Name = "General")]
        PresetGeneral = 1 << 15,

        [Display(Name = "Clothing")]
        Clothing = 1 << 16,

        [Display(Name = "Hair")]
        Hair = 1 << 17,

        [Display(Name = "Sound")]
        Sounds = 1 << 18,

        [Display(Name = "Texture")]
        Textures = 1 << 19,

        [Display(Name = "Asset")]
        Assets = 1 << 20,

        [Display(Name = "Script")]
        Scripts = 1 << 21,

        [Display(Name = "Legacy-Pose")]
        LegacyPose = 1 << 22,

        [Display(Name = "Legacy-Appearance")]
        LegacyAppearance = 1 << 23,

        [Display(Name = "Legacy-Preset")]
        LegacyPreset = 1 << 24,

        [Display(Name = "User")]
        UserFolders = 1 << 25,
    }

    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();
            return attr?.Name ?? value.ToString();
        }
    }
}