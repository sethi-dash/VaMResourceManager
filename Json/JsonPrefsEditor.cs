using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vrm.Json
{
    public class JsonPrefsEditor
    {
        public const string str_pluginsAlwaysEnabled = "pluginsAlwaysEnabled";
        public const string str_pluginsAlwaysDisabled = "pluginsAlwaysDisabled";
        public const string str_ignoreMissingDependencyErrors = "ignoreMissingDependencyErrors";
        public const string str_preloadMorphs = "preloadMorphs";
        private const string str_customOptions = "customOptions";
        public const string str_userTags = "userTags";
        private JArray _components = new JArray
        {
            new JObject { ["type"] = "DAZMesh" },
            new JObject { ["type"] = "DAZSkinWrap" },
            new JObject { ["type"] = "DAZSkinWrapMaterialOptions" },
            new JObject { ["type"] = "MVRPluginManager" },
        };

        private JObject _root;

        public JsonPrefsEditor(string json)
        {
            _root = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
        }

        public void AddComponentsToRoot()
        {
            _root["components"] = _components;
        }

        public void AddInComponents_MVRPluginManager()
        {
            var components = _root["components"] as JArray;
            var newComponent = new JObject
            {
                ["type"] = "MVRPluginManager"
            };
            components.Add(newComponent);
        }

        public void RemoveFromRoot_setUnlistedParamsToDefault()
        {
            _root.Remove("setUnlistedParamsToDefault");
        }

        public bool AddToStorables_ClothingPluginManager(string name, out string message)
        {
            message = null;
            JArray storables = _root["storables"] as JArray;

            if (storables != null && storables.Count > 0)
            {
                string id = storables[0]?["id"]?.ToString();

                if (!string.IsNullOrEmpty(id) && id.Contains(":"))
                {
                    string prefix = id.Split(':')[0];

                    // Create new storable object
                    JObject newStorable = new JObject
                    {
                        ["id"] = $"{prefix}:{name}",
                        ["plugins"] = new JObject
                        {
                            ["plugin#0"] = "Stopper.ClothingPluginManager.7:/Custom/Scripts/Stopper/ClothingPluginManager/ClothingPluginManager.cs"
                            //,["plugin#1"] = "Regguise.CustomShaderLoader.1:/Custom/Scripts/Regguise/CustomShaderLoader/CustomShaderLoader.cslist"
                        }
                    };

                    // Insert as the first item
                    storables.Insert(0, newStorable);

                    return true;
                }
                else
                {
                    message = "ID is missing or not in expected format.";
                    return false;
                }
            }
            else
            {
                message = "No 'storables' found.";
                return false;
            }
        }


        public void SetFlag(string key, bool value)
        {
            _root[key] = value.ToString().ToLower();
        }

        public void SetString(string key, string value)
        {
            _root[key] = value;
        }

        public void SetCustomOption(string key, bool value)
        {
            if (_root[str_customOptions] == null || _root[str_customOptions].Type != JTokenType.Object)
            {
                _root[str_customOptions] = new JObject();
            }

            JObject customOptions = (JObject)_root[str_customOptions];
            customOptions[key] = value.ToString().ToLower();
        }

        public string GetEditedJson()
        {
            return _root.ToString(Formatting.Indented);
        }

        public static void Test()
        {
            string path = "config.json";

            string json = File.ReadAllText(path);

            var editor = new JsonPrefsEditor(json);

            editor.SetFlag("pluginsAlwaysEnabled", true);
            editor.SetFlag("pluginsAlwaysDisabled", false);
            editor.SetFlag("ignoreMissingDependencyErrors", true);
            editor.SetCustomOption("preloadMorphs", false);

            string updatedJson = editor.GetEditedJson();
            File.WriteAllText(path, updatedJson);
        }
    }
}
