using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Vrm.Util
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (bool)value;
            bool isInverse = parameter?.ToString().ToLowerInvariant() == "inverse";
            bool isHidden = parameter?.ToString().ToLowerInvariant() == "hidden";
        
            if (isInverse)
                isVisible = !isVisible;

            var notVisible = isHidden ? Visibility.Hidden : Visibility.Collapsed;

            return isVisible ? Visibility.Visible : notVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
