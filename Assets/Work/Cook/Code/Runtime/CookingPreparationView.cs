using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingPreparationView : MonoBehaviour, ICookingPreparationView
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;
        [SerializeField] private CookingKnowledgeStore knowledgeStore;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI ingredientNameField;
        [SerializeField] private TextMeshProUGUI ingredientDescriptionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private TextMeshProUGUI optionsSummaryField;

        [Header("Display")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private string noIngredientText = "손질할 재료가 없습니다.";
        [SerializeField] private string noOptionText = "이 재료에는 등록된 손질법이 없습니다.";
        [SerializeField] private string noOptionButtonText = "그대로 진행";
        [SerializeField] private string unknownEffectText = "아직 결과를 모릅니다.";
        [SerializeField] private string knownEffectTitleText = "확인된 효과";

        [Header("Knowledge")]
        [SerializeField] private bool showAllEffectsForTesting;

        private readonly HashSet<string> _knownEffectKeys = new HashSet<string>();
        private bool _isSubscribed;

        private void Awake()
        {
            EnsureReferences();
            EnsureLayout();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureLayout();
            SubscribeFlowEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFlowEvents();
        }

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            flowRunner = runner;
            knowledgeStore = owner != null ? owner.KnowledgeStore : knowledgeStore;

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureLayout();

            if (isActiveAndEnabled)
            {
                SubscribeFlowEvents();
                Refresh();
            }
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

            IngredientSO ingredient = GetCurrentIngredient();
            if (ingredient == null)
            {
                BindEmptyState(noIngredientText);
                return;
            }

            BindIngredient(ingredient);
            SetText(optionsSummaryField, BuildOptionSummaryText(ingredient));
        }

        public IngredientSO GetCurrentIngredient()
        {
            EnsureReferences();
            return flowRunner != null ? flowRunner.GetNextUnpreparedIngredient() : null;
        }

        public IReadOnlyList<IngredientPreparationOption> GetCurrentOptions()
        {
            IngredientSO ingredient = GetCurrentIngredient();
            return GetPreparationOptions(ingredient);
        }

        public bool SelectCurrentPreparationByIndex(int optionIndex)
        {
            IngredientSO ingredient = GetCurrentIngredient();
            IReadOnlyList<IngredientPreparationOption> options = GetPreparationOptions(ingredient);

            if (optionIndex < 0 || options == null || optionIndex >= options.Count)
            {
                Debug.LogWarning($"CookingPreparationView could not select preparation index {optionIndex}.", this);
                return false;
            }

            return SelectPreparation(ingredient, options[optionIndex]);
        }

        public bool SelectCurrentPreparation(IngredientPreparationOption option)
        {
            return SelectPreparation(GetCurrentIngredient(), option);
        }

        public bool SelectPreparation(IngredientSO ingredient, IngredientPreparationOption option)
        {
            EnsureReferences();

            if (ingredient == null)
            {
                Debug.LogWarning("CookingPreparationView could not select a preparation because the ingredient is missing.", this);
                return false;
            }

            if (gamePanel != null)
                return gamePanel.SelectPreparation(ingredient, option);

            if (flowRunner == null)
                return false;

            if (option != null)
                LearnPreparationEffect(ingredient, option);

            bool selected = flowRunner.SelectPreparation(ingredient, option);
            if (selected && flowRunner.GetNextUnpreparedIngredient() == null)
                flowRunner.TryCompleteCooking(out _);

            Refresh();
            return selected;
        }

        private void BindIngredient(IngredientSO ingredient)
        {
            SetText(ingredientNameField, ingredient != null ? ingredient.DisplayName : noIngredientText);
            SetText(ingredientDescriptionField, BuildIngredientDescription(ingredient));
            SetText(progressField, BuildProgressText());
        }

        private void BindEmptyState(string message)
        {
            SetText(ingredientNameField, message);
            SetText(ingredientDescriptionField, string.Empty);
            SetText(progressField, string.Empty);
            SetText(optionsSummaryField, string.Empty);
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (flowRunner == null)
                flowRunner = gamePanel != null ? gamePanel.FlowRunner : GetComponentInParent<CookingFlowRunner>();

            if (knowledgeStore == null && gamePanel != null)
                knowledgeStore = gamePanel.KnowledgeStore;

            if (knowledgeStore == null)
                knowledgeStore = GetComponentInParent<CookingKnowledgeStore>();
        }

        private void EnsureLayout()
        {
            if (ingredientNameField != null && progressField != null)
                return;

            if (buildDefaultLayoutWhenMissing)
                Debug.LogWarning("CookingPreparationView is missing UI references. Assign a custom preparation UI instead of using generated layout.", this);
        }

        private IReadOnlyList<IngredientPreparationOption> GetPreparationOptions(IngredientSO ingredient)
        {
            if (flowRunner == null || ingredient == null)
                return Array.Empty<IngredientPreparationOption>();

            return flowRunner.GetPreparationOptions(ingredient);
        }

        private string BuildProgressText()
        {
            if (flowRunner == null)
                return string.Empty;

            CookingSession session = flowRunner.Controller.CurrentSession;
            if (session == null || session.SelectedIngredients.Count == 0)
                return string.Empty;

            int preparedCount = session.PreparedIngredients.Count;
            return $"손질 진행 {preparedCount} / {session.SelectedIngredients.Count}";
        }

        private string BuildOptionSummaryText(IngredientSO ingredient)
        {
            IReadOnlyList<IngredientPreparationOption> options = GetPreparationOptions(ingredient);
            if (options == null || options.Count == 0)
                return $"{noOptionText}\n{noOptionButtonText}";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                if (option == null)
                    continue;

                builder.AppendLine($"{i + 1}. {option.DisplayName}");
                string description = BuildOptionDescription(option);
                if (string.IsNullOrWhiteSpace(description) == false)
                    builder.AppendLine(description);
                builder.AppendLine(BuildKnownEffectText(ingredient, option));

                if (i < options.Count - 1)
                    builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildIngredientDescription(IngredientSO ingredient)
        {
            if (ingredient == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(ingredient.Description) == false)
                return ingredient.Description;

            return "재료를 어떻게 손질할지 선택합니다.";
        }

        private static string BuildOptionDescription(IngredientPreparationOption option)
        {
            if (option == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(option.Description) == false)
                return option.Description;

            if (option.Method != null && string.IsNullOrWhiteSpace(option.Method.Description) == false)
                return option.Method.Description;

            return "이 방식으로 재료를 손질합니다.";
        }

        private string BuildKnownEffectText(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (showAllEffectsForTesting == false && IsKnownEffect(ingredient, option) == false)
                return unknownEffectText;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(knownEffectTitleText);

            if (option.QualityDelta != 0)
                builder.AppendLine($"품질 변화: {option.QualityDelta:+#;-#;0}");

            AppendTags(builder, "추가 태그", option.AddTags);
            AppendTags(builder, "제거 태그", option.RemoveTags);

            if (string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
                builder.AppendLine($"이름 변화: {option.ResultNameModifier}");

            if (option.CausesDisgusting)
                builder.AppendLine("괴식 위험이 있습니다.");

            if (option.AddsPoison)
                builder.AppendLine("독성이 추가됩니다.");

            return builder.Length > knownEffectTitleText.Length + 1
                ? builder.ToString().TrimEnd()
                : $"{knownEffectTitleText}\n특별한 변화 없음";
        }

        private bool IsKnownEffect(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (option == null)
                return false;

            if (knowledgeStore != null)
                return knowledgeStore.IsPreparationEffectKnown(ingredient, option);

            return _knownEffectKeys.Contains(BuildEffectKey(ingredient, option));
        }

        private void LearnPreparationEffect(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (option == null)
                return;

            if (knowledgeStore != null)
            {
                knowledgeStore.LearnPreparationEffect(ingredient, option);
                return;
            }

            _knownEffectKeys.Add(BuildEffectKey(ingredient, option));
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

        private void HandleFlowStateChanged(CookingFlowState state)
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

        private static string BuildEffectKey(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string ingredientId = ingredient != null ? ingredient.IngredientId : string.Empty;
            string methodId = option != null && option.Method != null ? option.Method.MethodId : option?.DisplayName;
            return $"{ingredientId}:{methodId}";
        }

        private static void AppendTags(StringBuilder builder, string title, IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    names.Add(tags[i].DisplayName);
            }

            if (names.Count > 0)
                builder.AppendLine($"{title}: {string.Join(", ", names)}");
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
