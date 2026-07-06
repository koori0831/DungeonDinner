using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.NPC.Code.Data;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcQuestionPanel : MonoBehaviour
    {
        [SerializeField] private NpcConversationRunner runner;
        [SerializeField] private RectTransform optionRoot;
        [SerializeField] private Button questionButtonPrefab;
        [SerializeField] private Button skipButton;
        [SerializeField] private string skipButtonLabel = "요리하기";
        [SerializeField] private bool hideWhenNoOptions = true;

        private readonly List<Button> _spawnedButtons = new List<Button>();
        private CanvasGroup _canvasGroup;
        private LayoutGroup _optionLayoutGroup;

        private void Awake()
        {
            if (optionRoot == null)
                optionRoot = transform as RectTransform;

            EnsureOptionRootLayout();

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            SetSkipButtonLabel();
        }

        private void OnEnable()
        {
            if (runner == null)
                runner = FindFirstObjectByType<NpcConversationRunner>();

            if (runner != null)
            {
                runner.QuestionOptionsUpdated += HandleQuestionOptionsUpdated;
                runner.CookingStepReady += HandleCookingStepReady;
            }

            if (skipButton != null)
                skipButton.onClick.AddListener(HandleSkipButtonClicked);

            if (hideWhenNoOptions)
                SetVisible(false);
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.QuestionOptionsUpdated -= HandleQuestionOptionsUpdated;
                runner.CookingStepReady -= HandleCookingStepReady;
            }

            if (skipButton != null)
                skipButton.onClick.RemoveListener(HandleSkipButtonClicked);
        }

        private void HandleQuestionOptionsUpdated(IReadOnlyList<QuestionCategoryData> options)
        {
            EnsureOptionRootLayout();
            ClearButtons();

            bool hasOptions = options != null && options.Count > 0;
            if (hideWhenNoOptions)
                SetVisible(hasOptions);

            if (hasOptions == false)
                return;

            for (int i = 0; i < options.Count; i++)
            {
                QuestionCategoryData option = options[i];
                Button button = CreateQuestionButton();
                SetButtonLabel(button, option.DisplayName);

                string categoryId = option.CategoryId;
                button.onClick.AddListener(() =>
                {
                    ClearButtons();
                    if (hideWhenNoOptions)
                        SetVisible(false);

                    runner.SelectQuestionCategory(categoryId);
                });
                _spawnedButtons.Add(button);
            }

            if (skipButton != null)
                skipButton.gameObject.SetActive(true);
        }

        private void HandleCookingStepReady()
        {
            ClearButtons();

            if (hideWhenNoOptions)
                SetVisible(false);
        }

        private void HandleSkipButtonClicked()
        {
            runner?.SkipQuestions();
        }

        private Button CreateQuestionButton()
        {
            if (questionButtonPrefab != null)
            {
                Button button = Instantiate(questionButtonPrefab, optionRoot);
                button.gameObject.SetActive(true);
                PrepareButtonLayout(button);
                return button;
            }

            return CreateFallbackButton();
        }

        private Button CreateFallbackButton()
        {
            GameObject buttonObject = new GameObject("QuestionButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(optionRoot, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 48f;
            layoutElement.preferredHeight = 52f;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 8f);
            textRect.offsetMax = new Vector2(-14f, -8f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.color = Color.white;

            return buttonObject.GetComponent<Button>();
        }

        private void PrepareButtonLayout(Button button)
        {
            if (button == null)
                return;

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
            }

            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = button.gameObject.AddComponent<LayoutElement>();

            layoutElement.minHeight = Mathf.Max(layoutElement.minHeight, 48f);
            layoutElement.preferredHeight = Mathf.Max(layoutElement.preferredHeight, 52f);
            layoutElement.flexibleWidth = 1f;
        }

        private void SetButtonLabel(Button button, string label)
        {
            if (button == null)
                return;

            TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.text = label;
                return;
            }

            Text legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null)
                legacyText.text = label;
        }

        private void SetSkipButtonLabel()
        {
            if (skipButton == null)
                return;

            SetButtonLabel(skipButton, skipButtonLabel);
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                    Destroy(_spawnedButtons[i].gameObject);
            }

            _spawnedButtons.Clear();

            if (skipButton != null)
                skipButton.gameObject.SetActive(false);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private void EnsureOptionRootLayout()
        {
            if (optionRoot == null)
                return;

            _optionLayoutGroup = optionRoot.GetComponent<LayoutGroup>();
            if (_optionLayoutGroup != null)
                return;

            VerticalLayoutGroup verticalLayoutGroup = optionRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.spacing = 10f;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = optionRoot.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
                contentSizeFitter = optionRoot.gameObject.AddComponent<ContentSizeFitter>();

            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _optionLayoutGroup = verticalLayoutGroup;
        }
    }
}
