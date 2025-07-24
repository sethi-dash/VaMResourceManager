using System.Windows;
using Vrm.Util;

namespace Vrm.Window
{
    public partial class GuideWindow : System.Windows.Window
    {
        public GuideWindow()
        {
            InitializeComponent();
            Owner = UiHelper.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
        }
    }
}
