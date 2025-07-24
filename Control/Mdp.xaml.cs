using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Calendar = System.Windows.Controls.Calendar;

namespace Vrm.Control
{
    public class FixedMultiSelectCalendar : Calendar
    {
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            if (Mouse.Captured is CalendarItem)
            {
                Mouse.Capture(null);
            }
        }
    }

    public enum DateMode
    {
        [Display(Name = "All time")]
        AllTime,

        [Display(Name = "Modified (calendar)")]
        ModifiedCalendar,

        [Display(Name = "Created (calendar)")]
        CreatedCalendar,

        [Display(Name = "Modified (last N days)")]
        ModifiedLastNDays,

        [Display(Name = "Created (last N days)")]
        CreatedLastNDays
    }

    public partial class Mdp : UserControl
    {
        public DateMode Mode
        {
            get { return (DateMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(DateMode), typeof(Mdp), new PropertyMetadata(DateMode.AllTime, OnModeChanged));

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Mdp c)
            {
                c.Invalidate();
            }
        }

        #region 4 dates

        public DateTime? StartModified
        {
            get { return (DateTime?)GetValue(StartModifiedProperty); }
            set { SetValue(StartModifiedProperty, value); }
        }
        public static readonly DependencyProperty StartModifiedProperty =
            DependencyProperty.Register("StartModified", typeof(DateTime?), typeof(Mdp), new PropertyMetadata(null));


        public DateTime? EndModified
        {
            get { return (DateTime?)GetValue(EndModifiedProperty); }
            set { SetValue(EndModifiedProperty, value); }
        }
        public static readonly DependencyProperty EndModifiedProperty =
            DependencyProperty.Register("EndModified", typeof(DateTime?), typeof(Mdp), new PropertyMetadata(null));



        public DateTime? StartCreated
        {
            get { return (DateTime?)GetValue(StartCreatedProperty); }
            set { SetValue(StartCreatedProperty, value); }
        }
        public static readonly DependencyProperty StartCreatedProperty =
            DependencyProperty.Register("StartCreated", typeof(DateTime?), typeof(Mdp), new PropertyMetadata(null));


        public DateTime? EndCreated
        {
            get { return (DateTime?)GetValue(EndCreatedProperty); }
            set { SetValue(EndCreatedProperty, value); }
        }
        public static readonly DependencyProperty EndCreatedProperty =
            DependencyProperty.Register("EndCreated", typeof(DateTime?), typeof(Mdp), new PropertyMetadata(null));

        #endregion

        public DateTime? Start
        {
            get { return (DateTime?)GetValue(StartProperty); }
            set { SetValue(StartProperty, value); }
        }
        public static readonly DependencyProperty StartProperty =
            DependencyProperty.Register(nameof(Start), typeof(DateTime?), typeof(Mdp), new PropertyMetadata(null, OnStartChanged));

        private static void OnStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Mdp c)
                c.PART_Calendar.DisplayDateStart = (DateTime?)e.NewValue;
        }


        public DateTime? End
        {
            get { return (DateTime?)GetValue(EndProperty); }
            set { SetValue(EndProperty, value); }
        }
        public static readonly DependencyProperty EndProperty =
            DependencyProperty.Register(nameof(End), typeof(DateTime?), typeof(Mdp), new PropertyMetadata(null, OnEndChanged));

        private static void OnEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Mdp c)
                c.PART_Calendar.DisplayDateEnd = (DateTime?)e.NewValue;
        }


        public ObservableCollection<DateTime> SelectedDates
        {
            get { return (ObservableCollection<DateTime>)GetValue(SelectedDatesProperty); }
            set { SetValue(SelectedDatesProperty, value); }
        }
        public static readonly DependencyProperty SelectedDatesProperty =
            DependencyProperty.Register(nameof(SelectedDates), typeof(ObservableCollection<DateTime>), typeof(Mdp), new PropertyMetadata(null));


        public string ExprString
        {
            get { return (string)GetValue(ExprStringProperty); }
            set { SetValue(ExprStringProperty, value); }
        }
        public static readonly DependencyProperty ExprStringProperty =
            DependencyProperty.Register(nameof(ExprString), typeof(string), typeof(Mdp), new PropertyMetadata(null));


        public int LastN
        {
            get { return (int)GetValue(LastNProperty); }
            set { SetValue(LastNProperty, value); }
        }
        public static readonly DependencyProperty LastNProperty =
            DependencyProperty.Register(nameof(LastN), typeof(int), typeof(Mdp), new PropertyMetadata(1, OnLastNchanged));

        private static void OnLastNchanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Mdp c)
                c.Invalidate();
        }

        #region init

        public Mdp()
        {
            SelectedDates = new ObservableCollection<DateTime>();
            ExprString = "";

            InitializeComponent();
            Loaded += OnLoaded;
            PART_ClearButton.Click += PART_ClearButton_Click;
            DataContext = this;

            PART_Calendar.SelectedDatesChanged += (s, e) =>
            {
                foreach (DateTime addedDate in e.AddedItems)
                    SelectedDates.Add(addedDate);

                foreach (DateTime removedDate in e.RemovedItems)
                    SelectedDates.Remove(removedDate);

                Invalidate();
                OnFilterApplied();
            };

            Loaded += (s, e) =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                    mainWindow.LocationChanged += MainWindow_LocationChanged;
            };

            Unloaded += (s, e) =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                    mainWindow.LocationChanged -= MainWindow_LocationChanged;
            };

            Invalidate();
        }

        ~Mdp()
        {
            if (Application.Current?.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewMouseDown -= OnGlobalMouseDown;
                Application.Current.MainWindow.Deactivated -= CustomWindow_Deactivated;
                PART_ClearButton.Click -= PART_ClearButton_Click;
            }
        }

        #endregion

        private void Invalidate()
        {
            bool applied = Mode != DateMode.AllTime;
            PART_ClearButton.Visibility = applied ? Visibility.Visible : Visibility.Collapsed;
            _TextBlock.Foreground = applied ? Brushes.Black : Brushes.Gray;
            PART_Calendar.IsEnabled = (Mode == DateMode.ModifiedCalendar || Mode == DateMode.CreatedCalendar);
            PART_LastN.IsEnabled = (Mode == DateMode.CreatedLastNDays || Mode == DateMode.ModifiedLastNDays);

            switch (Mode)
            {
                case DateMode.AllTime:
                    ExprString = "All time";
                    break;
                case DateMode.ModifiedCalendar:
                    ExprString = SelectedDates.Count == 1 ? SelectedDates.First().ToShortDateString() : $"{SelectedDates.Count} days";
                    ExprString += " (modified)";
                    break;
                case DateMode.CreatedCalendar:
                    ExprString = SelectedDates.Count == 1 ? SelectedDates.First().ToShortDateString() : $"{SelectedDates.Count} days";
                    ExprString += " (created)";
                    break;
                case DateMode.ModifiedLastNDays:
                    ExprString = $"Last {LastN} days";
                    ExprString += " (modified)";
                    break;
                case DateMode.CreatedLastNDays:
                    ExprString = $"Last {LastN} days";
                    ExprString += " (created)";
                    break;
            }
        }

        public void SetMinMaxDates(DateTime? start, DateTime? end)
        {
            PART_Calendar.DisplayDateStart = start;
            PART_Calendar.DisplayDateEnd = end;
        }

        private void PART_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Mode = DateMode.AllTime;
            if (_popup != null && _popup.IsOpen)
                _popup.IsOpen = false;
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (_popup.IsOpen)
                _popup.IsOpen = false;
        }

        private void CustomWindow_Deactivated(object sender, EventArgs e)
        {
            if (_popup != null && _popup.IsOpen)
            {
                _popup.IsOpen = false;
                OnFilterApplied();
            }
        }

        private void OnFilterApplied()
        {
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            OnFilterApplied();
        }

        private void OnToggleClick(object sender, RoutedEventArgs e)
        {
            if (_popup != null)
            {
                if (_popup.IsOpen)
                {
                    _popup.IsOpen = false;
                    OnFilterApplied();
                }
                else
                {
                    _popup.PlacementTarget = this;
                    _popup.Placement = PlacementMode.Bottom;
                    _popup.HorizontalOffset = 0;
                    _popup.VerticalOffset = 0;
                    _popup.IsOpen = true;
                }
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            Application.Current.MainWindow.PreviewMouseDown += OnGlobalMouseDown;
            Application.Current.MainWindow.Deactivated += CustomWindow_Deactivated;
        }

        private void Popup_Closed(object sender, EventArgs e)
        {
            Application.Current.MainWindow.PreviewMouseDown -= OnGlobalMouseDown;
            Application.Current.MainWindow.Deactivated -= CustomWindow_Deactivated;
        }

        private void OnGlobalMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseOverTarget(_popup.Child as FrameworkElement, e) || IsMouseOverTarget(Root, e))
            {
                //Debug.WriteLine("over popup");
            }
            else
            {
                //Debug.WriteLine("not popup");
                if (_popup != null && _popup.IsOpen && !PART_CB.IsDropDownOpen)
                    _popup.IsOpen = false;
            }
        }

        private bool IsMouseOverTarget(FrameworkElement target, MouseButtonEventArgs e)
        {
            if (target == null || !target.IsVisible)
                return false;

            var clickedElement = Mouse.DirectlyOver as DependencyObject;

            while (clickedElement != null)
            {
                if (clickedElement == target)
                    return true;

                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            return false;
        }
    }
}