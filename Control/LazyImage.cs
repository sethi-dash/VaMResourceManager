using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Vrm.Util;

namespace Vrm.Control
{
    public class LazyImage : Image
    {
        public static readonly DependencyProperty SourcePathProperty =
            DependencyProperty.Register(nameof(SourcePath), typeof(string), typeof(LazyImage), new PropertyMetadata(null, OnSourcePathChanged));

        public string SourcePath
        {
            get => (string)GetValue(SourcePathProperty);
            set => SetValue(SourcePathProperty, value);
        }

        public int DecodePixelWidth = 512; //100

        private static void OnSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LazyImage)d).TryLoad().NoWarning();
        }

        private bool _loading = false;

        private BitmapImage _imgNoImage;

        private async Task TryLoad()
        {
            string path = SourcePath;

            if (_loading || string.IsNullOrEmpty(path)) //TODO remove _loading
                return;

            _loading = true;
            try
            {
                if (FileHelper.IsNoImagePath(path) || !FileHelper.FileExists(path))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if(_imgNoImage == null)
                                _imgNoImage = FileHelper.CreateImage(Settings.NoImagePath);
                            Source = _imgNoImage;
                        } catch { /**/ }
                    });
                }
                else
                {
                    var data = await Task.Run(() => FileHelper.ReadAllBytes(path));
                    if (data.Length == 0)
                        throw new Exception($"Cannot read file: {path}");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try { Source = FileHelper.CreateImage(data, DecodePixelWidth); } catch { /**/ }
                    });
                }
            }
            catch (Exception ex)
            {
                Settings.Logger.LogEx(ex);
            }
            finally
            {
                _loading = false;
            }
        }


        public LazyImage()
        {
            this.Loaded += (s, e) => TryLoad().NoWarning();
            this.IsVisibleChanged += (s, e) => TryLoad().NoWarning();
        }
    }

}
