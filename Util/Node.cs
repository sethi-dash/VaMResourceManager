using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vrm.Vam;

namespace Vrm.Util
{
    public class Node<T> : INotifyPropertyChanged
    {
        public T Value { get; set; }
        public List<Node<T>> Children {get; set;} = new List<Node<T>>();

        public string FullPath {get;set;}
        public string RelPath {get;set;}

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

        private bool _isMissing;
        public bool IsMissing
        {
            get => _isMissing;
            set => SetField(ref _isMissing, value);
        }

        public Node<T> Add(T value)
        {
            var node = new Node<T> { Value = value };
            Children.Add(node);
            return node;
        }

        public static Node<T> Build(T value, Func<T, IEnumerable<T>> getChildren)
        {
            var node = new Node<T> { Value = value };
            foreach (var c in getChildren(value))
            {
                node.Children.Add(Build(c, getChildren));
            }

            return node;
        }

        public static async Task<Node<T>> BuildTreeAsync(T rootValue, Func<T, Task<IEnumerable<T>>> getChildren)
        {
            return await Task.Run(() => BuildAsync(rootValue, getChildren));
        }

        private static async Task<Node<T>> BuildAsync(T value, Func<T, Task<IEnumerable<T>>> getChildren)
        {
            var node = new Node<T> { Value = value };

            foreach (var c in await getChildren(value))
                node.Children.Add(await BuildAsync(c, getChildren));

            return node;
        }

        public static IEnumerable<Node<T>> BuildTree(IEnumerable<T> topLevelItems, Func<T, IEnumerable<T>> getChildren)
        {
            return topLevelItems.Select(item => Build(item, getChildren));
        }

        public static Node<VarName> BuildVar(VarName item, Func<VarName, VarFile> getVar)
        {
            var node = new Node<VarName> { Value = item };
            var var = getVar(item);
            if (var.IsMissing)
            {
                node.IsMissing = true;
                node.FullPath = $"Missing:{item.FullName}";
                node.RelPath = $"Missing:{item.FullName}";
            }
            else
            {
                node.FullPath = var.Info.FullName;
                node.RelPath = var.RelativePath;
            }

            foreach (var d in var.Dependencies)
            {
                node.Children.Add(BuildVar(d, getVar));
            }

            return node;
        }

        public static List<Node<VarName>> BuildVarTree(IEnumerable<VarName> topLevelItems, Func<VarName, VarFile> getVar)
        {
            return topLevelItems.Select(item => BuildVar(item, getVar)).ToList();
        }

        public override string ToString()
        {
            return Value.ToString();
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

    public static class NodeHelper
    {
        public static Node<T> ConvertTree<TS, T>(Node<TS> source, Func<Node<TS>, T> converter)
        {
            if (source == null) 
                return null;

            var newNode = new Node<T>
            {
                Value = converter(source),
                IsExpanded = source.IsExpanded,
                IsMissing = source.IsMissing,
            };

            foreach (var child in source.Children)
            {
                var convertedChild = ConvertTree(child, converter);
                if (convertedChild != null)
                    newNode.Children.Add(convertedChild);
            }

            return newNode;
        }

        public static string PrintTreeFromList<T>(List<Node<T>> nodes)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < nodes.Count; i++)
            {
                bool isLast = i == nodes.Count - 1;
                sb.Append(PrintTree(nodes[i], "", isLast));
            }

            return sb.ToString();
        }

        public static string PrintTree<T>(Node<T> node, string indent = "", bool isLast = true)
        {
            if (node == null) return string.Empty;

            var sb = new StringBuilder();

            sb.Append(indent);
            sb.Append(isLast ? "└─" : "├─");
            sb.AppendLine(node.Value.ToString());

            indent += isLast ? "  " : "│ ";

            for (int i = 0; i < node.Children.Count; i++)
            {
                bool last = i == node.Children.Count - 1;
                sb.Append(PrintTree(node.Children[i], indent, last));
            }

            return sb.ToString();
        }
    }
}
