using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 순서대로 강조되는 절단 지점을 빠르게 클릭하는 다지기 미니게임
    /// </summary>
    public sealed class CookingChoppingMiniGameView : MonoBehaviour, ICookingMiniGameView
    {
        [SerializeField] private Button[] targetButtons;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Color pendingColor = new Color(0.45f, 0.3f, 0.16f, 1f);
        [SerializeField] private Color activeColor = new Color(1f, 0.72f, 0.2f, 1f);
        [SerializeField] private Color completedColor = new Color(0.35f, 0.75f, 0.32f, 1f);
        [SerializeField] private float perfectDuration = 3f;
        [SerializeField] private float normalDuration = 8f;

        private Action<CookingMiniGameResult> _completed;
        private UnityAction[] _targetActions;
        private int[] _targetOrder;
        private int _currentOrderIndex;
        private int _mistakeCount;
        private float _startedTime;
        private CookingGamePanel _owner;
        private bool _isInitialized;

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
            return miniGameType == CookingMiniGameType.Chopping;
        }

        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null
                || option == null
                || option.MiniGameType != CookingMiniGameType.Chopping
                || completed == null
                || targetButtons == null
                || targetButtons.Length == 0)
            {
                return false;
            }

            BindButtons();
            _completed = completed;
            _targetOrder = new int[targetButtons.Length];
            for (int i = 0; i < _targetOrder.Length; i++)
                _targetOrder[i] = i;

            ShuffleTargetOrder();
            _currentOrderIndex = 0;
            _mistakeCount = 0;
            _startedTime = Time.unscaledTime;
            SetText(titleField, $"{option.DisplayName} 미니게임");
            SetText(instructionField, "빛나는 절단 지점을 순서대로 빠르게 누르세요.");
            RefreshTargets();
            return true;
        }

        public void CancelMiniGame()
        {
            _completed = null;
            SetTargetsInteractable(false);
        }

        private void BindButtons()
        {
            if (_isInitialized == true || targetButtons == null)
                return;

            _targetActions = new UnityAction[targetButtons.Length];
            for (int i = 0; i < targetButtons.Length; i++)
            {
                int targetIndex = i;
                UnityAction action = () => HandleTargetClicked(targetIndex);
                _targetActions[i] = action;
                if (targetButtons[i] != null)
                    targetButtons[i].onClick.AddListener(action);
            }

            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancelClicked);

            _isInitialized = true;
        }

        private void UnbindButtons()
        {
            if (_isInitialized == false)
                return;

            if (targetButtons != null && _targetActions != null)
            {
                int count = Mathf.Min(targetButtons.Length, _targetActions.Length);
                for (int i = 0; i < count; i++)
                {
                    if (targetButtons[i] != null && _targetActions[i] != null)
                        targetButtons[i].onClick.RemoveListener(_targetActions[i]);
                }
            }

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);

            _isInitialized = false;
        }

        private void HandleTargetClicked(int targetIndex)
        {
            if (_completed == null || _targetOrder == null || _currentOrderIndex >= _targetOrder.Length)
                return;

            if (_targetOrder[_currentOrderIndex] != targetIndex)
            {
                _mistakeCount++;
                RefreshProgressText();
                return;
            }

            _currentOrderIndex++;
            if (_currentOrderIndex >= _targetOrder.Length)
            {
                CompleteMiniGame();
                return;
            }

            RefreshTargets();
        }

        private void CompleteMiniGame()
        {
            Action<CookingMiniGameResult> completed = _completed;
            if (completed == null)
                return;

            float duration = Time.unscaledTime - _startedTime;
            float speedScore = 1f - Mathf.InverseLerp(perfectDuration, normalDuration, duration);
            float accuracyScore = Mathf.Clamp01(1f - _mistakeCount * 0.15f);
            float score = Mathf.Clamp01(speedScore * 0.4f + accuracyScore * 0.6f);
            CookingMiniGameGrade grade = CookingMiniGameUtility.ResolveGrade(score);
            _completed = null;
            SetTargetsInteractable(false);
            completed.Invoke(CookingMiniGameUtility.CreateResult(
                CookingMiniGameType.Chopping,
                grade,
                score,
                "절단 지점을 일정하게 다졌습니다."));
        }

        private void ShuffleTargetOrder()
        {
            for (int i = _targetOrder.Length - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                int value = _targetOrder[i];
                _targetOrder[i] = _targetOrder[swapIndex];
                _targetOrder[swapIndex] = value;
            }
        }

        private void RefreshTargets()
        {
            if (targetButtons == null)
                return;

            int activeTarget = _targetOrder != null && _currentOrderIndex < _targetOrder.Length
                ? _targetOrder[_currentOrderIndex]
                : -1;
            for (int i = 0; i < targetButtons.Length; i++)
            {
                Button button = targetButtons[i];
                if (button == null)
                    continue;

                bool isCompleted = IsTargetCompleted(i);
                button.interactable = isCompleted == false;
                Color color = isCompleted == true ? completedColor : i == activeTarget ? activeColor : pendingColor;
                if (button.targetGraphic != null)
                    button.targetGraphic.color = color;
            }

            RefreshProgressText();
        }

        private bool IsTargetCompleted(int targetIndex)
        {
            if (_targetOrder == null)
                return false;

            for (int i = 0; i < _currentOrderIndex; i++)
            {
                if (_targetOrder[i] == targetIndex)
                    return true;
            }

            return false;
        }

        private void SetTargetsInteractable(bool interactable)
        {
            if (targetButtons == null)
                return;

            for (int i = 0; i < targetButtons.Length; i++)
            {
                if (targetButtons[i] != null)
                    targetButtons[i].interactable = interactable;
            }
        }

        private void RefreshProgressText()
        {
            int totalCount = _targetOrder != null ? _targetOrder.Length : 0;
            SetText(progressField, $"다지기 {_currentOrderIndex} / {totalCount}   실수 {_mistakeCount}");
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
