using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Vrm.Control
{
    public partial class NumericTextBox : UserControl
    {
        private static int minInt = 1;
        private static string min = minInt.ToString();
        private static int maxInt = 9999;
        private static string max = maxInt.ToString();

        public NumericTextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(NumericTextBox),
                new FrameworkPropertyMetadata(min, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, Validate(value));
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericTextBox)d;
            string validated = Validate(e.NewValue as string);
            if (control.PART_TextBox.Text != validated)
                control.PART_TextBox.Text = validated;
        }

        private static string Validate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return min;

            string digitsOnly = new string(input.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digitsOnly))
                return min;

            if (int.TryParse(digitsOnly, out int num))
                return Math.Min(maxInt, num).ToString();

            return min;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!int.TryParse(Text, out int value))
                value = minInt;

            int delta = e.Delta > 0 ? 1 : -1;
            value = Math.Max(minInt, Math.Min(maxInt, value + delta));
            Text = value.ToString();
            e.Handled = true;
        }
    }
}
