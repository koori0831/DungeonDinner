using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Editor
{
    public sealed class CookingDataEditorWindow : EditorWindow
    {
        private const string DefaultAssetFolder = "Assets/Work/Cook/Data";
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
            if (_isRestoringSelection)
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

            if (selected)
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

            _saveButton.SetEnabled(hasSelection && _hasUnsavedChanges);
            _revertButton.SetEnabled(hasSelection && _hasUnsavedChanges);
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

        private void DrawRecipeForm()
        {
            if (_recipeDraft == null)
                return;

            _recipeDraft.Priority = EditorGUILayout.IntField("매칭 우선순위", _recipeDraft.Priority);

            EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
            _recipeDraft.RecipeId = EditorGUILayout.TextField("레시피 ID", _recipeDraft.RecipeId);
            _recipeDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _recipeDraft.DisplayName);
            _recipeDraft.Category = (FoodCategorySO)EditorGUILayout.ObjectField("카테고리", _recipeDraft.Category, typeof(FoodCategorySO), false);

            EditorGUILayout.LabelField("설명");
            _recipeDraft.Description = EditorGUILayout.TextArea(_recipeDraft.Description, GUILayout.MinHeight(54f));
            EditorGUILayout.Space(8f);

            if (DrawObjectList("기본 태그", _recipeDraft.BaseTags, typeof(FoodTagSO), "+ 태그 추가"))
                MarkDraftDirty();

            DrawRequiredIngredients();
            DrawWarnings(BuildRecipeWarnings());
        }

        private void DrawCategoryForm()
        {
            if (_categoryDraft == null)
                return;

            EditorGUILayout.LabelField("카테고리 정보", EditorStyles.boldLabel);
            _categoryDraft.CategoryId = EditorGUILayout.TextField("카테고리 ID", _categoryDraft.CategoryId);
            _categoryDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _categoryDraft.DisplayName);
            _categoryDraft.Icon = (Sprite)EditorGUILayout.ObjectField("책갈피 아이콘", _categoryDraft.Icon, typeof(Sprite), false);
            EditorGUILayout.LabelField("설명");
            _categoryDraft.Description = EditorGUILayout.TextArea(_categoryDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("카테고리는 음식의 큰 분류입니다. 예: 찌개, 구이, 디저트, 괴식.", MessageType.None);
            DrawWarnings(BuildCategoryWarnings());
        }

        private void DrawIngredientCategoryForm()
        {
            if (_ingredientCategoryDraft == null)
                return;

            EditorGUILayout.LabelField("재료군 정보", EditorStyles.boldLabel);
            _ingredientCategoryDraft.CategoryId = EditorGUILayout.TextField("재료군 ID", _ingredientCategoryDraft.CategoryId);
            _ingredientCategoryDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _ingredientCategoryDraft.DisplayName);
            _ingredientCategoryDraft.Icon = (Sprite)EditorGUILayout.ObjectField("아이콘", _ingredientCategoryDraft.Icon, typeof(Sprite), false);
            EditorGUILayout.LabelField("설명");
            _ingredientCategoryDraft.Description = EditorGUILayout.TextArea(_ingredientCategoryDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("고기, 채소, 향신료처럼 레시피 슬롯에서 대체 가능한 큰 재료 묶음을 정의합니다.", MessageType.None);
            DrawWarnings(BuildIngredientCategoryWarnings());
        }

        private void DrawTagForm()
        {
            if (_tagDraft == null)
                return;

            EditorGUILayout.LabelField("태그 정보", EditorStyles.boldLabel);
            _tagDraft.TagId = EditorGUILayout.TextField("태그 ID", _tagDraft.TagId);
            _tagDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _tagDraft.DisplayName);
            EditorGUILayout.LabelField("설명");
            _tagDraft.Description = EditorGUILayout.TextArea(_tagDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("태그는 맛/온도/식감/위험 속성입니다. 예: spicy, sweet, poisonous, hot.", MessageType.None);
            DrawWarnings(BuildTagWarnings());
        }

        private void DrawMethodForm()
        {
            if (_methodDraft == null)
                return;

            EditorGUILayout.LabelField("손질법 정보", EditorStyles.boldLabel);
            _methodDraft.MethodId = EditorGUILayout.TextField("손질법 ID", _methodDraft.MethodId);
            _methodDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _methodDraft.DisplayName);
            EditorGUILayout.LabelField("설명");
            _methodDraft.Description = EditorGUILayout.TextArea(_methodDraft.Description, GUILayout.MinHeight(80f));
            EditorGUILayout.HelpBox("손질법 자체는 선택지 이름입니다. 이 손질법이 태그를 추가하거나 괴식을 만드는 효과는 재료 탭의 '손질법별 효과'에서 설정합니다.", MessageType.None);
            DrawWarnings(BuildMethodWarnings());
        }

        private void DrawIngredientForm()
        {
            if (_ingredientDraft == null)
                return;

            _ingredientDraft.Category = (IngredientCategorySO)EditorGUILayout.ObjectField("재료군", _ingredientDraft.Category, typeof(IngredientCategorySO), false);

            EditorGUILayout.LabelField("재료 정보", EditorStyles.boldLabel);
            _ingredientDraft.IngredientId = EditorGUILayout.TextField("재료 ID", _ingredientDraft.IngredientId);
            _ingredientDraft.DisplayName = EditorGUILayout.TextField("표시 이름", _ingredientDraft.DisplayName);
            _ingredientDraft.ModelPrefab = (GameObject)EditorGUILayout.ObjectField("3D 모델 프리팹", _ingredientDraft.ModelPrefab, typeof(GameObject), false);
            EditorGUILayout.LabelField("설명");
            _ingredientDraft.Description = EditorGUILayout.TextArea(_ingredientDraft.Description, GUILayout.MinHeight(64f));
            EditorGUILayout.Space(8f);

            if (DrawObjectList("재료 기본 태그", _ingredientDraft.BaseTags, typeof(FoodTagSO), "+ 기본 태그 추가"))
                MarkDraftDirty();

            DrawPreparationOptions();
            DrawWarnings(BuildIngredientWarnings());
        }

        private void DrawRequiredIngredients()
        {
            EditorGUILayout.LabelField("필요 재료", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("직접 선택한 재료가 이 목록과 매칭되면 이 레시피 음식으로 판정됩니다.", MessageType.None);

            for (int i = 0; i < _recipeDraft.RequiredIngredients.Count; i++)
            {
                IngredientRequirementDraft requirement = _recipeDraft.RequiredIngredients[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"필요 재료 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    _recipeDraft.RequiredIngredients.RemoveAt(i);
                    MarkDraftDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
                requirement.Ingredient = (IngredientSO)EditorGUILayout.ObjectField("기준 재료", requirement.Ingredient, typeof(IngredientSO), false);
                requirement.IngredientCategory = (IngredientCategorySO)EditorGUILayout.ObjectField("재료군 조건", requirement.IngredientCategory, typeof(IngredientCategorySO), false);
                requirement.RequiredPreparationMethod = (PreparationMethodSO)EditorGUILayout.ObjectField("필수 손질법", requirement.RequiredPreparationMethod, typeof(PreparationMethodSO), false);
                requirement.MinCount = Mathf.Max(0, EditorGUILayout.IntField("최소 개수", requirement.MinCount));
                requirement.MaxCount = Mathf.Max(0, EditorGUILayout.IntField("최대 개수 (0 = 제한 없음)", requirement.MaxCount));
                requirement.RecipeDefining = EditorGUILayout.Toggle("요리 결정 조건", requirement.RecipeDefining);
                requirement.AutoApplyRequiredPreparation = EditorGUILayout.Toggle("필수 손질 자동 적용", requirement.AutoApplyRequiredPreparation);
                requirement.RequireManualPreparation = EditorGUILayout.Toggle("직접 손질 필요", requirement.RequireManualPreparation);

                if (DrawObjectList("필수 태그", requirement.RequiredTags, typeof(FoodTagSO), "+ 필수 태그"))
                    MarkDraftDirty();

                if (DrawObjectList("단순 대체 재료", requirement.SimpleAlternatives, typeof(IngredientSO), "+ 대체 재료"))
                    MarkDraftDirty();

                if (DrawAlternativeList(requirement.Alternatives))
                    MarkDraftDirty();

                bool usePreparationModifier = EditorGUILayout.Toggle("손질 수식어 반영", requirement.UsePreparationResultNameModifier);
                if (usePreparationModifier != requirement.UsePreparationResultNameModifier)
                {
                    requirement.UsePreparationResultNameModifier = usePreparationModifier;
                    MarkDraftDirty();
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 필요 재료 추가"))
            {
                _recipeDraft.RequiredIngredients.Add(new IngredientRequirementDraft());
                MarkDraftDirty();
            }

            EditorGUILayout.Space(8f);
        }

        private static bool DrawAlternativeList(List<IngredientAlternativeDraft> alternatives)
        {
            bool changed = false;
            EditorGUILayout.LabelField("대체 재료", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("대체 재료를 사용했을 때 완성 음식 이름 앞에 붙일 수식어를 지정합니다. 예: 참치, 버섯, 고급.", MessageType.None);

            for (int i = 0; i < alternatives.Count; i++)
            {
                IngredientAlternativeDraft alternative = alternatives[i] ?? new IngredientAlternativeDraft();
                alternatives[i] = alternative;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"대체 재료 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    alternatives.RemoveAt(i);
                    changed = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                IngredientSO ingredient = (IngredientSO)EditorGUILayout.ObjectField("재료", alternative.Ingredient, typeof(IngredientSO), false);
                if (ingredient != alternative.Ingredient)
                {
                    alternative.Ingredient = ingredient;
                    changed = true;
                }

                string modifier = EditorGUILayout.TextField("이름 수식어", alternative.ResultNameModifier);
                if (modifier != alternative.ResultNameModifier)
                {
                    alternative.ResultNameModifier = modifier;
                    changed = true;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 대체 재료 추가"))
            {
                alternatives.Add(new IngredientAlternativeDraft());
                changed = true;
            }

            EditorGUILayout.Space(8f);
            return changed;
        }

        private void DrawPerfectRules()
        {
            EditorGUILayout.LabelField("정석 손질 조건", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("완벽한 음식 판정을 위해 각 재료가 선택해야 하는 손질법입니다.", MessageType.None);

            for (int i = 0; i < _recipeDraft.PerfectRules.Count; i++)
            {
                PerfectRuleDraft rule = _recipeDraft.PerfectRules[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"정석 조건 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    _recipeDraft.PerfectRules.RemoveAt(i);
                    MarkDraftDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
                rule.Ingredient = (IngredientSO)EditorGUILayout.ObjectField("재료", rule.Ingredient, typeof(IngredientSO), false);
                rule.PerfectMethod = (PreparationMethodSO)EditorGUILayout.ObjectField("정석 손질법", rule.PerfectMethod, typeof(PreparationMethodSO), false);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 정석 조건 추가"))
            {
                _recipeDraft.PerfectRules.Add(new PerfectRuleDraft());
                MarkDraftDirty();
            }

            if (GUILayout.Button("필요 재료를 정석 조건에 추가"))
            {
                AddMissingPerfectRulesFromRequirements();
                MarkDraftDirty();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawPreparationOptions()
        {
            EditorGUILayout.LabelField("손질법별 효과", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("이 재료에서 플레이어가 고를 수 있는 손질법과, 그 손질법이 요리 결과에 추가/제거할 태그 및 위험 효과를 설정합니다.", MessageType.Info);

            for (int i = 0; i < _ingredientDraft.PreparationOptions.Count; i++)
            {
                PreparationOptionDraft option = _ingredientDraft.PreparationOptions[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"손질 선택지 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    _ingredientDraft.PreparationOptions.RemoveAt(i);
                    MarkDraftDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
                option.Method = (PreparationMethodSO)EditorGUILayout.ObjectField("손질법", option.Method, typeof(PreparationMethodSO), false);
                option.DisplayNameOverride = EditorGUILayout.TextField("표시 이름 덮어쓰기", option.DisplayNameOverride);
                EditorGUILayout.LabelField("설명");
                option.Description = EditorGUILayout.TextArea(option.Description, GUILayout.MinHeight(48f));

                if (DrawObjectList("요리에 추가할 태그", option.AddTags, typeof(FoodTagSO), "+ 추가 태그"))
                    MarkDraftDirty();
                if (DrawObjectList("요리에서 제거할 태그", option.RemoveTags, typeof(FoodTagSO), "+ 제거 태그"))
                    MarkDraftDirty();

                option.QualityDelta = EditorGUILayout.IntField("품질 변화", option.QualityDelta);
                option.CausesDisgusting = EditorGUILayout.Toggle("괴식으로 만듦", option.CausesDisgusting);
                option.AddsPoison = EditorGUILayout.Toggle("독 속성 추가", option.AddsPoison);
                option.ResultNameModifier = EditorGUILayout.TextField("결과 이름 수식어", option.ResultNameModifier);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 손질 선택지 추가"))
            {
                _ingredientDraft.PreparationOptions.Add(new PreparationOptionDraft());
                MarkDraftDirty();
            }

            EditorGUILayout.Space(8f);
        }

        private static bool DrawObjectList<T>(string title, List<T> values, Type objectType, string addLabel)
            where T : UnityEngine.Object
        {
            bool changed = false;
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            for (int i = 0; i < values.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(24f));
                T newValue = (T)EditorGUILayout.ObjectField(values[i], objectType, false);
                if (newValue != values[i])
                {
                    values[i] = newValue;
                    changed = true;
                }

                if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                {
                    values.RemoveAt(i);
                    i--;
                    changed = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button(addLabel))
            {
                values.Add(null);
                changed = true;
            }

            EditorGUILayout.Space(8f);
            return changed;
        }

        private void DrawWarnings(List<string> warnings)
        {
            if (warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 입력값에서 눈에 띄는 문제는 없습니다.", MessageType.Info);
                return;
            }

            for (int i = 0; i < warnings.Count; i++)
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
        }

        private void SaveSelectedAsset()
        {
            if (_selectedAsset == null)
                return;

            Undo.RecordObject(_selectedAsset, $"{GetModeKoreanName(currentMode)} SO 수정");
            SerializedObject serialized = new SerializedObject(_selectedAsset);

            switch (currentMode)
            {
                case DataMode.Recipe:
                    SaveRecipe(serialized);
                    break;
                case DataMode.Category:
                    SaveCategory(serialized);
                    break;
                case DataMode.IngredientCategory:
                    SaveIngredientCategory(serialized);
                    break;
                case DataMode.Tag:
                    SaveTag(serialized);
                    break;
                case DataMode.PreparationMethod:
                    SaveMethod(serialized);
                    break;
                case DataMode.Ingredient:
                    SaveIngredient(serialized);
                    break;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selectedAsset);
            RenameSelectedAssetToMatchData();
            AssetDatabase.SaveAssets();

            BuildDraftFromSelection();
            _hasUnsavedChanges = false;
            RefreshAssets();
            UpdateDetailPanel();
            Debug.Log($"{GetModeKoreanName(currentMode)} SO 저장 완료: {GetAssetPath(_selectedAsset)}", _selectedAsset);
        }

        private void RenameSelectedAssetToMatchData()
        {
            if (_selectedAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(_selectedAsset);
            if (string.IsNullOrWhiteSpace(path))
                return;

            string desiredName = GetDesiredAssetName(_selectedAsset);
            if (string.IsNullOrWhiteSpace(desiredName))
                return;

            string currentName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(currentName, desiredName, StringComparison.Ordinal))
                return;

            string folder = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(folder))
                return;

            folder = folder.Replace('\\', '/');
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{desiredName}.asset");
            string uniqueName = Path.GetFileNameWithoutExtension(uniquePath);
            string error = AssetDatabase.RenameAsset(path, uniqueName);

            if (string.IsNullOrEmpty(error) == false)
                Debug.LogWarning($"SO 에셋 이름 변경 실패: {error}", _selectedAsset);
        }

        private void SaveRecipe(SerializedObject serialized)
        {
            SetString(serialized, "recipeId", _recipeDraft.RecipeId);
            SetString(serialized, "displayName", _recipeDraft.DisplayName);
            SetString(serialized, "description", _recipeDraft.Description);
            SetObject(serialized, "category", _recipeDraft.Category);
            SetInt(serialized, "priority", _recipeDraft.Priority);
            SetObjectArray(serialized, "baseTags", _recipeDraft.BaseTags);
            SetRequiredIngredients(serialized, _recipeDraft.RequiredIngredients);
            SetPerfectRules(serialized, Array.Empty<PerfectRuleDraft>());
        }

        private void SaveCategory(SerializedObject serialized)
        {
            SetString(serialized, "categoryId", _categoryDraft.CategoryId);
            SetString(serialized, "displayName", _categoryDraft.DisplayName);
            SetObject(serialized, "icon", _categoryDraft.Icon);
            SetString(serialized, "description", _categoryDraft.Description);
        }

        private void SaveIngredientCategory(SerializedObject serialized)
        {
            SetString(serialized, "categoryId", _ingredientCategoryDraft.CategoryId);
            SetString(serialized, "displayName", _ingredientCategoryDraft.DisplayName);
            SetObject(serialized, "icon", _ingredientCategoryDraft.Icon);
            SetString(serialized, "description", _ingredientCategoryDraft.Description);
        }

        private void SaveTag(SerializedObject serialized)
        {
            SetString(serialized, "tagId", _tagDraft.TagId);
            SetString(serialized, "displayName", _tagDraft.DisplayName);
            SetString(serialized, "description", _tagDraft.Description);
        }

        private void SaveMethod(SerializedObject serialized)
        {
            SetString(serialized, "methodId", _methodDraft.MethodId);
            SetString(serialized, "displayName", _methodDraft.DisplayName);
            SetString(serialized, "description", _methodDraft.Description);
        }

        private void SaveIngredient(SerializedObject serialized)
        {
            SetString(serialized, "ingredientId", _ingredientDraft.IngredientId);
            SetString(serialized, "displayName", _ingredientDraft.DisplayName);
            SetString(serialized, "description", _ingredientDraft.Description);
            SetObject(serialized, "category", _ingredientDraft.Category);
            SetObject(serialized, "modelPrefab", _ingredientDraft.ModelPrefab);
            SetObjectArray(serialized, "baseTags", _ingredientDraft.BaseTags);
            SetPreparationOptions(serialized, _ingredientDraft.PreparationOptions);
        }

        private void RevertSelectedAsset()
        {
            if (_selectedAsset == null)
                return;

            BuildDraftFromSelection();
            _hasUnsavedChanges = false;
            UpdateDetailPanel();
            _formContainer.MarkDirtyRepaint();
        }

        private void PingSelectedAsset()
        {
            if (_selectedAsset == null)
                return;

            Selection.activeObject = _selectedAsset;
            EditorGUIUtility.PingObject(_selectedAsset);
        }

        private void RegisterSelectedAssetToCatalog()
        {
            if (_selectedAsset == null || catalog == null || IsSelectedAssetInCatalog())
                return;

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty(GetCatalogPropertyName(currentMode));
            if (list == null || list.isArray == false)
                return;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = _selectedAsset;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            UpdateCatalogSummary();
            UpdateDetailPanel();
        }

        private void CreateNewAsset()
        {
            if (TryHandleUnsavedChanges() == false)
                return;

            string folder = NormalizeFolder(assetFolder);
            EnsureFolder(folder);

            UnityEngine.Object asset = CreateInstance(GetAssetType(currentMode));
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{GetDefaultFileName(currentMode)}.asset");
            AssetDatabase.CreateAsset(asset, path);

            SerializedObject serialized = new SerializedObject(asset);
            string id = Path.GetFileNameWithoutExtension(path);
            SetInitialValues(serialized, currentMode, id);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _selectedAsset = asset;
            if (catalog != null)
                RegisterSelectedAssetToCatalog();

            RefreshAssets(false);
            SelectAsset(asset);
            RestoreListSelection();
        }

        private bool TryHandleUnsavedChanges()
        {
            if (_hasUnsavedChanges == false)
                return true;

            int result = EditorUtility.DisplayDialogComplex(
                "저장되지 않은 수정사항",
                "현재 SO에 저장되지 않은 수정사항이 있습니다. 어떻게 할까요?",
                "저장",
                "버리기",
                "취소");

            if (result == 0)
            {
                SaveSelectedAsset();
                return true;
            }

            return result == 1;
        }

        private void RestoreListSelection()
        {
            if (_assetListView == null)
                return;

            _isRestoringSelection = true;
            int index = _selectedAsset != null ? _visibleAssets.IndexOf(_selectedAsset) : -1;
            if (index >= 0)
                _assetListView.SetSelection(index);
            else
                _assetListView.ClearSelection();

            _isRestoringSelection = false;
        }

        private void MarkDraftDirty()
        {
            _hasUnsavedChanges = true;
            UpdateDetailPanel();
        }

        private void AddMissingPerfectRulesFromRequirements()
        {
            for (int i = 0; i < _recipeDraft.RequiredIngredients.Count; i++)
            {
                IngredientSO ingredient = _recipeDraft.RequiredIngredients[i].Ingredient;
                if (ingredient == null || HasPerfectRule(ingredient))
                    continue;

                _recipeDraft.PerfectRules.Add(new PerfectRuleDraft { Ingredient = ingredient });
            }
        }

        private bool HasPerfectRule(IngredientSO ingredient)
        {
            for (int i = 0; i < _recipeDraft.PerfectRules.Count; i++)
            {
                if (_recipeDraft.PerfectRules[i].Ingredient == ingredient)
                    return true;
            }

            return false;
        }

        private bool IsSelectedAssetInCatalog()
        {
            if (_selectedAsset == null || catalog == null)
                return false;

            IReadOnlyList<UnityEngine.Object> values = GetCatalogValues(currentMode);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == _selectedAsset)
                    return true;
            }

            return false;
        }

        private IReadOnlyList<UnityEngine.Object> GetCatalogValues(DataMode mode)
        {
            List<UnityEngine.Object> values = new List<UnityEngine.Object>();
            if (catalog == null)
                return values;

            switch (mode)
            {
                case DataMode.Recipe:
                    AddObjects(values, catalog.Recipes);
                    break;
                case DataMode.Category:
                    AddObjects(values, catalog.Categories);
                    break;
                case DataMode.IngredientCategory:
                    AddObjects(values, catalog.IngredientCategories);
                    break;
                case DataMode.Tag:
                    AddObjects(values, catalog.Tags);
                    break;
                case DataMode.PreparationMethod:
                    AddObjects(values, catalog.PreparationMethods);
                    break;
                case DataMode.Ingredient:
                    AddObjects(values, catalog.Ingredients);
                    break;
            }

            return values;
        }

        private static void AddObjects<T>(List<UnityEngine.Object> target, IReadOnlyList<T> source)
            where T : UnityEngine.Object
        {
            for (int i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        private List<string> BuildRecipeWarnings()
        {
            List<string> warnings = new List<string>();
            if (_recipeDraft == null)
                return warnings;

            if (string.IsNullOrWhiteSpace(_recipeDraft.RecipeId))
                warnings.Add("레시피 ID가 비어 있습니다.");

            if (_recipeDraft.Category == null)
                warnings.Add("카테고리가 지정되지 않았습니다.");

            if (_recipeDraft.RequiredIngredients.Count == 0)
                warnings.Add("필요 재료가 없으면 직접 재료 선택으로 이 레시피를 매칭할 수 없습니다.");

            HashSet<IngredientSO> requiredIngredients = new HashSet<IngredientSO>();
            for (int i = 0; i < _recipeDraft.RequiredIngredients.Count; i++)
            {
                IngredientRequirementDraft requirement = _recipeDraft.RequiredIngredients[i];
                IngredientSO ingredient = requirement.Ingredient;
                bool hasAnyCondition = ingredient != null
                                       || requirement.IngredientCategory != null
                                       || requirement.RequiredTags.Count > 0
                                       || requirement.SimpleAlternatives.Count > 0
                                       || requirement.Alternatives.Count > 0;

                if (hasAnyCondition == false)
                {
                    warnings.Add($"필요 재료 {i + 1}번에 재료/재료군/태그/대체재료 조건이 없습니다.");
                    continue;
                }

                if (requirement.MaxCount > 0 && requirement.MaxCount < requirement.MinCount)
                    warnings.Add($"필요 재료 {i + 1}번의 최대 개수가 최소 개수보다 작습니다.");

                if (ingredient != null && requiredIngredients.Add(ingredient) == false)
                    warnings.Add($"중복된 필요 재료가 있습니다: {ingredient.DisplayName}");
            }

            return warnings;
        }

        private List<string> BuildCategoryWarnings()
        {
            List<string> warnings = new List<string>();
            if (_categoryDraft != null && string.IsNullOrWhiteSpace(_categoryDraft.CategoryId))
                warnings.Add("카테고리 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildIngredientCategoryWarnings()
        {
            List<string> warnings = new List<string>();
            if (_ingredientCategoryDraft != null && string.IsNullOrWhiteSpace(_ingredientCategoryDraft.CategoryId))
                warnings.Add("재료군 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildTagWarnings()
        {
            List<string> warnings = new List<string>();
            if (_tagDraft != null && string.IsNullOrWhiteSpace(_tagDraft.TagId))
                warnings.Add("태그 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildMethodWarnings()
        {
            List<string> warnings = new List<string>();
            if (_methodDraft != null && string.IsNullOrWhiteSpace(_methodDraft.MethodId))
                warnings.Add("손질법 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildIngredientWarnings()
        {
            List<string> warnings = new List<string>();
            if (_ingredientDraft == null)
                return warnings;

            if (string.IsNullOrWhiteSpace(_ingredientDraft.IngredientId))
                warnings.Add("재료 ID가 비어 있습니다.");

            if (_ingredientDraft.PreparationOptions.Count != 3)
                warnings.Add($"현재 손질 선택지가 {_ingredientDraft.PreparationOptions.Count}개입니다. 플레이어에게 3가지를 보여주려면 3개를 등록하세요.");

            HashSet<PreparationMethodSO> methods = new HashSet<PreparationMethodSO>();
            for (int i = 0; i < _ingredientDraft.PreparationOptions.Count; i++)
            {
                PreparationOptionDraft option = _ingredientDraft.PreparationOptions[i];
                if (option.Method == null)
                {
                    warnings.Add($"손질 선택지 {i + 1}번의 손질법이 비어 있습니다.");
                    continue;
                }

                if (methods.Add(option.Method) == false)
                    warnings.Add($"중복된 손질법이 있습니다: {option.Method.DisplayName}");
            }

            return warnings;
        }

        private static bool MatchesSearch(UnityEngine.Object asset, string keyword)
        {
            if (asset == null)
                return false;

            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            string normalized = keyword.Trim();
            return Contains(GetAssetId(asset), normalized)
                   || Contains(GetDisplayName(asset), normalized)
                   || Contains(GetListMeta(asset), normalized)
                   || Contains(GetAssetPath(asset), normalized);
        }

        private static bool Contains(string source, string value)
        {
            return string.IsNullOrEmpty(source) == false
                   && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CompareAssets(UnityEngine.Object left, UnityEngine.Object right)
        {
            return string.Compare(GetDisplayName(left), GetDisplayName(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetDisplayName(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case RecipeSO recipe:
                    return string.IsNullOrWhiteSpace(recipe.DisplayName) ? "(이름 없음)" : recipe.DisplayName;
                case FoodCategorySO category:
                    return string.IsNullOrWhiteSpace(category.DisplayName) ? "(이름 없음)" : category.DisplayName;
                case IngredientCategorySO ingredientCategory:
                    return string.IsNullOrWhiteSpace(ingredientCategory.DisplayName) ? "(이름 없음)" : ingredientCategory.DisplayName;
                case FoodTagSO tag:
                    return string.IsNullOrWhiteSpace(tag.DisplayName) ? "(이름 없음)" : tag.DisplayName;
                case PreparationMethodSO method:
                    return string.IsNullOrWhiteSpace(method.DisplayName) ? "(이름 없음)" : method.DisplayName;
                case IngredientSO ingredient:
                    return string.IsNullOrWhiteSpace(ingredient.DisplayName) ? "(이름 없음)" : ingredient.DisplayName;
                default:
                    return asset != null ? asset.name : "(없음)";
            }
        }

        private static string GetAssetId(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case RecipeSO recipe:
                    return recipe.RecipeId;
                case FoodCategorySO category:
                    return category.CategoryId;
                case IngredientCategorySO ingredientCategory:
                    return ingredientCategory.CategoryId;
                case FoodTagSO tag:
                    return tag.TagId;
                case PreparationMethodSO method:
                    return method.MethodId;
                case IngredientSO ingredient:
                    return ingredient.IngredientId;
                default:
                    return string.Empty;
            }
        }

        private static string GetListMeta(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case RecipeSO recipe:
                    return $"{recipe.RecipeId}  |  {(recipe.Category != null ? recipe.Category.DisplayName : "카테고리 없음")}";
                case FoodCategorySO category:
                    return $"{category.CategoryId}  |  음식 분류";
                case IngredientCategorySO ingredientCategory:
                    return $"{ingredientCategory.CategoryId}  |  재료군";
                case FoodTagSO tag:
                    return $"{tag.TagId}  |  맛/속성 태그";
                case PreparationMethodSO method:
                    return $"{method.MethodId}  |  재료 손질 선택지";
                case IngredientSO ingredient:
                    return $"{ingredient.IngredientId}  |  태그 {ingredient.BaseTags.Count} / 손질법 {ingredient.PreparationOptions.Count}";
                default:
                    return GetAssetPath(asset);
            }
        }

        private static string GetAssetPath(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(path) ? "저장 경로 없음" : path;
        }

        private static string GetDesiredAssetName(UnityEngine.Object asset)
        {
            string sourceName = GetAssetId(asset);
            if (string.IsNullOrWhiteSpace(sourceName))
                sourceName = GetDisplayName(asset);

            if (string.IsNullOrWhiteSpace(sourceName)
                || string.Equals(sourceName, "(이름 없음)", StringComparison.Ordinal))
            {
                sourceName = asset != null ? asset.name : string.Empty;
            }

            return SanitizeAssetName(sourceName);
        }

        private static string SanitizeAssetName(string value)
        {
            string safeName = string.IsNullOrWhiteSpace(value) ? "CookingData" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(invalid, '_');

            return string.IsNullOrWhiteSpace(safeName) ? "CookingData" : safeName;
        }

        private static string GetSearchFilter(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return "t:RecipeSO";
                case DataMode.Category:
                    return "t:FoodCategorySO";
                case DataMode.IngredientCategory:
                    return "t:IngredientCategorySO";
                case DataMode.Tag:
                    return "t:FoodTagSO";
                case DataMode.PreparationMethod:
                    return "t:PreparationMethodSO";
                case DataMode.Ingredient:
                    return "t:IngredientSO";
                default:
                    return "t:ScriptableObject";
            }
        }

        private static Type GetAssetType(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return typeof(RecipeSO);
                case DataMode.Category:
                    return typeof(FoodCategorySO);
                case DataMode.IngredientCategory:
                    return typeof(IngredientCategorySO);
                case DataMode.Tag:
                    return typeof(FoodTagSO);
                case DataMode.PreparationMethod:
                    return typeof(PreparationMethodSO);
                case DataMode.Ingredient:
                    return typeof(IngredientSO);
                default:
                    return typeof(ScriptableObject);
            }
        }

        private static string GetCatalogPropertyName(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return "recipes";
                case DataMode.Category:
                    return "categories";
                case DataMode.IngredientCategory:
                    return "ingredientCategories";
                case DataMode.Tag:
                    return "tags";
                case DataMode.PreparationMethod:
                    return "preparationMethods";
                case DataMode.Ingredient:
                    return "ingredients";
                default:
                    return string.Empty;
            }
        }

        private static string GetDefaultFileName(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return "NewRecipe";
                case DataMode.Category:
                    return "NewCategory";
                case DataMode.IngredientCategory:
                    return "NewIngredientCategory";
                case DataMode.Tag:
                    return "NewTag";
                case DataMode.PreparationMethod:
                    return "NewPreparationMethod";
                case DataMode.Ingredient:
                    return "NewIngredient";
                default:
                    return "NewCookingData";
            }
        }

        private static string GetModeKoreanName(DataMode mode)
        {
            if (mode == DataMode.IngredientCategory)
                return "재료군";

            switch (mode)
            {
                case DataMode.Recipe:
                    return "레시피";
                case DataMode.Category:
                    return "카테고리";
                case DataMode.Tag:
                    return "태그";
                case DataMode.PreparationMethod:
                    return "손질법";
                case DataMode.Ingredient:
                    return "재료";
                default:
                    return "데이터";
            }
        }

        private static string GetHelpText(DataMode mode)
        {
            if (mode == DataMode.IngredientCategory)
                return "재료군은 고기, 채소, 향신료처럼 레시피 슬롯에서 대체 가능한 큰 재료 묶음입니다.";

            switch (mode)
            {
                case DataMode.Recipe:
                    return "레시피는 완성 음식의 기준입니다. 필요 재료 슬롯에서 재료, 재료군, 태그, 필수 손질법, 개수 조건을 연결합니다.";
                case DataMode.Category:
                    return "카테고리는 음식의 큰 분류입니다. NPC 판정에서 FoodType/Category 기준으로 사용하기 좋습니다.";
                case DataMode.Tag:
                    return "태그는 맛, 온도, 식감, 독 같은 속성입니다. 재료 손질법은 요리에 태그를 추가하거나 제거할 수 있습니다.";
                case DataMode.PreparationMethod:
                    return "손질법은 재료가 제공하는 선택지의 기본 이름입니다. 실제 효과는 재료 탭에서 손질법별로 따로 설정합니다.";
                case DataMode.Ingredient:
                    return "재료는 기본 태그와 손질법별 효과를 가집니다. 각 손질 선택지에서 추가 태그, 제거 태그, 독/괴식 여부, 결과 이름 수식어를 설정하세요.";
                default:
                    return string.Empty;
            }
        }

        private static void SetInitialValues(SerializedObject serialized, DataMode mode, string id)
        {
            if (mode == DataMode.IngredientCategory)
            {
                SetString(serialized, "categoryId", id);
                SetString(serialized, "displayName", "새 재료군");
                return;
            }

            switch (mode)
            {
                case DataMode.Recipe:
                    SetString(serialized, "recipeId", id);
                    SetString(serialized, "displayName", "새 레시피");
                    break;
                case DataMode.Category:
                    SetString(serialized, "categoryId", id);
                    SetString(serialized, "displayName", "새 카테고리");
                    break;
                case DataMode.Tag:
                    SetString(serialized, "tagId", id);
                    SetString(serialized, "displayName", "새 태그");
                    break;
                case DataMode.PreparationMethod:
                    SetString(serialized, "methodId", id);
                    SetString(serialized, "displayName", "새 손질법");
                    break;
                case DataMode.Ingredient:
                    SetString(serialized, "ingredientId", id);
                    SetString(serialized, "displayName", "새 재료");
                    break;
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value ?? string.Empty;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetObjectArray<T>(SerializedObject serialized, string propertyName, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            SetRelativeObjectArray(property, values);
        }

        private static void SetRequiredIngredients(
            SerializedObject serialized,
            IReadOnlyList<IngredientRequirementDraft> requirements)
        {
            SerializedProperty property = serialized.FindProperty("requiredIngredients");
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (requirements == null)
                return;

            for (int i = 0; i < requirements.Count; i++)
            {
                IngredientRequirementDraft requirement = requirements[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("ingredient").objectReferenceValue = requirement.Ingredient;
                element.FindPropertyRelative("ingredientCategory").objectReferenceValue = requirement.IngredientCategory;
                element.FindPropertyRelative("requiredPreparationMethod").objectReferenceValue = requirement.RequiredPreparationMethod;
                element.FindPropertyRelative("minCount").intValue = requirement.MinCount;
                element.FindPropertyRelative("maxCount").intValue = requirement.MaxCount;
                element.FindPropertyRelative("recipeDefining").boolValue = requirement.RecipeDefining;
                element.FindPropertyRelative("autoApplyRequiredPreparation").boolValue = requirement.AutoApplyRequiredPreparation;
                element.FindPropertyRelative("requireManualPreparation").boolValue = requirement.RequireManualPreparation;
                SerializedProperty usePreparationModifier = element.FindPropertyRelative("usePreparationResultNameModifier");
                if (usePreparationModifier != null)
                    usePreparationModifier.boolValue = requirement.UsePreparationResultNameModifier;
                SetRelativeObjectArray(element.FindPropertyRelative("requiredTags"), requirement.RequiredTags);
                SetRelativeObjectArray(element.FindPropertyRelative("alternatives"), requirement.SimpleAlternatives);
                SetAlternativeOptions(element.FindPropertyRelative("alternativeOptions"), requirement.Alternatives);
            }
        }

        private static void SetAlternativeOptions(
            SerializedProperty property,
            IReadOnlyList<IngredientAlternativeDraft> alternatives)
        {
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (alternatives == null)
                return;

            for (int i = 0; i < alternatives.Count; i++)
            {
                IngredientAlternativeDraft alternative = alternatives[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("ingredient").objectReferenceValue = alternative.Ingredient;
                element.FindPropertyRelative("resultNameModifier").stringValue = alternative.ResultNameModifier ?? string.Empty;
            }
        }

        private static void SetPerfectRules(SerializedObject serialized, IReadOnlyList<PerfectRuleDraft> rules)
        {
            SerializedProperty property = serialized.FindProperty("perfectPreparationRules");
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (rules == null)
                return;

            for (int i = 0; i < rules.Count; i++)
            {
                PerfectRuleDraft rule = rules[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("ingredient").objectReferenceValue = rule.Ingredient;
                element.FindPropertyRelative("perfectMethod").objectReferenceValue = rule.PerfectMethod;
            }
        }

        private static void SetPreparationOptions(SerializedObject serialized, IReadOnlyList<PreparationOptionDraft> options)
        {
            SerializedProperty property = serialized.FindProperty("preparationOptions");
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (options == null)
                return;

            for (int i = 0; i < options.Count; i++)
            {
                PreparationOptionDraft option = options[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("method").objectReferenceValue = option.Method;
                element.FindPropertyRelative("displayNameOverride").stringValue = option.DisplayNameOverride ?? string.Empty;
                element.FindPropertyRelative("description").stringValue = option.Description ?? string.Empty;
                SetRelativeObjectArray(element.FindPropertyRelative("addTags"), option.AddTags);
                SetRelativeObjectArray(element.FindPropertyRelative("removeTags"), option.RemoveTags);
                element.FindPropertyRelative("qualityDelta").intValue = option.QualityDelta;
                element.FindPropertyRelative("causesDisgusting").boolValue = option.CausesDisgusting;
                element.FindPropertyRelative("addsPoison").boolValue = option.AddsPoison;
                element.FindPropertyRelative("resultNameModifier").stringValue = option.ResultNameModifier ?? string.Empty;
            }
        }

        private static void SetRelativeObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = values[i];
            }
        }

        private static List<T> ReadObjectArray<T>(SerializedObject serialized, string propertyName)
            where T : UnityEngine.Object
        {
            return ReadObjectArray<T>(serialized.FindProperty(propertyName));
        }

        private static List<T> ReadObjectArray<T>(SerializedProperty property)
            where T : UnityEngine.Object
        {
            List<T> values = new List<T>();
            if (property == null || property.isArray == false)
                return values;

            for (int i = 0; i < property.arraySize; i++)
                values.Add(property.GetArrayElementAtIndex(i).objectReferenceValue as T);

            return values;
        }

        private static string ReadString(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.stringValue : string.Empty;
        }

        private static int ReadInt(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.intValue : 0;
        }

        private static T ReadObject<T>(SerializedObject serialized, string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static string ReadRelativeString(SerializedProperty property, string propertyName)
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null ? relative.stringValue : string.Empty;
        }

        private static T ReadRelativeObject<T>(SerializedProperty property, string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null ? relative.objectReferenceValue as T : null;
        }

        private static bool ReadRelativeBool(SerializedProperty property, string propertyName)
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null && relative.boolValue;
        }

        private static int ReadRelativeInt(SerializedProperty property, string propertyName)
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null ? relative.intValue : 0;
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = NormalizeFolder(folder);
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string[] parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new InvalidOperationException("에셋 생성 위치는 Assets 폴더 안이어야 합니다.");

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (AssetDatabase.IsValidFolder(next) == false)
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return DefaultAssetFolder;

            return folder.Replace('\\', '/').TrimEnd('/');
        }

        private sealed class RecipeDraft
        {
            public string RecipeId;
            public string DisplayName;
            public string Description;
            public FoodCategorySO Category;
            public int Priority;
            public List<FoodTagSO> BaseTags = new List<FoodTagSO>();
            public List<IngredientRequirementDraft> RequiredIngredients = new List<IngredientRequirementDraft>();
            public List<PerfectRuleDraft> PerfectRules = new List<PerfectRuleDraft>();

            public static RecipeDraft From(RecipeSO recipe)
            {
                SerializedObject serialized = new SerializedObject(recipe);
                RecipeDraft draft = new RecipeDraft
                {
                    RecipeId = ReadString(serialized, "recipeId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Description = ReadString(serialized, "description"),
                    Category = ReadObject<FoodCategorySO>(serialized, "category"),
                    Priority = ReadInt(serialized, "priority"),
                    BaseTags = ReadObjectArray<FoodTagSO>(serialized, "baseTags")
                };

                for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
                {
                    RecipeIngredientRequirement source = recipe.RequiredIngredients[i];
                    IngredientRequirementDraft requirement = new IngredientRequirementDraft();
                    if (source != null)
                    {
                        requirement.Ingredient = source.Ingredient;
                        requirement.IngredientCategory = source.IngredientCategory;
                        requirement.RequiredTags = new List<FoodTagSO>(source.RequiredTags);
                        requirement.SimpleAlternatives = new List<IngredientSO>(source.Alternatives);
                        requirement.RequiredPreparationMethod = source.RequiredPreparationMethod;
                        requirement.MinCount = source.MinCount;
                        requirement.MaxCount = source.MaxCount;
                        requirement.RecipeDefining = source.RecipeDefining;
                        requirement.AutoApplyRequiredPreparation = source.AutoApplyRequiredPreparation;
                        requirement.RequireManualPreparation = source.RequireManualPreparation;
                        requirement.UsePreparationResultNameModifier = source.UsePreparationResultNameModifier;

                        for (int alternativeIndex = 0; alternativeIndex < source.AlternativeOptions.Count; alternativeIndex++)
                        {
                            RecipeIngredientAlternative alternative = source.AlternativeOptions[alternativeIndex];
                            if (alternative != null)
                            {
                                requirement.Alternatives.Add(new IngredientAlternativeDraft
                                {
                                    Ingredient = alternative.Ingredient,
                                    ResultNameModifier = alternative.ResultNameModifier
                                });
                            }
                        }

                    }

                    draft.RequiredIngredients.Add(requirement);
                }

                for (int i = 0; i < recipe.PerfectPreparationRules.Count; i++)
                {
                    RecipePreparationRule source = recipe.PerfectPreparationRules[i];
                    PerfectRuleDraft rule = new PerfectRuleDraft();
                    if (source != null)
                    {
                        rule.Ingredient = source.Ingredient;
                        rule.PerfectMethod = source.PerfectMethod;
                    }

                    draft.PerfectRules.Add(rule);
                }

                return draft;
            }
        }

        private sealed class CategoryDraft
        {
            public string CategoryId;
            public string DisplayName;
            public Sprite Icon;
            public string Description;

            public static CategoryDraft From(FoodCategorySO category)
            {
                SerializedObject serialized = new SerializedObject(category);
                return new CategoryDraft
                {
                    CategoryId = ReadString(serialized, "categoryId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Icon = ReadObject<Sprite>(serialized, "icon"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class IngredientCategoryDraft
        {
            public string CategoryId;
            public string DisplayName;
            public Sprite Icon;
            public string Description;

            public static IngredientCategoryDraft From(IngredientCategorySO category)
            {
                SerializedObject serialized = new SerializedObject(category);
                return new IngredientCategoryDraft
                {
                    CategoryId = ReadString(serialized, "categoryId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Icon = ReadObject<Sprite>(serialized, "icon"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class TagDraft
        {
            public string TagId;
            public string DisplayName;
            public string Description;

            public static TagDraft From(FoodTagSO tag)
            {
                SerializedObject serialized = new SerializedObject(tag);
                return new TagDraft
                {
                    TagId = ReadString(serialized, "tagId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class MethodDraft
        {
            public string MethodId;
            public string DisplayName;
            public string Description;

            public static MethodDraft From(PreparationMethodSO method)
            {
                SerializedObject serialized = new SerializedObject(method);
                return new MethodDraft
                {
                    MethodId = ReadString(serialized, "methodId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Description = ReadString(serialized, "description")
                };
            }
        }

        private sealed class IngredientDraft
        {
            public string IngredientId;
            public string DisplayName;
            public string Description;
            public IngredientCategorySO Category;
            public GameObject ModelPrefab;
            public List<FoodTagSO> BaseTags = new List<FoodTagSO>();
            public List<PreparationOptionDraft> PreparationOptions = new List<PreparationOptionDraft>();

            public static IngredientDraft From(IngredientSO ingredient)
            {
                SerializedObject serialized = new SerializedObject(ingredient);
                IngredientDraft draft = new IngredientDraft
                {
                    IngredientId = ReadString(serialized, "ingredientId"),
                    DisplayName = ReadString(serialized, "displayName"),
                    Description = ReadString(serialized, "description"),
                    Category = ReadObject<IngredientCategorySO>(serialized, "category"),
                    ModelPrefab = ReadObject<GameObject>(serialized, "modelPrefab"),
                    BaseTags = ReadObjectArray<FoodTagSO>(serialized, "baseTags")
                };

                SerializedProperty options = serialized.FindProperty("preparationOptions");
                if (options != null && options.isArray)
                {
                    for (int i = 0; i < options.arraySize; i++)
                    {
                        SerializedProperty element = options.GetArrayElementAtIndex(i);
                        draft.PreparationOptions.Add(new PreparationOptionDraft
                        {
                            Method = ReadRelativeObject<PreparationMethodSO>(element, "method"),
                            DisplayNameOverride = ReadRelativeString(element, "displayNameOverride"),
                            Description = ReadRelativeString(element, "description"),
                            AddTags = ReadObjectArray<FoodTagSO>(element.FindPropertyRelative("addTags")),
                            RemoveTags = ReadObjectArray<FoodTagSO>(element.FindPropertyRelative("removeTags")),
                            QualityDelta = ReadRelativeInt(element, "qualityDelta"),
                            CausesDisgusting = ReadRelativeBool(element, "causesDisgusting"),
                            AddsPoison = ReadRelativeBool(element, "addsPoison"),
                            ResultNameModifier = ReadRelativeString(element, "resultNameModifier")
                        });
                    }
                }

                return draft;
            }
        }

        private sealed class IngredientRequirementDraft
        {
            public IngredientSO Ingredient;
            public IngredientCategorySO IngredientCategory;
            public List<FoodTagSO> RequiredTags = new List<FoodTagSO>();
            public List<IngredientSO> SimpleAlternatives = new List<IngredientSO>();
            public List<IngredientAlternativeDraft> Alternatives = new List<IngredientAlternativeDraft>();
            public PreparationMethodSO RequiredPreparationMethod;
            public int MinCount = 1;
            public int MaxCount = 1;
            public bool RecipeDefining = true;
            public bool AutoApplyRequiredPreparation = true;
            public bool RequireManualPreparation;
            public bool UsePreparationResultNameModifier = true;
        }

        private sealed class IngredientAlternativeDraft
        {
            public IngredientSO Ingredient;
            public string ResultNameModifier;
        }

        private sealed class PerfectRuleDraft
        {
            public IngredientSO Ingredient;
            public PreparationMethodSO PerfectMethod;
        }

        private sealed class PreparationOptionDraft
        {
            public PreparationMethodSO Method;
            public string DisplayNameOverride;
            public string Description;
            public List<FoodTagSO> AddTags = new List<FoodTagSO>();
            public List<FoodTagSO> RemoveTags = new List<FoodTagSO>();
            public int QualityDelta;
            public bool CausesDisgusting;
            public bool AddsPoison;
            public string ResultNameModifier;
        }
    }
}
