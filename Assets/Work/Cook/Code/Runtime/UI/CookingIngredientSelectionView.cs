using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
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
        [SerializeField] private ScrollRect availableIngredientScrollRect;
        [SerializeField] private ScrollRect selectedIngredientScrollRect;
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
            Refresh(true);
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

            if (isActiveAndEnabled == true)
            {
                SubscribeFlowEvents();
                Refresh(true);
            }
        }

        public void SetIngredientSource(ICookingIngredientSource source)
        {
            if (_runtimeIngredientSource == source)
                return;

            UnsubscribeIngredientSourceEvents();
            _runtimeIngredientSource = source;

            if (isActiveAndEnabled == true)
                SubscribeIngredientSourceEvents();

            Refresh(true);
        }

        public void SetSelectionLimits(int minCount, int maxCount = 0)
        {
            minSelectedIngredients = Mathf.Max(0, minCount);
            maxSelectedIngredients = Mathf.Max(0, maxCount);

            if (maxSelectedIngredients > 0 && maxSelectedIngredients < minSelectedIngredients)
                maxSelectedIngredients = minSelectedIngredients;

            Refresh(true);
        }

        public void SetSearchQuery(string query)
        {
            _searchQuery = query ?? string.Empty;
            EnsureReferences();
            EnsureLayout();

            if (searchInputField != null && searchInputField.text != _searchQuery)
                searchInputField.SetTextWithoutNotify(_searchQuery);

            BindSearchField();
            Refresh(true);
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
            Refresh(false);
        }

        private void Refresh(bool resetScroll)
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
            RebuildAvailableIngredients(availableIngredients, selectedIngredients, resetScroll);
            RebuildSelectedIngredients(selectedIngredients, resetScroll);
            BindAvailableSummary(availableIngredients);
            SetConfirmInteractable(IsSelectionValid(selectedIngredients, out _));
            BindFocusedIngredientDetail();
        }

        public void ToggleIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            if (CanSelectMore(flowRunner.SelectedIngredients) == false)
                return;

            int selectedQuantity = CountIngredientOccurrences(flowRunner.SelectedIngredients, ingredient);
            if (selectedQuantity >= GetAvailableQuantity(ingredient))
                return;

            if (flowRunner.Controller.CurrentSession?.Mode == CookingMode.Recipe)
                flowRunner.AddRecipeIngredient(ingredient);
            else
                flowRunner.AddDirectIngredient(ingredient);

            Refresh(false);
        }

        public void RemoveIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            flowRunner.RemoveDirectIngredient(ingredient);
            Refresh(false);
        }

        public void ClearSelection()
        {
            if (flowRunner == null)
                return;

            List<IngredientSO> selected = new List<IngredientSO>(flowRunner.SelectedIngredients);
            for (int i = 0; i < selected.Count; i++)
                flowRunner.RemoveDirectIngredient(selected[i]);

            Refresh(true);
        }

        public void ConfirmSelection()
        {
            if (IsSelectionValid(flowRunner?.SelectedIngredients, out _) == false)
            {
                Refresh(false);
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
            IReadOnlyList<IngredientSO> selectedIngredients,
            bool resetScroll)
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
                if (hideUnavailableIngredients == true
                    && ResolveIngredientSource() is ICookingRecipePlanSource == false
                    && availableQuantity <= 0)
                    continue;

                int selectedQuantity = CountIngredientOccurrences(selectedIngredients, ingredient);
                bool selected = selectedQuantity > 0;
                bool interactable = selectedQuantity < availableQuantity
                                    && CanSelectMore(selectedIngredients) == true;
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

            RefreshListLayout(availableIngredientRoot, availableIngredientScrollRect, resetScroll);
        }

        private void BindAvailableSummary(IReadOnlyList<IngredientSO> ingredients)
        {
            int count = CountDisplayableIngredients(ingredients);
            ICookingIngredientSource source = ResolveIngredientSource();
            string sourceName = source != null ? source.SourceName : "카탈로그 전체";

            SetText(availableSummaryField, $"{availableTitleText} {count} ({sourceName})");
            SetText(emptyAvailableField, count == 0 ? BuildEmptyAvailableText() : string.Empty);
        }

        private void RebuildSelectedIngredients(IReadOnlyList<IngredientSO> selectedIngredients, bool resetScroll)
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

            RefreshListLayout(selectedIngredientRoot, selectedIngredientScrollRect, resetScroll);
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (flowRunner == null)
                flowRunner = gamePanel != null ? gamePanel.FlowRunner : GetComponentInParent<CookingFlowRunner>();

            if (availableIngredientScrollRect == null && availableIngredientRoot != null)
                availableIngredientScrollRect = availableIngredientRoot.GetComponentInParent<ScrollRect>();

            if (selectedIngredientScrollRect == null && selectedIngredientRoot != null)
                selectedIngredientScrollRect = selectedIngredientRoot.GetComponentInParent<ScrollRect>();

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
            return CookingIngredientSelectionSourceResolver.Resolve(
                this,
                ingredientSourceBehaviour,
                _runtimeIngredientSource,
                searchIngredientSourceInParents,
                searchIngredientSourceInChildren);
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
                return null;

            ICookingIngredientIconSource iconSource = ResolveIngredientSource() as ICookingIngredientIconSource;
            if (iconSource != null)
            {
                Sprite icon = iconSource.GetAvailableIngredientIcon(ingredient, gamePanel, flowRunner);
                if (icon != null)
                    return icon;
            }

            return CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
        }

        private string BuildAvailableIngredientLabel(IngredientSO ingredient, int availableQuantity)
        {
            ICookingRecipePlanSource planSource = ResolveIngredientSource() as ICookingRecipePlanSource;
            if (planSource != null)
            {
                int required = planSource.GetRequiredIngredientQuantity(ingredient);
                return $"{ingredient.DisplayName}  필요 {required} / 보유 {availableQuantity}";
            }

            return CookingIngredientSelectionTextFormatter.BuildAvailableIngredientLabel(
                ingredient,
                availableQuantity,
                showIngredientQuantities);
        }

        private void BindFocusedIngredientDetail()
        {
            if (_focusedIngredient == null)
            {
                SetText(ingredientDetailField, emptyIngredientDetailText);
                return;
            }

            SetText(
                ingredientDetailField,
                CookingIngredientSelectionTextFormatter.BuildIngredientDetailText(
                    _focusedIngredient,
                    GetAvailableQuantity(_focusedIngredient),
                    emptyIngredientDetailText));
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

        private void HandleSearchChanged(string value)
        {
            _searchQuery = value ?? string.Empty;
            Refresh(true);
        }

        private bool MatchesSearch(IngredientSO ingredient)
        {
            return CookingIngredientSearchMatcher.Matches(ingredient, _searchQuery);
        }

        private string BuildEmptyAvailableText()
        {
            return CookingIngredientSelectionTextFormatter.BuildEmptyAvailableText(
                _searchQuery,
                emptyAvailableText,
                emptySearchResultText);
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
            return CookingIngredientSelectionRules.IsSelectionCountValid(
                selectedIngredients,
                minSelectedIngredients,
                maxSelectedIngredients);
        }

        private bool CanSelectMore(IReadOnlyList<IngredientSO> selectedIngredients)
        {
            return CookingIngredientSelectionRules.CanSelectMore(selectedIngredients, maxSelectedIngredients);
        }

        private string BuildSelectedSummaryText(int selectedCount)
        {
            return CookingIngredientSelectionTextFormatter.BuildSelectedSummaryText(
                selectedTitleText,
                selectedCount,
                maxSelectedIngredients);
        }

        private string BuildSelectionRuleText(int selectedCount)
        {
            ICookingRecipePlanSource planSource = ResolveIngredientSource() as ICookingRecipePlanSource;
            if (planSource != null)
            {
                bool valid = planSource.IsSelectionValid(
                    flowRunner?.SelectedIngredients,
                    gamePanel,
                    flowRunner,
                    out string reason);
                return valid ? "레시피 슬롯과 보유 수량을 충족했습니다." : reason;
            }

            return CookingIngredientSelectionTextFormatter.BuildSelectionRuleText(
                selectedCount,
                minSelectedIngredients,
                maxSelectedIngredients);
        }

        private void SubscribeFlowEvents()
        {
            if (_isSubscribed == true)
                return;

            Bus<CookingFlowStateChangedEvent>.Events += HandleFlowStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeFlowEvents()
        {
            if (_isSubscribed == false)
                return;

            Bus<CookingFlowStateChangedEvent>.Events -= HandleFlowStateChanged;
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

            Bus<CookingIngredientSourceChangedEvent>.Events += HandleIngredientSourceChanged;
            _subscribedIngredientSource = source;
        }

        private void UnsubscribeIngredientSourceEvents()
        {
            if (_subscribedIngredientSource == null)
                return;

            Bus<CookingIngredientSourceChangedEvent>.Events -= HandleIngredientSourceChanged;
            _subscribedIngredientSource = null;
        }

        private void HandleFlowStateChanged(CookingFlowStateChangedEvent gameEvent)
        {
            if (gameEvent.Source != flowRunner)
                return;

            Refresh(false);
        }

        private void HandleIngredientSourceChanged(CookingIngredientSourceChangedEvent gameEvent)
        {
            if (gameEvent.Source != _subscribedIngredientSource)
                return;

            Refresh(true);
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
            return CookingIngredientSelectionRules.ContainsIngredient(ingredients, ingredient);
        }

        private static int CountIngredientOccurrences(
            IReadOnlyList<IngredientSO> ingredients,
            IngredientSO ingredient)
        {
            if (ingredients == null || ingredient == null)
                return 0;

            int count = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] == ingredient)
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

                if (hideUnavailableIngredients == true
                    && ResolveIngredientSource() is ICookingRecipePlanSource == false
                    && GetAvailableQuantity(ingredient) <= 0)
                    continue;

                count++;
            }

            return count;
        }

        private bool IsSelectionValid(
            IReadOnlyList<IngredientSO> selectedIngredients,
            out string reason)
        {
            ICookingRecipePlanSource planSource = ResolveIngredientSource() as ICookingRecipePlanSource;
            if (planSource != null)
                return planSource.IsSelectionValid(selectedIngredients, gamePanel, flowRunner, out reason);

            bool valid = IsSelectionCountValid(selectedIngredients);
            reason = valid ? string.Empty : BuildSelectionRuleText(selectedIngredients != null ? selectedIngredients.Count : 0);
            return valid;
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

        private static void RefreshListLayout(
            RectTransform contentRoot,
            ScrollRect scrollRect,
            bool resetScroll)
        {
            if (contentRoot == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            Canvas.ForceUpdateCanvases();
            if (scrollRect == null)
                return;

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform;
            if (viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);

            scrollRect.StopMovement();
            if (resetScroll == true)
            {
                scrollRect.verticalNormalizedPosition = 1f;
                scrollRect.horizontalNormalizedPosition = 0f;
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }

    }
}
