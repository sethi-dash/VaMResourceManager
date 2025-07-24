using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace Vrm.Json
{
    public class JsonNode : INotifyPropertyChanged
    {
        public string Display { get; set; }
        public List<JsonNode> Children { get; set; } = new List<JsonNode>();
        public bool HasChildren => Children.Count > 0;

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    public class JsonTreeBuilder
    {
        public JsonNode BuildTree(JToken token, string key = null)
        {
            if (token == null)
                return new JsonNode { Display = $"{key}: null" };

            if (token is JValue value)
            {
                return new JsonNode
                {
                    Display = key != null ? $"{key}: {value}" : value.ToString()
                };
            }

            var node = new JsonNode { Display = key ?? token.Type.ToString() };

            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                    node.Children.Add(BuildTree(prop.Value, prop.Name));
            }
            else if (token is JArray array)
            {
                int index = 0;
                foreach (var item in array)
                    node.Children.Add(BuildTree(item, $"[{index++}]"));
            }

            return node;
        }
    }

}