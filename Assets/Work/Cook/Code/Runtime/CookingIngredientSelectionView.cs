using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingIngredientSelectionView : MonoBehaviour, ICookingIngredientSelectionView
    {
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

        [Header("Prefabs")]
        [SerializeField] private CookingIngredientButtonView availableIngredientButtonPrefab;
        [SerializeField] private CookingIngredientButtonView selectedIngredientButtonPrefab;

        [Header("Selection Rules")]
        [SerializeField, Min(0)] private int minSelectedIngredients = 1;
        [SerializeField, Min(0)] private int maxSelectedIngredients;
        [SerializeField] private bool showIngredientQuantities = true;
        [SerializeField] private bool hideUnavailableIngredients = true;

        [Header("Text")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private string availableTitleText = "가방";
        [SerializeField] private string selectedTitleText = "선택한 재료";
        [SerializeField] private string emptyAvailableText = "사용 가능한 재료 없음";
        [SerializeField] private string emptySearchResultText = "검색 조건에 맞는 재료 없음";
        [SerializeField] private string emptySelectedText = "선택된 재료 없음";
        [SerializeField] private string emptyIngredientDetailText = "재료에 마우스를 올리면 정보가 표시됩니다.";

        private bool _isSubscribed;
        private ICookingIngredientSource _runtimeIngredientSource;
        private ICookingIngredientSource _subscribedIngredientSource;
        private IngredientSO _focusedIngredient;
        private string _searchQuery = string.Empty;

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
                if (hideUnavailableIngredients == true && availableQuantity <= 0)
                    continue;

                bool selected = ContainsIngredient(selectedIngredients, ingredient);
                bool interactable = selected == true
                                    || (availableQuantity > 0 && CanSelectMore(selectedIngredients) == true);
                Button button = CreateIngredientButton(
                    availableIngredientRoot,
                    ingredient,
                    BuildAvailableIngredientLabel(ingredient, availableQuantity),
                    GetIngredientIcon(ingredient),
                    () => ToggleIngredient(ingredient),
                    interactable,
                    false,
                    selected);
                if (button == null)
                {
                    continue;
                }
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
                    ingredient,
                    ingredient.DisplayName,
                    GetIngredientIcon(ingredient),
                    () => RemoveIngredient(ingredient),
                    true,
                    true,
                    false);
                if (button == null)
                {
                    continue;
                }
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
            if (HasRequiredLayoutReferences() == true)
            {
                return;
            }

            Debug.LogError("CookingIngredientSelectionView is missing inspector layout references or ingredient button prefabs. Assign references from a prefab/scene object.", this);
        }

        private bool HasRequiredLayoutReferences()
        {
            return availableIngredientRoot != null
                   && selectedIngredientRoot != null
                   && searchInputField != null
                   && availableSummaryField != null
                   && selectedSummaryField != null
                   && selectionRuleField != null
                   && ingredientDetailField != null
                   && emptyAvailableField != null
                   && emptySelectedField != null
                   && confirmButton != null
                   && clearButton != null
                   && availableIngredientButtonPrefab != null
                   && selectedIngredientButtonPrefab != null;
        }

        private Button CreateIngredientButton(
            Transform parent,
            IngredientSO ingredient,
            string label,
            Sprite icon,
            UnityEngine.Events.UnityAction action,
            bool interactable,
            bool useSelectedPrefab,
            bool selected)
        {
            CookingIngredientButtonView prefab = useSelectedPrefab == true && selectedIngredientButtonPrefab != null
                ? selectedIngredientButtonPrefab
                : availableIngredientButtonPrefab;

            if (prefab != null)
            {
                CookingIngredientButtonView view = Instantiate(prefab, parent);
                view.Bind(
                    label,
                    icon,
                    selected,
                    interactable,
                    action,
                    () => FocusIngredient(ingredient),
                    () => ClearFocusedIngredient(ingredient));
                return view.Button;
            }

            Debug.LogError("CookingIngredientSelectionView ingredient button prefab is missing. Assign availableIngredientButtonPrefab/selectedIngredientButtonPrefab.", this);
            return null;
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

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null)
                    child.gameObject.SetActive(false);

                if (Application.isPlaying == true)
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
    }
}
