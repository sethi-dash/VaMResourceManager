using System;
using System.Collections.Generic;
using System.Linq;

namespace Vrm.Util
{
    public class PathNode
    {
        public string Name { get; set; }
        public Dictionary<string, PathNode> Children { get; set; } = new Dictionary<string, PathNode>();
        public bool IsFile { get; set; } = false;

        public void PrintTree(PathNode node, string indent = "")
        {
            foreach (var child in node.Children.Values.OrderBy(c => c.IsFile).ThenBy(c => c.Name))
            {
                Console.WriteLine($"{indent}{child.Name}{(child.IsFile ? "" : "/")}");
                if (!child.IsFile)
                    PrintTree(child, indent + "  ");
            }
        }

        public static PathNode BuildTree(HashSet<string> entries)
        {
            var root = new PathNode { Name = "" };

            foreach (var entry in entries)
            {
                var parts = entry.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var current = root;

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    bool isFile = (i == parts.Length - 1);

                    if (!current.Children.TryGetValue(part, out PathNode next))
                    {
                        next = new PathNode
                        {
                            Name = part,
                            IsFile = isFile
                        };
                        current.Children[part] = next;
                    }

                    current = next;
                }
            }

            return root;
        }

        public static Node<T> ConvertTree<T>(PathNode source, Func<PathNode, T> converter)
        {
            if (source == null) 
                return null;

            var newNode = new Node<T>
            {
                Value = converter(source),
            };

            foreach (var child in source.Children)
            {
                var convertedChild = ConvertTree<T>(child.Value, converter);
                if (convertedChild != null)
                    newNode.Children.Add(convertedChild);
            }

            return newNode;
        }
    }
}
