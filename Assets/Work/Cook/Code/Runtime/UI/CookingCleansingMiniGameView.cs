using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 오염물 위를 드래그해 제거율과 불필요한 문지름을 판정하는 씻기 미니게임
    /// </summary>
    public sealed class CookingCleansingMiniGameView : MonoBehaviour, ICookingMiniGameView,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform interactionArea;
        [SerializeField] private Image ingredientImage;
        [SerializeField] private Image brushImage;
        [SerializeField] private Image[] stainImages;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private Button cancelButton;
        [SerializeField] private float cleaningRadius = 75f;
        [SerializeField, Range(0.01f, 0.5f)] private float cleaningAmountPerSample = 0.08f;

        private Action<CookingMiniGameResult> _completed;
        private float[] _remainingDirt;
        private int _activePointerId = int.MinValue;
        private int _totalSamples;
        private int _wastedSamples;
        private CookingGamePanel _owner;
        private bool _isCancelButtonBound;

        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            _owner = owner;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            BindCancelButton();
        }

        private void OnDestroy()
        {
            if (cancelButton != null && _isCancelButtonBound == true)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
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
            return miniGameType == CookingMiniGameType.Cleansing;
        }

        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null
                || option == null
                || option.MiniGameType != CookingMiniGameType.Cleansing
                || completed == null
                || interactionArea == null
                || stainImages == null
                || stainImages.Length == 0)
            {
                return false;
            }

            _completed = completed;
            _remainingDirt = new float[stainImages.Length];
            _activePointerId = int.MinValue;
            _totalSamples = 0;
            _wastedSamples = 0;

            if (ingredientImage != null)
            {
                ingredientImage.sprite = CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
                ingredientImage.enabled = ingredientImage.sprite != null;
                ingredientImage.preserveAspect = true;
            }

            for (int i = 0; i < stainImages.Length; i++)
            {
                _remainingDirt[i] = 1f;
                SetStainAlpha(i, 1f);
            }

            if (brushImage != null)
                brushImage.gameObject.SetActive(false);

            SetText(titleField, $"{option.DisplayName} 미니게임");
            SetText(instructionField, "오염물을 문질러 모두 씻어내세요.");
            RefreshProgressText();
            return true;
        }

        public void CancelMiniGame()
        {
            _completed = null;
            _remainingDirt = null;
            _activePointerId = int.MinValue;
            if (brushImage != null)
                brushImage.gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_completed == null || eventData == null)
                return;

            _activePointerId = eventData.pointerId;
            ProcessPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerId != _activePointerId)
                return;

            ProcessPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerId == _activePointerId)
                ProcessPointer(eventData);

            _activePointerId = int.MinValue;
            if (brushImage != null)
                brushImage.gameObject.SetActive(false);
        }

        private void ProcessPointer(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    interactionArea,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPosition) == false)
            {
                return;
            }

            if (brushImage != null)
            {
                brushImage.gameObject.SetActive(true);
                brushImage.rectTransform.anchoredPosition = localPosition;
            }

            _totalSamples++;
            bool cleanedAny = false;
            for (int i = 0; i < stainImages.Length; i++)
            {
                if (_remainingDirt[i] <= 0f || stainImages[i] == null)
                    continue;

                float distance = Vector2.Distance(localPosition, stainImages[i].rectTransform.anchoredPosition);
                if (distance > cleaningRadius)
                    continue;

                cleanedAny = true;
                _remainingDirt[i] = Mathf.Max(0f, _remainingDirt[i] - cleaningAmountPerSample);
                SetStainAlpha(i, _remainingDirt[i]);
            }

            if (cleanedAny == false)
                _wastedSamples++;

            RefreshProgressText();
            if (IsCompletelyClean() == true)
                CompleteMiniGame();
        }

        private void CompleteMiniGame()
        {
            Action<CookingMiniGameResult> completed = _completed;
            if (completed == null)
                return;

            float wasteRatio = _totalSamples > 0 ? (float)_wastedSamples / _totalSamples : 1f;
            float score = Mathf.Clamp01(1f - wasteRatio * 1.5f);
            CookingMiniGameGrade grade = CookingMiniGameUtility.ResolveGrade(score);
            _completed = null;
            completed.Invoke(CookingMiniGameUtility.CreateResult(
                CookingMiniGameType.Cleansing,
                grade,
                score,
                "오염물을 씻어 재료를 깨끗하게 손질했습니다."));
        }

        private bool IsCompletelyClean()
        {
            if (_remainingDirt == null)
                return false;

            for (int i = 0; i < _remainingDirt.Length; i++)
            {
                if (_remainingDirt[i] > 0f)
                    return false;
            }

            return true;
        }

        private void SetStainAlpha(int index, float value)
        {
            if (stainImages[index] == null)
                return;

            Color color = stainImages[index].color;
            color.a = Mathf.Clamp01(value);
            stainImages[index].color = color;
        }

        private void RefreshProgressText()
        {
            if (_remainingDirt == null || _remainingDirt.Length == 0)
            {
                SetText(progressField, string.Empty);
                return;
            }

            float remaining = 0f;
            for (int i = 0; i < _remainingDirt.Length; i++)
                remaining += _remainingDirt[i];

            float cleanRatio = 1f - remaining / _remainingDirt.Length;
            SetText(progressField, $"세척 진행도 {cleanRatio * 100f:0}%");
        }

        private void BindCancelButton()
        {
            if (cancelButton == null || _isCancelButtonBound == true)
                return;

            cancelButton.onClick.AddListener(HandleCancelClicked);
            _isCancelButtonBound = true;
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
