using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingResultView : MonoBehaviour, ICookingResultView
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;

        [Header("Layout References")]
        [SerializeField] private Image dishIconImage;
        [SerializeField] private TextMeshProUGUI dishNameField;
        [SerializeField] private TextMeshProUGUI resultSummaryField;
        [SerializeField] private TextMeshProUGUI npcMatchField;
        [SerializeField] private RectTransform preparationSection;
        [SerializeField] private RectTransform preparationRoot;
        [SerializeField] private RectTransform reasonsSection;
        [SerializeField] private TextMeshProUGUI reasonsField;
        [SerializeField] private Button handToNpcButton;

        [Header("Prefabs")]
        [SerializeField] private CookingPreparedIngredientRowView preparedIngredientRowPrefab;

        [Header("View Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;

        [Header("Text")]
        [SerializeField] private string noResultText = "완성된 음식이 없습니다.";

        private CookingGamePanel _subscribedPanel;

        private void Awake()
        {
            EnsureReferences();
            EnsureLayout();
            BindButton();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureLayout();
            BindButton();
            SubscribePanelEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribePanelEvents();
        }

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            flowRunner = runner;

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureLayout();
            BindButton();

            if (isActiveAndEnabled)
            {
                SubscribePanelEvents();
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

            DishResult result = GetCurrentResult();
            if (result == null)
            {
                BindEmptyState();
                return;
            }

            BindDishIcon(result);
            SetText(dishNameField, result.DisplayName);
            SetText(resultSummaryField, BuildResultSummaryText(result));
            SetText(npcMatchField, BuildNpcMatchText(result));
            RebuildPreparationEntries(result);
            BindReasons(result);
            SetHandButtonInteractable(gamePanel != null && gamePanel.CanHandCurrentResultToNpc());
        }

        private DishResult GetCurrentResult()
        {
            return gamePanel != null
                ? gamePanel.GetCurrentDishResult()
                : flowRunner?.LastResult;
        }

        private void BindEmptyState()
        {
            SetText(dishNameField, noResultText);
            SetText(resultSummaryField, string.Empty);
            SetText(npcMatchField, "요리 결과가 준비되면 NPC 예상 반응이 표시됩니다.");
            SetText(reasonsField, string.Empty);
            BindDishIcon(null);
            ClearChildren(preparationRoot);
            SetSectionPreferredHeight(preparationSection, 94f);
            SetSectionPreferredHeight(reasonsSection, 84f);
            SetHandButtonInteractable(false);
        }

        private void RebuildPreparationEntries(DishResult result)
        {
            ClearChildren(preparationRoot);

            if (preparationRoot == null)
                return;

            int count = result?.PreparedIngredients?.Count ?? 0;
            if (count == 0)
            {
                SetSectionPreferredHeight(preparationSection, 94f);
                return;
            }

            for (int i = 0; i < count; i++)
                CreatePreparationEntry(preparationRoot, result.PreparedIngredients[i], i);

            SetSectionPreferredHeight(preparationSection, 58f + count * 84f);
        }

        private void BindReasons(DishResult result)
        {
            if (result == null || result.Reasons.Count == 0)
            {
                SetText(reasonsField, "추가 판정 사유 없음");
                SetSectionPreferredHeight(reasonsSection, 78f);
                return;
            }

            SetText(reasonsField, BuildReasonText(result));
            SetSectionPreferredHeight(reasonsSection, 72f + result.Reasons.Count * 26f);
        }

        private void CreatePreparationEntry(Transform parent, PreparedIngredientState prepared, int index)
        {
            if (preparedIngredientRowPrefab != null)
            {
                CookingPreparedIngredientRowView view = Instantiate(preparedIngredientRowPrefab, parent);
                Sprite icon = prepared != null ? CookingTempVisualUtility.ResolveIngredientIcon(prepared.Ingredient) : null;
                view.Bind(BuildPreparedIngredientText(prepared, index), icon);
                return;
            }

            Debug.LogError("CookingResultView preparedIngredientRowPrefab is missing. Assign a row prefab.", this);
        }

        private void HandToNpc()
        {
            gamePanel?.AdvanceFromResult();
        }

        private void BindDishIcon(DishResult result)
        {
            if (dishIconImage == null)
            {
                return;
            }

            dishIconImage.sprite = CookingTempVisualUtility.ResolveDishIcon(result);
            dishIconImage.color = Color.white;
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
            if (HasRequiredLayoutReferences() == true)
            {
                return;
            }

            Debug.LogError("CookingResultView is missing inspector layout references or preparedIngredientRowPrefab. Assign references from a prefab/scene object.", this);
        }

        private bool HasRequiredLayoutReferences()
        {
            return dishIconImage != null
                   && dishNameField != null
                   && resultSummaryField != null
                   && npcMatchField != null
                   && preparationSection != null
                   && preparationRoot != null
                   && reasonsSection != null
                   && reasonsField != null
                   && handToNpcButton != null
                   && preparedIngredientRowPrefab != null;
        }

        private string BuildResultSummaryText(DishResult result)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"품질: {BuildQualityText(result.Quality)}");
            builder.Append($"  |  완성도: {result.QualityScore:+#;-#;0}");
            builder.Append($"  |  기준: {BuildRecipeText(result)}");
            builder.Append($"  |  카테고리: {BuildCategoryText(result.Category)}");
            builder.AppendLine();
            builder.Append($"태그: {BuildTagText(result.Tags)}");

            if (result.IsDisgusting)
                builder.Append("  |  혐오 판정");

            return builder.ToString();
        }

        private string BuildNpcMatchText(DishResult result)
        {
            if (result == null)
                return "요리 결과가 없어 NPC 요청과 비교할 수 없습니다.";

            if (gamePanel != null)
            {
                if (gamePanel.TryBuildNpcMatchReport(result, out NpcDishMatchReport panelReport) == false)
                    return "현재 NPC 주문 정보가 아직 준비되지 않았습니다.\nNPC 대화에서 요리 단계까지 진행한 뒤 다시 확인해 주세요.";

                return BuildNpcMatchReportText(result, panelReport, true);
            }

            NpcConversationRunner runner = gamePanel != null ? gamePanel.NpcRunner : null;
            if (runner == null)
                runner = FindFirstObjectByType<NpcConversationRunner>();

            if (runner == null)
                return "현재 씬에서 NpcConversationRunner를 찾지 못했습니다.\nNPC 대화 UI와 연결되면 예상 판정이 표시됩니다.";

            if (CookingNpcDishAdapter.TryBuildMatchReport(runner, result, out NpcDishMatchReport report) == false)
                return "현재 NPC 주문 정보가 아직 준비되지 않았습니다.\nNPC 대화에서 요리 단계까지 진행한 뒤 다시 확인해 주세요.";

            int percent = Mathf.RoundToInt(report.MatchRatio * 100f);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"NPC: {ValueOrNone(report.Order?.NpcId)}  |  예상 반응: {BuildNpcResultText(report.Evaluation?.Result ?? NpcConversationResult.Wrong)}");
            builder.AppendLine($"요청 일치도: {report.MatchScore}/{report.MaxMatchScore} ({percent}%)");
            builder.AppendLine($"레시피: {BuildMatchStateText(report.RecipeMatches)}  목표 {ValueOrNone(report.Order?.CorrectRecipeId)} / 제출 {ValueOrNone(report.Dish?.RecipeId)}");
            builder.AppendLine($"분류: {BuildMatchStateText(report.FoodTypeMatches)}  목표 {BuildStringListText(report.Order?.AllowedFoodTypes)} / 제출 {ValueOrNone(report.Dish?.FoodType)}");
            builder.AppendLine($"필수 태그: 맞음 {BuildStringListText(report.MatchedRequiredTags)} / 부족 {BuildStringListText(report.MissingRequiredTags)}");
            builder.AppendLine($"선호 태그: 맞음 {BuildStringListText(report.MatchedPreferredTags)} / 없음 {BuildStringListText(report.MissingPreferredTags)}");

            if (report.MatchedAvoidTags.Count > 0)
                builder.AppendLine($"회피 태그 감지: {BuildStringListText(report.MatchedAvoidTags)}");

            if (report.Dish != null && (report.Dish.IsDisgusting || report.MatchedDisgustingTags.Count > 0))
                builder.AppendLine($"실패 위험: {BuildStringListText(report.MatchedDisgustingTags)}");

            if (report.Evaluation != null)
                builder.AppendLine($"판정 사유: {report.Evaluation.Reason}");

            return builder.ToString();
        }

        private string BuildNpcMatchReportText(
            DishResult result,
            NpcDishMatchReport report,
            bool includeRewardPreview)
        {
            if (report == null)
                return "현재 NPC 주문 정보가 아직 준비되지 않았습니다.\nNPC 대화에서 요리 단계까지 진행한 뒤 다시 확인해 주세요.";

            int percent = Mathf.RoundToInt(report.MatchRatio * 100f);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"NPC: {ValueOrNone(report.Order?.NpcId)}  |  예상 반응: {BuildNpcResultText(report.Evaluation?.Result ?? NpcConversationResult.Wrong)}");
            builder.AppendLine($"요청 일치도: {report.MatchScore}/{report.MaxMatchScore} ({percent}%)");

            if (includeRewardPreview && gamePanel != null)
                builder.AppendLine($"예상 보상: {gamePanel.PreviewRewardAmount(result)}");

            builder.AppendLine($"레시피: {BuildMatchStateText(report.RecipeMatches)}  목표 {ValueOrNone(report.Order?.CorrectRecipeId)} / 제출 {ValueOrNone(report.Dish?.RecipeId)}");
            builder.AppendLine($"분류: {BuildMatchStateText(report.FoodTypeMatches)}  목표 {BuildStringListText(report.Order?.AllowedFoodTypes)} / 제출 {ValueOrNone(report.Dish?.FoodType)}");
            builder.AppendLine($"필수 태그: 맞음 {BuildStringListText(report.MatchedRequiredTags)} / 부족 {BuildStringListText(report.MissingRequiredTags)}");
            builder.AppendLine($"선호 태그: 맞음 {BuildStringListText(report.MatchedPreferredTags)} / 없음 {BuildStringListText(report.MissingPreferredTags)}");

            if (report.MatchedAvoidTags.Count > 0)
                builder.AppendLine($"회피 태그 감지: {BuildStringListText(report.MatchedAvoidTags)}");

            if (report.Dish != null && (report.Dish.IsDisgusting || report.MatchedDisgustingTags.Count > 0))
                builder.AppendLine($"실패 위험: {BuildStringListText(report.MatchedDisgustingTags)}");

            if (report.Evaluation != null)
                builder.AppendLine($"판정 사유: {report.Evaluation.Reason}");

            return builder.ToString();
        }

        private static string BuildPreparedIngredientText(PreparedIngredientState prepared, int index)
        {
            if (prepared == null)
                return $"{index + 1}. 알 수 없는 재료";

            StringBuilder builder = new StringBuilder();
            string ingredientName = prepared.Ingredient != null ? prepared.Ingredient.DisplayName : "알 수 없는 재료";
            string methodName = prepared.Method != null ? prepared.Method.DisplayName : "손질 없음";
            builder.AppendLine($"{index + 1}. {ingredientName}");
            builder.AppendLine($"손질: {methodName}");
            if (prepared.HasMiniGameResult == true)
            {
                builder.AppendLine($"미니게임: {BuildMiniGameGradeText(prepared.MiniGameResult.Grade)}");
                if (string.IsNullOrWhiteSpace(prepared.MiniGameFeedbackText) == false)
                    builder.AppendLine($"피드백: {prepared.MiniGameFeedbackText}");
            }

            builder.Append($"효과: {BuildPreparedEffectText(prepared)}");
            return builder.ToString();
        }

        private static string BuildPreparedEffectText(PreparedIngredientState prepared)
        {
            List<string> parts = new List<string>();

            if (prepared.QualityDelta != 0)
                parts.Add($"품질 {prepared.QualityDelta:+#;-#;0}");

            AddTagPart(parts, "추가 태그", prepared.AddedTags);
            AddTagPart(parts, "제거 태그", prepared.RemoveTags);

            if (string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                parts.Add($"이름 변화 {prepared.ResultNameModifier}");

            if (prepared.CausesDisgusting)
                parts.Add("혐오 위험");

            if (prepared.AddsPoison)
                parts.Add("독성 추가");

            return parts.Count > 0 ? string.Join(" / ", parts) : "변화 없음";
        }

        private static string BuildMiniGameGradeText(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return "완벽";
                case CookingMiniGameGrade.Good:
                    return "좋음";
                case CookingMiniGameGrade.Normal:
                    return "보통";
                case CookingMiniGameGrade.Bad:
                default:
                    return "아쉬움";
            }
        }

        private static void AddTagPart(List<string> parts, string title, IReadOnlyList<FoodTagSO> tags)
        {
            string tagText = BuildTagText(tags);
            if (string.IsNullOrWhiteSpace(tagText) == false && tagText != "없음")
                parts.Add($"{title} {tagText}");
        }

        private static string BuildReasonText(DishResult result)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Reasons.Count; i++)
                builder.AppendLine($"- {result.Reasons[i]}");

            return builder.ToString();
        }

        private static string BuildRecipeText(DishResult result)
        {
            if (result.BaseRecipe != null)
                return result.BaseRecipe.DisplayName;

            return result.IsRecipeMatched ? "알 수 없는 레시피" : "직접 조합";
        }

        private static string BuildCategoryText(FoodCategorySO category)
        {
            return category != null ? category.DisplayName : "분류 없음";
        }

        private static string BuildTagText(IReadOnlyList<FoodTagSO> tags)
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

        private static string BuildStringListText(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return "없음";

            return string.Join(", ", values);
        }

        private static string BuildQualityText(DishQuality quality)
        {
            switch (quality)
            {
                case DishQuality.Perfect:
                    return "완벽";
                case DishQuality.Altered:
                    return "변형";
                case DishQuality.Disgusting:
                    return "혐오";
                case DishQuality.Normal:
                default:
                    return "보통";
            }
        }

        private static string BuildNpcResultText(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return "완전 일치";
                case NpcConversationResult.Correct:
                    return "요청 충족";
                case NpcConversationResult.Similar:
                    return "비슷함";
                case NpcConversationResult.Disgusting:
                case NpcConversationResult.Wrong:
                default:
                    return "불일치";
            }
        }

        private static string BuildMatchStateText(bool matches)
        {
            return matches ? "일치" : "불일치";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "없음" : value;
        }

        private void BindButton()
        {
            if (handToNpcButton == null)
                return;

            handToNpcButton.onClick.RemoveListener(HandToNpc);
            handToNpcButton.onClick.AddListener(HandToNpc);
        }

        private void SetHandButtonInteractable(bool interactable)
        {
            if (handToNpcButton != null)
                handToNpcButton.interactable = interactable;
        }

        private void SubscribePanelEvents()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanelEvents();

            if (gamePanel == null)
                return;

            gamePanel.ResultReady += HandleResultReady;
            gamePanel.SnapshotChanged += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanelEvents()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.ResultReady -= HandleResultReady;
            _subscribedPanel.SnapshotChanged -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleResultReady(DishResult result)
        {
            Refresh();
        }

        private void HandleSnapshotChanged(CookingGameSnapshot snapshot)
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

        private static void SetSectionPreferredHeight(RectTransform section, float preferredHeight)
        {
            if (section == null)
                return;

            LayoutElement element = section.GetComponent<LayoutElement>();
            if (element == null)
                return;

            element.preferredHeight = preferredHeight;
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
