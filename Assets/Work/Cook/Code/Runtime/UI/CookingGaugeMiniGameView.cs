using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 진행 상태를 관찰하고 목표 구간에서 멈추는 조리 미니게임
    /// </summary>
    public sealed class CookingGaugeMiniGameView : MonoBehaviour, ICookingMiniGameView
    {
        [SerializeField] private CookingMiniGameType miniGameType;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private RectTransform targetZone;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private string actionName = "조리";
        [SerializeField] private string instructionText = "적절한 상태에서 멈추세요.";
        [SerializeField] private float duration = 5f;
        [SerializeField, Range(0f, 1f)] private float targetMin = 0.55f;
        [SerializeField, Range(0f, 1f)] private float targetMax = 0.75f;

        private Action<CookingMiniGameResult> _completed;
        private CookingGamePanel _owner;
        private float _progress;
        private bool _isRunning;
        private bool _buttonsBound;

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            _owner = owner;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            BindButtons();
            RefreshTargetZone();
        }

        private void Update()
        {
            if (_isRunning == false)
                return;

            float safeDuration = Mathf.Max(0.1f, duration);
            _progress = Mathf.Clamp01(_progress + Time.unscaledDeltaTime / safeDuration);
            RefreshProgress();
            if (_progress >= 1f)
                CompleteAtCurrentProgress();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            if (titleField != null)
                titleField.font = value;
            if (instructionField != null)
                instructionField.font = value;
            if (progressField != null)
                progressField.font = value;
        }

        public bool CanPlay(CookingMiniGameType value)
        {
            return miniGameType != CookingMiniGameType.None && value == miniGameType;
        }

        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null
                || option == null
                || completed == null
                || CanPlay(option.MiniGameType) == false
                || progressSlider == null
                || stopButton == null)
            {
                return false;
            }

            _completed = completed;
            _progress = 0f;
            _isRunning = true;
            stopButton.interactable = true;
            SetText(titleField, $"{option.DisplayName} 미니게임");
            SetText(instructionField, instructionText);
            RefreshTargetZone();
            RefreshProgress();
            return true;
        }

        public void CancelMiniGame()
        {
            _completed = null;
            _isRunning = false;
            if (stopButton != null)
                stopButton.interactable = false;
        }

        private void CompleteAtCurrentProgress()
        {
            Action<CookingMiniGameResult> completed = _completed;
            if (completed == null)
                return;

            _completed = null;
            _isRunning = false;
            stopButton.interactable = false;

            float center = (targetMin + targetMax) * 0.5f;
            float halfWidth = Mathf.Max(0.01f, (targetMax - targetMin) * 0.5f);
            float normalizedDistance = Mathf.Abs(_progress - center) / halfWidth;
            float score = normalizedDistance <= 1f
                ? Mathf.Lerp(0.7f, 1f, 1f - normalizedDistance)
                : Mathf.Clamp01(0.7f - (normalizedDistance - 1f) * 0.35f);
            CookingMiniGameGrade grade = CookingMiniGameUtility.ResolveGrade(score);
            completed.Invoke(CookingMiniGameUtility.CreateResult(
                miniGameType,
                grade,
                score,
                BuildFeedbackText(grade)));
        }

        private string BuildFeedbackText(CookingMiniGameGrade grade)
        {
            string gradeText = grade == CookingMiniGameGrade.Perfect
                ? "완벽한 시점"
                : grade == CookingMiniGameGrade.Good
                    ? "좋은 시점"
                    : grade == CookingMiniGameGrade.Normal ? "무난한 시점" : "아쉬운 시점";
            return $"{actionName}을 {gradeText}에 마쳤습니다.";
        }

        private void RefreshProgress()
        {
            if (progressSlider != null)
                progressSlider.value = _progress;

            SetText(progressField, $"{actionName} 진행도 {_progress * 100f:0}%");
        }

        private void RefreshTargetZone()
        {
            if (targetZone == null)
                return;

            float minimum = Mathf.Clamp01(Mathf.Min(targetMin, targetMax));
            float maximum = Mathf.Clamp01(Mathf.Max(targetMin, targetMax));
            targetZone.anchorMin = new Vector2(minimum, 0f);
            targetZone.anchorMax = new Vector2(maximum, 1f);
            targetZone.offsetMin = Vector2.zero;
            targetZone.offsetMax = Vector2.zero;
        }

        private void BindButtons()
        {
            if (_buttonsBound == true)
                return;

            if (stopButton != null)
                stopButton.onClick.AddListener(CompleteAtCurrentProgress);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancelClicked);
            _buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (_buttonsBound == false)
                return;

            if (stopButton != null)
                stopButton.onClick.RemoveListener(CompleteAtCurrentProgress);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
            _buttonsBound = false;
        }

        private void HandleCancelClicked()
        {
            if (_owner != null)
                _owner.CancelActiveMiniGame();
        }

        private static void SetText(TextMeshProUGUI field, string value)
        {
            if (field != null)
                field.text = value ?? string.Empty;
        }
    }
}
