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

        [Header("UI References")]
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

        [Header("Display")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private string availableTitleText = "사용 가능한 재료";
        [SerializeField] private string selectedTitleText = "선택한 재료";
        [SerializeField] private string emptyAvailableText = "사용 가능한 재료 없음";
        [SerializeField] private string emptySearchResultText = "검색 조건에 맞는 재료 없음";
        [SerializeField] private string emptySelectedText = "선택한 재료 없음";
        [SerializeField] private string emptyIngredientDetailText = "재료를 선택하면 정보가 표시됩니다.";

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
            BindControls();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureLayout();
            BindControls();
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
            BindControls();

            if (isActiveAndEnabled)
            {
                SubscribeFlowEvents();
                SubscribeIngredientSourceEvents();
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

            if (searchInputField != null && searchInputField.text != _searchQuery)
                searchInputField.SetTextWithoutNotify(_searchQuery);

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

            IReadOnlyList<IngredientSO> selectedIngredients = flowRunner != null
                ? flowRunner.SelectedIngredients
                : Array.Empty<IngredientSO>();
            IReadOnlyList<IngredientSO> availableIngredients = GetAvailableIngredients();
            int selectedCount = CountIngredients(selectedIngredients);
            int availableCount = CountDisplayableIngredients(availableIngredients);

            ICookingIngredientSource source = ResolveIngredientSource();
            string sourceName = source != null ? source.SourceName : "카탈로그";

            SetText(availableSummaryField, $"{availableTitleText} {availableCount} ({sourceName})");
            SetText(selectedSummaryField, BuildSelectedSummaryText(selectedCount));
            SetText(selectionRuleField, BuildSelectionRuleText(selectedCount));
            SetText(emptyAvailableField, availableCount == 0 ? BuildEmptyAvailableText() : string.Empty);
            SetText(emptySelectedField, selectedCount == 0 ? emptySelectedText : string.Empty);
            SetText(ingredientDetailField, BuildDetailText(selectedIngredients));
            SetConfirmInteractable(IsSelectionCountValid(selectedIngredients));
        }

        public void ToggleIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            if (ContainsIngredient(flowRunner.SelectedIngredients, ingredient))
            {
                flowRunner.RemoveDirectIngredient(ingredient);
                Refresh();
                return;
            }

            if (CanSelectMore(flowRunner.SelectedIngredients) == false)
                return;

            if (GetAvailableQuantity(ingredient) <= 0)
                return;

            if (flowRunner.Controller.CurrentSession?.Mode == CookingMode.Recipe)
                flowRunner.AddRecipeIngredient(ingredient);
            else
                flowRunner.AddDirectIngredient(ingredient);

            FocusIngredient(ingredient);
            Refresh();
        }

        public void RemoveIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            flowRunner.RemoveDirectIngredient(ingredient);
            ClearFocusedIngredient(ingredient);
            Refresh();
        }

        public void ClearSelection()
        {
            if (flowRunner == null)
                return;

            List<IngredientSO> selected = new List<IngredientSO>(flowRunner.SelectedIngredients);
            for (int i = 0; i < selected.Count; i++)
                flowRunner.RemoveDirectIngredient(selected[i]);

            _focusedIngredient = null;
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

        public void FocusIngredient(IngredientSO ingredient)
        {
            _focusedIngredient = ingredient;
            SetText(ingredientDetailField, BuildIngredientDetailText(_focusedIngredient));
        }

        public void ClearFocusedIngredient()
        {
            _focusedIngredient = null;
            SetText(ingredientDetailField, emptyIngredientDetailText);
        }

        private void ClearFocusedIngredient(IngredientSO ingredient)
        {
            if (_focusedIngredient != ingredient)
                return;

            ClearFocusedIngredient();
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (flowRunner == null)
                flowRunner = gamePanel != null ? gamePanel.FlowRunner : GetComponentInParent<CookingFlowRunner>();
        }

        private void EnsureLayout()
        {
            if (selectedSummaryField != null && confirmButton != null)
                return;

            if (buildDefaultLayoutWhenMissing)
                Debug.LogWarning("CookingIngredientSelectionView is missing UI references. Assign a custom ingredient selection UI instead of using generated layout.", this);
        }

        private void BindControls()
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

            if (searchInputField != null)
            {
                searchInputField.onValueChanged.RemoveListener(HandleSearchChanged);
                searchInputField.onValueChanged.AddListener(HandleSearchChanged);
            }
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

            if (ingredientSourceBehaviour is ICookingIngredientSource source)
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

        private string BuildDetailText(IReadOnlyList<IngredientSO> selectedIngredients)
        {
            if (_focusedIngredient != null)
                return BuildIngredientDetailText(_focusedIngredient);

            if (selectedIngredients == null || selectedIngredients.Count == 0)
                return emptyIngredientDetailText;

            List<string> names = new List<string>();
            for (int i = 0; i < selectedIngredients.Count; i++)
            {
                if (selectedIngredients[i] != null)
                    names.Add(selectedIngredients[i].DisplayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : emptyIngredientDetailText;
        }

        private string BuildIngredientDetailText(IngredientSO ingredient)
        {
            if (ingredient == null)
                return emptyIngredientDetailText;

            StringBuilder builder = new StringBuilder();
            builder.Append(ingredient.DisplayName);

            if (showIngredientQuantities)
                builder.Append($" x{GetAvailableQuantity(ingredient)}");

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

        private string BuildSelectedSummaryText(int selectedCount)
        {
            if (maxSelectedIngredients > 0)
                return $"{selectedTitleText} {selectedCount}/{maxSelectedIngredients}";

            return $"{selectedTitleText} {selectedCount}";
        }

        private string BuildSelectionRuleText(int selectedCount)
        {
            if (maxSelectedIngredients > 0)
                return $"최소 {minSelectedIngredients}, 최대 {maxSelectedIngredients}개 선택 ({selectedCount}개)";

            return $"최소 {minSelectedIngredients}개 선택 ({selectedCount}개)";
        }

        private string BuildEmptyAvailableText()
        {
            return string.IsNullOrWhiteSpace(_searchQuery) ? emptyAvailableText : emptySearchResultText;
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

        private bool IsSelectionCountValid(IReadOnlyList<IngredientSO> ingredients)
        {
            int count = CountIngredients(ingredients);
            if (count < minSelectedIngredients)
                return false;

            return maxSelectedIngredients <= 0 || count <= maxSelectedIngredients;
        }

        private bool CanSelectMore(IReadOnlyList<IngredientSO> ingredients)
        {
            return maxSelectedIngredients <= 0 || CountIngredients(ingredients) < maxSelectedIngredients;
        }

        private bool MatchesSearch(IngredientSO ingredient)
        {
            if (ingredient == null)
                return false;

            string query = NormalizeSearch(_searchQuery);
            if (string.IsNullOrWhiteSpace(query))
                return true;

            return ContainsSearchText(ingredient.DisplayName, query)
                   || ContainsSearchText(ingredient.Description, query)
                   || ContainsTagSearchText(ingredient.BaseTags, query)
                   || ContainsPreparationSearchText(ingredient.PreparationOptions, query);
        }

        private void HandleSearchChanged(string value)
        {
            _searchQuery = value ?? string.Empty;
            Refresh();
        }

        private void SetConfirmInteractable(bool interactable)
        {
            if (confirmButton != null)
                confirmButton.interactable = interactable;
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
            _subscribedIngredientSource = source;

            if (_subscribedIngredientSource != null)
                _subscribedIngredientSource.IngredientsChanged += HandleIngredientSourceChanged;
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
            if (isActiveAndEnabled)
                Refresh();
        }

        private void HandleIngredientSourceChanged()
        {
            if (isActiveAndEnabled)
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

        private static string BuildTagListText(IReadOnlyList<FoodTagSO> tags)
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

        private static string BuildPreparationOptionListText(IReadOnlyList<IngredientPreparationOption> options)
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
            return ingredients != null ? ingredients.Count : 0;
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

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
