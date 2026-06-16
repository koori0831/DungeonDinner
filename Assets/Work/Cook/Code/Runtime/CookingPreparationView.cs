using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
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

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color panelColor = new Color(0.04f, 0.035f, 0.03f, 0.88f);
        [SerializeField] private Color boardColor = new Color(0.44f, 0.30f, 0.18f, 1f);
        [SerializeField] private Color ingredientColor = new Color(0.78f, 0.66f, 0.47f, 1f);
        [SerializeField] private Color cardColor = new Color(0.18f, 0.15f, 0.12f, 0.96f);
        [SerializeField] private Color cardHoverColor = new Color(0.30f, 0.23f, 0.16f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.55f, 0.70f, 0.46f, 1f);

        [Header("Text")]
        [SerializeField] private string titleText = "재료 손질";
        [SerializeField] private string noIngredientText = "손질할 재료가 없습니다.";
        [SerializeField] private string noOptionText = "이 재료에는 등록된 손질법이 없습니다.";
        [SerializeField] private string noOptionButtonText = "그대로 진행";
        [SerializeField] private string unknownEffectText = "아직 결과를 모릅니다.";
        [SerializeField] private string knownEffectTitleText = "확인한 효과";

        [Header("Knowledge")]
        [SerializeField] private bool showAllEffectsForTesting;

        private readonly HashSet<string> _knownEffectKeys = new HashSet<string>();
        private static Sprite _generatedFallbackSprite;
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
            RectTransform card = CreateCardObject(cardRoot, "NoPreparationOptionCard");
            AddLayoutElement(card.gameObject, 0f, -1f, 1f, 1f);

            TextMeshProUGUI title = CreateText(card, "Title", noOptionText, 18f, TextAlignmentOptions.Center);
            AddLayoutElement(title.gameObject, -1f, 72f, -1f, 0f);

            Button button = CreateButton(card, noOptionButtonText, () => SelectPreparation(ingredient, null), buttonColor);
            AddLayoutElement(button.gameObject, -1f, 46f, -1f, 0f);
        }

        private void CreatePreparationCard(IngredientSO ingredient, IngredientPreparationOption option, int index)
        {
            RectTransform card = CreateCardObject(cardRoot, $"PreparationCard_{index}");
            AddLayoutElement(card.gameObject, 0f, -1f, 1f, 1f);

            TextMeshProUGUI icon = CreateText(card, "Icon", BuildOptionIconText(index, option), 28f, TextAlignmentOptions.Center);
            icon.color = new Color(1f, 0.88f, 0.58f, 1f);
            AddLayoutElement(icon.gameObject, -1f, 42f, -1f, 0f);

            TextMeshProUGUI name = CreateText(card, "Name", option.DisplayName, 18f, TextAlignmentOptions.Center);
            name.textWrappingMode = TextWrappingModes.Normal;
            name.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(name.gameObject, -1f, 42f, -1f, 0f);

            TextMeshProUGUI description = CreateText(card, "Description", BuildOptionDescription(option), 14f, TextAlignmentOptions.TopLeft);
            description.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(description.gameObject, -1f, 74f, -1f, 0f);
            description.gameObject.SetActive(false);

            TextMeshProUGUI effect = CreateText(card, "Effect", BuildKnownEffectText(ingredient, option), 13f, TextAlignmentOptions.TopLeft);
            effect.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(effect.gameObject, -1f, 78f, -1f, 0f);
            effect.gameObject.SetActive(false);

            Button button = CreateButton(card, "선택", () => SelectPreparation(ingredient, option), buttonColor);
            AddLayoutElement(button.gameObject, -1f, 42f, -1f, 0f);

            BindHover(card, description.gameObject, effect.gameObject);
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
            if (buildDefaultLayoutWhenMissing == false)
                return;

            if (boardRoot != null
                && ingredientNameField != null
                && progressField != null
                && cardRoot != null)
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
            rect.anchorMin = new Vector2(0.05f, 0.04f);
            rect.anchorMax = new Vector2(0.95f, 0.92f);
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

            boardRoot = CreateLayoutObject(transform, "CuttingBoard");
            Image boardImage = boardRoot.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(boardImage);
            boardImage.color = boardColor;
            AddLayoutElement(boardRoot.gameObject, -1f, 0f, 1f, 2.4f);

            VerticalLayoutGroup boardLayout = boardRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            boardLayout.padding = new RectOffset(22, 22, 20, 20);
            boardLayout.spacing = 10f;
            boardLayout.childControlWidth = true;
            boardLayout.childControlHeight = true;
            boardLayout.childForceExpandWidth = true;
            boardLayout.childForceExpandHeight = false;

            RectTransform ingredientPlate = CreateLayoutObject(boardRoot, "IngredientOnBoard");
            Image ingredientImage = ingredientPlate.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(ingredientImage);
            ingredientImage.color = ingredientColor;
            AddLayoutElement(ingredientPlate.gameObject, -1f, 132f, -1f, 0f);

            VerticalLayoutGroup ingredientLayout = ingredientPlate.gameObject.AddComponent<VerticalLayoutGroup>();
            ingredientLayout.padding = new RectOffset(18, 18, 16, 16);
            ingredientLayout.spacing = 8f;
            ingredientLayout.childControlWidth = true;
            ingredientLayout.childControlHeight = true;
            ingredientLayout.childForceExpandWidth = true;
            ingredientLayout.childForceExpandHeight = false;

            ingredientNameField = CreateText(ingredientPlate, "IngredientName", string.Empty, 26f, TextAlignmentOptions.Center);
            AddLayoutElement(ingredientNameField.gameObject, -1f, 38f, -1f, 0f);

            ingredientDescriptionField = CreateText(ingredientPlate, "IngredientDescription", string.Empty, 15f, TextAlignmentOptions.Center);
            ingredientDescriptionField.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(ingredientDescriptionField.gameObject, -1f, 50f, -1f, 0f);

            progressField = CreateText(boardRoot, "Progress", string.Empty, 15f, TextAlignmentOptions.Center);
            AddLayoutElement(progressField.gameObject, -1f, 42f, -1f, 0f);

            cardRoot = CreateLayoutObject(transform, "PreparationCards");
            HorizontalLayoutGroup cardLayout = cardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardLayout.spacing = 12f;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = true;
            AddLayoutElement(cardRoot.gameObject, -1f, 0f, 1f, 1.6f);
        }

        private RectTransform CreateCardObject(Transform parent, string name)
        {
            RectTransform card = CreateLayoutObject(parent, name);
            Image image = card.gameObject.AddComponent<Image>();
            ApplyGeneratedSprite(image);
            image.color = cardColor;

            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return card;
        }

        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Color color)
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
            button.colors = colors;

            if (action != null)
                button.onClick.AddListener(action);

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 15f, TextAlignmentOptions.Center);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
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

        private void BindHover(RectTransform card, params GameObject[] detailObjects)
        {
            if (card == null || detailObjects == null)
                return;

            EventTrigger trigger = card.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                card.localScale = new Vector3(1.06f, 1.06f, 1f);
                if (card.TryGetComponent(out Image image))
                    image.color = cardHoverColor;
                SetDetailsActive(detailObjects, true);
            });

            AddEventTrigger(trigger, EventTriggerType.PointerExit, _ =>
            {
                card.localScale = Vector3.one;
                if (card.TryGetComponent(out Image image))
                    image.color = cardColor;
                SetDetailsActive(detailObjects, false);
            });
        }

        private static void SetDetailsActive(IReadOnlyList<GameObject> detailObjects, bool active)
        {
            if (detailObjects == null)
                return;

            for (int i = 0; i < detailObjects.Count; i++)
            {
                if (detailObjects[i] != null)
                    detailObjects[i].SetActive(active);
            }
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

        private static void AddEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
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
                name = "GeneratedCookingPreparationUiSpriteTexture",
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
            _generatedFallbackSprite.name = "GeneratedCookingPreparationUiSprite";
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
