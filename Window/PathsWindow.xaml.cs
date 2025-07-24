using System.Collections.ObjectModel;
using System.Linq;
using Vrm.Vam;

namespace Vrm.Window
{
    public partial class PathsWindow : System.Windows.Window
    {
        public ObservableCollection<FolderPathItem> FolderPaths { get; set; }

        public PathsWindow()
        {
            InitializeComponent();
            DataContext = this;
            FolderPaths = new ObservableCollection<FolderPathItem>(Folders.Type2RelPath.Select(kv => new FolderPathItem { Type = kv.Key, Path = kv.Value }));
        }
    }

    public class FolderPathItem
    {
        public FolderType Type { get; set; }
        public string Path { get; set; }
    }
}
