using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Vrm.Cfg;
using Vrm.Control;
using Vrm.Refs;
using Vrm.Util;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmMain : VmBase
    {
        private const int C_LEFT_PANE_WIDTH = 230;

        #region common

        public ShowTools ShowTools;

        private string _title = Settings.AppName;
        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        public VmProgress Progress { get; } = new VmProgress();

        protected override void UpdateStatus()
        {
            SelectedTab?.OnUpdateTools(ShowTools);
        }

        #endregion

        #region lar filter

        private Predicate<object> _laPred;

        private bool _showLaFilter;
        public bool ShowLaFilter
        {
            get => _showLaFilter;
            set => SetField(ref _showLaFilter, value);
        }

        public ICommand CmdOnLaFilterApplied { get; }
        public ICommand CmdOnLaFilterLoaded { get; }
        public ObservableCollection<PredicateCondition> LaFilterPreds { get; } = new ObservableCollection<PredicateCondition>();

        #endregion

        #region fav-hide filter

        private Predicate<object> _favHidePred;

        private bool _showFavHideFilter;
        public bool ShowFavHideFilter
        {
            get => _showFavHideFilter;
            set => SetField(ref _showFavHideFilter, value);
        }

        public ICommand CmdOnFavHideFilterApplied { get; }
        public ICommand CmdOnFavHideFilterLoaded { get; }
        public ObservableCollection<PredicateCondition> FavHideFilterPreds { get; } = new ObservableCollection<PredicateCondition>();

        #endregion

        #region creator filter

        private Predicate<object> _creatorFilterPred;
        private string _creatorFilter;
        public string CreatorFilter
        {
            get => _creatorFilter;
            set
            {
                if (SetField(ref _creatorFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        private bool _showCreatorFilter;
        public bool ShowCreatorFilter
        {
            get => _showCreatorFilter;
            set => SetField(ref _showCreatorFilter, value);
        }

        #endregion

        #region name filter

        private Predicate<object> _nameFilterPred;
        private string _nameFilter;
        public string NameFilter
        {
            get => _nameFilter;
            set
            {
                if (SetField(ref _nameFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        private bool _showNameFilter;
        public bool ShowNameFilter
        {
            get => _showNameFilter;
            set => SetField(ref _showNameFilter, value);
        }

        #endregion

        #region version filter

        private Predicate<object> _versionFilterPred;

        private int? _versionIntFilter;
        public int? VersionIntFilter
        {
            get => _versionIntFilter;
            set
            {
                if (value == _versionIntFilter)
                    return;
                _versionIntFilter = value;
                OnPropertyChanged();

                ApplyFilter();
            }
        }


        private string _versionFilter;
        public string VersionFilter
        {
            get => _versionFilter;
            set
            {
                if (SetField(ref _versionFilter, value))
                {
                    if (Int32.TryParse(value, out var v) && v > 0)
                    {
                        VersionIntFilter = v;
                    }
                    else
                    {
                        VersionIntFilter = null;
                        VersionFilter = "";
                    }
                }
            }
        }

        private bool _showVersionFilter;
        public bool ShowVersionFilter
        {
            get => _showVersionFilter;
            set => SetField(ref _showVersionFilter, value);
        }

        #endregion

        #region tag filter

        private Predicate<object> _tagFilterPred;
        private string _tagFilter;
        public string TagFilter
        {
            get => _tagFilter;
            set
            {
                if (SetField(ref _tagFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        private bool _showTagFilter;
        public bool ShowTagFilter
        {
            get => _showTagFilter;
            set => SetField(ref _showTagFilter, value);
        }

        #endregion

        #region date filter

        private Predicate<object> _datesFilterPred;

        private bool _showDates;
        public bool ShowDates
        {
            get => _showDates;
            set => SetField(ref _showDates, value);
        }

        public ObservableCollection<DateTime> Dates { get; } = new ObservableCollection<DateTime>();
        private HashSet<DateTime> _datesHash = new HashSet<DateTime>();

        private DateMode _dateMode;
        public DateMode DateMode
        {
            get => _dateMode;
            set
            {
                if (SetField(ref _dateMode, value))
                {
                    ApplyFilter();
                }
            }
        }

        private int _lastNDays = 1;
        public int LastNDays
        {
            get => _lastNDays;
            set
            {
                if (SetField(ref _lastNDays, value))
                {
                    ApplyFilter();
                }
            }
        }


        private DateTime? _dateStartCreated;
        public DateTime? DateStartCreated
        {
            get => _dateStartCreated;
            set => SetField(ref _dateStartCreated, value);
        }
        private DateTime? _dateEndCreated;
        public DateTime? DateEndCreated
        {
            get => _dateEndCreated;
            set => SetField(ref _dateEndCreated, value);
        }

        private DateTime? _dateStartModified;
        public DateTime? DateStartModified
        {
            get => _dateStartModified;
            set => SetField(ref _dateStartModified, value);
        }
        private DateTime? _dateEndModified;
        public DateTime? DateEndModified
        {
            get => _dateEndModified;
            set => SetField(ref _dateEndModified, value);
        }

        #endregion

        #region gender clothing and hair filter

        private Predicate<object> _genderFilterPred;

        public VmCheckedCb GenderFilter { get; }

        #endregion

        #region grouping

        public VmCheckedCb Grouping { get; }

        private bool _showGrouping;
        public bool ShowGrouping
        {
            get => _showGrouping;
            set => SetField(ref _showGrouping, value);
        }

        #endregion

        #region sorting

        public VmCheckedCb Sorting { get; }

        private bool _showSort;
        public bool ShowSort
        {
            get => _showSort;
            set => SetField(ref _showSort, value);
        }

        private bool _isSortAllowed = true;

        public void ApplySortFromConfig()
        {
            _isSortAllowed = false;
            try
            {
                foreach (var item in Settings.Config.Sort)
                {
                    foreach (var cb in Sorting.Items)
                    {
                        if (item.Name == cb.Name && item.Asc == cb.Asc)
                        {
                            cb.IsChecked = true;
                            break;
                        }
                    }
                }
                Sorter.Default.Sort(Sorting.Items2, ToolVar?.Tabs.Concat(ToolUserData?.Tabs));
            }
            finally
            {
                _isSortAllowed = true;
            }
        }

        #endregion

        #region selection

        public static string NameConditionShowOnlyCheckedElements = "Show only checked elements";
        private Predicate<object> _showOnlyCheckedElementsPred;
        private bool _showSelection;
        public bool ShowSelection
        {
            get => _showSelection;
            set => SetField(ref _showSelection, value);
        }

        public VmCheckedCb Selection { get; }
        public ICommand CmdCheckAllVisible { get; }
        public ICommand CmdUncheckAllVisible { get; }
        public ICommand CmdCheckAllVisibleThisTab { get; }
        public ICommand CmdUncheckAllVisibleThisTab { get; }
        public ICommand CmdCheckAllVisibleVarTab { get; }
        public ICommand CmdUncheckAllVisibleVarTab { get; }
        public ICommand CmdCheckAllVisibleUserDataTab { get; }
        public ICommand CmdUncheckAllVisibleUserDataTab { get; }

        #endregion

        #region categories

        public ICommand CmdCheckAllCatsVar;
        public ICommand CmdUncheckAllCatsVar;
        public ICommand CmdCheckAllCatsUserData;
        public ICommand CmdUncheckAllCatsUserData;

        public VmCheckedCb CatsVar { get; }
        private bool _showVarCats;
        public bool ShowVarCats
        {
            get => _showVarCats;
            set => SetField(ref _showVarCats, value);
        }

        public VmCheckedCb CatsUserData { get; }
        private bool _showUserDataCats;
        public bool ShowUserDataCats
        {
            get => _showUserDataCats;
            set => SetField(ref _showUserDataCats, value);
        }

        #endregion

        #region extra

        public ICommand CmdStartSelection {get;}
        public ICommand CmdEndSelection {get;}
        public VmCmdBtn CmdSelection {get;}
        public ICommand CmdClear_All_Filters {get;}
        public ICommand CmdClear_CreatorNameVersion_Filters {get;}

        public VmCheckedCb ExtraTools { get; }

        #endregion

        #region tabs

        public VmToolVam ToolVam;
        public VmToolVar ToolVar;
        public VmToolUserData ToolUserData;
        public VmToolLogger ToolLogger;

        #endregion

        #region ref panel

        private bool _isReferencesEnabled = true;
        public bool IsReferencesEnabled
        {
            get => _isReferencesEnabled;
            set
            {
                if (value == _isReferencesEnabled)
                    return;
                _isReferencesEnabled = value;
                OnPropertyChanged(nameof(IsReferencesEnabled));

                UpdateReferencesPanelWidth();

                _selectionMode = false;
                OnUpdateSelectionMode();
                OnPropertyChanged(nameof(SelectionMode));
            }
        }

        private void UpdateReferencesPanelWidth()
        {
            if (_isReferencesEnabled)
            {
                PanelWidth = _savedPanelWidth;
            }
            else
            {
                _savedPanelWidth = PanelWidth;
                PanelWidth = new GridLength(0);
            }
        }

        private GridLength _panelWidth = new GridLength(C_LEFT_PANE_WIDTH);
        public GridLength PanelWidth
        {
            get => _panelWidth;
            set
            {
                if (_panelWidth != value)
                {
                    if (IsReferencesEnabled && value.Value < 50)
                        value = new GridLength(50);
                    _panelWidth = value;
                    OnPropertyChanged(nameof(PanelWidth));
                }
            }
        }

        private GridLength _savedPanelWidth = new GridLength(C_LEFT_PANE_WIDTH);

        public VmRefs Refs { get; } = new VmRefs();

        #endregion

        #region selection mode

        private bool _selectionMode;
        public bool SelectionMode
        {
            get => _selectionMode;
            set
            {
                if (value == _selectionMode)
                    return;
                _selectionMode = value;
                OnPropertyChanged();

                OnUpdateSelectionMode();

                if(value && _isReferencesEnabled)
                {
                    _isReferencesEnabled = false;
                    UpdateReferencesPanelWidth();
                }
                OnPropertyChanged(nameof(IsReferencesEnabled));
            }
        }

        private void OnUpdateSelectionMode()
        {
            ShowSelection = _selectionMode;
        }

        #endregion

        #region BrowsedVar

        private Predicate<object> _browsedVarFilterPred;
        private VarFile _browsedVar;
        public VarFile BrowsedVar
        {
            get => _browsedVar;
            set
            {
                if (Equals(value, _browsedVar))
                    return;
                _browsedVar = value;
                OnPropertyChanged();
                ApplyFilter(FilterMode.BrowsedVar);
            }
        }

        #endregion

        #region init

        public VmMain()
        {
            Logger = Settings.Logger;
            Refs.MainVm = this;

            #region grouping

            Grouping = new VmCheckedCb
            {
                ByText = "Grouped by ",
                NothingCheckedText = "Group",
                StaticItems = true,
                OnChanged = (item) =>
                {
                    SetGrouping(item.IsChecked, item.Name, ToolVar.Tabs);
                }
            };
            Grouping.Items.Add(new CbOption(nameof(VmImageElement.Creator), null, false, null));
            Grouping.Items.Add(new CbOption(nameof(VmImageElement.Package), null, false, null));
            Grouping.UpdateIsFilterEnabled();

            #endregion
            #region sorting

            Sorting = new VmCheckedCb
            {
                ByText = "Sorted by ",
                NothingCheckedText = "Sort",
                StaticItems = false,
                OnChanged = (item) =>
                {
                    if (!_isSortAllowed)
                        return;
                    Sorter.Default.Sort(Sorting.Items2, ToolVar?.Tabs.Concat(ToolUserData?.Tabs));
                    Settings.Config.Sort = Sorting.Items2.Select(x => new SortItemCfg(x.Name, x.Asc.Value)).ToList();
                },
                OnToggled = (item) =>
                {
                    if (!_isSortAllowed)
                        return;
                    Sorter.Default.Sort(Sorting.Items2, ToolVar?.Tabs.Concat(ToolUserData?.Tabs));
                    Settings.Config.Sort = Sorting.Items2.Select(x => new SortItemCfg(x.Name, x.Asc.Value)).ToList();
                }
            };
            Sorting.Items.Add(new CbOption(Sorter.Name, false, false, "name"));
            Sorting.Items.Add(new CbOption(Sorter.Name, true, false, "name"));
            Sorting.Items.Add(new CbOption(Sorter.Creator, false, false, "creator"));
            Sorting.Items.Add(new CbOption(Sorter.Creator, true, false, "creator"));
            Sorting.Items.Add(new CbOption(Sorter.Size, false, false, "size"));
            Sorting.Items.Add(new CbOption(Sorter.Size, true, false, "size"));
            Sorting.Items.Add(new CbOption(Sorter.Created, false, false, "created"));
            Sorting.Items.Add(new CbOption(Sorter.Created, true, false, "created"));
            Sorting.Items.Add(new CbOption(Sorter.Modified, false, false, "modified"));
            Sorting.Items.Add(new CbOption(Sorter.Modified, true, false, "modified"));
            Sorting.UpdateIsFilterEnabled();

            #endregion
            #region selection

            CmdCheckAllVisible = new RelayCommand(x =>
            {
                SetChecks(true, true);
            }, x => true);
            CmdUncheckAllVisible = new RelayCommand(x =>
            {
                SetChecks(true, false);
            }, x => true);

            CmdCheckAllVisibleThisTab = new RelayCommand(x =>
            {
                if (SelectedTab is VmToolUserData vm)
                    vm.SelectedTab.SetChecks(true, true);
                if (SelectedTab is VmToolVar vmv)
                    vmv.SelectedTab.SetChecks(true, true);
            }, x => true);
            CmdUncheckAllVisibleThisTab = new RelayCommand(x =>
            {
                if (SelectedTab is VmToolUserData vm)
                    vm.SelectedTab.SetChecks(true, false);
                if (SelectedTab is VmToolVar vmv)
                    vmv.SelectedTab.SetChecks(true, false);
            }, x => true);

            CmdCheckAllVisibleVarTab = new RelayCommand(x =>
            {
                ToolVar.SetChecks(true, true);
            }, x => true);
            CmdUncheckAllVisibleVarTab = new RelayCommand(x =>
            {
                ToolVar.SetChecks(true, false);
            }, x => true);

            CmdCheckAllVisibleUserDataTab = new RelayCommand(x =>
            {
                ToolUserData.SetChecks(true, true);
            }, x => true);
            CmdUncheckAllVisibleUserDataTab = new RelayCommand(x =>
            {
                ToolUserData.SetChecks(true, false);
            }, x => true);

            Selection = new VmCheckedCb
            {
                StaticText = "Select",
                IsFilterCanBeEnabledOrDisabled = false,
                OnChanged = (item) =>
                {
                    ApplyFilter();
                }
            };
            Selection.Items.Add(new CbOption(NameConditionShowOnlyCheckedElements, null, false, null));
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Check all (this tab)", CmdCheckAllVisibleThisTab) });
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Uncheck all (this tab)", CmdUncheckAllVisibleThisTab) });
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Check all (var)", CmdCheckAllVisibleVarTab) });
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Uncheck all (var)", CmdUncheckAllVisibleVarTab) });
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Check all (user data)", CmdCheckAllVisibleUserDataTab) });
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Uncheck all (user data)", CmdUncheckAllVisibleUserDataTab) });
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Check all", CmdCheckAllVisible) });
            Selection.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Uncheck all", CmdUncheckAllVisible) });

            _showOnlyCheckedElementsPred = x =>
            {
                if (x is VmElementBase el)
                    return el.IsChecked;
                else
                    return false;
            };

            #endregion
            #region cats

            CmdCheckAllCatsVar = new RelayCommand(x =>
            {
                SetAllTabsOptionVisibility(CatsVar, true);
            }, x => true);
            CmdUncheckAllCatsVar = new RelayCommand(x =>
            {
                SetAllTabsOptionVisibility(CatsVar, false);
            }, x => true);
            CatsVar = new VmCheckedCb
            {
                StaticText = $"Tabs",
                FooterText = "Hidden tabs do not receive any data during scan",
                IsFilterCanBeEnabledOrDisabled = false,
                OnChanged = (item) =>
                {
                    SetTabVisibility(ToolVar, item);
                    UpdateVarTabName();
                    ToolVar.UpdateName();
                }
            };
            CatsVar.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Check all", CmdCheckAllCatsVar) });
            CatsVar.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Uncheck all", CmdUncheckAllCatsVar) });
            foreach (var item in Settings.Config.VarTabs)
                CatsVar.Items.Add(new CbOption(item.Name, null, item.IsEnabled, null) { Tag = item });
            UpdateVarTabName();

            CmdCheckAllCatsUserData = new RelayCommand(x =>
            {
                SetAllTabsOptionVisibility(CatsUserData, true);
            }, x => true);
            CmdUncheckAllCatsUserData = new RelayCommand(x =>
            {
                SetAllTabsOptionVisibility(CatsUserData, false);
            }, x => true);
            CatsUserData = new VmCheckedCb
            {
                StaticText = $"Tabs",
                FooterText = "Hidden tabs do not receive any data during scan",
                IsFilterCanBeEnabledOrDisabled = false,
                OnChanged = (item) =>
                {
                    SetTabVisibility(ToolUserData, item);
                    UpdateUserDataTabName();
                    ToolUserData.UpdateName();
                }
            };
            CatsUserData.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Check all", CmdCheckAllCatsUserData) });
            CatsUserData.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Uncheck all", CmdUncheckAllCatsUserData) });
            foreach (var item in Settings.Config.UserDataTabs)
                CatsUserData.Items.Add(new CbOption(item.Name, null, item.IsEnabled, null) { Tag = item });
            UpdateUserDataTabName();

            #endregion
            #region lar filter

            CmdOnLaFilterApplied = new RelayCommand(x =>
            {
                if (x is LogicalExpressionTable c)
                {
                    if (!Equals(_laPred, c.Expr))
                    {
                        _laPred = c.Expr;
                        ApplyFilter();
                    }
                }
            });

            CmdOnLaFilterLoaded = new RelayCommand(x =>
            {
                if (x is LogicalExpressionTable c)
                {
                    _laPred = c.Expr;
                }
            });

            void OnUpDown(PredicateCondition item, bool up)
            {
                if(up)
                    LaFilterPreds.MoveItemUp(item);
                else
                    LaFilterPreds.MoveItemDown(item);
            }

            LaFilterPreds.Add(new PredicateCondition()
            {
                Name = "Loaded",
                Or = true,
                OnUpDown = OnUpDown,
                PredicateFunc = x =>
                {
                    if (x is VmElementBase el)
                    {
                        return el.IsLoaded;
                    }
                    else
                        return false;
                }
            });
            LaFilterPreds.Add(new PredicateCondition()
            {
                Name = "Archived",
                Or = true,
                OnUpDown = OnUpDown,
                PredicateFunc = x =>
                {
                    if (x is VmElementBase el)
                    {
                        return el.IsInArchive;
                    }
                    else
                        return false;
                }
            });
            LaFilterPreds.Add(new PredicateCondition()
            {
                Name = "Referenced",
                OnUpDown = OnUpDown,
                PredicateFunc = x =>
                {
                    if (x is VmElementBase el)
                        return Refs.RefsRelativePathHash.Contains(el.RelativePath);
                    else
                        return false;
                }
            });

            #endregion
            #region Hidden - favorited

            CmdOnFavHideFilterApplied = new RelayCommand(x =>
            {
                if (x is LogicalExpressionTable c)
                {
                    if (!Equals(_favHidePred, c.Expr))
                    {
                        if(FavHideFilterPreds.Any(x1=>x1.IsActive))
                            _favHidePred = c.Expr;
                        else
                            _favHidePred = x2 => true;
                        ApplyFilter();
                    }
                }
            });

            CmdOnFavHideFilterLoaded = new RelayCommand(x =>
            {
                if (x is LogicalExpressionTable c)
                {
                    if(FavHideFilterPreds.Any(x1=>x1.IsActive))
                        _favHidePred = c.Expr;
                    else
                        _favHidePred = x2 => true;
                }
            });

            FavHideFilterPreds.Add(new PredicateCondition()
            {
                Name = "Hidden",
                OnUpDown = OnUpDown,
                PredicateFunc = x =>
                {
                    if (x is VmElementBase el)
                        return el.ElementInfo.IsHide;
                    else
                        return false;
                }
            });
            FavHideFilterPreds.Add(new PredicateCondition()
            {
                Name = "Favorited",
                OnUpDown = OnUpDown,
                PredicateFunc = x =>
                {
                    if (x is VmElementBase el)
                        return el.ElementInfo.IsFav;
                    else
                        return false;
                }
            });

            #endregion
            #region creator

            _creatorFilterPred = x =>
            {
                if (x is VmElementBase el && (el.IsVar || el.IsVarSelf)) //The creator filter does not work on user items
                    return el.Creator?.IndexOf(CreatorFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                else
                    return true;
            };

            #endregion
            #region name

            _nameFilterPred = x =>
            {
                if (x is VmElementBase el)
                    return el.Name.IndexOf(NameFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                else
                    return false;
            };

            #endregion
            #region version

            _versionFilterPred = x =>
            {
                if (x is VmElementBase el && el.IsVar)
                    return el.Var.Name.Version == _versionIntFilter;
                else
                    return true;
            };

            #endregion
            #region browsed var

            _browsedVarFilterPred = x =>
            {
                if (x is VmElementBase el && el.IsVar)
                {
                    if (el.IsVarSelf)
                        return true;
                    return el.Var == BrowsedVar;
                }
                else
                    return false;
            };

            #endregion
            #region tag

            _tagFilterPred = x =>
            {
                if (x is VmElementBase el && !string.IsNullOrWhiteSpace(el.ElementInfo.UserTags))
                {
                    var tags = TagFilter.Trim().Split(new char[] { ',', ' ', ';' });
                    foreach (var tag in tags)
                    {
                        if (el.ElementInfo.UserTags.Equals(tag, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                    return false;
            };

            #endregion
            #region dates

            _datesFilterPred = x =>
            {
                if (x is VmElementBase el)
                {
                    if (DateMode == DateMode.AllTime)
                        return true;
                    switch (DateMode)
                    {
                        case DateMode.AllTime:
                            return true;
                        case DateMode.ModifiedCalendar:
                            return _datesHash.Contains(el.Modified.Date);
                        case DateMode.CreatedCalendar:
                            return _datesHash.Contains(el.Created.Date);
                        case DateMode.ModifiedLastNDays:
                            return el.Modified.Date >= DateTime.Now.Date.AddDays(-LastNDays);
                        case DateMode.CreatedLastNDays:
                            return el.Created.Date >= DateTime.Now.Date.AddDays(-LastNDays);
                        default:
                            return false;
                    }
                }
                else
                    return false;
            };

            Dates.CollectionChanged += (s, e) =>
            {
                _datesHash = Dates.ToHashSet();
                ApplyFilter();
            };

            #endregion
            #region gender

            _genderFilterPred = item =>
            {
                if (item is VmElementBase el)
                {
                    return FileHelper.ContainsAnyFolder(el.FullName, GenderFilter.Items.Where(x => x.IsChecked).Select(x => x.Name));
                }
                else
                    return true;
            };

            GenderFilter = new VmCheckedCb
            {
                ByText = "",
                NothingCheckedText = "F/M/N",
                StaticItems = false,
                AllIsNothing = true,
                OnChanged = item =>
                {
                    ApplyFilter();
                },
                OnShowHide = showed =>
                {
                }
            };
            GenderFilter.Items.Add(new CbOption("Female", null, false, null));
            GenderFilter.Items.Add(new CbOption("Male", null, false, null));
            GenderFilter.Items.Add(new CbOption("Neutral", null, false, null));
            GenderFilter.UpdateIsFilterEnabled();

            #endregion
            #region extra tools

            CmdStartSelection = new RelayCommand(_ =>
            {
                CmdSelection.Caption = "End selection";
                CmdSelection.Command = CmdEndSelection;
                SelectionMode = true;
            });

            CmdEndSelection = new RelayCommand(_ =>
            {
                CmdSelection.Caption = "Start selection";
                CmdSelection.Command = CmdStartSelection;
                SelectionMode = false;
            });

            CmdSelection = new VmCmdBtn("Start selection", CmdStartSelection);

            CmdClear_All_Filters = new RelayCommand(x =>
            {
                ExtraTools.IsPopupOpen = false;
                IsFilterActive = false;

                try
                {
                    foreach (var p in LaFilterPreds)
                        if(p.Name != "Referenced")
                            p.Or = true;
                    _laPred = _ => true;
                    foreach (var p in FavHideFilterPreds)
                        p.Or = true;
                    _favHidePred = _ => true;
                    CreatorFilter = "";
                    NameFilter = "";
                    VersionFilter = "";
                    TagFilter = "";
                    DateMode = DateMode.AllTime;
                    foreach (var item in GenderFilter.Items)
                        item.IsChecked = true;
                    GenderFilter.UpdateIsFilterEnabled();
                }
                finally
                {
                    IsFilterActive = true;
                }
                ApplyFilter();
            });

            CmdClear_CreatorNameVersion_Filters = new RelayCommand(x =>
            {
                ExtraTools.IsPopupOpen = false;
                IsFilterActive = false;

                CreatorFilter = "";
                NameFilter = "";
                VersionFilter = "";

                IsFilterActive = true;
                ApplyFilter();
            });

            ExtraTools = new VmCheckedCb()
            {
                ByText = "",
                NothingCheckedText = "Extras",
            };
            ExtraTools.Items.Add(new CbOption("", null, false, null) { Action = CmdSelection });
            ExtraTools.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn(@"Clear Creator\Name\Ver filters", CmdClear_CreatorNameVersion_Filters) });
            ExtraTools.Items.Add(new CbOption("", null, false, null) { Action = new VmCmdBtn("Clear all filters", CmdClear_All_Filters) });

            #endregion

            ShowTools = new ShowTools(
                x=>ShowLaFilter = x, x=>ShowFavHideFilter = x, x => GenderFilter.Show = x,
                x=>ShowTagFilter = x, x=>ShowDates = x, x=>ShowSort = x,
                x=>ShowVersionFilter = x, x=> ShowVarCats = x, x=>ShowUserDataCats = x,
                x=>ShowGrouping = x, x=>ShowCreatorFilter = x, x=>ShowNameFilter = x, x=>ExtraTools.Show = x);

            #region main tabs

            ToolVam = new VmToolVam
            {
                Logger = Logger,
                ParentTab = this
            };
            Tabs.Add(ToolVam);

            ToolVar = new VmToolVar
            {
                Logger = Logger,
                ParentTab = this
            };
            Tabs.Add(ToolVar);

            ToolUserData = new VmToolUserData
            {
                Logger = Logger,
                ParentTab = this
            };
            Tabs.Add(ToolUserData);

            ToolLogger = Settings.Logger;
            Tabs.Add(ToolLogger);

            SelectedTab = Tabs.First();

            #endregion
        }

        #endregion

        public async Task Delete(IList<VmElementBase> items)
        {
            var vars = items.Where(x => x.IsVarSelf).Select(x => x.Var).ToList();
            var userItems = items.Where(x => x.IsUserItem).Select(x => x.UserItem).ToList();

            Delete(vars);
            Delete(userItems);
            ToolVam.CmdScan.Execute(null);
            await ToolVam.CmdScan.ExecutionTask;
        }

        // Remove entries from file references and cached references; handle all other cases via Scan.
        public void Delete(IList<UserItem> items)
        {
            bool refFilesChanged = false;
            foreach (var userItem in items)
            {
                //file references
                foreach (var refItemComposite in Refs.Items)
                {
                    RefItemCfg toDelete = null;
                    foreach (var refItem in refItemComposite.Items)
                    {
                        if (!refItem.IsVar && FileHelper.ArePathsEqual(refItem.Files.First(), userItem.Info.RelativePath, true))
                        {
                            toDelete = refItem;
                            break;
                        }
                    }

                    if (toDelete != null)
                    {
                        refItemComposite.Items.Remove(toDelete);
                        refFilesChanged = true;
                    }
                }

                //caches references
                var dependent = DepsHelper.GetRefs(userItem.GetRef(), new HashSet<RefItemCfg>(RefItemCfg.Eq)).ToList();
                dependent.Add(userItem.GetRef());
                foreach (var item in dependent)
                {
                    string dir = item.IsInArchive ? Settings.Config.VamArchivePath : Settings.Config.VamPath;
                    var fullPath = FileHelper.PathCombine(dir, item.Files.First());
                    string path = FileHelper.GetFileName_Meta_Var(fullPath, userItem.IsInArchive);
                    if (FileHelper.FileExists(path))
                        FileHelper.FileDelete(path);
                }

                foreach (var f in userItem.Files)
                {
                    if(FileHelper.FileExists(f.FullName))
                        FileHelper.FileDelete(f.FullName);
                }
            }
            if (refFilesChanged)
                Refs.Save();
        }

        // Remove entries from file references and cached references; handle all other cases via Scan.
        public void Delete(IList<VarFile> items)
        {
            bool refFilesChanged = false;
            foreach (var var in items)
            {
                //file references
                foreach (var refItemComposite in Refs.Items)
                {
                    RefItemCfg toDelete = null;
                    foreach (var refItem in refItemComposite.Items)
                    {
                        if (refItem.IsVar && FileHelper.ArePathsEqual(refItem.Files.First(), var.RelativePath))
                        {
                            toDelete = refItem;
                            break;
                        }
                    }

                    if (toDelete != null)
                    {
                        refItemComposite.Items.Remove(toDelete);
                        refFilesChanged = true;
                    }
                }

                //caches references
                //At the moment, the vars caches do not contain a list of resources that depend on them. Therefore, these caches can be left undeleted.
                //var dependent = DepsHelper.GetRefs(var.GetRef(), new HashSet<RefItemCfg>(RefItemCfg.Eq)).ToList();
                //dependent.Add(var.GetRef());
                //foreach (var d in dependent)
                //{
                //    string dir = d.IsInArchive ? Settings.Config.VamArchivePath : Settings.Config.VamPath;
                //    var fullPath = FileHelper.PathCombine(dir, d.Files.First());
                //    var path = FileHelper.GetFileName_Meta_Var(fullPath, var.IsInArchive);
                //    if (FileHelper.FileExists(path))
                //        FileHelper.FileDelete(path);
                //}

                if(FileHelper.FileExists(var.Info.FullName))
                    FileHelper.FileDelete(var.Info.FullName);
            }
            if (refFilesChanged)
                Refs.Save();
        }

        public void FilterToVar(VarName name)
        {
            IsFilterActive = false;
            try
            {
                CreatorFilter = name.Creator;
                NameFilter = name.Name;
                VersionFilter = name.Version.ToString();
            }
            finally
            {
                IsFilterActive = true;
            }
            ApplyFilter();
        }

        public void BrowseVar(VarName name)
        {
            ToolVar.BrowseVar(name);
        }

        public bool IsFilterActive = true;
        public void ApplyFilter(FilterMode m = FilterMode.Unset)
        {
            if (!IsFilterActive)
                return;

            //Debug.WriteLine("Filter");

            var predBuilder = new PredicateBuilder<object>();

            predBuilder.And(_laPred);
            predBuilder.And(_favHidePred);

            if (Selection.GetValue(NameConditionShowOnlyCheckedElements))
                predBuilder.And(_showOnlyCheckedElementsPred);

            if (!string.IsNullOrWhiteSpace(CreatorFilter))
            {
                predBuilder.And(_creatorFilterPred);
            }

            if (!string.IsNullOrWhiteSpace(NameFilter))
            {
                predBuilder.And(_nameFilterPred);
            }

            if (VersionIntFilter.HasValue)
            {
                predBuilder.And(_versionFilterPred);
            }

            if (!string.IsNullOrWhiteSpace(TagFilter))
            {
                predBuilder.And(_tagFilterPred);
            }

            predBuilder.And(_datesFilterPred);

            if (GenderFilter.Items.Any(x => x.IsChecked) && GenderFilter.Items.Count(x=>x.IsChecked) < 3) //GenderFilter.Show && 
                predBuilder.And(_genderFilterPred);


            var p = predBuilder.Build();
            if (m != FilterMode.BrowsedVar)
            {
                foreach (var t in Tabs)
                {
                    t.OnApplyVarFilter(m, p);
                }
            }

            foreach (var t in Tabs)
            {
                if(BrowsedVar != null)
                    t.OnApplyFilter(m, _browsedVarFilterPred);
                else
                    t.OnApplyFilter(m, p);
            }
        }

        #region overrides

        public override void OnRemove(IList<VmElementBase> elements)
        {
            foreach (var t in Tabs)
                t.OnRemove(elements);
        }

        public override IEnumerable<VmElementBase> GetCheckedElements()
        {
            foreach (var t in Tabs)
            {
                foreach (var item in t.GetCheckedElements())
                {
                    yield return item;
                }
            }
        }

        public override void SetElementsChecked(HashSet<RefItemCfg> refs)
        {
            foreach (var t in Tabs)
            {
                t.SetElementsChecked(refs);
            }
        }

        public override void ReceivedVarCheck(VmBase source, VarFile var, bool isChecked)
        {
            if (Refs.EditedItem != null)
            {
                foreach (var t in Tabs)
                {
                    t.UpdateVarChecks(source, var, isChecked);
                }
            }
        }

        public override void ReceivedElementCheck(VmBase source, VmElementBase el, bool isChecked)
        {
        }

        public override void SetChecks(bool onlyVisible, bool isChecked)
        {
            foreach (var t in Tabs)
            {
                t.SetChecks(onlyVisible, isChecked);
            }
        }

        #endregion

        #region utils

        private void UpdateUserDataTabName()
        {
            CatsUserData.StaticText = $"Tabs ({CatsUserData.Items.Count(x => x.Tag != null && x.IsChecked)})";
        }

        private void UpdateVarTabName()
        {
            CatsVar.StaticText = $"Tabs ({CatsVar.Items.Count(x => x.Tag != null && x.IsChecked)})";
        }

        private void SetTabVisibility(VmBase parent, CbOption item)
        {
            foreach (var t in parent.Tabs)
            {
                if (item.Tag is CategoryCfg cfg && cfg.Type == t.Type && cfg.WithoutPresets == t.ShowWithoutPresets)
                {
                    t.IsVisible = item.IsChecked;
                    cfg.IsEnabled = item.IsChecked;
                    break;
                }
            }

            if (parent.SelectedTab == null)
                parent.SelectedTab = parent.Tabs.FirstOrDefault(x => x.IsVisible);
            else if (!parent.SelectedTab.IsVisible)
                parent.SelectedTab = parent.Tabs.FirstOrDefault(x => x.IsVisible);
        }

        private void SetAllTabsOptionVisibility(VmCheckedCb vm, bool isVisible)
        {
            foreach (var item in vm.Items)
            {
                item.IsChecked = isVisible;
            }
        }

        private void SetGrouping(bool propertyValue, string propertyName, IEnumerable<VmBase> tabs)
        {
            foreach (var t in tabs)
            {
                if (t is VmElements ve)
                {
                    var list = ve.GroupDescriptors;
                    if (propertyValue)
                    {
                        if (!list.Contains(propertyName))
                            list.Add(propertyName);
                    }
                    else
                        list.Remove(propertyName);
                    ve.GroupDescriptors = list;
                }
            }
        }

        #endregion
    }
}
