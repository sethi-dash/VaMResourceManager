using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Vrm.Control
{
    public class WatermarkTextBox : TextBox
    {
        static WatermarkTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WatermarkTextBox), new FrameworkPropertyMetadata(typeof(WatermarkTextBox)));
            var dict = new ResourceDictionary
            {
                Source = new Uri("Control/WatermarkTextBoxStyle.xaml", UriKind.RelativeOrAbsolute)
            };

            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        public WatermarkTextBox()
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.PreviewMouseDown += OnGlobalMouseDown;
            KeyDown += Wtb_KeyDown;
        }

        private void Wtb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var binding = BindingOperations.GetBindingExpression((TextBox)sender, TextBox.TextProperty);
                binding?.UpdateSource();
            }
        }

        ~WatermarkTextBox()
        {
            if (Application.Current?.MainWindow != null)
                Application.Current.MainWindow.PreviewMouseDown -= OnGlobalMouseDown;
        }

        private void OnGlobalMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsKeyboardFocused)
                return;

            // Checking if the click was outside the current Wtb
            var clickedElement = e.OriginalSource as DependencyObject;
            if (!IsDescendantOfThisControl(clickedElement))
            {
                var scope = FocusManager.GetFocusScope(this);
                FocusManager.SetFocusedElement(scope, null);
                Keyboard.ClearFocus();
            }
        }

        private bool IsDescendantOfThisControl(DependencyObject clickedElement)
        {
            while (clickedElement != null)
            {
                if (clickedElement == this)
                    return true;

                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            return false;
        }

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(WatermarkTextBox), new PropertyMetadata(string.Empty));

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_ClearButton") is ButtonBase clearButton)
            {
                clearButton.Click += (s, e) =>
                {
                    this.Clear();
                    var scope = FocusManager.GetFocusScope(this);
                    FocusManager.SetFocusedElement(scope, null);
                    Keyboard.ClearFocus(); // Just in case
                };
            }
        }
    }

    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringIsNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;
            return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseStringIsNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WatermarkVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var text = values[0] as string;
            var isFocused = values[1] as bool? ?? false;

            return string.IsNullOrEmpty(text) && !isFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
