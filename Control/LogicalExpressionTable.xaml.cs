using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Vrm.Util;

//https://betterinformatics.com/resources/inf1-cl/venn/

namespace Vrm.Control
{
    public class PredicateCondition : INotifyPropertyChanged
    {
        public ICommand CmdUp {get;}
        public ICommand CmdDown {get;}

        public PredicateCondition()
        {
            CmdUp = new RelayCommand(x =>
            {
                OnUpDown?.Invoke(x as PredicateCondition, true);
            });
            CmdDown = new RelayCommand(x =>
            {
                OnUpDown?.Invoke(x as PredicateCondition, false);
            });
        }

        public Action<PredicateCondition, bool> OnUpDown;

        public string Name { get; set; }
        public Predicate<object> PredicateFunc { get; set; }

        private bool _and;
        public bool And
        {
            get => _and;
            set
            {
                if (_and != value)
                {
                    _and = value;
                    if (value) Or = false;
                    OnPropertyChanged(nameof(And));
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        private bool _or;
        public bool Or
        {
            get => _or;
            set
            {
                if (_or != value)
                {
                    _or = value;
                    if (value) And = false;
                    OnPropertyChanged(nameof(Or));
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        private bool _not;
        public bool Not
        {
            get => _not;
            set
            {
                _not = value;
                OnPropertyChanged(nameof(Not));
            }
        }

        public bool IsActive => And || Or;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public partial class LogicalExpressionTable : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty PredicatesProperty =
            DependencyProperty.Register("Predicates", typeof(ObservableCollection<PredicateCondition>), typeof(LogicalExpressionTable),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPredicatesChanged));

        public static readonly DependencyProperty ExprStringProperty =
            DependencyProperty.Register("ExprString", typeof(string), typeof(LogicalExpressionTable),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ExprProperty =
            DependencyProperty.Register("Expr", typeof(Predicate<object>), typeof(LogicalExpressionTable),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<PredicateCondition> Predicates
        {
            get => (ObservableCollection<PredicateCondition>)GetValue(PredicatesProperty);
            set => SetValue(PredicatesProperty, value);
        }

        public string ExprString
        {
            get => (string)GetValue(ExprStringProperty);
            private set => SetValue(ExprStringProperty, value);
        }

        public Predicate<object> Expr
        {
            get => (Predicate<object>)GetValue(ExprProperty);
            private set => SetValue(ExprProperty, value);
        }

        private string _emptyItemsTitle = "Empty";
        public string EmptyItemsTitle
        {
            get => _emptyItemsTitle;
            set
            {
                if (value == _emptyItemsTitle)
                    return;
                _emptyItemsTitle = value;
                OnPropertyChanged(nameof(EmptyItemsTitle));
            }
        }

        private string _firstColumnHeader = "Header text";
        public string FirstColumnHeader
        {
            get => _firstColumnHeader;
            set
            {
                if (value == _firstColumnHeader)
                    return;
                _firstColumnHeader = value;
                OnPropertyChanged(nameof(FirstColumnHeader));
            }
        }

        public LogicalExpressionTable()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewMouseDown += OnGlobalMouseDown;
                Application.Current.MainWindow.Deactivated += CustomWindow_Deactivated;
            }
            DataContext = this;
        }

        public event EventHandler<Predicate<object>> ExprChanged;

        private void InternalSetExpr(Predicate<object> newValue)
        {
            SetValue(ExprProperty, newValue);
            ExprChanged?.Invoke(this, newValue);
        }

        public event EventHandler FilterApplied;

        private void OnFilterApplied()
        {
            FilterApplied?.Invoke(this, EventArgs.Empty);
        }

        private void CustomWindow_Deactivated(object sender, EventArgs e)
        {
            if (_popup != null && _popup.IsOpen)
            {
                _popup.IsOpen = false;
                OnFilterApplied();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateExpression();
        }

        private static void OnPredicatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LogicalExpressionTable)d;

            if (e.OldValue is ObservableCollection<PredicateCondition> oldCollection)
            {
                oldCollection.CollectionChanged -= control.Predicates_CollectionChanged;
                foreach (var item in oldCollection)
                {
                    item.PropertyChanged -= control.Predicate_PropertyChanged;
                }
            }

            if (e.NewValue is ObservableCollection<PredicateCondition> newCollection)
            {
                newCollection.CollectionChanged += control.Predicates_CollectionChanged;
                foreach (var item in newCollection)
                {
                    item.PropertyChanged += control.Predicate_PropertyChanged;
                }
            }

            control.UpdateExpression();
        }

        private void Predicates_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (PredicateCondition item in e.OldItems)
                    item.PropertyChanged -= Predicate_PropertyChanged;

            if (e.NewItems != null)
                foreach (PredicateCondition item in e.NewItems)
                    item.PropertyChanged += Predicate_PropertyChanged;

            UpdateExpression();
        }

        private void Predicate_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PredicateCondition.And) ||
                e.PropertyName == nameof(PredicateCondition.Or) ||
                e.PropertyName == nameof(PredicateCondition.Not))
            {
                UpdateExpression();
            }
        }

        private void UpdateExpression()
        {
            UpdateExprString();
            UpdateExprDelegate();
        }

        public bool ModePreviousRow { get; set; } = false;

        private void UpdateExprString()
        {
            if (Predicates == null || Predicates.Count == 0)
            {
                ExprString = "";
                return;
            }

            var activePredicates = Predicates
                .Where(p => p.IsActive)
                .ToList();

            if (activePredicates.Count == 0)
            {
                ExprString = "";
                return;
            }

            // Processing the first predicate
            var firstPredicate = activePredicates[0];
            var expressionParts = new List<string>
            {
                firstPredicate.Not ? $"NOT {firstPredicate.Name}" : firstPredicate.Name
            };

            // Processing the remaining predicates with their operators
            for (int i = 1; i < activePredicates.Count; i++)
            {
                var current = activePredicates[i];
                var previous = activePredicates[i - 1];

                var predRow = ModePreviousRow ? previous : current;
                string op = predRow.And ? "AND" : "OR";

                string currentPart = current.Not ? $"NOT {current.Name}" : current.Name;

                expressionParts.Add(op);
                expressionParts.Add(currentPart);
            }

            // Optimizing the expression by grouping AND with higher priority
            ExprString = BuildGroupedExpression(expressionParts);
        }

        private Predicate<object> _nullPred = x => false; //null

        private void UpdateExprDelegate()
        {
            if (Predicates == null || Predicates.Count == 0)
            {
                InternalSetExpr(_nullPred);
                return;
            }

            var activePredicates = Predicates
                .Where(p => p.IsActive)
                .ToList();

            if (activePredicates.Count == 0)
            {
                InternalSetExpr(_nullPred);
                return;
            }

            // Starting with the first predicate
            Predicate<object> resultPredicate = GetCombinedPredicate(activePredicates[0]);

            // Combining with the remaining predicates
            for (int i = 1; i < activePredicates.Count; i++)
            {
                var current = activePredicates[i];
                var previous = activePredicates[i - 1];

                Predicate<object> currentPredicate = GetCombinedPredicate(current);

                var predRow = ModePreviousRow ? previous : current;
                if (predRow.And)
                {
                    resultPredicate = CombineWithAnd(resultPredicate, currentPredicate);
                }
                else
                {
                    resultPredicate = CombineWithOr(resultPredicate, currentPredicate);
                }
            }

            InternalSetExpr(resultPredicate);
        }

        private Predicate<object> CombineWithAnd(Predicate<object> left, Predicate<object> right)
        {
            return obj => left(obj) && right(obj);
        }

        private Predicate<object> CombineWithOr(Predicate<object> left, Predicate<object> right)
        {
            return obj => left(obj) || right(obj);
        }

        private Predicate<object> GetCombinedPredicate(PredicateCondition condition)
        {
            if (condition.PredicateFunc == null)
                return _ => false;

            return condition.Not
                ? obj => !condition.PredicateFunc(obj)
                : condition.PredicateFunc;
        }

        private string BuildGroupedExpression(List<string> parts)
        {
            if (parts.Count == 1) return parts[0];

            var andGroups = new List<List<string>>();
            var currentGroup = new List<string> { parts[0] };

            for (int i = 1; i < parts.Count; i += 2)
            {
                string op = parts[i];
                string term = parts[i + 1];

                if (op == "AND")
                {
                    currentGroup.Add(term);
                }
                else
                {
                    andGroups.Add(currentGroup);
                    currentGroup = new List<string> { term };
                }
            }

            andGroups.Add(currentGroup);

            // Assembling AND groups
            var andExpressions = andGroups
                .Select(g => g.Count > 1 ? $"({string.Join(" AND ", g)})" : g[0])
                .ToList();

            // Connecting all ORs
            return string.Join(" OR ", andExpressions);
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

                var rotate = this.FindName("ArrowRotate") as RotateTransform;
                if (rotate != null)
                    rotate.Angle = _popup.IsOpen ? 180 : 0;
            }
        }

        private void FilterPopup_Closed(object sender, EventArgs e)
        {
            var rotate = this.FindName("ArrowRotate") as RotateTransform;
            if (rotate != null)
                rotate.Angle = 0;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnGlobalMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_popup != null && _popup.IsOpen)
            {
                var clickedElement = Mouse.DirectlyOver as DependencyObject;

                // Checking: the click was outside the UserControl and outside the Popup
                if (!IsDescendantOfThis(clickedElement, this) &&
                    !IsDescendantOfThis(clickedElement, _popup.Child))
                {
                    _popup.IsOpen = false;
                    OnFilterApplied();
                }
            }
        }

        private bool IsDescendantOfThis(DependencyObject child, DependencyObject parent)
        {
            while (child != null)
            {
                if (child == parent)
                    return true;
                child = VisualTreeHelper.GetParent(child);
            }
            return false;
        }

        ~LogicalExpressionTable()
        {
            if (Application.Current?.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewMouseDown -= OnGlobalMouseDown;
                Application.Current.MainWindow.Deactivated -= CustomWindow_Deactivated;
            }
        }
    }
}