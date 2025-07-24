using System.Windows;
using Vrm.Util;

namespace Vrm.Window
{
    public partial class LicenseWindow : System.Windows.Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
            Owner = UiHelper.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
        }
    }
}
