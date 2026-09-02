using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Info;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingRecipeDisplayPanel : InfoDisplayPanel
    {
        [Header("Recipe Fields")]
        [SerializeField] private TextMeshProUGUI requiredIngredientsField;
        [SerializeField] private TextMeshProUGUI knownEffectiveTagsField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonLabel;
        [SerializeField] private string confirmRecipeText = "레시피 확정";
        [SerializeField] private string directSelectionText = "재료 직접 선택";

        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingKnowledgeStore knowledgeStore;
        [SerializeField] private bool showConfirmButtonForDirectSelection = true;

        private CookingRecipeEntryData _currentEntry;
        private ScrollRect _knowledgeScrollRect;
        private RectTransform _knowledgeContent;
        private RectTransform _variantListRoot;
        private TextMeshProUGUI _descriptionBodyField;
        private TextMeshProUGUI _requirementsBodyField;
        private TextMeshProUGUI _completionBodyField;
        private TextMeshProUGUI _tagsBodyField;
        private TextMeshProUGUI _guestsBodyField;
        private TextMeshProUGUI _emptyVariantsField;
        private bool _knowledgeEventsSubscribed;

        public void SetGamePanel(CookingGamePanel value)
        {
            gamePanel = value;
        }

        public override void InitializeDisplay(Action backAction)
        {
            base.InitializeDisplay(backAction);
            EnsureGamePanel();
            EnsureKnowledgeStore();
            EnsureKnowledgeLayout();
            SubscribeKnowledgeEvents();
            SetText(requiredIngredientsField, string.Empty);
            SetText(knownEffectiveTagsField, string.Empty);
            SetConfirmButton(false, string.Empty);

            if (confirmButton == null)
            {
                Debug.LogWarning("CookingRecipeDisplayPanel needs a confirm button before it can confirm recipes.", this);
                return;
            }

            confirmButton.onClick.RemoveListener(ConfirmCurrentEntry);
            confirmButton.onClick.AddListener(ConfirmCurrentEntry);
        }

        public override void Enable(InfoDictionaryEntryData displayInfo)
        {
            base.Enable(displayInfo);

            _currentEntry = displayInfo as CookingRecipeEntryData;
            BindRecipeFields();
        }

        private void OnDestroy()
        {
            if (_knowledgeEventsSubscribed)
                Bus<CookingKnowledgeChangedEvent>.Events -= HandleKnowledgeChanged;
        }

        private void BindRecipeFields()
        {
            if (_currentEntry == null)
            {
                SetText(requiredIngredientsField, string.Empty);
                SetText(knownEffectiveTagsField, string.Empty);
                SetConfirmButton(false, confirmRecipeText);
                return;
            }

            SetText(requiredIngredientsField, BuildRequiredIngredientText(_currentEntry));
            SetText(knownEffectiveTagsField, BuildKnownEffectiveTagText(_currentEntry));
            BindKnowledgeBody();
            bool canConfirm = _currentEntry.IsDirectIngredientSelection
                ? showConfirmButtonForDirectSelection
                : gamePanel != null
                  && gamePanel.AllowRecipeConfirmation
                  && _currentEntry.IsDiscovered;
            SetConfirmButton(canConfirm, _currentEntry.IsDirectIngredientSelection ? directSelectionText : confirmRecipeText);
        }

        private void ConfirmCurrentEntry()
        {
            if (_currentEntry == null)
                return;

            EnsureGamePanel();
            if (gamePanel == null)
            {
                Debug.LogWarning("CookingRecipeDisplayPanel needs a CookingGamePanel before it can confirm a selection.", this);
                return;
            }

            if (_currentEntry.IsDirectIngredientSelection)
            {
                Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Raise(
                    new CookingDirectIngredientSelectionOpenRequestedEvent(gamePanel));
                return;
            }

            if (gamePanel.AllowRecipeConfirmation == false || _currentEntry.IsDiscovered == false)
                return;

            Bus<CookingRecipeConfirmRequestedEvent>.Raise(
                new CookingRecipeConfirmRequestedEvent(gamePanel, _currentEntry.Recipe));
        }

        private void ConfirmVariant(string variantId)
        {
            if (_currentEntry?.Recipe == null || string.IsNullOrWhiteSpace(variantId))
                return;
            EnsureGamePanel();
            if (gamePanel == null || gamePanel.AllowRecipeConfirmation == false)
                return;
            Bus<CookingRecipeConfirmRequestedEvent>.Raise(
                new CookingRecipeConfirmRequestedEvent(gamePanel, _currentEntry.Recipe, variantId));
        }

        private void EnsureGamePanel()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void EnsureKnowledgeStore()
        {
            if (knowledgeStore == null && gamePanel != null)
                knowledgeStore = gamePanel.KnowledgeStore;
            if (knowledgeStore == null)
                knowledgeStore = GetComponentInParent<CookingKnowledgeStore>();
        }

        private void SubscribeKnowledgeEvents()
        {
            if (_knowledgeEventsSubscribed)
                return;
            Bus<CookingKnowledgeChangedEvent>.Events += HandleKnowledgeChanged;
            _knowledgeEventsSubscribed = true;
        }

        private void HandleKnowledgeChanged(CookingKnowledgeChangedEvent gameEvent)
        {
            if (knowledgeStore == null || gameEvent.Source != knowledgeStore || _currentEntry?.Recipe == null)
                return;
            BindKnowledgeBody();
        }

        private void BindKnowledgeBody()
        {
            EnsureKnowledgeLayout();
            if (_knowledgeContent == null)
                return;

            ClearChildren(_variantListRoot);
            if (_currentEntry == null)
                return;

            if (_currentEntry.IsDirectIngredientSelection)
            {
                SetText(_descriptionBodyField, _currentEntry.Description);
                SetText(_requirementsBodyField, "가방에서 사용할 재료를 직접 고릅니다.");
                SetText(_completionBodyField, string.Empty);
                SetText(_tagsBodyField, string.Empty);
                SetText(_guestsBodyField, string.Empty);
                SetText(_emptyVariantsField, string.Empty);
                return;
            }

            EnsureKnowledgeStore();
            CookingRecipeKnowledgeSnapshot snapshot = knowledgeStore != null
                ? knowledgeStore.GetRecipeKnowledge(_currentEntry.Recipe)
                : null;
            CookingRecipeKnowledgePresentationModel model =
                new CookingRecipeKnowledgePresentationBuilder(knowledgeStore?.Catalog).Build(snapshot);
            SetText(_descriptionBodyField, model.RecipeDescription);
            SetText(_requirementsBodyField, BuildRequiredIngredientText(_currentEntry));
            SetText(_completionBodyField, model.CompletionSummary);
            SetText(_tagsBodyField, model.KnownTags);
            SetText(_guestsBodyField, model.GuestSummaries);
            SetText(_emptyVariantsField, model.Variants.Count == 0 ? "발견한 변형이 없습니다." : string.Empty);

            TMP_FontAsset font = nameField != null ? nameField.font : null;
            for (int i = 0; i < model.Variants.Count; i++)
            {
                CookingRecipeVariantRowView row = CookingRecipeVariantRowView.Create(_variantListRoot, font);
                row.Bind(model.Variants[i], ConfirmVariant);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_knowledgeContent);
            Canvas.ForceUpdateCanvases();
            if (_knowledgeScrollRect != null)
                _knowledgeScrollRect.verticalNormalizedPosition = 1f;
        }

        private void EnsureKnowledgeLayout()
        {
            if (_knowledgeContent != null)
                return;

            if (descriptionField != null)
                descriptionField.gameObject.SetActive(false);
            if (requiredIngredientsField != null)
                requiredIngredientsField.gameObject.SetActive(false);
            if (knownEffectiveTagsField != null)
                knownEffectiveTagsField.gameObject.SetActive(false);

            GameObject scrollObject = new GameObject(
                "KnowledgeScroll",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            scrollObject.transform.SetParent(transform, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(24f, 56f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -128f);
            scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
            _knowledgeScrollRect = scrollObject.GetComponent<ScrollRect>();
            _knowledgeScrollRect.horizontal = false;
            _knowledgeScrollRect.vertical = true;
            _knowledgeScrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportObject.GetComponent<Image>().color = Color.white;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            _knowledgeContent = contentObject.GetComponent<RectTransform>();
            _knowledgeContent.anchorMin = new Vector2(0f, 1f);
            _knowledgeContent.anchorMax = new Vector2(1f, 1f);
            _knowledgeContent.pivot = new Vector2(0.5f, 1f);
            _knowledgeContent.anchoredPosition = Vector2.zero;
            _knowledgeContent.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _knowledgeScrollRect.viewport = viewport;
            _knowledgeScrollRect.content = _knowledgeContent;

            TMP_FontAsset font = nameField != null ? nameField.font : null;
            _descriptionBodyField = CreateBodyText(_knowledgeContent, "Description", font, 17f);
            _requirementsBodyField = CreateBodyText(_knowledgeContent, "Requirements", font, 16f);
            _completionBodyField = CreateBodyText(_knowledgeContent, "Completion", font, 17f);
            _tagsBodyField = CreateBodyText(_knowledgeContent, "KnownTags", font, 16f);
            _guestsBodyField = CreateBodyText(_knowledgeContent, "Guests", font, 16f);
            CreateBodyText(_knowledgeContent, "VariantTitle", font, 19f).text = "발견한 변형";
            _emptyVariantsField = CreateBodyText(_knowledgeContent, "VariantEmpty", font, 16f);
            GameObject variantList = new GameObject(
                "VariantList",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            variantList.transform.SetParent(_knowledgeContent, false);
            _variantListRoot = variantList.GetComponent<RectTransform>();
            VerticalLayoutGroup variantLayout = variantList.GetComponent<VerticalLayoutGroup>();
            variantLayout.spacing = 8f;
            variantLayout.childControlHeight = true;
            variantLayout.childControlWidth = true;
            variantLayout.childForceExpandHeight = false;
            variantList.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (confirmButton != null && confirmButton.transform is RectTransform confirmRect)
            {
                confirmRect.anchorMin = new Vector2(1f, 0f);
                confirmRect.anchorMax = new Vector2(1f, 0f);
                confirmRect.pivot = new Vector2(1f, 0f);
                confirmRect.anchoredPosition = new Vector2(-24f, 14f);
            }
        }

        private static TextMeshProUGUI CreateBodyText(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private void SetConfirmButton(bool interactable, string label)
        {
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(interactable);
                confirmButton.interactable = interactable;
            }

            if (confirmButtonLabel != null)
                confirmButtonLabel.text = label;
        }

        private static string BuildRequiredIngredientText(CookingRecipeEntryData entry)
        {
            if (entry.IsDirectIngredientSelection)
                return "가방에서 사용할 재료를 직접 고릅니다.";

            RecipeSO recipe = entry.Recipe;
            if (recipe == null || recipe.RequiredIngredients.Count == 0)
                return "필요 재료: 없음";

            if (entry.IsDiscovered == false)
                return entry.HasAttempted
                    ? "아직 정확한 재료와 손질법은 정리되지 않았습니다. 이번에 시도한 조합은 도감에 기록됩니다."
                    : "아직 정확한 재료와 손질법을 알 수 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("필요 재료");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                string requirementText = BuildRequirementText(requirement);
                if (string.IsNullOrWhiteSpace(requirementText))
                    continue;

                builder.Append("- ");
                builder.Append(requirementText);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildRequirementText(RecipeIngredientRequirement requirement)
        {
            if (requirement == null)
                return string.Empty;

            List<string> targets = new List<string>();

            if (requirement.Ingredient != null)
                targets.Add(requirement.Ingredient.DisplayName);

            if (requirement.IngredientCategory != null)
                targets.Add($"{requirement.IngredientCategory.DisplayName} 재료군");

            AppendTagTargets(targets, requirement.RequiredTags);
            AppendSimpleAlternativeTargets(targets, requirement.Alternatives);
            AppendAlternativeOptionTargets(targets, requirement.AlternativeOptions);

            if (targets.Count == 0)
                targets.Add("아무 재료");

            StringBuilder builder = new StringBuilder();
            builder.Append(string.Join(" / ", targets));
            AppendCountText(builder, requirement);
            AppendPreparationText(builder, requirement.RequiredPreparationMethods);
            return builder.ToString();
        }

        private static void AppendTagTargets(ICollection<string> targets, IReadOnlyList<FoodTagSO> tags)
        {
            if (targets == null || tags == null || tags.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                FoodTagSO tag = tags[i];
                if (tag != null)
                    names.Add(tag.DisplayName);
            }

            if (names.Count > 0)
                targets.Add($"태그: {string.Join(", ", names)}");
        }

        private static void AppendSimpleAlternativeTargets(
            ICollection<string> targets,
            IReadOnlyList<IngredientSO> alternatives)
        {
            if (targets == null || alternatives == null || alternatives.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < alternatives.Count; i++)
            {
                IngredientSO ingredient = alternatives[i];
                if (ingredient != null)
                    names.Add(ingredient.DisplayName);
            }

            if (names.Count > 0)
                targets.Add($"대체: {string.Join(", ", names)}");
        }

        private static void AppendAlternativeOptionTargets(
            ICollection<string> targets,
            IReadOnlyList<RecipeIngredientAlternative> alternatives)
        {
            if (targets == null || alternatives == null || alternatives.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < alternatives.Count; i++)
            {
                RecipeIngredientAlternative alternative = alternatives[i];
                if (alternative != null && alternative.Ingredient != null)
                    names.Add(alternative.Ingredient.DisplayName);
            }

            if (names.Count > 0)
                targets.Add($"대체: {string.Join(", ", names)}");
        }

        private static void AppendCountText(StringBuilder builder, RecipeIngredientRequirement requirement)
        {
            if (builder == null || requirement == null)
                return;

            if (requirement.MinCount <= 1 && requirement.HasMaxCount && requirement.MaxCount <= 1)
                return;

            if (requirement.HasMaxCount)
                builder.Append($" x{requirement.MinCount}-{requirement.MaxCount}");
            else
                builder.Append($" x{requirement.MinCount}+");
        }

        private static void AppendPreparationText(StringBuilder builder, IReadOnlyList<PreparationMethodSO> methods)
        {
            if (builder == null || methods == null || methods.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < methods.Count; i++)
            {
                PreparationMethodSO method = methods[i];
                if (method != null)
                    names.Add(method.DisplayName);
            }

            if (names.Count > 0)
                builder.Append($" ({string.Join(" / ", names)})");
        }

        private static void AppendAlternativeText(
            StringBuilder builder,
            IReadOnlyList<RecipeIngredientAlternative> alternatives)
        {
            if (alternatives == null || alternatives.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < alternatives.Count; i++)
            {
                RecipeIngredientAlternative alternative = alternatives[i];
                if (alternative != null && alternative.Ingredient != null)
                    names.Add(alternative.Ingredient.DisplayName);
            }

            if (names.Count > 0)
                builder.Append($" (대체: {string.Join(", ", names)})");
        }

        private static string BuildKnownEffectiveTagText(CookingRecipeEntryData entry)
        {
            if (entry.IsDirectIngredientSelection)
                return string.Empty;

            if (entry.KnownEffectiveTags == null || entry.KnownEffectiveTags.Count == 0)
                return "유효 태그: 아직 알아낸 정보 없음";

            List<string> tags = new List<string>();
            for (int i = 0; i < entry.KnownEffectiveTags.Count; i++)
            {
                FoodTagSO tag = entry.KnownEffectiveTags[i];
                if (tag != null)
                    tags.Add(tag.DisplayName);
            }

            return tags.Count > 0
                ? $"유효 태그: {string.Join(", ", tags)}"
                : "유효 태그: 아직 알아낸 정보 없음";
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }
    }
}
