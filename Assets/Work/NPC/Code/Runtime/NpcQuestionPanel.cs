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
        [SerializeField] private RectTransform conversationContentRoot;
        [SerializeField, Min(0f)] private float conversationInsetWhenVisible = 324f;
        [SerializeField] private Button questionButtonPrefab;
        [SerializeField] private Button skipButton;
        [SerializeField] private string skipButtonLabel = "요리하기";
        [SerializeField] private bool hideWhenNoOptions = true;

        private readonly List<Button> _spawnedButtons = new List<Button>();
        private CanvasGroup _canvasGroup;
        private Vector2 _conversationDefaultOffsetMin;
        private bool _hasConversationDefaultOffset;

        private void Awake()
        {
            if (optionRoot == null)
            {
                Debug.LogError("NpcQuestionPanel optionRoot is missing. Assign a content root RectTransform in the inspector.", this);
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("NpcQuestionPanel CanvasGroup is missing. Add it to the prefab or scene object.", this);
            }

            if (conversationContentRoot != null)
            {
                _conversationDefaultOffsetMin = conversationContentRoot.offsetMin;
                _hasConversationDefaultOffset = true;
            }

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

            if (hideWhenNoOptions == true)
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
            ClearButtons();

            bool hasOptions = options != null && options.Count > 0;
            if (hideWhenNoOptions == true)
                SetVisible(hasOptions);

            if (hasOptions == false)
                return;

            for (int i = 0; i < options.Count; i++)
            {
                QuestionCategoryData option = options[i];
                Button button = CreateQuestionButton();
                if (button == null)
                {
                    continue;
                }

                SetButtonLabel(button, option.DisplayName);

                string categoryId = option.CategoryId;
                button.onClick.AddListener(() =>
                {
                    ClearButtons();
                    if (hideWhenNoOptions == true)
                        SetVisible(false);

                    runner.SelectQuestionCategory(categoryId);
                });
                _spawnedButtons.Add(button);
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
                skipButton.transform.SetAsLastSibling();
            }

            RefreshOptionLayout();
        }

        private void HandleCookingStepReady()
        {
            ClearButtons();

            if (hideWhenNoOptions == true)
                SetVisible(false);
        }

        private void HandleSkipButtonClicked()
        {
            runner?.SkipQuestions();
        }

        private Button CreateQuestionButton()
        {
            if (optionRoot == null)
            {
                Debug.LogError("NpcQuestionPanel cannot create a question button because optionRoot is missing. Assign a content root RectTransform in the inspector.", this);
                return null;
            }

            if (questionButtonPrefab != null)
            {
                Button button = Instantiate(questionButtonPrefab, optionRoot);
                button.gameObject.SetActive(true);
                return button;
            }

            Debug.LogError("NpcQuestionPanel questionButtonPrefab is missing. Assign a question button prefab in the inspector.", this);
            return null;
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
            {
                legacyText.text = label;
            }
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
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }

            ApplyConversationInset(visible);
        }

        private void ApplyConversationInset(bool visible)
        {
            if (conversationContentRoot == null || _hasConversationDefaultOffset == false)
                return;

            Vector2 offsetMin = _conversationDefaultOffsetMin;
            if (visible == true)
                offsetMin.y += conversationInsetWhenVisible;

            conversationContentRoot.offsetMin = offsetMin;
            LayoutRebuilder.MarkLayoutForRebuild(conversationContentRoot);
        }

        private void RefreshOptionLayout()
        {
            if (optionRoot == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(optionRoot);
        }

    }
}
