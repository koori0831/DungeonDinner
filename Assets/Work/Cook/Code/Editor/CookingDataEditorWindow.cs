using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Editor
{
    public sealed partial class CookingDataEditorWindow : EditorWindow
    {
        private const string DefaultAssetFolder = "Assets/Work/Cook/Data";
        private const string RecipeAssetFolder = DefaultAssetFolder + "/Recipes";
        private const string CategoryAssetFolder = DefaultAssetFolder + "/FoodCategories";
        private const string IngredientCategoryAssetFolder = DefaultAssetFolder + "/IngredientCategories";
        private const string TagAssetFolder = DefaultAssetFolder + "/Tags";
        private const string MethodAssetFolder = DefaultAssetFolder + "/PreparationMethods";
        private const string IngredientAssetFolder = DefaultAssetFolder + "/Ingredients";
        private const string StylePath = "Assets/Work/Cook/Code/Editor/CookingDataEditorWindow.uss";

        [SerializeField] private string assetFolder = DefaultAssetFolder;
        [SerializeField] private CookingDataCatalogSO catalog;
        [SerializeField] private DataMode currentMode = DataMode.Recipe;

        private readonly List<UnityEngine.Object> _allAssets = new List<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> _visibleAssets = new List<UnityEngine.Object>();

        private Label _panelHeadingLabel;
        private Label _catalogSummaryLabel;
        private Label _countLabel;
        private TextField _searchField;
        private Button _createButton;
        private Button _recipeModeButton;
        private Button _categoryModeButton;
        private Button _ingredientCategoryModeButton;
        private Button _tagModeButton;
        private Button _methodModeButton;
        private Button _ingredientModeButton;
        private ListView _assetListView;

        private Label _selectedTitleLabel;
        private Label _selectedMetaLabel;
        private Label _saveStateLabel;
        private Button _saveButton;
        private Button _revertButton;
        private Button _pingButton;
        private Button _registerButton;
        private IMGUIContainer _formContainer;
        private Label _helpTextLabel;

        private UnityEngine.Object _selectedAsset;
        private RecipeDraft _recipeDraft;
        private CategoryDraft _categoryDraft;
        private IngredientCategoryDraft _ingredientCategoryDraft;
        private TagDraft _tagDraft;
        private MethodDraft _methodDraft;
        private IngredientDraft _ingredientDraft;
        private bool _hasUnsavedChanges;
        private bool _isRestoringSelection;

        private enum DataMode
        {
            Recipe,
            Category,
            IngredientCategory,
            Tag,
            PreparationMethod,
            Ingredient
        }

        [MenuItem("Tools/Dungeon Dinner/Cooking Data Editor")]
        [MenuItem("Window/Dungeon Dinner/Cooking Data Editor")]
        public static void Open()
        {
            CookingDataEditorWindow window = GetWindow<CookingDataEditorWindow>("요리 데이터");
            window.minSize = new Vector2(980f, 640f);
            window.Show();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            root.AddToClassList("cooking-editor-root");
            root.Add(BuildTopBar());

            VisualElement content = new VisualElement();
            content.AddToClassList("fixed-layout");
            content.Add(BuildListPanel());
            content.Add(BuildDetailPanel());
            root.Add(content);

            RefreshAssets(false);
            UpdateModeButtons();
            UpdateDetailPanel();
        }

        private void OnProjectChange()
        {
            if (_assetListView == null)
                return;

            RefreshAssets();
        }

        private VisualElement BuildTopBar()
        {
            VisualElement topBar = new VisualElement();
            topBar.AddToClassList("top-bar");

            VisualElement titleBlock = new VisualElement();
            titleBlock.AddToClassList("title-block");

            Label title = new Label("요리 데이터 SO 편집기");
            title.AddToClassList("window-title");
            titleBlock.Add(title);

            Label guide = new Label("왼쪽에서 데이터 종류와 SO를 고르고, 오른쪽에서 수정한 뒤 저장하세요.");
            guide.AddToClassList("window-guide");
            titleBlock.Add(guide);
            topBar.Add(titleBlock);

            ObjectField catalogField = new ObjectField("카탈로그")
            {
                objectType = typeof(CookingDataCatalogSO),
                allowSceneObjects = false,
                value = catalog
            };
            catalogField.AddToClassList("catalog-field");
            catalogField.RegisterValueChangedCallback(evt =>
            {
                catalog = evt.newValue as CookingDataCatalogSO;
                UpdateCatalogSummary();
                UpdateDetailPanel();
            });
            topBar.Add(catalogField);

            return topBar;
        }

        private VisualElement BuildListPanel()
        {
            VisualElement panel = new VisualElement();
            panel.AddToClassList("left-panel");

            _panelHeadingLabel = new Label();
            _panelHeadingLabel.AddToClassList("panel-heading");
            panel.Add(_panelHeadingLabel);

            VisualElement modeTabs = new VisualElement();
            modeTabs.AddToClassList("mode-tabs");
            _ingredientCategoryModeButton = BuildModeButton("재료군", DataMode.IngredientCategory);
            _recipeModeButton = BuildModeButton("레시피", DataMode.Recipe);
            _categoryModeButton = BuildModeButton("카테고리", DataMode.Category);
            _tagModeButton = BuildModeButton("태그", DataMode.Tag);
            _methodModeButton = BuildModeButton("손질법", DataMode.PreparationMethod);
            _ingredientModeButton = BuildModeButton("재료", DataMode.Ingredient);
            modeTabs.Add(_recipeModeButton);
            modeTabs.Add(_categoryModeButton);
            modeTabs.Add(_ingredientCategoryModeButton);
            modeTabs.Add(_tagModeButton);
            modeTabs.Add(_methodModeButton);
            modeTabs.Add(_ingredientModeButton);
            panel.Add(modeTabs);

            _catalogSummaryLabel = new Label();
            _catalogSummaryLabel.AddToClassList("summary-label");
            panel.Add(_catalogSummaryLabel);

            _searchField = new TextField("검색");
            _searchField.AddToClassList("search-field");
            _searchField.RegisterValueChangedCallback(_ => ApplyFilter());
            panel.Add(_searchField);

            TextField folderField = new TextField("생성 위치")
            {
                value = assetFolder
            };
            folderField.AddToClassList("folder-field");
            folderField.RegisterValueChangedCallback(evt => assetFolder = evt.newValue);
            panel.Add(folderField);

            VisualElement actions = new VisualElement();
            actions.AddToClassList("action-row");
            Button refreshButton = new Button(() => RefreshAssets()) { text = "새로고침" };
            _createButton = new Button(CreateNewAsset);
            actions.Add(refreshButton);
            actions.Add(_createButton);
            panel.Add(actions);

            _countLabel = new Label();
            _countLabel.AddToClassList("count-label");
            panel.Add(_countLabel);

            _assetListView = new ListView(_visibleAssets, 64f, MakeListItem, BindListItem)
            {
                selectionType = SelectionType.Single,
                showBorder = false
            };
            _assetListView.AddToClassList("recipe-list");
            _assetListView.selectionChanged += OnSelectionChanged;
            panel.Add(_assetListView);

            return panel;
        }

        private VisualElement BuildDetailPanel()
        {
            VisualElement panel = new VisualElement();
            panel.AddToClassList("right-panel");

            VisualElement header = new VisualElement();
            header.AddToClassList("detail-header");

            VisualElement titleBlock = new VisualElement();
            titleBlock.AddToClassList("detail-title-block");

            _selectedTitleLabel = new Label();
            _selectedTitleLabel.AddToClassList("detail-title");
            titleBlock.Add(_selectedTitleLabel);

            _selectedMetaLabel = new Label();
            _selectedMetaLabel.AddToClassList("detail-meta");
            titleBlock.Add(_selectedMetaLabel);
            header.Add(titleBlock);

            _saveStateLabel = new Label();
            _saveStateLabel.AddToClassList("save-state");
            header.Add(_saveStateLabel);
            panel.Add(header);

            ScrollView detailScroll = new ScrollView(ScrollViewMode.Vertical);
            detailScroll.AddToClassList("detail-scroll");

            VisualElement helpBox = new VisualElement();
            helpBox.AddToClassList("help-box");
            _helpTextLabel = new Label();
            helpBox.Add(_helpTextLabel);
            detailScroll.Add(helpBox);

            _formContainer = new IMGUIContainer(DrawSelectedForm);
            _formContainer.AddToClassList("recipe-form");
            detailScroll.Add(_formContainer);
            panel.Add(detailScroll);

            VisualElement actions = new VisualElement();
            actions.AddToClassList("footer-actions");
            _saveButton = new Button(SaveSelectedAsset) { text = "수정사항 저장" };
            _revertButton = new Button(RevertSelectedAsset) { text = "되돌리기" };
            _pingButton = new Button(PingSelectedAsset) { text = "프로젝트에서 표시" };
            _registerButton = new Button(RegisterSelectedAssetToCatalog) { text = "카탈로그에 등록" };
            actions.Add(_saveButton);
            actions.Add(_revertButton);
            actions.Add(_pingButton);
            actions.Add(_registerButton);
            panel.Add(actions);

            return panel;
        }

        private Button BuildModeButton(string text, DataMode mode)
        {
            Button button = new Button(() => SwitchMode(mode)) { text = text };
            button.AddToClassList("mode-tab");
            return button;
        }

        private VisualElement MakeListItem()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("recipe-list-item");

            Label name = new Label { name = "name" };
            name.AddToClassList("recipe-list-name");
            row.Add(name);

            Label meta = new Label { name = "meta" };
            meta.AddToClassList("recipe-list-meta");
            row.Add(meta);

            return row;
        }

        private void BindListItem(VisualElement element, int index)
        {
            UnityEngine.Object asset = _visibleAssets[index];
            Label name = element.Q<Label>("name");
            Label meta = element.Q<Label>("meta");

            name.text = GetDisplayName(asset);
            meta.text = GetListMeta(asset);
        }

        private void SwitchMode(DataMode mode)
        {
            if (currentMode == mode)
                return;

            if (TryHandleUnsavedChanges() == false)
                return;

            currentMode = mode;
            ClearSelection();
            RefreshAssets(false);
            UpdateModeButtons();
            UpdateDetailPanel();
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (_isRestoringSelection == true)
                return;

            UnityEngine.Object nextAsset = null;
            foreach (object selectedItem in selectedItems)
            {
                nextAsset = selectedItem as UnityEngine.Object;
                break;
            }

            if (nextAsset == _selectedAsset)
                return;

            if (TryHandleUnsavedChanges() == false)
            {
                RestoreListSelection();
                return;
            }

            SelectAsset(nextAsset);
        }

        private void SelectAsset(UnityEngine.Object asset)
        {
            _selectedAsset = asset;
            BuildDraftFromSelection();
            _hasUnsavedChanges = false;
            UpdateDetailPanel();
            _formContainer?.MarkDirtyRepaint();
        }

        private void ClearSelection()
        {
            _selectedAsset = null;
            _recipeDraft = null;
            _categoryDraft = null;
            _ingredientCategoryDraft = null;
            _tagDraft = null;
            _methodDraft = null;
            _ingredientDraft = null;
            _hasUnsavedChanges = false;
            _assetListView?.ClearSelection();
        }

        private void BuildDraftFromSelection()
        {
            _recipeDraft = null;
            _categoryDraft = null;
            _ingredientCategoryDraft = null;
            _tagDraft = null;
            _methodDraft = null;
            _ingredientDraft = null;

            switch (_selectedAsset)
            {
                case RecipeSO recipe:
                    _recipeDraft = RecipeDraft.From(recipe);
                    break;
                case FoodCategorySO category:
                    _categoryDraft = CategoryDraft.From(category);
                    break;
                case IngredientCategorySO ingredientCategory:
                    _ingredientCategoryDraft = IngredientCategoryDraft.From(ingredientCategory);
                    break;
                case FoodTagSO tag:
                    _tagDraft = TagDraft.From(tag);
                    break;
                case PreparationMethodSO method:
                    _methodDraft = MethodDraft.From(method);
                    break;
                case IngredientSO ingredient:
                    _ingredientDraft = IngredientDraft.From(ingredient);
                    break;
            }
        }

        private void RefreshAssets(bool keepSelection = true)
        {
            UnityEngine.Object previousSelection = keepSelection ? _selectedAsset : null;

            _allAssets.Clear();
            string[] guids = AssetDatabase.FindAssets(GetSearchFilter(currentMode));
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, GetAssetType(currentMode));
                if (asset != null)
                    _allAssets.Add(asset);
            }

            _allAssets.Sort(CompareAssets);
            ApplyFilter(false);
            _assetListView?.Rebuild();

            if (previousSelection != null && _allAssets.Contains(previousSelection))
            {
                SelectAsset(previousSelection);
                RestoreListSelection();
            }
            else if (_visibleAssets.Count > 0)
            {
                SelectAsset(_visibleAssets[0]);
                RestoreListSelection();
            }
            else
            {
                ClearSelection();
            }

            UpdateListHeader();
            UpdateCatalogSummary();
        }

        private void ApplyFilter(bool rebuildList = true)
        {
            string keyword = _searchField != null ? _searchField.value : string.Empty;
            _visibleAssets.Clear();

            for (int i = 0; i < _allAssets.Count; i++)
            {
                UnityEngine.Object asset = _allAssets[i];
                if (MatchesSearch(asset, keyword))
                    _visibleAssets.Add(asset);
            }

            if (_countLabel != null)
                _countLabel.text = $"표시 중: {_visibleAssets.Count}개 / 전체: {_allAssets.Count}개";

            if (rebuildList && _assetListView != null)
            {
                _assetListView.Rebuild();
                RestoreListSelection();
            }
        }

        private void UpdateModeButtons()
        {
            SetModeButtonState(_recipeModeButton, currentMode == DataMode.Recipe);
            SetModeButtonState(_categoryModeButton, currentMode == DataMode.Category);
            SetModeButtonState(_ingredientCategoryModeButton, currentMode == DataMode.IngredientCategory);
            SetModeButtonState(_tagModeButton, currentMode == DataMode.Tag);
            SetModeButtonState(_methodModeButton, currentMode == DataMode.PreparationMethod);
            SetModeButtonState(_ingredientModeButton, currentMode == DataMode.Ingredient);
            UpdateListHeader();
        }

        private static void SetModeButtonState(Button button, bool selected)
        {
            if (button == null)
                return;

            if (selected == true)
                button.AddToClassList("mode-tab-selected");
            else
                button.RemoveFromClassList("mode-tab-selected");
        }

        private void UpdateListHeader()
        {
            if (_panelHeadingLabel != null)
                _panelHeadingLabel.text = $"{GetModeKoreanName(currentMode)} 목록";

            if (_createButton != null)
                _createButton.text = $"새 {GetModeKoreanName(currentMode)}";
        }

        private void UpdateCatalogSummary()
        {
            if (_catalogSummaryLabel == null)
                return;

            if (catalog == null)
            {
                _catalogSummaryLabel.text = "카탈로그 미선택: 목록은 프로젝트 전체 SO를 보여줍니다.";
                return;
            }

            _catalogSummaryLabel.text =
                $"카탈로그: 레시피 {catalog.Recipes.Count} / 카테고리 {catalog.Categories.Count} / 태그 {catalog.Tags.Count} / 손질법 {catalog.PreparationMethods.Count} / 재료 {catalog.Ingredients.Count}";
        }

        private void UpdateDetailPanel()
        {
            bool hasSelection = _selectedAsset != null;

            _selectedTitleLabel.text = hasSelection ? GetDisplayName(_selectedAsset) : $"{GetModeKoreanName(currentMode)}를 선택하세요";
            _selectedMetaLabel.text = hasSelection
                ? $"{GetAssetId(_selectedAsset)}  |  {GetAssetPath(_selectedAsset)}"
                : $"왼쪽 목록에서 {GetModeKoreanName(currentMode)} SO를 선택하면 여기서 수정할 수 있습니다.";

            _saveStateLabel.text = hasSelection
                ? (_hasUnsavedChanges ? "저장되지 않은 수정사항 있음" : "저장됨")
                : "선택 없음";

            if (_helpTextLabel != null)
                _helpTextLabel.text = GetHelpText(currentMode);

            if (_saveButton == null)
                return;

            _saveButton.SetEnabled(hasSelection == true && _hasUnsavedChanges == true);
            _revertButton.SetEnabled(hasSelection == true && _hasUnsavedChanges == true);
            _pingButton.SetEnabled(hasSelection);
            _registerButton.SetEnabled(hasSelection && catalog != null && IsSelectedAssetInCatalog() == false);
        }

        private void DrawSelectedForm()
        {
            if (_selectedAsset == null)
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox($"수정할 {GetModeKoreanName(currentMode)} SO를 왼쪽 목록에서 선택하세요.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();

            switch (currentMode)
            {
                case DataMode.Recipe:
                    DrawRecipeForm();
                    break;
                case DataMode.Category:
                    DrawCategoryForm();
                    break;
                case DataMode.IngredientCategory:
                    DrawIngredientCategoryForm();
                    break;
                case DataMode.Tag:
                    DrawTagForm();
                    break;
                case DataMode.PreparationMethod:
                    DrawMethodForm();
                    break;
                case DataMode.Ingredient:
                    DrawIngredientForm();
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                MarkDraftDirty();
        }

    }
}
