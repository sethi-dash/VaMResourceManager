using System.Collections.Generic;
using System.Linq;
using Vrm.Util;

namespace Vrm.Vam
{
    public static class Ext
    {
        public static string Var = ".var";
        public static HashSet<string> Morphs = new HashSet<string>{".vmb", ".vmi", ".dsf"};
        public static string Preset = ".vap";
        public static string Package = ".var";
        public static HashSet<string> Hair= new HashSet<string>{".vab", ".vaj", ".vam"};
        public static HashSet<string> Clothing = new HashSet<string>{".vab", ".vaj", ".vam"};
        public static HashSet<string> HairAndClothing = new HashSet<string>{".vab", ".vaj", ".vam"};
        public static string Scene = ".json";
        public static string Subscene = ".json";
        public static string SceneAndSubscene = ".json";
        public static string LegacyScene = ".vac";
        public static string Favorite = ".fav";
        public static string DotVamDotHide = ".vam.hide";
        public static string Hide = ".hide";
        public static string Prefs = ".prefs";
        public static string ClothingPlugins = ".clothingplugins";
        public static string HairPlugins = ".hairplugins";
        public static HashSet<string> Script = new HashSet<string>{".cs", ".cslist"};
        public static string Asset = ".assetbundle";
        public static string Jpg = ".jpg";
        public static string Json = ".json";
        public static string Vaj = ".vaj";
        public static string Vam = ".vam";
        public static string Vmi = ".vmi";
        public static string UiAssistCfg = ".uiap";
        public static HashSet<string> Music = new HashSet<string>{".ogg", ".wav", ".mp3"};
        public static HashSet<string> Textures = new HashSet<string>{".jpg", ".tif", ".png", ".bmp", ".tiff"};

        public static HashSet<string> ContainsDeps = FileHelper.NormalizePaths(GetDeps()).ToHashSet();
        public static readonly HashSet<string> Resources = FileHelper.NormalizePaths(GetResources()).ToHashSet();

        public static bool CanContainDeps(string ext)
        {
            return ContainsDeps.Contains(ext.ToLowerInvariant());
        }

        public static bool CanContainResources(string ext)
        {
            return Resources.Contains(ext.ToLowerInvariant());
        }

        public static bool IsScene(string ext)
        {
            return Ext.IsMatch(ext, Ext.Scene);
        }

        public static bool IsHairOrClothing(string ext)
        {
            foreach (var item in Ext.HairAndClothing)
            {
                if (Ext.IsMatch(ext, item))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsMatch(string ext1, string ext2)
        {
            return ext1.IndexOf(ext2, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsFileExtMatch(string filePath, string ext2)
        {
            return IsMatch(FileHelper.GetExtension(filePath), ext2);
        }

        public static bool IsFileCanBeTexture(string filePath)
        {
            var ext = FileHelper.GetExtension(filePath);
            return Textures.Contains(ext);
        }

        private static IEnumerable<string> GetDeps()
        {
            return HairAndClothing.Except(new[] { "vab" })
                .Concat(new[] { UiAssistCfg, SceneAndSubscene, Preset, Package });
        }

        private static IEnumerable<string> GetResources()
        {
            return new[]
                {
                    Preset,
                    SceneAndSubscene,
                    Asset,
                    UiAssistCfg,
                    Jpg
                }.Concat(Morphs)
                .Concat(HairAndClothing)
                .Concat(Script)
                .Concat(Music)
                .Concat(Textures);
        }
    }
}