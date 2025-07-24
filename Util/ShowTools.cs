using System;

namespace Vrm.Util
{
    public class ShowTools
    {
        public ShowTools(Action<bool> showLa, Action<bool> showFavHide,
            Action<bool> gender, Action<bool> tag, Action<bool> dates, 
            Action<bool> sort, Action<bool> showVersion, Action<bool> varCats,
            Action<bool> userDataCats, Action<bool> grouping, 
            Action<bool> showCreator, Action<bool> showName,
            Action<bool> extras)
        {
            _showLa = showLa;
            _showFavHide = showFavHide;
            _gender = gender;
            _tag = tag;
            _dates = dates;
            _sort = sort;
            _showVersion = showVersion;
            _varCats = varCats;
            _userDataCats = userDataCats;
            _grouping = grouping;
            _showCreator = showCreator;
            _showName = showName;
            _extras = extras;
        }

        public void Update(bool la, bool favHide, bool gender, bool tag,
            bool dates, bool sort, bool version, bool varCats, bool userDataCats,
            bool grouping, bool creator, bool name, bool extras)
        {
            ShowLaFilter = la;
            ShowFavHideFilter = favHide;
            GenderFilter = gender;
            TagFilter = tag;
            ShowDates = dates;
            ShowSort = sort;
            ShowVersionFilter = version;
            ShowVarCats = varCats;
            ShowUserDataCats = userDataCats;
            ShowGrouping = grouping;
            ShowCreatorFilter = creator;
            ShowNameFilter = name;
            ShowExtras = extras;
        }

        public void UpdateAll(bool v)
        {
            Update(v, v, v, v, v, v, v, v, v, v, v, v, v);
        }

        public void Invoke()
        {
            _showLa?.Invoke(_showLaFilter);
            _showFavHide?.Invoke(_showFavHideFilter);
            _gender?.Invoke(_genderFilter);
            _tag?.Invoke(_tagFilter);
            _dates?.Invoke(_showDates);
            _sort?.Invoke(_sortSort);
            _showVersion?.Invoke(_showVersionFilter);
            _varCats?.Invoke(_showVarCats);
            _userDataCats?.Invoke(_showUserDataCats);
            _grouping?.Invoke(_showGrouping);
            _showCreator?.Invoke(_showCreatorFilter);
            _showName?.Invoke(_showNameFilter);
            _extras?.Invoke(_showExtras);
        }

        private readonly Action<bool> _showLa;
        private bool _showLaFilter;
        public bool ShowLaFilter
        {
            get => _showLaFilter;
            set
            {
                _showLaFilter = value;
            }
        }

        private readonly Action<bool> _showFavHide;
        private bool _showFavHideFilter;
        public bool ShowFavHideFilter
        {
            get => _showFavHideFilter;
            set
            {
                _showFavHideFilter = value;
            }
        }

        private readonly Action<bool> _gender;
        private bool _genderFilter;
        public bool GenderFilter
        {
            get => _genderFilter;
            set
            {
                _genderFilter = value;
            }
        }

        private readonly Action<bool> _tag;
        private bool _tagFilter;
        public bool TagFilter
        {
            get => _tagFilter;
            set
            {
                _tagFilter = value;
            }
        }

        private readonly Action<bool> _dates;
        private bool _showDates;
        public bool ShowDates
        {
            get => _showDates;
            set
            {
                _showDates = value;
            }
        }

        private readonly Action<bool> _sort;
        private bool _sortSort;
        public bool ShowSort
        {
            get => _sortSort;
            set
            {
                _sortSort = value;
            }
        }

        private readonly Action<bool> _showVersion;
        private bool _showVersionFilter;
        public bool ShowVersionFilter
        {
            get => _showVersionFilter;
            set
            {
                _showVersionFilter = value;
            }
        }

        private readonly Action<bool> _varCats;
        private bool _showVarCats;
        public bool ShowVarCats
        {
            get => _showVarCats;
            set
            {
                _showVarCats = value;
            }
        }

        private readonly Action<bool> _userDataCats;
        private bool _showUserDataCats;
        public bool ShowUserDataCats
        {
            get => _showUserDataCats;
            set
            {
                _showUserDataCats = value;
            }
        }

        private readonly Action<bool> _grouping;
        private bool _showGrouping;
        public bool ShowGrouping
        {
            get => _showGrouping;
            set
            {
                _showGrouping = value;
            }
        }

        private readonly Action<bool> _showCreator;
        private bool _showCreatorFilter;
        public bool ShowCreatorFilter
        {
            get => _showCreatorFilter;
            set
            {
                _showCreatorFilter = value;
            }
        }

        private readonly Action<bool> _showName;
        private bool _showNameFilter;
        public bool ShowNameFilter
        {
            get => _showNameFilter;
            set
            {
                _showNameFilter = value;
            }
        }

        
        private readonly Action<bool> _extras;
        private bool _showExtras;
        public bool ShowExtras
        {
            get => _showExtras;
            set
            {
                _showExtras = value;
            }
        }
    }

}
