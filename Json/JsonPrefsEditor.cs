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

        private JObject _root;

        public JsonPrefsEditor(string json)
        {
            _root = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
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
