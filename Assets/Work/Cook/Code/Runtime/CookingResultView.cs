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
        [SerializeField] private TextMeshProUGUI dishNameField;
        [SerializeField] private TextMeshProUGUI resultSummaryField;
        [SerializeField] private TextMeshProUGUI npcMatchField;
        [SerializeField] private RectTransform preparationSection;
        [SerializeField] private RectTransform preparationRoot;
        [SerializeField] private RectTransform reasonsSection;
        [SerializeField] private TextMeshProUGUI reasonsField;
        [SerializeField] private Button handToNpcButton;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color panelColor = new Color(0.045f, 0.035f, 0.028f, 0.90f);
        [SerializeField] private Color sectionColor = new Color(0.13f, 0.105f, 0.08f, 0.94f);
        [SerializeField] private Color plateColor = new Color(0.31f, 0.23f, 0.16f, 0.98f);
        [SerializeField] private Color entryColor = new Color(0.20f, 0.16f, 0.12f, 0.96f);
        [SerializeField] private Color primaryButtonColor = new Color(0.62f, 0.77f, 0.48f, 1f);
        [SerializeField] private Color disabledButtonColor = new Color(0.34f, 0.31f, 0.28f, 1f);

        [Header("Text")]
        [SerializeField] private string titleText = "요리 완성";
        [SerializeField] private string noResultText = "완성된 음식이 없습니다.";
        [SerializeField] private string handToNpcText = "NPC에게 건네주기";

        private static Sprite _generatedFallbackSprite;
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
                TextMeshProUGUI empty = CreateText(
                    preparationRoot,
                    "EmptyPreparation",
                    "손질된 재료가 없습니다.",
                    14f,
                    TextAlignmentOptions.Center);
                empty.textWrappingMode = TextWrappingModes.Normal;
                AddLayoutElement(empty.gameObject, -1f, 44f, -1f, 0f);
                SetSectionPreferredHeight(preparationSection, 116f);
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
            RectTransform entry = CreateLayoutObject(parent, $"PreparedIngredient_{index}");
            Image image = entry.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(image);
            image.color = entryColor;

            TextMeshProUGUI text = CreateText(
                entry,
                "Description",
                BuildPreparedIngredientText(prepared, index),
                14f,
                TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(10f, 8f);
            text.rectTransform.offsetMax = new Vector2(-10f, -8f);

            AddLayoutElement(entry.gameObject, -1f, 76f, -1f, 0f);
        }

        private void HandToNpc()
        {
            gamePanel?.HandResultToNpc();
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
            if (buildDefaultLayoutWhenMissing == false)
                return;

            if (dishNameField != null
                && resultSummaryField != null
                && npcMatchField != null
                && preparationRoot != null
                && reasonsField != null
                && handToNpcButton != null)
            {
                return;
            }

            BuildDefaultLayout();
        }

        private void BuildDefaultLayout()
        {
            RectTransform rect = EnsureRectTransform(gameObject);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0.09f, 0.07f);
            rect.anchorMax = new Vector2(0.91f, 0.91f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = GetOrAdd<Image>(gameObject);
            ApplyGeneratedSprite(background);
            background.color = panelColor;
            background.raycastTarget = true;

            VerticalLayoutGroup rootLayout = GetOrAdd<VerticalLayoutGroup>(gameObject);
            rootLayout.padding = new RectOffset(18, 18, 14, 14);
            rootLayout.spacing = 12f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText(transform, "Title", titleText, 24f, TextAlignmentOptions.Left);
            AddLayoutElement(title.gameObject, -1f, 34f, -1f, 0f);

            RectTransform dishSection = CreateSection(transform, "DishSection", "완성된 음식", 132f, plateColor);
            dishNameField = CreateText(dishSection, "DishName", string.Empty, 27f, TextAlignmentOptions.Center);
            dishNameField.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(dishNameField.gameObject, -1f, 42f, -1f, 0f);

            resultSummaryField = CreateText(dishSection, "ResultSummary", string.Empty, 15f, TextAlignmentOptions.Center);
            resultSummaryField.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(resultSummaryField.gameObject, -1f, 48f, -1f, 0f);

            RectTransform detailContent = CreateScrollContent(transform, "ResultDetails");

            RectTransform npcSection = CreateSection(detailContent, "NpcMatchSection", "NPC 예상 반응", 164f, sectionColor);
            npcMatchField = CreateText(npcSection, "NpcMatch", string.Empty, 14f, TextAlignmentOptions.TopLeft);
            npcMatchField.textWrappingMode = TextWrappingModes.Normal;
            npcMatchField.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(npcMatchField.gameObject, -1f, 118f, -1f, 0f);

            preparationSection = CreateSection(detailContent, "PreparationSection", "손질 내역", 116f, sectionColor);
            preparationRoot = CreateLayoutObject(preparationSection, "PreparationEntries");
            VerticalLayoutGroup preparationLayout = preparationRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            preparationLayout.spacing = 6f;
            preparationLayout.childControlWidth = true;
            preparationLayout.childControlHeight = true;
            preparationLayout.childForceExpandWidth = true;
            preparationLayout.childForceExpandHeight = false;
            AddLayoutElement(preparationRoot.gameObject, -1f, -1f, 1f, 0f);

            reasonsSection = CreateSection(detailContent, "ReasonSection", "판정 사유", 84f, sectionColor);
            reasonsField = CreateText(reasonsSection, "Reasons", string.Empty, 14f, TextAlignmentOptions.TopLeft);
            reasonsField.textWrappingMode = TextWrappingModes.Normal;
            reasonsField.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(reasonsField.gameObject, -1f, 42f, -1f, 0f);

            RectTransform actionRow = CreateLayoutObject(transform, "ActionRow");
            HorizontalLayoutGroup actionLayout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 8f;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;
            AddLayoutElement(actionRow.gameObject, -1f, 46f, -1f, 0f);

            handToNpcButton = CreateButton(actionRow, handToNpcText, HandToNpc, primaryButtonColor);
        }

        private RectTransform CreateSection(
            Transform parent,
            string name,
            string title,
            float preferredHeight,
            Color color)
        {
            RectTransform section = CreateLayoutObject(parent, name);
            Image image = section.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(image);
            image.color = color;

            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI label = CreateText(section, "SectionTitle", title, 17f, TextAlignmentOptions.Left);
            AddLayoutElement(label.gameObject, -1f, 26f, -1f, 0f);
            AddLayoutElement(section.gameObject, -1f, preferredHeight, -1f, 0f);
            return section;
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
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        private Button CreateButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            Color color)
        {
            GameObject buttonObject = new GameObject($"Button_{SanitizeName(label)}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            ApplyGeneratedSprite(image);
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
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
            return button;
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

        private string BuildResultSummaryText(DishResult result)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"품질: {BuildQualityText(result.Quality)}");
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
                builder.AppendLine($"혐오 위험: {BuildStringListText(report.MatchedDisgustingTags)}");

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
                builder.AppendLine($"혐오 위험: {BuildStringListText(report.MatchedDisgustingTags)}");

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
            builder.Append($"효과: {BuildPreparedEffectText(prepared)}");
            return builder.ToString();
        }

        private static string BuildPreparedEffectText(PreparedIngredientState prepared)
        {
            List<string> parts = new List<string>();

            if (prepared.QualityDelta != 0)
                parts.Add($"품질 {prepared.QualityDelta:+#;-#;0}");

            AddTagPart(parts, "추가 태그", prepared.AddTags);
            AddTagPart(parts, "제거 태그", prepared.RemoveTags);

            if (string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                parts.Add($"이름 변화 {prepared.ResultNameModifier}");

            if (prepared.CausesDisgusting)
                parts.Add("혐오 위험");

            if (prepared.AddsPoison)
                parts.Add("독성 추가");

            return parts.Count > 0 ? string.Join(" / ", parts) : "변화 없음";
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
                    return "혐오";
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
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanelEvents()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.ResultReady -= HandleResultReady;
            _subscribedPanel = null;
        }

        private void HandleResultReady(DishResult result)
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

        private static void SetSectionPreferredHeight(RectTransform section, float preferredHeight)
        {
            if (section == null)
                return;

            LayoutElement element = GetOrAdd<LayoutElement>(section.gameObject);
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

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            if (target.TryGetComponent(out T component))
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

        private static Sprite GetGeneratedFallbackSprite()
        {
            if (_generatedFallbackSprite != null)
                return _generatedFallbackSprite;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "GeneratedCookingResultUiSpriteTexture",
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
            _generatedFallbackSprite.name = "GeneratedCookingResultUiSprite";
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
