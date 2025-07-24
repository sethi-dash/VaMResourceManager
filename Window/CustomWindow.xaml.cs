using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Vrm.Control;
using Vrm.Util;
using Vrm.Vm;

namespace Vrm.Window
{
    public partial class CustomWindow : System.Windows.Window
    {
        public CustomWindow()
        {
            InitializeComponent();

            this.MouseDoubleClick += MainWindow_MouseDoubleClick;
            this.MouseLeftButtonUp += MainWindow_MouseLeftButtonDown;
            this.Closing += MainWindow_Closing;
            this.Loaded += MainWindow_Loaded;
        }

        #region Refs

        public bool IsEditMode
        {
            get { return (bool)GetValue(IsEditModeProperty); }
            set { SetValue(IsEditModeProperty, value); }
        }
        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(CustomWindow), new PropertyMetadata(false));


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is VmMain vm)
            {
                vm.Refs.PropertyChanged += VmRefs_PropertyChanged;
                vm.PropertyChanged += Vm_PropertyChanged;
                vm.Refs.Load();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is VmMain vm)
            {
                vm.Refs.Save();
                vm.Refs.PropertyChanged -= VmRefs_PropertyChanged;
                vm.PropertyChanged -= Vm_PropertyChanged;
            }
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VmMain.SelectionMode))
                SetValue(IsEditModeProperty, ((VmMain)sender).SelectionMode);
        }

        private void VmRefs_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VmRefs.IsEditMode))
                SetValue(IsEditModeProperty, ((VmRefs)sender).IsEditMode);
        }

        #endregion

        #region Image Viewer

        private System.Windows.Window _window;

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_window == null)
                return;
            Point clickPosition = e.GetPosition(this);
            var hitElement = GetElementUnderMouse(this, clickPosition);
            if (hitElement != null)
            {
                if (hitElement is FrameworkElement fe && fe.DataContext is VmImageElement im)
                {
                    var c = new LazyImage();
                    c.DecodePixelWidth = 512;
                    c.SourcePath = im.ImagePath;
                    _window.Content = c;
                    _window.Title = im.Name;
                }
            }
        }

        private void MainWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(this);
            var hitElement = GetElementUnderMouse(this, clickPosition);
            if (hitElement != null)
            {
                if (hitElement is FrameworkElement fe && fe.DataContext is VmImageElement im)
                {
                    var window = _window ?? CreateWindow();
                    window.Closed += Window_Closed;
                    window.Title = "Image Viewer";
                    window.Width = 512;
                    window.Height = 512;
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    var c = new LazyImage();
                    c.DecodePixelWidth = 512;
                    c.SourcePath = im.ImagePath;

                    window.Content = c;
                    window.Show();
                    window.Activate();

                    _window = window;
                }
                else if (hitElement is FrameworkElement fe1 && fe1.DataContext is VmRefItem itemVm)
                {
                    if (this.DataContext is VmMain mainVm)
                    {
                        if(mainVm.Refs.CmdRename.CanExecute(itemVm))
                            mainVm.Refs.CmdRename.Execute(itemVm);
                    }
                }
                else if(hitElement is FrameworkElement fe2 && fe2.DataContext is Node<DepsTreeItem> node)
                {
                    if (node.Value.IsRefItemCfg)
                    {

                    }
                }
            }
        }

        private System.Windows.Window CreateWindow()
        {
            var window = new System.Windows.Window();
            window.Title = "Image Viewer";
            window.Width = 512;
            window.Height = 512;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Topmost = true;
            return window;
        }

        private DependencyObject GetElementUnderMouse(Visual reference, Point position)
        {
            HitTestResult result = VisualTreeHelper.HitTest(reference, position);
            return result?.VisualHit;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _window = null;
        }

        #endregion

        #region window commands

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    // Double-click — maximize or restore
                    WindowState = (WindowState == WindowState.Maximized)
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
                else
                {
                    // Single-click — dragging
                    DragMove();
                }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();

            var item1 = new MenuItem { Header = "About" };
            item1.Click += (s, args) => TextBoxDialog.ShowDialog("About", 
                $"VaM Resource Manager\n" +
                $"Version 1.0\n" +
                $"Open Source: https://github.com/sethi-dash/VamResourceManager\n"+
                $"License: GNU General Public License v3.0\n" +
                $"Author: sethi_dash@outlook.com");

            var item2 = new MenuItem { Header = "Dependencies" };
            item2.Click += (s, args) => new LicenseWindow().ShowDialog();

            var item3 = new MenuItem { Header = "Paths" };
            item3.Click += (s, args) => new PathsWindow().ShowDialog();

            var item4 = new MenuItem { Header = "Guide" };
            item4.Click += (s, args) => new GuideWindow().ShowDialog();

            var item5 = new MenuItem { Header = "Donate" };
            item5.Click += (s, args) => new DonateWindow().ShowDialog();

            contextMenu.Items.Add(item1);
            contextMenu.Items.Add(item2);
            contextMenu.Items.Add(item3);
            contextMenu.Items.Add(item4);
            contextMenu.Items.Add(item5);

            contextMenu.PlacementTarget = sender as UIElement;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        #endregion

        #region resize

        private void Resize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var element = sender as FrameworkElement;
            if (element == null) return;

            int direction = HTCAPTION;

            switch (element.Name)
            {
                case "LeftGrip":
                    direction = HTLEFT;
                    break;
                case "RightGrip":
                    direction = HTRIGHT;
                    break;
                case "TopGrip":
                    direction = HTTOP;
                    break;
                case "BottomGrip":
                    direction = HTBOTTOM;
                    break;
                case "TopLeftGrip":
                    direction = HTTOPLEFT;
                    break;
                case "TopRightGrip":
                    direction = HTTOPRIGHT;
                    break;
                case "BottomLeftGrip":
                    direction = HTBOTTOMLEFT;
                    break;
                case "BottomRightGrip":
                    direction = HTBOTTOMRIGHT;
                    break;
            }

            ResizeWindow(direction);
        }

        private void ResizeWindow(int direction)
        {
            SendMessage(new WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, direction, 0);
        }

        // WinAPI
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCAPTION = 2;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Optional: to eliminate visual artifacts on Win10
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return IntPtr.Zero;
        }

        #endregion
    }
}