using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace Vrm.Util
{
    public class ScrollToSelectedBehavior : Behavior<ListView>
    {
        public bool InvokeScroll
        {
            get => (bool)GetValue(InvokeScrollProperty);
            set => SetValue(InvokeScrollProperty, value);
        }

        public static readonly DependencyProperty InvokeScrollProperty =
            DependencyProperty.Register(
                nameof(InvokeScroll),
                typeof(bool),
                typeof(ScrollToSelectedBehavior),
                new PropertyMetadata(false, OnInvokeScrollChanged));

        private static void OnInvokeScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                var behavior = (ScrollToSelectedBehavior)d;
                behavior.ScrollToSelected();

                UiHelper.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Resetting the flag for re-invocation
                    d.SetValue(InvokeScrollProperty, false);  
                }), DispatcherPriority.ApplicationIdle);
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += OnSelectionChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollToSelected();
        }

        private void ScrollToSelected()
        {
            UiHelper.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (AssociatedObject?.SelectedItem == null)
                    return;

                AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);

                var item = AssociatedObject.ItemContainerGenerator.ContainerFromItem(AssociatedObject.SelectedItem) as ListViewItem;
                item?.Focus();
            }), DispatcherPriority.ApplicationIdle);
        }
    }
}
