using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Vrm.Vm;

namespace Vrm.Util
{
    public class RefOpsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is VmRefItemOps op)
            {
                if (parameter.ToString() == "end")
                {
                    if (op == VmRefItemOps.End)
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
                else if (parameter.ToString() == "btn")
                {
                    if(op == VmRefItemOps.All)
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
