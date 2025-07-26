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
using WpfToolkit.Controls;

namespace Vrm.Util
{
    public class UpdateLayoutBehavior : Behavior<ListView>
    {
        public bool Invoke
        {
            get => (bool)GetValue(InvokeProperty);
            set => SetValue(InvokeProperty, value);
        }

        public static readonly DependencyProperty InvokeProperty =
            DependencyProperty.Register(
                nameof(Invoke),
                typeof(bool),
                typeof(UpdateLayoutBehavior),
                new PropertyMetadata(false, OnInvokeChanged));

        private static void OnInvokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                var behavior = (UpdateLayoutBehavior)d;
                behavior.Update();

                UiHelper.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Resetting the flag for re-invocation
                    d.SetValue(InvokeProperty, false);  
                }), DispatcherPriority.ApplicationIdle);
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }


        private void Update()
        {
            UiHelper.Dispatcher.BeginInvoke(new Action(() =>
            {
                if(AssociatedObject == null)
                    return;

                AssociatedObject.UpdateLayout();
                var p = GetWrapPanelFromListView(AssociatedObject);
                if (p != null)
                {
                    p.ItemSize = new Size(UIState.Instance.ImgSize, UIState.Instance.ImgSize);
                }

            }), DispatcherPriority.ApplicationIdle);
        }

        private VirtualizingWrapPanel GetWrapPanelFromListView(ListView listView)
        {
            if (listView == null)
                return null;

            var itemsPresenter = UiHelper.FindVisualChild<ItemsPresenter>(listView);
            if (itemsPresenter == null)
                return null;

            var panel = UiHelper.FindVisualChild<VirtualizingWrapPanel>(itemsPresenter);
            return panel;
        }
    }
}
