using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 손질 정확도를 판정하는 1차 타이밍 미니게임 View
    /// </summary>
    public sealed class CookingTimingMiniGameView : MonoBehaviour, ICookingMiniGameView
    {
        private const string PANEL_NAME = "Panel";
        private const string TITLE_NAME = "Title";
        private const string DESCRIPTION_NAME = "Description";
        private const string GAUGE_ROOT_NAME = "GaugeRoot";
        private const string TARGET_ZONE_NAME = "TargetZone";
        private const string CURSOR_NAME = "Cursor";
        private const string GRADE_NAME = "Grade";
        private const string FEEDBACK_NAME = "Feedback";
        private const string ACTION_BUTTON_NAME = "ActionButton";
        private const string ACTION_BUTTON_LABEL_NAME = "Label";

        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingFlowRunner flowRunner;
        [SerializeField] private CookingMiniGameType supportedMiniGameType = CookingMiniGameType.Slicing;

        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private TextMeshProUGUI gradeField;
        [SerializeField] private TextMeshProUGUI feedbackField;
        [SerializeField] private RectTransform gaugeRoot;
        [SerializeField] private RectTransform targetZone;
        [SerializeField] private RectTransform cursor;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonLabel;

        [Header("View Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField, Range(0f, 1f)] private float targetCenter = 0.5f;
        [SerializeField] private bool randomizeTargetCenter = true;
        [SerializeField, Range(0.05f, 0.45f)] private float randomTargetMin = 0.22f;
        [SerializeField, Range(0.05f, 0.95f)] private float randomTargetMax = 0.78f;
        [SerializeField, Min(0.1f)] private float timeLimit = 5f;
        [SerializeField, Min(0.1f)] private float cursorSpeed = 1.2f;
        [SerializeField, Range(0.01f, 0.5f)] private float perfectWindow = 0.045f;
        [SerializeField, Range(0.01f, 0.5f)] private float goodWindow = 0.11f;
        [SerializeField, Range(0.01f, 0.5f)] private float normalWindow = 0.22f;
        [SerializeField] private int perfectQualityDelta = 2;
        [SerializeField] private int goodQualityDelta = 1;
        [SerializeField] private int normalQualityDelta;
        [SerializeField] private int badQualityDelta = -1;

        [Header("Text")]
        [SerializeField] private string titleText = "손질 미니게임";
        [SerializeField] private string descriptionFormat = "{0}을(를) {1}합니다. 목표 구간에 맞춰 칼질하세요.";
        [SerializeField] private string actionText = "칼질하기";
        [SerializeField] private string waitingGradeText = "타이밍을 맞추세요";
        [SerializeField] private string perfectFeedbackText = "완벽하게 손질했습니다.";
        [SerializeField] private string goodFeedbackText = "깔끔하게 손질했습니다.";
        [SerializeField] private string normalFeedbackText = "무난하게 손질했습니다.";
        [SerializeField] private string badFeedbackText = "손질이 거칠게 끝났습니다.";

        private Action<CookingMiniGameResult> _completed;
        private CookingMiniGameType _currentMiniGameType = CookingMiniGameType.None;
        private float _elapsed;
        private float _cursorPosition;
        private bool _isPlaying;

        private void Awake()
        {
            EnsureReferences();
            BindButton();
            ApplyFontToExistingTexts();
            SetVisible(false);
        }

        private void OnEnable()
        {
            EnsureReferences();
            BindButton();
            ApplyFontToExistingTexts();
            RefreshGaugeLayout();
        }

        private void OnDisable()
        {
            _isPlaying = false;
        }

        private void Update()
        {
            if (_isPlaying == false)
                return;

            _elapsed += Time.deltaTime;
            _cursorPosition = Mathf.PingPong(_elapsed * cursorSpeed, 1f);
            ApplyCursorPosition();

            if (_elapsed >= timeLimit)
                Complete(CookingMiniGameGrade.Bad);
        }

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            flowRunner = runner;

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureReferences();
            BindButton();
            RefreshGaugeLayout();
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            ApplyFontToExistingTexts();
        }

        public bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType != CookingMiniGameType.None && miniGameType == supportedMiniGameType;
        }

        public void StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            _completed = completed;
            _currentMiniGameType = option != null ? option.MiniGameType : supportedMiniGameType;
            _elapsed = 0f;
            _cursorPosition = 0f;
            _isPlaying = true;

            if (randomizeTargetCenter == true)
                targetCenter = UnityEngine.Random.Range(randomTargetMin, randomTargetMax);

            SetVisible(true);
            SetText(titleField, titleText);
            SetText(descriptionField, BuildDescriptionText(ingredient, option));
            SetText(gradeField, waitingGradeText);
            SetText(feedbackField, string.Empty);
            SetText(actionButtonLabel, actionText);
            SetInteractable(actionButton, true);
            RefreshGaugeLayout();
            ApplyCursorPosition();
        }

        public void CancelMiniGame()
        {
            _isPlaying = false;
            _completed = null;
            _currentMiniGameType = CookingMiniGameType.None;
            SetVisible(false);
        }

        public void SubmitTiming()
        {
            if (_isPlaying == false)
                return;

            Complete(EvaluateGrade());
        }

        private void Complete(CookingMiniGameGrade grade)
        {
            if (_isPlaying == false)
                return;

            _isPlaying = false;
            SetInteractable(actionButton, false);

            float distance = Mathf.Abs(_cursorPosition - targetCenter);
            float score = Mathf.Clamp01(1f - distance / Mathf.Max(0.001f, normalWindow));
            string feedbackText = GetFeedbackText(grade);
            CookingMiniGameResult result = new CookingMiniGameResult(
                _currentMiniGameType,
                grade,
                score,
                GetQualityDelta(grade),
                feedbackText);

            SetText(gradeField, grade.ToString());
            SetText(feedbackField, feedbackText);

            Action<CookingMiniGameResult> completed = _completed;
            _completed = null;
            _currentMiniGameType = CookingMiniGameType.None;
            completed?.Invoke(result);
        }

        private CookingMiniGameGrade EvaluateGrade()
        {
            float distance = Mathf.Abs(_cursorPosition - targetCenter);
            if (distance <= perfectWindow)
                return CookingMiniGameGrade.Perfect;

            if (distance <= goodWindow)
                return CookingMiniGameGrade.Good;

            if (distance <= normalWindow)
                return CookingMiniGameGrade.Normal;

            return CookingMiniGameGrade.Bad;
        }

        private int GetQualityDelta(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return perfectQualityDelta;
                case CookingMiniGameGrade.Good:
                    return goodQualityDelta;
                case CookingMiniGameGrade.Normal:
                    return normalQualityDelta;
                case CookingMiniGameGrade.Bad:
                default:
                    return badQualityDelta;
            }
        }

        private string GetFeedbackText(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return perfectFeedbackText;
                case CookingMiniGameGrade.Good:
                    return goodFeedbackText;
                case CookingMiniGameGrade.Normal:
                    return normalFeedbackText;
                case CookingMiniGameGrade.Bad:
                default:
                    return badFeedbackText;
            }
        }

        private string BuildDescriptionText(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string ingredientName = ingredient != null ? ingredient.DisplayName : "재료";
            string optionName = option != null && string.IsNullOrWhiteSpace(option.DisplayName) == false
                ? option.DisplayName
                : "손질";

            return string.Format(descriptionFormat, ingredientName, optionName);
        }

        private void RefreshGaugeLayout()
        {
            if (gaugeRoot == null)
                return;

            if (targetZone != null)
            {
                float width = gaugeRoot.rect.width;
                float zoneWidth = Mathf.Max(8f, width * normalWindow * 2f);
                targetZone.sizeDelta = new Vector2(zoneWidth, targetZone.sizeDelta.y);
                targetZone.anchoredPosition = new Vector2(ToGaugeX(targetCenter), targetZone.anchoredPosition.y);
            }

            ApplyCursorPosition();
        }

        private void ApplyCursorPosition()
        {
            if (cursor == null || gaugeRoot == null)
                return;

            cursor.anchoredPosition = new Vector2(ToGaugeX(_cursorPosition), cursor.anchoredPosition.y);
        }

        private float ToGaugeX(float normalized)
        {
            if (gaugeRoot == null)
                return 0f;

            return Mathf.Lerp(-gaugeRoot.rect.width * 0.5f, gaugeRoot.rect.width * 0.5f, Mathf.Clamp01(normalized));
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (flowRunner == null)
                flowRunner = gamePanel != null ? gamePanel.FlowRunner : GetComponentInParent<CookingFlowRunner>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (actionButton == null)
                actionButton = GetComponentInChildren<Button>(true);

            if (NeedsDefaultLayout() == true)
                EnsureDefaultLayout();
        }

        private bool NeedsDefaultLayout()
        {
            return canvasGroup == null
                   || titleField == null
                   || descriptionField == null
                   || gradeField == null
                   || feedbackField == null
                   || gaugeRoot == null
                   || targetZone == null
                   || cursor == null
                   || actionButton == null
                   || actionButtonLabel == null;
        }

        private void EnsureDefaultLayout()
        {
            RectTransform root = transform as RectTransform;
            SetStretch(root);

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image rootImage = GetComponent<Image>();
            if (rootImage == null)
                rootImage = gameObject.AddComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.55f);
            rootImage.raycastTarget = true;

            RectTransform panel = EnsureImageRect(
                null,
                PANEL_NAME,
                transform,
                new Color(0.12f, 0.09f, 0.07f, 0.96f));
            SetCenter(panel, new Vector2(760f, 420f), Vector2.zero);

            titleField = EnsureText(
                titleField,
                TITLE_NAME,
                panel,
                titleText,
                34f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.88f, 0.55f, 1f));
            SetTop(titleField.rectTransform, new Vector2(680f, 52f), new Vector2(0f, -34f));

            descriptionField = EnsureText(
                descriptionField,
                DESCRIPTION_NAME,
                panel,
                "목표 구간에 맞춰 칼질하세요.",
                22f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.95f, 0.9f, 0.82f, 1f));
            SetTop(descriptionField.rectTransform, new Vector2(680f, 68f), new Vector2(0f, -92f));

            gaugeRoot = EnsureImageRect(
                gaugeRoot,
                GAUGE_ROOT_NAME,
                panel,
                new Color(0.22f, 0.18f, 0.14f, 1f));
            SetCenter(gaugeRoot, new Vector2(560f, 42f), new Vector2(0f, 18f));

            targetZone = EnsureImageRect(
                targetZone,
                TARGET_ZONE_NAME,
                gaugeRoot,
                new Color(0.33f, 0.72f, 0.42f, 0.78f));
            SetCenter(targetZone, new Vector2(160f, 42f), Vector2.zero);

            cursor = EnsureImageRect(
                cursor,
                CURSOR_NAME,
                gaugeRoot,
                new Color(1f, 0.86f, 0.32f, 1f));
            SetCenter(cursor, new Vector2(14f, 68f), Vector2.zero);

            gradeField = EnsureText(
                gradeField,
                GRADE_NAME,
                panel,
                waitingGradeText,
                26f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.95f, 0.8f, 1f));
            SetCenter(gradeField.rectTransform, new Vector2(600f, 42f), new Vector2(0f, -62f));

            feedbackField = EnsureText(
                feedbackField,
                FEEDBACK_NAME,
                panel,
                string.Empty,
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.86f, 0.82f, 0.74f, 1f));
            SetCenter(feedbackField.rectTransform, new Vector2(650f, 48f), new Vector2(0f, -112f));

            actionButton = EnsureButton(actionButton, panel);
            RectTransform buttonRect = actionButton.transform as RectTransform;
            SetBottom(buttonRect, new Vector2(240f, 58f), new Vector2(0f, 34f));

            actionButtonLabel = EnsureText(
                actionButtonLabel,
                ACTION_BUTTON_LABEL_NAME,
                buttonRect,
                actionText,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white);
            SetStretch(actionButtonLabel.rectTransform);
        }

        private RectTransform EnsureImageRect(
            RectTransform current,
            string objectName,
            Transform parent,
            Color color)
        {
            RectTransform rectTransform = current;
            if (rectTransform == null)
                rectTransform = FindChildRect(parent, objectName);

            if (rectTransform == null)
            {
                GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                gameObject.transform.SetParent(parent, false);
                rectTransform = gameObject.GetComponent<RectTransform>();
            }

            if (rectTransform.parent != parent)
                rectTransform.SetParent(parent, false);

            Image image = rectTransform.GetComponent<Image>();
            if (image == null)
                image = rectTransform.gameObject.AddComponent<Image>();

            image.color = color;
            image.raycastTarget = true;
            return rectTransform;
        }

        private TextMeshProUGUI EnsureText(
            TextMeshProUGUI current,
            string objectName,
            Transform parent,
            string defaultText,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
        {
            TextMeshProUGUI textField = current;
            if (textField == null)
                textField = FindChildText(parent, objectName);

            if (textField == null)
            {
                GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                gameObject.transform.SetParent(parent, false);
                textField = gameObject.GetComponent<TextMeshProUGUI>();
                textField.text = defaultText ?? string.Empty;
            }

            if (textField.transform.parent != parent)
                textField.transform.SetParent(parent, false);

            if (fontAsset != null)
                textField.font = fontAsset;

            textField.fontSize = fontSize;
            textField.fontStyle = fontStyle;
            textField.alignment = alignment;
            textField.color = color;
            textField.raycastTarget = false;
            return textField;
        }

        private Button EnsureButton(Button current, Transform parent)
        {
            Button button = current;
            if (button == null)
                button = FindChildButton(parent, ACTION_BUTTON_NAME);

            if (button == null)
            {
                GameObject gameObject = new GameObject(ACTION_BUTTON_NAME, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                gameObject.transform.SetParent(parent, false);
                button = gameObject.GetComponent<Button>();
            }

            if (button.transform.parent != parent)
                button.transform.SetParent(parent, false);

            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();
            image.color = new Color(0.72f, 0.42f, 0.18f, 1f);
            image.raycastTarget = true;
            button.targetGraphic = image;
            return button;
        }

        private static RectTransform FindChildRect(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            RectTransform[] rectTransforms = parent.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                if (rectTransforms[i] != null && rectTransforms[i].name == objectName)
                    return rectTransforms[i];
            }

            return null;
        }

        private static TextMeshProUGUI FindChildText(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            TextMeshProUGUI[] textFields = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < textFields.Length; i++)
            {
                if (textFields[i] != null && textFields[i].name == objectName)
                    return textFields[i];
            }

            return null;
        }

        private static Button FindChildButton(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            Button[] buttons = parent.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == objectName)
                    return buttons[i];
            }

            return null;
        }

        private void BindButton()
        {
            if (actionButton == null)
                return;

            actionButton.onClick.RemoveListener(SubmitTiming);
            actionButton.onClick.AddListener(SubmitTiming);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible == true ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
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

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
                selectable.interactable = interactable;
        }

        private static void SetStretch(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetCenter(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void SetTop(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void SetBottom(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
