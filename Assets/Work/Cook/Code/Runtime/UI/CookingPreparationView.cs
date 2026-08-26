using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingPreparationView : MonoBehaviour, ICookingPreparationView
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;
        [SerializeField] private CookingKnowledgeStore knowledgeStore;

        [Header("Layout References")]
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private TextMeshProUGUI ingredientNameField;
        [SerializeField] private TextMeshProUGUI ingredientDescriptionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private RectTransform cardRoot;

        [Header("Prefabs")]
        [SerializeField] private CookingPreparationOptionCardView preparationOptionCardPrefab;

        [Header("View Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;

        [Header("Text")]
        [SerializeField] private string noIngredientText = "손질할 재료가 없습니다.";
        [SerializeField] private string noOptionText = "이 재료에는 등록된 손질법이 없습니다.";
        [SerializeField] private string noOptionButtonText = "그대로 진행";
        [SerializeField] private string unknownEffectText = "아직 결과를 모릅니다.";
        [SerializeField] private string knownEffectTitleText = "확인한 효과";

        [Header("Knowledge")]
        [SerializeField] private bool showAllEffectsForTesting;

        private readonly HashSet<string> _knownEffectKeys = new HashSet<string>();
        private bool _isSubscribed;
        private bool _isCompletingCooking;

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

            if (flowRunner == null)
            {
                BindEmptyState("손질 데이터 없음");
                return;
            }

            IngredientSO ingredient = flowRunner.GetNextUnpreparedIngredient();
            if (ingredient == null)
            {
                CompleteCookingOnce();
                return;
            }

            BindIngredient(ingredient);
            RebuildCards(ingredient);
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
            ClearChildren(cardRoot);
        }

        private void RebuildCards(IngredientSO ingredient)
        {
            ClearChildren(cardRoot);

            if (cardRoot == null || ingredient == null)
                return;

            IReadOnlyList<IngredientPreparationOption> options = flowRunner.GetPreparationOptions(ingredient);
            if (options == null || options.Count == 0)
            {
                CreateNoOptionCard(ingredient);
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                if (option == null)
                    continue;

                CreatePreparationCard(ingredient, option, i);
            }
        }

        private void CreateNoOptionCard(IngredientSO ingredient)
        {
            if (preparationOptionCardPrefab != null)
            {
                CookingPreparationOptionCardView view = Instantiate(preparationOptionCardPrefab, cardRoot);
                view.Bind(
                    string.Empty,
                    null,
                    noOptionText,
                    string.Empty,
                    string.Empty,
                    noOptionButtonText,
                    false,
                    () => SelectPreparation(ingredient, null));
                return;
            }

            Debug.LogError("CookingPreparationView preparationOptionCardPrefab is missing. Assign a card prefab.", this);
        }

        private void CreatePreparationCard(IngredientSO ingredient, IngredientPreparationOption option, int index)
        {
            if (preparationOptionCardPrefab != null)
            {
                CookingPreparationOptionCardView view = Instantiate(preparationOptionCardPrefab, cardRoot);
                Sprite prefabIconSprite = GetOptionIconSprite(option);
                view.Bind(
                    BuildOptionIconText(index, option),
                    prefabIconSprite,
                    option.DisplayName,
                    BuildOptionDescription(option),
                    BuildKnownEffectText(ingredient, option),
                    "선택",
                    true,
                    () => SelectPreparation(ingredient, option));
                return;
            }

            Debug.LogError("CookingPreparationView preparationOptionCardPrefab is missing. Assign a card prefab.", this);
        }

        private void SelectPreparation(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (gamePanel != null)
            {
                gamePanel.SelectPreparation(ingredient, option);
                return;
            }

            if (flowRunner == null || ingredient == null)
                return;

            if (option != null)
                LearnPreparationEffect(ingredient, option);

            flowRunner.SelectPreparation(ingredient, option);
            Refresh();
        }

        private void CompleteCookingOnce()
        {
            if (_isCompletingCooking)
                return;

            _isCompletingCooking = true;
            gamePanel?.CompleteCooking();
            _isCompletingCooking = false;
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
            if (HasRequiredLayoutReferences() == true)
            {
                return;
            }

            Debug.LogError("CookingPreparationView is missing inspector layout references or preparationOptionCardPrefab. Assign references from a prefab/scene object.", this);
        }

        private bool HasRequiredLayoutReferences()
        {
            return boardRoot != null
                   && ingredientNameField != null
                   && ingredientDescriptionField != null
                   && progressField != null
                   && cardRoot != null
                   && preparationOptionCardPrefab != null;
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

        private static string BuildIngredientDescription(IngredientSO ingredient)
        {
            if (ingredient == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(ingredient.Description) == false)
                return ingredient.Description;

            return "도마 위에 올려진 재료를 어떻게 손질할지 선택합니다.";
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
                ? builder.ToString()
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

        private static string BuildEffectKey(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string ingredientId = ingredient != null ? ingredient.IngredientId : string.Empty;
            string methodId = option != null && option.Method != null ? option.Method.MethodId : option?.DisplayName;
            return $"{ingredientId}:{methodId}";
        }

        private static string BuildOptionIconText(int index, IngredientPreparationOption option)
        {
            if (option != null && string.IsNullOrWhiteSpace(option.DisplayName) == false)
                return option.DisplayName.Substring(0, 1);

            return (index + 1).ToString();
        }

        private static Sprite GetOptionIconSprite(IngredientPreparationOption option)
        {
            if (option == null || option.Method == null)
                return null;

            return option.Method.IconSprite;
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
    }
}
