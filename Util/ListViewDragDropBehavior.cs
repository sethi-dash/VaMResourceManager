using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Vrm.Util
{
    public static class ListViewDragDropBehavior
    {
        private static object _draggedItem;
        private static Point _startPoint;
        private static InsertionAdorner _insertionAdorner;
        private static ListViewItem _currentAdornerItem;
        private static bool _currentAdornerAbove;

        public static readonly DependencyProperty EnableDragDropProperty =
            DependencyProperty.RegisterAttached(
                "EnableDragDrop",
                typeof(bool),
                typeof(ListViewDragDropBehavior),
                new UIPropertyMetadata(false, OnEnableDragDropChanged));

        public static bool GetEnableDragDrop(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableDragDropProperty);
        }

        public static void SetEnableDragDrop(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableDragDropProperty, value);
        }

        private static void OnEnableDragDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listView = d as ListView;
            if (listView == null) return;

            if ((bool)e.NewValue)
            {
                listView.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
                listView.PreviewMouseMove += ListView_PreviewMouseMove;
                listView.DragOver += ListView_DragOver;
                listView.DragLeave += ListView_DragLeave;
                listView.Drop += ListView_Drop;
                listView.AllowDrop = true;
            }
            else
            {
                listView.PreviewMouseLeftButtonDown -= ListView_PreviewMouseLeftButtonDown;
                listView.PreviewMouseMove -= ListView_PreviewMouseMove;
                listView.DragOver -= ListView_DragOver;
                listView.DragLeave -= ListView_DragLeave;
                listView.Drop -= ListView_Drop;
                listView.AllowDrop = false;
            }
        }

        private static void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            var listView = sender as ListView;
            var item = GetListViewItemUnderMouse(listView, e.GetPosition(listView));
            if (item != null)
            {
                _draggedItem = item.DataContext;
            }
        }

        private static void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedItem == null || e.LeftButton != MouseButtonState.Pressed) return;

            Point currentPos = e.GetPosition(null);
            Vector diff = _startPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var listView = sender as ListView;
                DragDrop.DoDragDrop(listView, _draggedItem, DragDropEffects.Move);
            }
        }

        private static void ListView_DragOver(object sender, DragEventArgs e)
        {
            var listView = sender as ListView;
            var pos = e.GetPosition(listView);
            var itemContainer = GetListViewItemUnderMouse(listView, pos);

            if (itemContainer != null)
            {
                bool above = IsMouseAbove(itemContainer, pos);

                if (_currentAdornerItem != itemContainer || _currentAdornerAbove != above)
                {
                    RemoveAdorner();

                    _insertionAdorner = new InsertionAdorner(itemContainer, above);
                    _currentAdornerItem = itemContainer;
                    _currentAdornerAbove = above;
                }
            }
            else
            {
                // Attempt to insert before the first element
                if (listView.Items.Count > 0)
                {
                    var firstItemContainer = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    if (firstItemContainer != null)
                    {
                        double firstTop = firstItemContainer.TranslatePoint(new Point(0, 0), listView).Y;
                        if (pos.Y < firstTop)
                        {
                            bool above = true;

                            if (_currentAdornerItem != firstItemContainer || _currentAdornerAbove != above)
                            {
                                RemoveAdorner();

                                _insertionAdorner = new InsertionAdorner(firstItemContainer, above);
                                _currentAdornerItem = firstItemContainer;
                                _currentAdornerAbove = above;
                            }
                        }
                        else
                        {
                            RemoveAdorner();
                        }
                    }
                }
            }

            e.Effects = DragDropEffects.Move;
        }

        private static void ListView_DragLeave(object sender, DragEventArgs e)
        {
            RemoveAdorner();
        }

        private static void ListView_Drop(object sender, DragEventArgs e)
        {
            var listView = sender as ListView;
            var itemsSource = listView?.ItemsSource as IList;
            if (_draggedItem == null || itemsSource == null) return;

            var pos = e.GetPosition(listView);
            var targetItem = GetListViewItemUnderMouse(listView, pos);

            RemoveAdorner();

            int oldIndex = itemsSource.IndexOf(_draggedItem);
            int newIndex = itemsSource.Count;

            if (targetItem != null)
            {
                int targetIndex = itemsSource.IndexOf(targetItem.DataContext);
                bool insertAbove = IsMouseAbove(targetItem, pos);
                newIndex = insertAbove ? targetIndex : targetIndex + 1;
            }
            else
            {
                // Insert at the beginning if the cursor is above the first element
                if (listView.Items.Count > 0)
                {
                    var firstItemContainer = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    if (firstItemContainer != null)
                    {
                        double firstTop = firstItemContainer.TranslatePoint(new Point(0, 0), listView).Y;
                        if (pos.Y < firstTop)
                        {
                            newIndex = 0;
                        }
                    }
                }
            }

            if (newIndex > oldIndex) newIndex--; // Accounting for element removal

            if (oldIndex != newIndex && oldIndex >= 0 && newIndex >= 0 && newIndex <= itemsSource.Count)
            {
                itemsSource.Remove(_draggedItem);
                itemsSource.Insert(newIndex, _draggedItem);
            }

            _draggedItem = null;
        }

        private static bool IsMouseAbove(ListViewItem item, Point mousePosition)
        {
            var position = mousePosition.Y - item.TranslatePoint(new Point(0, 0), item).Y;
            return position < item.ActualHeight / 2;
        }

        private static void RemoveAdorner()
        {
            _insertionAdorner?.Detach();
            _insertionAdorner = null;
            _currentAdornerItem = null;
            _currentAdornerAbove = false;
        }

        private static ListViewItem GetListViewItemUnderMouse(ListView listView, Point point)
        {
            var element = listView.InputHitTest(point) as DependencyObject;
            while (element != null && !(element is ListViewItem))
            {
                element = VisualTreeHelper.GetParent(element);
            }
            return element as ListViewItem;
        }
    }

    public class InsertionAdorner : Adorner
    {
        private readonly bool _isInsertingAbove;
        private readonly AdornerLayer _adornerLayer;

        public InsertionAdorner(UIElement adornedElement, bool isInsertingAbove)
            : base(adornedElement)
        {
            _isInsertingAbove = isInsertingAbove;
            _adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);

            _adornerLayer.Add(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (AdornedElement is FrameworkElement element)
            {
                double width = element.ActualWidth;
                double y = _isInsertingAbove ? 0 : element.ActualHeight;

                Point start = new Point(0, y);
                Point end = new Point(width, y);

                Pen pen = new Pen(Brushes.Gray, 2);
                drawingContext.DrawLine(pen, start, end);
            }
        }

        public void Detach()
        {
            _adornerLayer?.Remove(this);
        }
    }
}
