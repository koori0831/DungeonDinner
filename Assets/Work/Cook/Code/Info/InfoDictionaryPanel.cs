using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryPanel : MonoBehaviour
    {
        private const ViewHaveInfoEnum DefaultDisplayViewType = ViewHaveInfoEnum.Name | ViewHaveInfoEnum.Image | ViewHaveInfoEnum.Description;

        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private List<InfoDictionaryCategoryData> initialCategoryDataList = new List<InfoDictionaryCategoryData>();
        [SerializeField] private List<InfoDisplayPanel> displayPrefabs = new List<InfoDisplayPanel>();

        private readonly List<InfoDictionaryCategoryData> _categories = new List<InfoDictionaryCategoryData>();
        private readonly Dictionary<ViewHaveInfoEnum, InfoDisplayPanel> _displayPanels =
            new Dictionary<ViewHaveInfoEnum, InfoDisplayPanel>();
        private readonly Dictionary<InfoDictionaryEntryData, EntryNavigationContext> _navigationContexts =
            new Dictionary<InfoDictionaryEntryData, EntryNavigationContext>();

        private string _currentCategoryDisplayName;
        private ViewHaveInfoEnum _currentDisplayViewType;
        private string _currentDisplayEntryName;
        private bool _isDisplayOpen;

        public IReadOnlyList<InfoDictionaryCategoryData> CurrentCategories => _categories;

        public void Awake()
        {
            BuildDisplayLookup();

            if (buildOnAwake)
                Initialize(initialCategoryDataList);
        }

        public void Initialize(IReadOnlyList<InfoDictionaryCategoryData> categories)
        {
            DictionaryRestoreState restoreState = CaptureRestoreState();
            _categories.Clear();
            _navigationContexts.Clear();
            BuildDisplayLookup();

            if (categories != null)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    if (categories[i] == null)
                        continue;

                    _categories.Add(categories[i]);
                    RegisterNavigationContexts(categories[i]);
                }
            }

            RestoreState(restoreState);
        }

        public void OpenCategory(string categoryDisplayName)
        {
            _currentCategoryDisplayName = categoryDisplayName;
            _isDisplayOpen = false;
            _currentDisplayEntryName = null;
            AllDisableDisplay();
        }

        public void ShowEntry(InfoDictionaryEntryData info)
        {
            if (info == null)
                return;

            if (_navigationContexts.TryGetValue(info, out EntryNavigationContext context))
            {
                EnableDisplay(context.ViewType, info);
                return;
            }

            EnableDisplay(DefaultDisplayViewType, info);
        }

        public void EnableDisplay(ViewHaveInfoEnum key, InfoDictionaryEntryData info)
        {
            AllDisableDisplay();
            _currentDisplayViewType = key;
            _currentDisplayEntryName = info != null ? info.DisplayName : null;
            _isDisplayOpen = info != null;

            InfoDisplayPanel display = GetDisplay(key);
            if (display == null)
            {
                Debug.LogWarning($"InfoDictionaryPanel could not find a display panel for view type '{key}'. Assign a scene display panel instead of a generated prefab.", this);
                return;
            }

            ConfigureNavigation(display, info);
            display.Enable(info);
        }

        public void EnableScrollView(InfoDictionaryScrollViewField view)
        {
            if (view == null)
                return;

            _isDisplayOpen = false;
            _currentDisplayEntryName = null;
            AllDisableDisplay();
            view.Enable();
        }

        public void AllDisableDisplay()
        {
            foreach (InfoDisplayPanel display in _displayPanels.Values)
            {
                if (display != null)
                    display.Disable();
            }
        }

        public void AllDisableScrollView()
        {
        }

        public void BackDisplay()
        {
            AllDisableDisplay();
            _isDisplayOpen = false;
            _currentDisplayEntryName = null;
        }

        public InfoDisplayPanel GetDisplayPrefab(ViewHaveInfoEnum viewEnum)
        {
            return GetDisplay(viewEnum);
        }

        private void BuildDisplayLookup()
        {
            _displayPanels.Clear();

            for (int i = 0; i < displayPrefabs.Count; i++)
            {
                InfoDisplayPanel display = displayPrefabs[i];
                if (display == null)
                    continue;

                if (_displayPanels.ContainsKey(display.ViewInfo) == false)
                {
                    display.InitializeDisplay(BackDisplay);
                    display.Disable();
                    _displayPanels.Add(display.ViewInfo, display);
                }
            }
        }

        private void RegisterNavigationContexts(InfoDictionaryCategoryData categoryData)
        {
            if (categoryData?.Entries == null)
                return;

            for (int i = 0; i < categoryData.Entries.Count; i++)
            {
                InfoDictionaryEntryData entry = categoryData.Entries[i];
                if (entry == null)
                    continue;

                _navigationContexts[entry] = new EntryNavigationContext(categoryData.ViewType, categoryData.Entries, i);
            }
        }

        private void ConfigureNavigation(InfoDisplayPanel display, InfoDictionaryEntryData info)
        {
            if (display == null)
                return;

            if (info == null || _navigationContexts.TryGetValue(info, out EntryNavigationContext context) == false)
            {
                display.SetSiblingNavigation(null, null, false, false);
                return;
            }

            InfoDictionaryEntryData previous = context.Index > 0 ? context.Entries[context.Index - 1] : null;
            InfoDictionaryEntryData next = context.Index < context.Entries.Count - 1 ? context.Entries[context.Index + 1] : null;

            display.SetSiblingNavigation(
                previous != null ? () => EnableDisplay(context.ViewType, previous) : null,
                next != null ? () => EnableDisplay(context.ViewType, next) : null,
                previous != null,
                next != null);
        }

        private void RestoreState(DictionaryRestoreState state)
        {
            if (state.IsDisplayOpen
                && string.IsNullOrWhiteSpace(state.EntryDisplayName) == false
                && TryFindEntry(_categories, state.ViewType, state.EntryDisplayName, out InfoDictionaryEntryData entry))
            {
                EnableDisplay(state.ViewType, entry);
                return;
            }

            if (string.IsNullOrWhiteSpace(state.CategoryDisplayName) == false)
                OpenCategory(state.CategoryDisplayName);
        }

        private DictionaryRestoreState CaptureRestoreState()
        {
            return new DictionaryRestoreState(
                _currentCategoryDisplayName,
                _currentDisplayViewType,
                _currentDisplayEntryName,
                _isDisplayOpen);
        }

        private InfoDisplayPanel GetDisplay(ViewHaveInfoEnum viewEnum)
        {
            if (_displayPanels.TryGetValue(viewEnum, out InfoDisplayPanel display))
                return display;

            if (viewEnum == DefaultDisplayViewType)
                return null;

            _displayPanels.TryGetValue(DefaultDisplayViewType, out display);
            return display;
        }

        private static bool TryFindEntry(
            IReadOnlyList<InfoDictionaryCategoryData> categories,
            ViewHaveInfoEnum viewType,
            string entryDisplayName,
            out InfoDictionaryEntryData entry)
        {
            entry = null;

            if (categories == null || string.IsNullOrWhiteSpace(entryDisplayName))
                return false;

            for (int i = 0; i < categories.Count; i++)
            {
                InfoDictionaryCategoryData category = categories[i];
                if (category == null || category.ViewType != viewType || category.Entries == null)
                    continue;

                for (int j = 0; j < category.Entries.Count; j++)
                {
                    InfoDictionaryEntryData candidate = category.Entries[j];
                    if (candidate != null && candidate.DisplayName == entryDisplayName)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private readonly struct EntryNavigationContext
        {
            public readonly ViewHaveInfoEnum ViewType;
            public readonly IReadOnlyList<InfoDictionaryEntryData> Entries;
            public readonly int Index;

            public EntryNavigationContext(
                ViewHaveInfoEnum viewType,
                IReadOnlyList<InfoDictionaryEntryData> entries,
                int index)
            {
                ViewType = viewType;
                Entries = entries;
                Index = index;
            }
        }

        private readonly struct DictionaryRestoreState
        {
            public readonly string CategoryDisplayName;
            public readonly ViewHaveInfoEnum ViewType;
            public readonly string EntryDisplayName;
            public readonly bool IsDisplayOpen;

            public DictionaryRestoreState(
                string categoryDisplayName,
                ViewHaveInfoEnum viewType,
                string entryDisplayName,
                bool isDisplayOpen)
            {
                CategoryDisplayName = categoryDisplayName;
                ViewType = viewType;
                EntryDisplayName = entryDisplayName;
                IsDisplayOpen = isDisplayOpen;
            }
        }
    }
}
