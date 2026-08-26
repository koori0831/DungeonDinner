using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryPanel : MonoBehaviour
    {
        private const ViewHaveInfoEnum DefaultDisplayViewType = ViewHaveInfoEnum.Name | ViewHaveInfoEnum.Image | ViewHaveInfoEnum.Description;

        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private float y_Offset, default_X_Value;
        [SerializeField] private Transform viewParent, bockmarkParent;
        [SerializeField] private InfoBockmarkBtn bockmarkPrefab;
        [SerializeField] private InfoDictionaryScrollViewField scrollViewPrefavb;
        [SerializeField] private List<InfoDictionaryCategoryData> initialCategoryDataList = new List<InfoDictionaryCategoryData>();
        [SerializeField] private List<InfoDisplayPanel> displayPrefabs = new List<InfoDisplayPanel>();

        private readonly List<InfoDictionaryScrollViewField> _viewList = new List<InfoDictionaryScrollViewField>();
        private readonly List<InfoBockmarkBtn> _bockmarkList = new List<InfoBockmarkBtn>();
        private readonly Dictionary<ViewHaveInfoEnum, InfoDisplayPanel> displayDic = new Dictionary<ViewHaveInfoEnum, InfoDisplayPanel>();
        private readonly Dictionary<string, InfoDictionaryScrollViewField> _scrollViewsByCategory =
            new Dictionary<string, InfoDictionaryScrollViewField>();
        private readonly Dictionary<string, InfoBockmarkBtn> _bockmarksByCategory =
            new Dictionary<string, InfoBockmarkBtn>();
        private readonly Dictionary<InfoDictionaryEntryData, EntryNavigationContext> _navigationContexts =
            new Dictionary<InfoDictionaryEntryData, EntryNavigationContext>();
        private InfoDictionaryScrollViewField _currentScrollView;
        private InfoBockmarkBtn _currentBockmark;
        private string _currentCategoryDisplayName;
        private ViewHaveInfoEnum _currentDisplayViewType;
        private string _currentDisplayEntryName;
        private bool _isDisplayOpen;

        public void Awake()
        {
            if (buildOnAwake)
                Initialize(initialCategoryDataList);
        }

        public void Initialize(IReadOnlyList<InfoDictionaryCategoryData> categories)
        {
            DictionaryRestoreState restoreState = CaptureRestoreState();
            ClearGeneratedViews();

            if (categories == null)
                return;

            if (CanBuild() == false)
                return;

            BuildCategories(categories);
            RestoreState(categories, restoreState);
        }

        private bool CanBuild()
        {
            bool canBuild = true;

            if (viewParent == null)
            {
                Debug.LogWarning("InfoDictionaryPanel needs a view parent before it can build dictionary views.", this);
                canBuild = false;
            }

            if (bockmarkParent == null)
            {
                Debug.LogWarning("InfoDictionaryPanel needs a bockmark parent before it can build category buttons.", this);
                canBuild = false;
            }

            if (scrollViewPrefavb == null)
            {
                Debug.LogWarning("InfoDictionaryPanel needs a scroll view prefab before it can build dictionary views.", this);
                canBuild = false;
            }

            if (bockmarkPrefab == null)
            {
                Debug.LogWarning("InfoDictionaryPanel needs a bockmark prefab before it can build category buttons.", this);
                canBuild = false;
            }

            return canBuild;
        }

        private void BuildCategories(IReadOnlyList<InfoDictionaryCategoryData> categories)
        {
            for (int i = 0; i < categories.Count; ++i)
            {
                InfoDictionaryCategoryData categoryData = categories[i];
                if (categoryData == null)
                    continue;

                InfoDisplayPanel displayPanel;

                if (displayDic.ContainsKey(categoryData.ViewType))
                    displayPanel = displayDic[categoryData.ViewType];
                else
                {
                    InfoDisplayPanel displayPrefab = GetDisplayPrefab(categoryData.ViewType);
                    if (displayPrefab == null)
                    {
                        Debug.LogWarning($"InfoDictionaryPanel could not find display prefab for view type '{categoryData.ViewType}'. Category '{categoryData.DisplayName}' was skipped.", this);
                        continue;
                    }

                    displayPanel = Instantiate(displayPrefab, viewParent);
                    displayPanel.InitializeDisplay(BackDisplay);
                    displayPanel.Disable();
                    displayDic.Add(categoryData.ViewType, displayPanel);
                }

                InfoDictionaryScrollViewField view = Instantiate(scrollViewPrefavb, viewParent);
                InfoBockmarkBtn bockmark = Instantiate(bockmarkPrefab, bockmarkParent);
                _viewList.Add(view);
                _bockmarkList.Add(bockmark);
                RegisterCategoryLookup(categoryData.DisplayName, view, bockmark);
                RegisterNavigationContexts(categoryData);
                view.InitializeField(categoryData.Entries, info => EnableDisplay(categoryData.ViewType, info));
                view.Disable();
                bockmark.Rect.anchoredPosition = new Vector2(default_X_Value, y_Offset * i);
                string categoryDisplayName = categoryData.DisplayName;
                bockmark.InitializeBtn(() => EnableScrollView(view, bockmark, categoryDisplayName), categoryData.DisplayName, categoryData.MarkIcon);
            }
        }

        private void ClearGeneratedViews()
        {
            foreach (InfoDictionaryScrollViewField view in _viewList)
                DestroyGeneratedObject(view);

            foreach (InfoBockmarkBtn bockmark in _bockmarkList)
                DestroyGeneratedObject(bockmark);

            foreach (InfoDisplayPanel displayPanel in displayDic.Values)
                DestroyGeneratedObject(displayPanel);

            _viewList.Clear();
            _bockmarkList.Clear();
            displayDic.Clear();
            _scrollViewsByCategory.Clear();
            _bockmarksByCategory.Clear();
            _navigationContexts.Clear();
            _currentScrollView = null;
            _currentBockmark = null;
        }

        private void DestroyGeneratedObject(Component component)
        {
            if (component == null)
                return;

            if (Application.isPlaying)
                Destroy(component.gameObject);
            else
                DestroyImmediate(component.gameObject);
        }

        public void EnableScrollView(InfoDictionaryScrollViewField view)
        {
            if (view == null)
                return;

            _currentScrollView = view;
            _isDisplayOpen = false;
            _currentDisplayEntryName = null;

            AllDisableScrollView();
            AllDisableDisplay();

            view.Enable();
        }

        private void EnableScrollView(InfoDictionaryScrollViewField view, InfoBockmarkBtn bockmark, string categoryDisplayName)
        {
            if (view == null)
                return;

            _currentCategoryDisplayName = categoryDisplayName;
            SelectBockmark(bockmark);
            EnableScrollView(view);
        }

        private void SelectBockmark(InfoBockmarkBtn bockmark)
        {
            if (_currentBockmark == bockmark)
            {
                _currentBockmark?.SetSelected(true);
                return;
            }

            if (_currentBockmark != null)
                _currentBockmark.SetSelected(false);

            _currentBockmark = bockmark;

            if (_currentBockmark != null)
                _currentBockmark.SetSelected(true);
        }

        public void EnableDisplay(ViewHaveInfoEnum key, InfoDictionaryEntryData info)
        {
            AllDisableScrollView();
            AllDisableDisplay();
            _currentDisplayViewType = key;
            _currentDisplayEntryName = info != null ? info.DisplayName : null;
            _isDisplayOpen = info != null;

            if (displayDic.TryGetValue(key, out InfoDisplayPanel display))
            {
                ConfigureNavigation(display, info);
                display.Enable(info);
                return;
            }

            InfoDisplayPanel fallbackDisplay = GetDisplay(key);
            if (fallbackDisplay == null)
            {
                Debug.LogWarning($"InfoDictionaryPanel could not find active display panel for view type '{key}'.", this);
                return;
            }

            ConfigureNavigation(fallbackDisplay, info);
            fallbackDisplay.Enable(info);
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

        public void AllDisableDisplay() => displayDic.Values.ToList().ForEach(item =>
        {
            if (item != null)
                item.Disable();
        });

        public void AllDisableScrollView() => _viewList.ForEach(view =>
        {
            if (view != null)
                view.Disable();
        });

        public void BackDisplay()
        {
            AllDisableScrollView();
            AllDisableDisplay();

            if (_currentScrollView == null)
                return;

            _isDisplayOpen = false;
            _currentDisplayEntryName = null;
            _currentScrollView.Enable();
        }

        private DictionaryRestoreState CaptureRestoreState()
        {
            return new DictionaryRestoreState(
                _currentCategoryDisplayName,
                _currentDisplayViewType,
                _currentDisplayEntryName,
                _isDisplayOpen);
        }

        private void RegisterCategoryLookup(
            string categoryDisplayName,
            InfoDictionaryScrollViewField view,
            InfoBockmarkBtn bockmark)
        {
            if (string.IsNullOrWhiteSpace(categoryDisplayName))
                return;

            _scrollViewsByCategory[categoryDisplayName] = view;
            _bockmarksByCategory[categoryDisplayName] = bockmark;
        }

        private void RestoreState(
            IReadOnlyList<InfoDictionaryCategoryData> categories,
            DictionaryRestoreState state)
        {
            if (state.IsDisplayOpen
                && string.IsNullOrWhiteSpace(state.EntryDisplayName) == false
                && TryFindEntry(categories, state.ViewType, state.EntryDisplayName, out InfoDictionaryEntryData entry))
            {
                EnableDisplay(state.ViewType, entry);
                return;
            }

            if (string.IsNullOrWhiteSpace(state.CategoryDisplayName) == false
                && _scrollViewsByCategory.TryGetValue(state.CategoryDisplayName, out InfoDictionaryScrollViewField view))
            {
                _bockmarksByCategory.TryGetValue(state.CategoryDisplayName, out InfoBockmarkBtn bockmark);
                EnableScrollView(view, bockmark, state.CategoryDisplayName);
            }
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

        public InfoDisplayPanel GetDisplayPrefab(ViewHaveInfoEnum viewEnum)
        {
            for (int i = 0; i < displayPrefabs.Count; i++)
            {
                InfoDisplayPanel displayPrefab = displayPrefabs[i];
                if (displayPrefab != null && displayPrefab.ViewInfo == viewEnum)
                    return displayPrefab;
            }

            if (viewEnum == DefaultDisplayViewType)
                return null;

            for (int i = 0; i < displayPrefabs.Count; i++)
            {
                InfoDisplayPanel displayPrefab = displayPrefabs[i];
                if (displayPrefab != null && displayPrefab.ViewInfo == DefaultDisplayViewType)
                    return displayPrefab;
            }

            return null;
        }

        private InfoDisplayPanel GetDisplay(ViewHaveInfoEnum viewEnum)
        {
            if (displayDic.TryGetValue(viewEnum, out InfoDisplayPanel display))
                return display;

            displayDic.TryGetValue(DefaultDisplayViewType, out display);
            return display;
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
