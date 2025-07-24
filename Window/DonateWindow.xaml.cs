using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Vrm.Util;

namespace Vrm.Window
{
    public partial class DonateWindow : System.Windows.Window
    {
        public DonateWindow()
        {
            InitializeComponent();

            qrBitcoin.Source = FileHelper.CreateImage(Settings.QrBtc);
            qrEth.Source = FileHelper.CreateImage(Settings.QrEth);
        }

        private async void CopyBitcoin_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(BitcoinAddress.Text);

            copiedBtc.Visibility = Visibility.Visible;
            copiedBtc.Opacity = 1;
            await FadeOut(copiedBtc, TimeSpan.FromSeconds(2));
        }

        private async void CopyEth_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(EthAddress.Text);
            copiedEth.Visibility = Visibility.Visible;
            copiedEth.Opacity = 1;
            await FadeOut(copiedEth, TimeSpan.FromSeconds(2));
        }

        private Task FadeOut(FrameworkElement el, TimeSpan time)
        {
            var tcs = new TaskCompletionSource<bool>();

            var animation = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(time),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) =>
            {
                el.Opacity = 0;
                el.Visibility = Visibility.Collapsed;
                tcs.SetResult(true);
            };

            el.BeginAnimation(UIElement.OpacityProperty, animation);

            return tcs.Task;
        }
    }
}
