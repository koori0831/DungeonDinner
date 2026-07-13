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
    /// 일정한 간격으로 막자를 내려쳐 목표 입자 크기를 만드는 빻기 미니게임
    /// </summary>
    public sealed class CookingGrindingMiniGameView : MonoBehaviour, ICookingMiniGameView
    {
        [SerializeField] private Button strikeButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Slider particleSlider;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private int requiredStrikeCount = 10;
        [SerializeField] private float targetInterval = 0.45f;
        [SerializeField] private float intervalTolerance = 0.35f;

        private Action<CookingMiniGameResult> _completed;
        private CookingGamePanel _owner;
        private int _strikeCount;
        private float _lastStrikeTime;
        private float _rhythmScoreSum;
        private int _rhythmSampleCount;
        private bool _buttonsBound;

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            _owner = owner;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);
            BindButtons();
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

        public bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Grinding;
        }

        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null
                || option == null
                || option.MiniGameType != CookingMiniGameType.Grinding
                || completed == null
                || strikeButton == null
                || requiredStrikeCount <= 0)
            {
                return false;
            }

            _completed = completed;
            _strikeCount = 0;
            _lastStrikeTime = 0f;
            _rhythmScoreSum = 0f;
            _rhythmSampleCount = 0;
            strikeButton.interactable = true;
            SetText(titleField, $"{option.DisplayName} 미니게임");
            SetText(instructionField, "일정한 박자로 막자를 내려치세요.");
            RefreshProgress();
            return true;
        }

        public void CancelMiniGame()
        {
            _completed = null;
            if (strikeButton != null)
                strikeButton.interactable = false;
        }

        private void HandleStrikeClicked()
        {
            if (_completed == null)
                return;

            float currentTime = Time.unscaledTime;
            if (_strikeCount > 0)
            {
                float interval = currentTime - _lastStrikeTime;
                float rhythmScore = 1f - Mathf.Abs(interval - targetInterval) / Mathf.Max(0.01f, intervalTolerance);
                _rhythmScoreSum += Mathf.Clamp01(rhythmScore);
                _rhythmSampleCount++;
            }

            _lastStrikeTime = currentTime;
            _strikeCount++;
            RefreshProgress();
            if (_strikeCount >= requiredStrikeCount)
                CompleteMiniGame();
        }

        private void CompleteMiniGame()
        {
            Action<CookingMiniGameResult> completed = _completed;
            if (completed == null)
                return;

            float score = _rhythmSampleCount > 0 ? _rhythmScoreSum / _rhythmSampleCount : 0f;
            CookingMiniGameGrade grade = CookingMiniGameUtility.ResolveGrade(score);
            _completed = null;
            strikeButton.interactable = false;
            completed.Invoke(CookingMiniGameUtility.CreateResult(
                CookingMiniGameType.Grinding,
                grade,
                score,
                "재료를 일정한 입자로 빻았습니다."));
        }

        private void RefreshProgress()
        {
            float ratio = requiredStrikeCount > 0 ? (float)_strikeCount / requiredStrikeCount : 0f;
            if (particleSlider != null)
                particleSlider.value = ratio;
            SetText(progressField, $"빻기 {_strikeCount} / {requiredStrikeCount}");
        }

        private void BindButtons()
        {
            if (_buttonsBound == true)
                return;
            if (strikeButton != null)
                strikeButton.onClick.AddListener(HandleStrikeClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancelClicked);
            _buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (_buttonsBound == false)
                return;
            if (strikeButton != null)
                strikeButton.onClick.RemoveListener(HandleStrikeClicked);
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
