using System;
using System.Globalization;
using System.Windows.Data;

namespace Vrm.Util
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
                return (bytes / 1024.0 / 1024.0).ToString("F2") + " MB";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public static FileSizeConverter Default { get; } = new FileSizeConverter();
    }
}
