using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using Vrm.Json;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmJsonTree : VmBase
    {
        public ObservableCollection<JsonNode> JsonNodes { get; } = new ObservableCollection<JsonNode>();

        private string _metaJson;
        public string MetaJson
        {
            get => _metaJson;
            set
            {
                if (SetField(ref _metaJson, value))
                {
                    JsonNodes.Clear();

                    if(string.IsNullOrWhiteSpace(value))
                        return;

                    try
                    {
                        var jtoken = JToken.Parse(value);
                        var builder = new JsonTreeBuilder();
                        var root = builder.BuildTree(jtoken, "Meta");

                        foreach (var item in root.Children)
                            JsonNodes.Add(item);
                    }
                    catch
                    {
                        JsonNodes.Add(new JsonNode(){Display = "<Corrupted meta.json>"});
                    }
                }
            }
        }

        private void Update()
        {
            if (SelectedItem != null && SelectedItem.IsVar)
                MetaJson = SelectedItem.Var.MetaJson;
            else
            {
                MetaJson = "";
                JsonNodes.Clear();
                JsonNodes.Add(new JsonNode(){Display = "No .var selected"});
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            Update();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        protected override void OnSelectedItemChanged()
        {
            Update();
        }

        public VmJsonTree()
        {
            Name = "Meta";
        }
    }
}
