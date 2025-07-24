using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Vrm.Util;

namespace Vrm.Window
{
    public partial class TextBoxDialog : System.Windows.Window
    {
        public TextBoxDialog(IEnumerable<string> items)
        {
            InitializeComponent();
            MyTextBox.Text = string.Join(Environment.NewLine, items);
            Owner = UiHelper.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
        }


        public bool ShowOkBtn
        {
            get => btn_ok.Visibility == Visibility.Visible;
            set
            {
                btn_ok.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool ShowSystemBtns
        {
            get => WindowStyle != WindowStyle.None;
            set
            {
                WindowStyle = value ? WindowStyle.SingleBorderWindow : WindowStyle.None;
            }
        }

        public TextBoxDialog(string title, IEnumerable<string> items) : this(items)
        {
            Title = title;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        public static bool? ShowDialog(string title, IEnumerable<string> items)
        {
            return new TextBoxDialog(title, items).ShowDialog();
        }

        public static bool? ShowDialog(string title, string msg)
        {
            return new TextBoxDialog(title, new []{msg}).ShowDialog();
        }

        public static TextBoxDialog ShowWindow(string title, string msg)
        {
            return new TextBoxDialog(title, new []{msg});
        }

        public static TextBoxDialog ShowWindow(string title, params string[] items)
        {
            return new TextBoxDialog(title, items);
        }

        public static bool? ShowDialog(string title, string msg, IEnumerable<string> items)
        {
            return new TextBoxDialog(title, new []{msg}.Concat(items)).ShowDialog();
        }

        public static TextBoxDialog ShowProgress(string title)
        {
            var w = ShowWindow("", title, "Please wait...");
            w.ShowOkBtn = false;
            w.ShowSystemBtns = false;
            w.Topmost = true;
            w.Owner.IsEnabled = false;
            w.Show();
            w.Closed += (s, e) => { w.Owner.IsEnabled = true; };
            return w;
        }
    }
}