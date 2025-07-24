using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Vrm.Util
{
    public static class UiHelper
    {
        private static Dispatcher _dispatcher;
        public static Dispatcher Dispatcher => _dispatcher;

        /// <summary>
        /// Called from the main UI thread (usually in App.xaml.cs or in a window).
        /// </summary>
        public static void Initialize()
        {
            _dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("Dispatcher is not available. Ensure WPF Application is initialized.");
        }

        public static void Invoke(Action action)
        {
            if (_dispatcher == null)
                throw new InvalidOperationException("MainThreadDispatcher is not initialized.");

            if (_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                _dispatcher.Invoke(action);
            }
        }

        public static Task InvokeAsync(Action action)
        {
            if (_dispatcher == null)
                throw new InvalidOperationException("MainThreadDispatcher is not initialized.");

            return _dispatcher.InvokeAsync(action).Task;
        }

        public static Task<T> InvokeAsync<T>(Func<T> func)
        {
            if (_dispatcher == null)
                throw new InvalidOperationException("MainThreadDispatcher is not initialized.");

            return _dispatcher.InvokeAsync(func).Task;
        }

        public static System.Windows.Window MainWindow => Application.Current.MainWindow;

        public static void MoveItemUp<T>(this ObservableCollection<T> collection, T item)
        {
            int oldIndex = collection.IndexOf(item);
            if (oldIndex > 0)
            {
                collection.Move(oldIndex, oldIndex - 1);
            }
        }

        public static void MoveItemDown<T>(this ObservableCollection<T> collection, T item)
        {
            int oldIndex = collection.IndexOf(item);
            if (oldIndex < collection.Count - 1)
            {
                collection.Move(oldIndex, oldIndex + 1);
            }
        }
    }

}
