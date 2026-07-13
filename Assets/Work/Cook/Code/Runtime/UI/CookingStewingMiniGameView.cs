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
    /// 불 조절, 젓기, 거품 걷기를 순서대로 반복하는 끓이기 미니게임
    /// </summary>
    public sealed class CookingStewingMiniGameView : MonoBehaviour, ICookingMiniGameView
    {
        [SerializeField] private Button heatButton;
        [SerializeField] private Button stirButton;
        [SerializeField] private Button skimButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private int requiredRounds = 3;
        [SerializeField] private float perfectDuration = 5f;
        [SerializeField] private float normalDuration = 12f;

        private Action<CookingMiniGameResult> _completed;
        private CookingGamePanel _owner;
        private int _stepIndex;
        private int _mistakeCount;
        private float _startedTime;
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
            return miniGameType == CookingMiniGameType.Stewing;
        }

        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null
                || option == null
                || option.MiniGameType != CookingMiniGameType.Stewing
                || completed == null
                || heatButton == null
                || stirButton == null
                || skimButton == null
                || requiredRounds <= 0)
            {
                return false;
            }

            _completed = completed;
            _stepIndex = 0;
            _mistakeCount = 0;
            _startedTime = Time.unscaledTime;
            SetButtonsInteractable(true);
            SetText(titleField, $"{option.DisplayName} 미니게임");
            RefreshInstruction();
            return true;
        }

        public void CancelMiniGame()
        {
            _completed = null;
            SetButtonsInteractable(false);
        }

        private void HandleHeatClicked()
        {
            HandleStep(0);
        }

        private void HandleStirClicked()
        {
            HandleStep(1);
        }

        private void HandleSkimClicked()
        {
            HandleStep(2);
        }

        private void HandleStep(int selectedStep)
        {
            if (_completed == null)
                return;

            int expectedStep = _stepIndex % 3;
            if (selectedStep != expectedStep)
            {
                _mistakeCount++;
                RefreshInstruction();
                return;
            }

            _stepIndex++;
            if (_stepIndex >= requiredRounds * 3)
            {
                CompleteMiniGame();
                return;
            }

            RefreshInstruction();
        }

        private void CompleteMiniGame()
        {
            Action<CookingMiniGameResult> completed = _completed;
            if (completed == null)
                return;

            float duration = Time.unscaledTime - _startedTime;
            float speedScore = 1f - Mathf.InverseLerp(perfectDuration, normalDuration, duration);
            float accuracyScore = Mathf.Clamp01(1f - _mistakeCount * 0.12f);
            float score = speedScore * 0.35f + accuracyScore * 0.65f;
            CookingMiniGameGrade grade = CookingMiniGameUtility.ResolveGrade(score);
            _completed = null;
            SetButtonsInteractable(false);
            completed.Invoke(CookingMiniGameUtility.CreateResult(
                CookingMiniGameType.Stewing,
                grade,
                score,
                "불과 국물 상태를 조절해 깊은 맛을 냈습니다."));
        }

        private void RefreshInstruction()
        {
            int expectedStep = _stepIndex % 3;
            string action = expectedStep == 0 ? "불 조절" : expectedStep == 1 ? "젓기" : "거품 걷기";
            int currentRound = Mathf.Min(requiredRounds, _stepIndex / 3 + 1);
            SetText(instructionField, $"다음 행동: {action}");
            SetText(progressField, $"끓이기 {currentRound} / {requiredRounds}   실수 {_mistakeCount}");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (heatButton != null)
                heatButton.interactable = interactable;
            if (stirButton != null)
                stirButton.interactable = interactable;
            if (skimButton != null)
                skimButton.interactable = interactable;
        }

        private void BindButtons()
        {
            if (_buttonsBound == true)
                return;
            if (heatButton != null)
                heatButton.onClick.AddListener(HandleHeatClicked);
            if (stirButton != null)
                stirButton.onClick.AddListener(HandleStirClicked);
            if (skimButton != null)
                skimButton.onClick.AddListener(HandleSkimClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancelClicked);
            _buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (_buttonsBound == false)
                return;
            if (heatButton != null)
                heatButton.onClick.RemoveListener(HandleHeatClicked);
            if (stirButton != null)
                stirButton.onClick.RemoveListener(HandleStirClicked);
            if (skimButton != null)
                skimButton.onClick.RemoveListener(HandleSkimClicked);
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
