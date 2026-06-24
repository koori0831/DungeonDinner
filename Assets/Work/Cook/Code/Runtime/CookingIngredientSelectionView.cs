using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingIngredientSelectionView : MonoBehaviour, ICookingIngredientSelectionView
    {
        private const int CurrentDefaultLayoutVersion = 7;
        private const float SELECTED_GRID_BUTTON_SCALE = 1.07f;

        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;
        [SerializeField] private MonoBehaviour ingredientSourceBehaviour;
        [SerializeField] private bool searchIngredientSourceInParents = true;
        [SerializeField] private bool searchIngredientSourceInChildren = true;

        [Header("Layout References")]
        [SerializeField] private RectTransform availableIngredientRoot;
        [SerializeField] private RectTransform selectedIngredientRoot;
        [SerializeField] private TMP_InputField searchInputField;
        [SerializeField] private TextMeshProUGUI availableSummaryField;
        [SerializeField] private TextMeshProUGUI selectedSummaryField;
        [SerializeField] private TextMeshProUGUI selectionRuleField;
        [SerializeField] private TextMeshProUGUI ingredientDetailField;
        [SerializeField] private TextMeshProUGUI emptyAvailableField;
        [SerializeField] private TextMeshProUGUI emptySelectedField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button clearButton;

        [Header("Selection Rules")]
        [SerializeField, Min(0)] private int minSelectedIngredients = 1;
        [SerializeField, Min(0)] private int maxSelectedIngredients;
        [SerializeField] private bool showIngredientQuantities = true;
        [SerializeField] private bool hideUnavailableIngredients = true;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private bool rebuildTemporaryDefaultLayoutWhenVersionChanges = true;
        [SerializeField] private int defaultLayoutVersion;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite labelSprite;
        [SerializeField] private Sprite ingredientButtonSprite;
        [SerializeField] private Color panelColor = new Color(0.20f, 0.12f, 0.065f, 0.94f);
        [SerializeField] private Color sectionColor = new Color(0.30f, 0.20f, 0.12f, 0.96f);
        [SerializeField] private Color defaultButtonColor = new Color(0.68f, 0.50f, 0.30f, 1f);
        [SerializeField] private Color selectedButtonColor = new Color(0.60f, 0.72f, 0.42f, 1f);
        [SerializeField] private Color disabledButtonColor = new Color(0.36f, 0.33f, 0.29f, 1f);

        [Header("Text")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private string titleText = "재료 직접 선택";
        [SerializeField] private string availableTitleText = "가방";
        [SerializeField] private string selectedTitleText = "선택한 재료";
        [SerializeField] private string searchPlaceholderText = "재료 검색";
        [SerializeField] private string emptyAvailableText = "사용 가능한 재료 없음";
        [SerializeField] private string emptySearchResultText = "검색 조건에 맞는 재료 없음";
        [SerializeField] private string emptySelectedText = "선택된 재료 없음";
        [SerializeField] private string emptyIngredientDetailText = "재료에 마우스를 올리면 정보가 표시됩니다.";
        [SerializeField] private string confirmText = "재료 확정";
        [SerializeField] private string clearText = "비우기";

        private bool _isSubscribed;
        private ICookingIngredientSource _runtimeIngredientSource;
        private ICookingIngredientSource _subscribedIngredientSource;
        private IngredientSO _focusedIngredient;
        private string _searchQuery = string.Empty;
        private static Sprite _generatedFallbackSprite;

        private void OnValidate()
        {
            minSelectedIngredients = Mathf.Max(0, minSelectedIngredients);

            if (maxSelectedIngredients > 0 && maxSelectedIngredients < minSelectedIngredients)
                maxSelectedIngredients = minSelectedIngredients;
        }

        private void Awake()
        {
            EnsureReferences();
            EnsureLayout();
            BindFixedButtons();
            BindSearchField();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureLayout();
            BindFixedButtons();
            BindSearchField();
            SubscribeFlowEvents();
            SubscribeIngredientSourceEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFlowEvents();
            UnsubscribeIngredientSourceEvents();
        }

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            flowRunner = runner;

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureLayout();
            BindFixedButtons();
            BindSearchField();

            if (isActiveAndEnabled)
            {
                SubscribeFlowEvents();
                Refresh();
            }
        }

        public void SetIngredientSource(ICookingIngredientSource source)
        {
            if (_runtimeIngredientSource == source)
                return;

            UnsubscribeIngredientSourceEvents();
            _runtimeIngredientSource = source;

            if (isActiveAndEnabled)
                SubscribeIngredientSourceEvents();

            Refresh();
        }

        public void SetSelectionLimits(int minCount, int maxCount = 0)
        {
            minSelectedIngredients = Mathf.Max(0, minCount);
            maxSelectedIngredients = Mathf.Max(0, maxCount);

            if (maxSelectedIngredients > 0 && maxSelectedIngredients < minSelectedIngredients)
                maxSelectedIngredients = minSelectedIngredients;

            Refresh();
        }

        public void SetSearchQuery(string query)
        {
            _searchQuery = query ?? string.Empty;
            EnsureReferences();
            EnsureLayout();

            if (searchInputField != null && searchInputField.text != _searchQuery)
                searchInputField.SetTextWithoutNotify(_searchQuery);

            BindSearchField();
            Refresh();
        }

        public ICookingIngredientSource GetCurrentIngredientSource()
        {
            return ResolveIngredientSource();
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            ApplyFontToExistingTexts();
        }

        public void Refresh()
        {
            EnsureReferences();
            EnsureLayout();

            if (flowRunner == null)
            {
                SetConfirmInteractable(false);
                SetText(selectedSummaryField, "재료 데이터 없음");
                SetText(selectionRuleField, BuildSelectionRuleText(0));
                SetText(emptySelectedField, emptySelectedText);
                SetText(ingredientDetailField, emptyIngredientDetailText);
                return;
            }

            IReadOnlyList<IngredientSO> selectedIngredients = flowRunner.SelectedIngredients;
            IReadOnlyList<IngredientSO> availableIngredients = GetAvailableIngredients();
            RebuildAvailableIngredients(availableIngredients, selectedIngredients);
            RebuildSelectedIngredients(selectedIngredients);
            BindAvailableSummary(availableIngredients);
            SetConfirmInteractable(IsSelectionCountValid(selectedIngredients));
            BindFocusedIngredientDetail();
        }

        public void ToggleIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            if (ContainsIngredient(flowRunner.SelectedIngredients, ingredient))
                flowRunner.RemoveDirectIngredient(ingredient);
            else
            {
                if (CanSelectMore(flowRunner.SelectedIngredients) == false)
                    return;

                if (flowRunner.Controller.CurrentSession?.Mode == CookingMode.Recipe)
                    flowRunner.AddRecipeIngredient(ingredient);
                else
                    flowRunner.AddDirectIngredient(ingredient);
            }

            Refresh();
        }

        public void RemoveIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            flowRunner.RemoveDirectIngredient(ingredient);
            Refresh();
        }

        public void ClearSelection()
        {
            if (flowRunner == null)
                return;

            List<IngredientSO> selected = new List<IngredientSO>(flowRunner.SelectedIngredients);
            for (int i = 0; i < selected.Count; i++)
                flowRunner.RemoveDirectIngredient(selected[i]);

            Refresh();
        }

        public void ConfirmSelection()
        {
            if (IsSelectionCountValid(flowRunner?.SelectedIngredients) == false)
            {
                Refresh();
                return;
            }

            if (gamePanel != null)
            {
                gamePanel.ConfirmDirectIngredients();
                return;
            }

            flowRunner?.ConfirmDirectIngredients();
        }

        private void RebuildAvailableIngredients(
            IReadOnlyList<IngredientSO> ingredients,
            IReadOnlyList<IngredientSO> selectedIngredients)
        {
            ClearChildren(availableIngredientRoot);

            if (availableIngredientRoot == null || ingredients == null)
                return;

            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientSO ingredient = ingredients[i];
                if (ingredient == null)
                    continue;

                if (MatchesSearch(ingredient) == false)
                    continue;

                int availableQuantity = GetAvailableQuantity(ingredient);
                if (hideUnavailableIngredients && availableQuantity <= 0)
                    continue;

                bool selected = ContainsIngredient(selectedIngredients, ingredient);
                Button button = CreateIngredientButton(
                    availableIngredientRoot,
                    BuildAvailableIngredientLabel(ingredient, availableQuantity),
                    GetIngredientIcon(ingredient),
                    selected ? selectedButtonColor : defaultButtonColor,
                    () => ToggleIngredient(ingredient),
                    selected);
                button.interactable = selected == true
                                      || availableQuantity > 0 && CanSelectMore(selectedIngredients) == true;
                BindIngredientPointerEvents(button.gameObject, ingredient);
            }
        }

        private void BindAvailableSummary(IReadOnlyList<IngredientSO> ingredients)
        {
            int count = CountDisplayableIngredients(ingredients);
            ICookingIngredientSource source = ResolveIngredientSource();
            string sourceName = source != null ? source.SourceName : "카탈로그 전체";

            SetText(availableSummaryField, $"{availableTitleText} {count} ({sourceName})");
            SetText(emptyAvailableField, count == 0 ? BuildEmptyAvailableText() : string.Empty);
        }

        private void RebuildSelectedIngredients(IReadOnlyList<IngredientSO> selectedIngredients)
        {
            ClearChildren(selectedIngredientRoot);

            int selectedCount = selectedIngredients != null ? selectedIngredients.Count : 0;
            SetText(selectedSummaryField, BuildSelectedSummaryText(selectedCount));
            SetText(selectionRuleField, BuildSelectionRuleText(selectedCount));
            SetText(emptySelectedField, selectedCount == 0 ? emptySelectedText : string.Empty);

            if (selectedIngredientRoot == null || selectedIngredients == null)
                return;

            for (int i = 0; i < selectedIngredients.Count; i++)
            {
                IngredientSO ingredient = selectedIngredients[i];
                if (ingredient == null)
                    continue;

                Button button = CreateIngredientButton(
                    selectedIngredientRoot,
                    ingredient.DisplayName,
                    GetIngredientIcon(ingredient),
                    selectedButtonColor,
                    () => RemoveIngredient(ingredient),
                    false);
                BindIngredientPointerEvents(button.gameObject, ingredient);
            }
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (flowRunner == null)
                flowRunner = gamePanel != null ? gamePanel.FlowRunner : GetComponentInParent<CookingFlowRunner>();
        }

        private IReadOnlyList<IngredientSO> GetAvailableIngredients()
        {
            ICookingIngredientSource source = ResolveIngredientSource();
            if (source != null)
            {
                IReadOnlyList<IngredientSO> ingredients = source.GetAvailableIngredients(gamePanel, flowRunner);
                if (ingredients != null)
                    return ingredients;
            }

            return flowRunner != null ? flowRunner.Ingredients : Array.Empty<IngredientSO>();
        }

        private ICookingIngredientSource ResolveIngredientSource()
        {
            if (_runtimeIngredientSource != null)
                return _runtimeIngredientSource;

            ICookingIngredientSource source = ingredientSourceBehaviour as ICookingIngredientSource;
            if (source != null)
                return source;

            if (searchIngredientSourceInParents)
            {
                source = FindIngredientSource(GetComponentsInParent<MonoBehaviour>(true));
                if (source != null)
                    return source;
            }

            return searchIngredientSourceInChildren
                ? FindIngredientSource(GetComponentsInChildren<MonoBehaviour>(true))
                : null;
        }

        private int GetAvailableQuantity(IngredientSO ingredient)
        {
            if (ingredient == null)
                return 0;

            ICookingIngredientQuantitySource quantitySource = ResolveIngredientSource() as ICookingIngredientQuantitySource;
            if (quantitySource == null)
                return 1;

            return Mathf.Max(0, quantitySource.GetAvailableIngredientQuantity(ingredient, gamePanel, flowRunner));
        }

        private Sprite GetIngredientIcon(IngredientSO ingredient)
        {
            if (ingredient == null)
            {
                return null;
            }

            ICookingIngredientIconSource iconSource = ResolveIngredientSource() as ICookingIngredientIconSource;
            if (iconSource != null)
            {
                Sprite icon = iconSource.GetAvailableIngredientIcon(ingredient, gamePanel, flowRunner);
                if (icon != null)
                {
                    return icon;
                }
            }

            return CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
        }

        private string BuildAvailableIngredientLabel(IngredientSO ingredient, int availableQuantity)
        {
            string displayName = ingredient != null ? ingredient.DisplayName : string.Empty;
            if (showIngredientQuantities == false)
                return displayName;

            return $"{displayName} x{availableQuantity}";
        }

        private void BindFocusedIngredientDetail()
        {
            if (_focusedIngredient == null)
            {
                SetText(ingredientDetailField, emptyIngredientDetailText);
                return;
            }

            SetText(ingredientDetailField, BuildIngredientDetailText(_focusedIngredient));
        }

        private void FocusIngredient(IngredientSO ingredient)
        {
            _focusedIngredient = ingredient;
            BindFocusedIngredientDetail();
        }

        private void ClearFocusedIngredient(IngredientSO ingredient)
        {
            if (_focusedIngredient != ingredient)
                return;

            _focusedIngredient = null;
            BindFocusedIngredientDetail();
        }

        private string BuildIngredientDetailText(IngredientSO ingredient)
        {
            if (ingredient == null)
                return emptyIngredientDetailText;

            StringBuilder builder = new StringBuilder();
            builder.Append(ingredient.DisplayName);
            builder.Append($"  x{GetAvailableQuantity(ingredient)}");

            if (string.IsNullOrWhiteSpace(ingredient.Description) == false)
                builder.AppendLine().Append(ingredient.Description);

            builder.AppendLine();
            builder.Append("태그: ");
            builder.Append(BuildTagListText(ingredient.BaseTags));
            builder.AppendLine();
            builder.Append("손질법: ");
            builder.Append(BuildPreparationOptionListText(ingredient.PreparationOptions));
            return builder.ToString();
        }

        private string BuildTagListText(IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return "없음";

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    names.Add(tags[i].DisplayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : "없음";
        }

        private string BuildPreparationOptionListText(IReadOnlyList<IngredientPreparationOption> options)
        {
            if (options == null || options.Count == 0)
                return "없음";

            List<string> names = new List<string>();
            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                if (option != null && string.IsNullOrWhiteSpace(option.DisplayName) == false)
                    names.Add(option.DisplayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : "없음";
        }

        private void HandleSearchChanged(string value)
        {
            _searchQuery = value ?? string.Empty;
            Refresh();
        }

        private bool MatchesSearch(IngredientSO ingredient)
        {
            if (ingredient == null)
                return false;

            string query = NormalizeSearch(_searchQuery);
            if (string.IsNullOrWhiteSpace(query))
                return true;

            if (ContainsSearchText(ingredient.DisplayName, query)
                || ContainsSearchText(ingredient.Description, query)
                || ContainsTagSearchText(ingredient.BaseTags, query)
                || ContainsPreparationSearchText(ingredient.PreparationOptions, query))
            {
                return true;
            }

            return false;
        }

        private string BuildEmptyAvailableText()
        {
            return string.IsNullOrWhiteSpace(_searchQuery) ? emptyAvailableText : emptySearchResultText;
        }

        private static bool ContainsTagSearchText(IReadOnlyList<FoodTagSO> tags, string query)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                FoodTagSO tag = tags[i];
                if (tag != null
                    && (ContainsSearchText(tag.DisplayName, query)
                        || ContainsSearchText(tag.TagId, query)
                        || ContainsSearchText(tag.Description, query)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsPreparationSearchText(
            IReadOnlyList<IngredientPreparationOption> options,
            string query)
        {
            if (options == null)
                return false;

            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                PreparationMethodSO method = option?.Method;
                if (option != null
                    && (ContainsSearchText(option.DisplayName, query)
                        || ContainsSearchText(option.Description, query)
                        || ContainsSearchText(method != null ? method.DisplayName : string.Empty, query)
                        || ContainsSearchText(method != null ? method.MethodId : string.Empty, query)
                        || ContainsSearchText(method != null ? method.Description : string.Empty, query)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSearchText(string value, string query)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(query))
                return false;

            return NormalizeSearch(value).Contains(query);
        }

        private static string NormalizeSearch(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private void EnsureLayout()
        {
            if (buildDefaultLayoutWhenMissing == false)
                return;

            if (ShouldRebuildTemporaryDefaultLayout())
            {
                ClearChildren(transform);
                availableIngredientRoot = null;
                selectedIngredientRoot = null;
                searchInputField = null;
                availableSummaryField = null;
                selectedSummaryField = null;
                selectionRuleField = null;
                ingredientDetailField = null;
                emptyAvailableField = null;
                emptySelectedField = null;
                confirmButton = null;
                clearButton = null;
            }

            if (availableIngredientRoot != null
                && selectedIngredientRoot != null
                && selectedSummaryField != null
                && confirmButton != null)
            {
                EnsureSupplementalLayout();
                return;
            }

            BuildDefaultLayout();
            defaultLayoutVersion = CurrentDefaultLayoutVersion;
        }

        private bool ShouldRebuildTemporaryDefaultLayout()
        {
            return rebuildTemporaryDefaultLayoutWhenVersionChanges
                   && defaultLayoutVersion < CurrentDefaultLayoutVersion
                   && gameObject.name.Contains("TemporaryIngredientSelectionView");
        }

        private void EnsureSupplementalLayout()
        {
            EnsureAvailableSupplementalLayout();
            EnsureSelectedSupplementalLayout();
        }

        private void EnsureAvailableSupplementalLayout()
        {
            Transform bagSection = FindSectionRoot(availableIngredientRoot);
            if (bagSection == null)
                return;

            if (availableSummaryField == null)
                availableSummaryField = bagSection.GetComponentInChildren<TextMeshProUGUI>(true);

            if (searchInputField == null)
            {
                searchInputField = FindNamedInputField(bagSection, "IngredientSearchField");
                if (searchInputField == null)
                {
                    searchInputField = CreateSearchInput(bagSection);
                    searchInputField.transform.SetSiblingIndex(Mathf.Min(1, bagSection.childCount - 1));
                }
            }

            if (ingredientDetailField == null)
            {
                ingredientDetailField = FindNamedText(bagSection, "IngredientDetail");
                if (ingredientDetailField == null)
                {
                    ingredientDetailField = CreateText(
                        bagSection,
                        "IngredientDetail",
                        emptyIngredientDetailText,
                        13f,
                        TextAlignmentOptions.TopLeft);
                    ingredientDetailField.transform.SetSiblingIndex(Mathf.Min(1, bagSection.childCount - 1));
                    AddLayoutElement(ingredientDetailField.gameObject, -1f, 92f, -1f, 0f);
                }

                ingredientDetailField.textWrappingMode = TextWrappingModes.Normal;
                ingredientDetailField.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (emptyAvailableField == null)
            {
                emptyAvailableField = FindNamedText(bagSection, "EmptyAvailable");
                if (emptyAvailableField == null)
                {
                    emptyAvailableField = CreateText(
                        bagSection,
                        "EmptyAvailable",
                        emptyAvailableText,
                        15f,
                        TextAlignmentOptions.Center);
                    AddLayoutElement(emptyAvailableField.gameObject, -1f, 28f, -1f, 0f);
                }
            }
        }

        private void EnsureSelectedSupplementalLayout()
        {
            Transform selectedSection = FindSectionRoot(selectedIngredientRoot);
            if (selectedSection == null)
                return;

            EnsureScrollContentScrollbar(selectedIngredientRoot);

            if (selectionRuleField == null)
            {
                selectionRuleField = FindNamedText(selectedSection, "SelectionRule");
                if (selectionRuleField == null)
                {
                    selectionRuleField = CreateText(
                        selectedSection,
                        "SelectionRule",
                        string.Empty,
                        13f,
                        TextAlignmentOptions.Left);
                    selectionRuleField.transform.SetSiblingIndex(Mathf.Min(1, selectedSection.childCount - 1));
                    AddLayoutElement(selectionRuleField.gameObject, -1f, 24f, -1f, 0f);
                }
            }
        }

        private void BuildDefaultLayout()
        {
            RectTransform rect = EnsureRectTransform(gameObject);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(820f, 330f);
            rect.anchoredPosition = new Vector2(820f, 18f);

            Image background = GetOrAdd<Image>(gameObject);
            ApplyGeneratedSprite(background);
            background.color = new Color(0f, 0f, 0f, 0f);
            background.raycastTarget = true;

            VerticalLayoutGroup rootLayout = GetOrAdd<VerticalLayoutGroup>(gameObject);
            rootLayout.padding = new RectOffset(24, 24, 18, 22);
            rootLayout.spacing = 10f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            RectTransform header = CreateLayoutObject(transform, "BagHeader");
            HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 10f;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            AddLayoutElement(header.gameObject, -1f, 34f, -1f, 0f);

            TextMeshProUGUI title = CreateText(header, "Title", titleText, 22f, TextAlignmentOptions.Left);
            AddLayoutElement(title.gameObject, 220f, 34f, 0f, 0f);

            TextMeshProUGUI hint = CreateText(header, "Hint", "대화창 아래에서 가방을 열어 재료를 고릅니다.", 13f, TextAlignmentOptions.Left);
            hint.color = new Color(1f, 0.90f, 0.74f, 0.82f);
            AddLayoutElement(hint.gameObject, -1f, 34f, 1f, 0f);

            RectTransform body = CreateLayoutObject(transform, "Body");
            HorizontalLayoutGroup bodyLayout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 12f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;
            AddLayoutElement(body.gameObject, -1f, -1f, 1f, 1f);

            RectTransform bagPanel = CreateSection(body, "BagSection", availableTitleText);
            availableSummaryField = bagPanel.GetComponentInChildren<TextMeshProUGUI>();
            searchInputField = CreateSearchInput(bagPanel);
            ingredientDetailField = CreateText(bagPanel, "IngredientDetail", emptyIngredientDetailText, 13f, TextAlignmentOptions.TopLeft);
            ingredientDetailField.textWrappingMode = TextWrappingModes.Normal;
            ingredientDetailField.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(ingredientDetailField.gameObject, -1f, 58f, -1f, 0f);
            availableIngredientRoot = CreateGridScrollContent(bagPanel, "AvailableIngredients");
            emptyAvailableField = CreateText(bagPanel, "EmptyAvailable", emptyAvailableText, 15f, TextAlignmentOptions.Center);
            AddLayoutElement(emptyAvailableField.gameObject, -1f, 28f, -1f, 0f);
            AddLayoutElement(bagPanel.gameObject, 0f, -1f, 2.6f, 1f);

            RectTransform selectedPanel = CreateSection(body, "SelectedSection", selectedTitleText);
            selectedSummaryField = selectedPanel.GetComponentInChildren<TextMeshProUGUI>();
            selectionRuleField = CreateText(selectedPanel, "SelectionRule", string.Empty, 13f, TextAlignmentOptions.Left);
            AddLayoutElement(selectionRuleField.gameObject, -1f, 24f, -1f, 0f);
            selectedIngredientRoot = CreateScrollContent(selectedPanel, "SelectedIngredients");
            emptySelectedField = CreateText(selectedPanel, "EmptySelected", emptySelectedText, 15f, TextAlignmentOptions.Center);
            AddLayoutElement(emptySelectedField.gameObject, -1f, 28f, -1f, 0f);
            AddLayoutElement(selectedPanel.gameObject, 0f, -1f, 1f, 1f);

            RectTransform actionRow = CreateLayoutObject(selectedPanel, "ActionRow");
            HorizontalLayoutGroup actionLayout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 8f;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;
            AddLayoutElement(actionRow.gameObject, -1f, 44f, -1f, 0f);

            clearButton = CreateActionButton(actionRow, clearText, ClearSelection, defaultButtonColor);
            confirmButton = CreateActionButton(actionRow, confirmText, ConfirmSelection, selectedButtonColor);
        }

        private RectTransform CreateSection(Transform parent, string name, string title)
        {
            RectTransform section = CreateLayoutObject(parent, name);
            Image image = section.gameObject.AddComponent<Image>();
            ApplyUiAssetSprite(image, panelSprite);
            bool isBag = string.Equals(name, "BagSection", StringComparison.OrdinalIgnoreCase);
            bool isPocket = string.Equals(name, "SelectedSection", StringComparison.OrdinalIgnoreCase);
            image.color = panelSprite != null
                ? Color.white
                : isBag == true
                    ? new Color(0.36f, 0.22f, 0.12f, 0.98f)
                    : isPocket == true ? new Color(0.18f, 0.12f, 0.08f, 0.96f) : sectionColor;

            Shadow shadow = section.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(2f, -5f);

            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = isBag ? new RectOffset(22, 22, 12, 18) : new RectOffset(16, 16, 14, 16);
            layout.spacing = isBag ? 8f : 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (isBag)
                CreateBagMouth(section);

            TextMeshProUGUI label = CreateText(section, "SectionTitle", title, 18f, TextAlignmentOptions.Left);
            label.color = isBag ? new Color(1f, 0.88f, 0.62f, 1f) : Color.white;
            AddLayoutElement(label.gameObject, -1f, 30f, -1f, 0f);
            return section;
        }

        private void CreateBagMouth(Transform parent)
        {
            RectTransform mouth = CreateLayoutObject(parent, "BagMouth");
            Image mouthImage = mouth.gameObject.AddComponent<Image>();
            ApplyUiAssetSprite(mouthImage, labelSprite);
            mouthImage.color = labelSprite != null ? Color.white : new Color(0.16f, 0.085f, 0.045f, 0.96f);
            AddLayoutElement(mouth.gameObject, -1f, 24f, -1f, 0f);

            HorizontalLayoutGroup layout = mouth.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 5, 5);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateMouthStitch(mouth, 44f);
            CreateMouthStitch(mouth, 92f);
            CreateMouthStitch(mouth, 44f);
        }

        private void CreateMouthStitch(Transform parent, float width)
        {
            RectTransform stitch = CreateLayoutObject(parent, "Stitch");
            Image image = stitch.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(image);
            image.color = new Color(0.72f, 0.52f, 0.30f, 0.78f);
            AddLayoutElement(stitch.gameObject, width, 7f, 0f, 0f);
        }

        private RectTransform CreateScrollContent(Transform parent, string name)
        {
            RectTransform viewport = CreateLayoutObject(parent, $"{name}Viewport");
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(viewportImage);
            viewportImage.color = new Color(0f, 0f, 0f, 0.18f);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddLayoutElement(viewport.gameObject, -1f, -1f, 1f, 1f);

            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 18f;

            RectTransform content = CreateLayoutObject(viewport, name);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 18, 8, 8);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = CreateVerticalScrollbar(viewport);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return content;
        }

        private RectTransform CreateGridScrollContent(Transform parent, string name)
        {
            RectTransform viewport = CreateLayoutObject(parent, $"{name}Viewport");
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(viewportImage);
            viewportImage.color = new Color(0.08f, 0.045f, 0.025f, 0.38f);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddLayoutElement(viewport.gameObject, -1f, -1f, 1f, 1f);

            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 18f;

            RectTransform content = CreateLayoutObject(viewport, name);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.spacing = new Vector2(12f, 12f);
            grid.cellSize = new Vector2(100f, 74f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        private Button CreateIngredientButton(
            Transform parent,
            string label,
            Sprite icon,
            Color color,
            UnityEngine.Events.UnityAction action,
            bool selected)
        {
            Button button = CreateActionButton(parent, label, action, color);
            ApplyIngredientButtonVisual(button, color);
            bool inGrid = parent != null && parent.GetComponent<GridLayoutGroup>() != null;
            AddLayoutElement(button.gameObject, -1f, inGrid ? 74f : 42f, -1f, 0f);

            if (icon != null)
            {
                AddIconToIngredientButton(button, icon, inGrid);
            }

            if (selected == true)
            {
                ApplySelectedIngredientVisual(button, inGrid);
            }

            return button;
        }

        private void ApplyIngredientButtonVisual(Button button, Color color)
        {
            if (button == null || ingredientButtonSprite == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            ApplyUiAssetSprite(image, ingredientButtonSprite);
            image.color = Color.white;
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, color, 0.10f);
            colors.pressedColor = Color.Lerp(Color.white, Color.black, 0.16f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = disabledButtonColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private Scrollbar CreateVerticalScrollbar(RectTransform viewport)
        {
            GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(viewport, false);

            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(10f, 0f);

            Image background = scrollbarObject.GetComponent<Image>();
            ApplyGeneratedSprite(background);
            background.color = new Color(0f, 0f, 0f, 0.34f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(2f, 2f);
            handleRect.offsetMax = new Vector2(-2f, -2f);

            Image handleImage = handleObject.GetComponent<Image>();
            ApplyGeneratedSprite(handleImage);
            handleImage.color = new Color(0.92f, 0.72f, 0.46f, 0.86f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;
            return scrollbar;
        }

        private void EnsureScrollContentScrollbar(RectTransform contentRoot)
        {
            if (contentRoot == null || contentRoot.parent == null)
            {
                return;
            }

            RectTransform viewport = contentRoot.parent as RectTransform;
            if (viewport == null)
            {
                return;
            }

            ScrollRect scrollRect = viewport.GetComponent<ScrollRect>();
            if (scrollRect == null || scrollRect.verticalScrollbar != null)
            {
                return;
            }

            scrollRect.verticalScrollbar = CreateVerticalScrollbar(viewport);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            VerticalLayoutGroup contentLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
            if (contentLayout != null)
            {
                contentLayout.padding = new RectOffset(
                    contentLayout.padding.left,
                    Mathf.Max(contentLayout.padding.right, 18),
                    contentLayout.padding.top,
                    contentLayout.padding.bottom);
            }
        }

        private void ApplySelectedIngredientVisual(Button button, bool inGrid)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform != null && inGrid == true)
            {
                rectTransform.localScale = new Vector3(SELECTED_GRID_BUTTON_SCALE, SELECTED_GRID_BUTTON_SCALE, 1f);
            }

            Outline outline = GetOrAdd<Outline>(button.gameObject);
            outline.effectColor = new Color(1f, 0.90f, 0.55f, 0.78f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        private void AddIconToIngredientButton(Button button, Sprite icon, bool inGrid)
        {
            if (button == null || icon == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = inGrid == true ? new Vector2(24f, 24f) : new Vector2(22f, 22f);

            Image image = iconObject.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                return;
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.offsetMin = new Vector2(inGrid == true ? 34f : 32f, 4f);
            labelRect.offsetMax = new Vector2(-6f, -4f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private TMP_InputField CreateSearchInput(Transform parent)
        {
            GameObject inputObject = new GameObject(
                "IngredientSearchField",
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_InputField),
                typeof(LayoutElement));
            inputObject.transform.SetParent(parent, false);

            Image image = inputObject.GetComponent<Image>();
            ApplyUiAssetSprite(image, labelSprite);
            image.color = labelSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.24f);

            TextMeshProUGUI text = CreateText(inputObject.transform, "Text", string.Empty, 15f, TextAlignmentOptions.Left);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(10f, 4f);
            text.rectTransform.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI placeholder = CreateText(
                inputObject.transform,
                "Placeholder",
                searchPlaceholderText,
                15f,
                TextAlignmentOptions.Left);
            placeholder.color = new Color(1f, 1f, 1f, 0.48f);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(10f, 4f);
            placeholder.rectTransform.offsetMax = new Vector2(-10f, -4f);

            TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.text = _searchQuery;

            AddLayoutElement(inputObject, -1f, 34f, -1f, 0f);
            return input;
        }

        private Button CreateActionButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            Color color)
        {
            GameObject buttonObject = new GameObject($"Button_{SanitizeName(label)}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            ApplyUiAssetSprite(image, labelSprite);
            Color visualColor = labelSprite != null ? Color.white : color;
            image.color = visualColor;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = visualColor;
            colors.highlightedColor = Color.Lerp(visualColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(visualColor, Color.black, 0.16f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = disabledButtonColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (action != null)
                button.onClick.AddListener(action);

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 15f, TextAlignmentOptions.Center);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 15f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return button;
        }

        private void BindIngredientPointerEvents(GameObject target, IngredientSO ingredient)
        {
            if (target == null || ingredient == null)
                return;

            EventTrigger trigger = GetOrAdd<EventTrigger>(target);
            trigger.triggers.Clear();

            EventTrigger.Entry enter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            enter.callback.AddListener(_ => FocusIngredient(ingredient));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            exit.callback.AddListener(_ => ClearFocusedIngredient(ingredient));
            trigger.triggers.Add(exit);
        }

        private TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            if (fontAsset != null)
                label.font = fontAsset;
            label.color = Color.white;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private void BindFixedButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmSelection);
                confirmButton.onClick.AddListener(ConfirmSelection);
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveListener(ClearSelection);
                clearButton.onClick.AddListener(ClearSelection);
            }
        }

        private void BindSearchField()
        {
            if (searchInputField == null)
                return;

            searchInputField.onValueChanged.RemoveListener(HandleSearchChanged);
            searchInputField.onValueChanged.AddListener(HandleSearchChanged);
            _searchQuery = searchInputField.text ?? string.Empty;
        }

        private void SetConfirmInteractable(bool interactable)
        {
            if (confirmButton != null)
                confirmButton.interactable = interactable;
        }

        private bool IsSelectionCountValid(IReadOnlyList<IngredientSO> selectedIngredients)
        {
            int count = CountIngredients(selectedIngredients);
            return count >= minSelectedIngredients
                   && (maxSelectedIngredients <= 0 || count <= maxSelectedIngredients);
        }

        private bool CanSelectMore(IReadOnlyList<IngredientSO> selectedIngredients)
        {
            return maxSelectedIngredients <= 0 || CountIngredients(selectedIngredients) < maxSelectedIngredients;
        }

        private string BuildSelectedSummaryText(int selectedCount)
        {
            if (maxSelectedIngredients > 0)
                return $"{selectedTitleText} {selectedCount} / {maxSelectedIngredients}";

            return $"{selectedTitleText} {selectedCount}";
        }

        private string BuildSelectionRuleText(int selectedCount)
        {
            if (maxSelectedIngredients > 0)
            {
                if (selectedCount < minSelectedIngredients)
                    return $"최소 {minSelectedIngredients}개, 최대 {maxSelectedIngredients}개 선택";

                if (selectedCount >= maxSelectedIngredients)
                    return $"최대 {maxSelectedIngredients}개까지 선택했습니다.";

                return $"최소 {minSelectedIngredients}개, 최대 {maxSelectedIngredients}개 선택";
            }

            if (selectedCount < minSelectedIngredients)
                return $"최소 {minSelectedIngredients}개 이상 선택";

            return string.Empty;
        }

        private void SubscribeFlowEvents()
        {
            if (_isSubscribed || flowRunner == null)
                return;

            flowRunner.StateChanged += HandleFlowStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeFlowEvents()
        {
            if (_isSubscribed == false || flowRunner == null)
                return;

            flowRunner.StateChanged -= HandleFlowStateChanged;
            _isSubscribed = false;
        }

        private void SubscribeIngredientSourceEvents()
        {
            ICookingIngredientSource source = ResolveIngredientSource();
            if (_subscribedIngredientSource == source)
                return;

            UnsubscribeIngredientSourceEvents();

            if (source == null)
                return;

            source.IngredientsChanged += HandleIngredientSourceChanged;
            _subscribedIngredientSource = source;
        }

        private void UnsubscribeIngredientSourceEvents()
        {
            if (_subscribedIngredientSource == null)
                return;

            _subscribedIngredientSource.IngredientsChanged -= HandleIngredientSourceChanged;
            _subscribedIngredientSource = null;
        }

        private void HandleFlowStateChanged(CookingFlowState state)
        {
            Refresh();
        }

        private void HandleIngredientSourceChanged()
        {
            Refresh();
        }

        private void ApplyFontToExistingTexts()
        {
            if (fontAsset == null)
                return;

            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].font = fontAsset;
            }
        }

        private static bool ContainsIngredient(IReadOnlyList<IngredientSO> ingredients, IngredientSO ingredient)
        {
            if (ingredients == null || ingredient == null)
                return false;

            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] == ingredient)
                    return true;
            }

            return false;
        }

        private static int CountIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null)
                return 0;

            int count = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] != null)
                    count++;
            }

            return count;
        }

        private int CountDisplayableIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null)
                return 0;

            int count = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientSO ingredient = ingredients[i];
                if (ingredient == null)
                    continue;

                if (MatchesSearch(ingredient) == false)
                    continue;

                if (hideUnavailableIngredients && GetAvailableQuantity(ingredient) <= 0)
                    continue;

                count++;
            }

            return count;
        }

        private static ICookingIngredientSource FindIngredientSource(IReadOnlyList<MonoBehaviour> behaviours)
        {
            if (behaviours == null)
                return null;

            for (int i = 0; i < behaviours.Count; i++)
            {
                if (behaviours[i] is ICookingIngredientSource source)
                    return source;
            }

            return null;
        }

        private static Transform FindSectionRoot(RectTransform contentRoot)
        {
            if (contentRoot == null)
                return null;

            Transform viewport = contentRoot.parent;
            return viewport != null ? viewport.parent : null;
        }

        private static TextMeshProUGUI FindNamedText(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
                return null;

            TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].name == objectName)
                    return labels[i];
            }

            return null;
        }

        private static TMP_InputField FindNamedInputField(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
                return null;

            TMP_InputField[] inputFields = root.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < inputFields.Length; i++)
            {
                if (inputFields[i] != null && inputFields[i].name == objectName)
                    return inputFields[i];
            }

            return null;
        }

        private static RectTransform CreateLayoutObject(Transform parent, string name)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            return item.GetComponent<RectTransform>();
        }

        private static RectTransform EnsureRectTransform(GameObject target)
        {
            RectTransform rect = target.transform as RectTransform;
            if (rect != null)
                return rect;

            return target.AddComponent<RectTransform>();
        }

        private static LayoutElement AddLayoutElement(
            GameObject target,
            float preferredWidth,
            float preferredHeight,
            float flexibleWidth,
            float flexibleHeight)
        {
            LayoutElement element = GetOrAdd<LayoutElement>(target);
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            if (target.TryGetComponent(out T component) == true)
                return component;

            return target.AddComponent<T>();
        }

        private static void ApplyGeneratedSprite(Image image)
        {
            if (image == null)
                return;

            if (image.sprite == null)
                image.sprite = GetGeneratedFallbackSprite();

            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private void ApplyUiAssetSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                return;
            }

            ApplyGeneratedSprite(image);
        }

        private static Sprite GetGeneratedFallbackSprite()
        {
            if (_generatedFallbackSprite != null)
                return _generatedFallbackSprite;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "GeneratedCookingUiSpriteTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            _generatedFallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _generatedFallbackSprite.name = "GeneratedCookingUiSprite";
            return _generatedFallbackSprite;
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Empty";

            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsLetterOrDigit(chars[i]) == false)
                    chars[i] = '_';
            }

            return new string(chars);
        }
    }
}
