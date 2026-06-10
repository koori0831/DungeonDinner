using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingIngredientSelectionView : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;

        [Header("Layout References")]
        [SerializeField] private RectTransform availableIngredientRoot;
        [SerializeField] private RectTransform selectedIngredientRoot;
        [SerializeField] private TextMeshProUGUI selectedSummaryField;
        [SerializeField] private TextMeshProUGUI emptySelectedField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button clearButton;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private Color panelColor = new Color(0.05f, 0.04f, 0.03f, 0.88f);
        [SerializeField] private Color sectionColor = new Color(0.12f, 0.10f, 0.08f, 0.92f);
        [SerializeField] private Color defaultButtonColor = new Color(0.78f, 0.70f, 0.56f, 1f);
        [SerializeField] private Color selectedButtonColor = new Color(0.54f, 0.72f, 0.48f, 1f);
        [SerializeField] private Color disabledButtonColor = new Color(0.36f, 0.33f, 0.29f, 1f);

        [Header("Text")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private string titleText = "재료 직접 선택";
        [SerializeField] private string availableTitleText = "임시 가방";
        [SerializeField] private string selectedTitleText = "선택한 재료";
        [SerializeField] private string emptySelectedText = "선택된 재료 없음";
        [SerializeField] private string confirmText = "재료 확정";
        [SerializeField] private string clearText = "비우기";

        private bool _isSubscribed;
        private static Sprite _generatedFallbackSprite;

        private void Awake()
        {
            EnsureReferences();
            EnsureLayout();
            BindFixedButtons();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureLayout();
            BindFixedButtons();
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

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureLayout();
            BindFixedButtons();

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
                SetConfirmInteractable(false);
                SetText(selectedSummaryField, "재료 데이터 없음");
                SetText(emptySelectedField, emptySelectedText);
                return;
            }

            IReadOnlyList<IngredientSO> selectedIngredients = flowRunner.SelectedIngredients;
            RebuildAvailableIngredients(flowRunner.Ingredients, selectedIngredients);
            RebuildSelectedIngredients(selectedIngredients);
            SetConfirmInteractable(selectedIngredients.Count > 0);
        }

        private void ToggleIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            if (ContainsIngredient(flowRunner.SelectedIngredients, ingredient))
                flowRunner.RemoveDirectIngredient(ingredient);
            else
                flowRunner.AddDirectIngredient(ingredient);

            Refresh();
        }

        private void RemoveIngredient(IngredientSO ingredient)
        {
            if (ingredient == null || flowRunner == null)
                return;

            flowRunner.RemoveDirectIngredient(ingredient);
            Refresh();
        }

        private void ClearSelection()
        {
            if (flowRunner == null)
                return;

            List<IngredientSO> selected = new List<IngredientSO>(flowRunner.SelectedIngredients);
            for (int i = 0; i < selected.Count; i++)
                flowRunner.RemoveDirectIngredient(selected[i]);

            Refresh();
        }

        private void ConfirmSelection()
        {
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

                bool selected = ContainsIngredient(selectedIngredients, ingredient);
                CreateIngredientButton(
                    availableIngredientRoot,
                    ingredient.DisplayName,
                    selected ? selectedButtonColor : defaultButtonColor,
                    () => ToggleIngredient(ingredient));
            }
        }

        private void RebuildSelectedIngredients(IReadOnlyList<IngredientSO> selectedIngredients)
        {
            ClearChildren(selectedIngredientRoot);

            int selectedCount = selectedIngredients != null ? selectedIngredients.Count : 0;
            SetText(selectedSummaryField, $"{selectedTitleText} {selectedCount}");
            SetText(emptySelectedField, selectedCount == 0 ? emptySelectedText : string.Empty);

            if (selectedIngredientRoot == null || selectedIngredients == null)
                return;

            for (int i = 0; i < selectedIngredients.Count; i++)
            {
                IngredientSO ingredient = selectedIngredients[i];
                if (ingredient == null)
                    continue;

                CreateIngredientButton(
                    selectedIngredientRoot,
                    ingredient.DisplayName,
                    selectedButtonColor,
                    () => RemoveIngredient(ingredient));
            }
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

            if (availableIngredientRoot != null
                && selectedIngredientRoot != null
                && selectedSummaryField != null
                && confirmButton != null)
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
            rect.anchorMin = new Vector2(0.06f, 0.06f);
            rect.anchorMax = new Vector2(0.94f, 0.66f);
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

            TextMeshProUGUI title = CreateText(transform, "Title", titleText, 22f, TextAlignmentOptions.Left);
            AddLayoutElement(title.gameObject, -1f, 34f, -1f, 0f);

            RectTransform body = CreateLayoutObject(transform, "Body");
            HorizontalLayoutGroup bodyLayout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 12f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;
            AddLayoutElement(body.gameObject, -1f, -1f, 1f, 1f);

            RectTransform bagPanel = CreateSection(body, "BagSection", availableTitleText);
            availableIngredientRoot = CreateScrollContent(bagPanel, "AvailableIngredients");
            AddLayoutElement(bagPanel.gameObject, 0f, -1f, 2.2f, 1f);

            RectTransform selectedPanel = CreateSection(body, "SelectedSection", selectedTitleText);
            selectedSummaryField = selectedPanel.GetComponentInChildren<TextMeshProUGUI>();
            selectedIngredientRoot = CreateScrollContent(selectedPanel, "SelectedIngredients");
            emptySelectedField = CreateText(selectedPanel, "EmptySelected", emptySelectedText, 15f, TextAlignmentOptions.Center);
            AddLayoutElement(emptySelectedField.gameObject, -1f, 28f, -1f, 0f);
            AddLayoutElement(selectedPanel.gameObject, 0f, -1f, 1f, 1f);

            RectTransform actionRow = CreateLayoutObject(transform, "ActionRow");
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
            ApplyGeneratedSprite(image);
            image.color = sectionColor;

            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI label = CreateText(section, "SectionTitle", title, 18f, TextAlignmentOptions.Left);
            AddLayoutElement(label.gameObject, -1f, 30f, -1f, 0f);
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
            contentLayout.spacing = 6f;
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

        private Button CreateIngredientButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateActionButton(parent, label, action, color);
            AddLayoutElement(button.gameObject, -1f, 38f, -1f, 0f);
            return button;
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
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
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

        private void HandleFlowStateChanged(CookingFlowState state)
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
