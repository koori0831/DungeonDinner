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
    /// 절단선을 따라 드래그한 정확도로 결과를 산출하는 썰기 미니게임
    /// </summary>
    public sealed class CookingSlicingMiniGameView : MonoBehaviour, ICookingMiniGameView,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Layout")]
        [SerializeField] private RectTransform interactionArea;
        [SerializeField] private Image ingredientImage;
        [SerializeField] private Image knifeImage;
        [SerializeField] private Image[] cutLineImages;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private Button cancelButton;

        [Header("Judgement")]
        [SerializeField] private float startTolerance = 55f;
        [SerializeField] private float lineTolerance = 42f;
        [SerializeField, Range(0f, 1f)] private float requiredProgress = 0.88f;
        [SerializeField, Range(0f, 1f)] private float perfectThreshold = 0.9f;
        [SerializeField, Range(0f, 1f)] private float goodThreshold = 0.7f;
        [SerializeField, Range(0f, 1f)] private float normalThreshold = 0.45f;
        [SerializeField, Range(0f, 1f)] private float mistakePenalty = 0.1f;

        [Header("Result")]
        [SerializeField] private int perfectQualityDelta = 2;
        [SerializeField] private int goodQualityDelta = 1;
        [SerializeField] private int normalQualityDelta;
        [SerializeField] private int badQualityDelta = -1;
        [SerializeField] private Color pendingLineColor = new Color(1f, 0.88f, 0.45f, 0.9f);
        [SerializeField] private Color completedLineColor = new Color(0.38f, 0.85f, 0.42f, 0.95f);

        private Action<CookingMiniGameResult> _completed;
        private bool[] _completedLines;
        private int _completedLineCount;
        private int _mistakeCount;
        private int _activeLineIndex = -1;
        private int _activePointerId = int.MinValue;
        private Vector2 _activeStart;
        private Vector2 _activeEnd;
        private float _activeLineLengthSquared;
        private float _maximumProgress;
        private float _deviationSum;
        private int _deviationSampleCount;
        private float _precisionSum;
        private TMP_FontAsset _fontAsset;
        private CookingGamePanel _owner;
        private bool _isCancelButtonBound;

        /// <summary>
        /// 썰기 미니게임 UI 초기화
        /// </summary>
        /// <param name="owner">요리 패널</param>
        /// <param name="runner">요리 플로우 러너</param>
        /// <param name="defaultFontAsset">기본 UI 폰트</param>
        public void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null)
        {
            _owner = owner;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureReferences();
            BindCancelButton();
            ResetInteractionState();
        }

        private void OnDestroy()
        {
            if (cancelButton != null && _isCancelButtonBound == true)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }

        /// <summary>
        /// 썰기 미니게임 텍스트에 폰트 적용
        /// </summary>
        /// <param name="value">적용할 폰트</param>
        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            _fontAsset = value;
            if (titleField != null)
                titleField.font = value;
            if (instructionField != null)
                instructionField.font = value;
            if (progressField != null)
                progressField.font = value;
        }

        /// <summary>
        /// 썰기 타입 지원 여부 확인
        /// </summary>
        /// <param name="miniGameType">확인할 미니게임 타입</param>
        /// <returns>썰기 타입 여부</returns>
        public bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Slicing;
        }

        /// <summary>
        /// 재료와 절단선을 초기화하고 썰기 미니게임 시작
        /// </summary>
        /// <param name="ingredient">손질 대상 재료</param>
        /// <param name="option">선택한 손질 옵션</param>
        /// <param name="completed">미니게임 완료 콜백</param>
        /// <returns>시작 성공 여부</returns>
        public bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            EnsureReferences();
            if (ingredient == null
                || option == null
                || option.MiniGameType != CookingMiniGameType.Slicing
                || completed == null
                || interactionArea == null
                || cutLineImages == null
                || cutLineImages.Length == 0)
            {
                return false;
            }

            _completed = completed;
            _completedLines = new bool[cutLineImages.Length];
            _completedLineCount = 0;
            _mistakeCount = 0;
            _precisionSum = 0f;
            ResetActiveDrag();

            if (ingredientImage != null)
            {
                ingredientImage.sprite = CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
                ingredientImage.enabled = ingredientImage.sprite != null;
                ingredientImage.preserveAspect = true;
            }

            for (int i = 0; i < cutLineImages.Length; i++)
            {
                if (cutLineImages[i] != null)
                    cutLineImages[i].color = pendingLineColor;
            }

            if (knifeImage != null)
                knifeImage.gameObject.SetActive(false);

            SetText(titleField, $"{option.DisplayName} 미니게임");
            SetText(instructionField, "절단선의 한쪽 끝에서 반대쪽 끝까지 드래그하세요.");
            RefreshProgressText();
            return true;
        }

        /// <summary>
        /// 진행 중인 썰기 미니게임 취소
        /// </summary>
        public void CancelMiniGame()
        {
            _completed = null;
            ResetInteractionState();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_completed == null || _activeLineIndex >= 0 || eventData == null)
                return;

            if (TryGetLocalPointerPosition(eventData, out Vector2 pointerPosition) == false)
                return;

            int lineIndex = FindClosestAvailableLineEndpoint(pointerPosition, out Vector2 start, out Vector2 end);
            if (lineIndex < 0)
            {
                RegisterMistake();
                return;
            }

            _activeLineIndex = lineIndex;
            _activePointerId = eventData.pointerId;
            _activeStart = start;
            _activeEnd = end;
            _activeLineLengthSquared = (_activeEnd - _activeStart).sqrMagnitude;
            _maximumProgress = 0f;
            _deviationSum = 0f;
            _deviationSampleCount = 0;
            UpdateActiveDrag(pointerPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;

            if (TryGetLocalPointerPosition(eventData, out Vector2 pointerPosition) == false)
                return;

            UpdateActiveDrag(pointerPosition);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;

            if (TryGetLocalPointerPosition(eventData, out Vector2 pointerPosition) == true)
                UpdateActiveDrag(pointerPosition);

            float averageDeviation = _deviationSampleCount > 0
                ? _deviationSum / _deviationSampleCount
                : lineTolerance;
            bool isSuccessful = _maximumProgress >= requiredProgress && averageDeviation <= lineTolerance;
            int completedLineIndex = _activeLineIndex;
            ResetActiveDrag();

            if (isSuccessful == false)
            {
                RegisterMistake();
                return;
            }

            _completedLines[completedLineIndex] = true;
            _completedLineCount++;
            _precisionSum += Mathf.Clamp01(1f - averageDeviation / lineTolerance);
            if (cutLineImages[completedLineIndex] != null)
                cutLineImages[completedLineIndex].color = completedLineColor;

            RefreshProgressText();
            if (_completedLineCount >= _completedLines.Length)
                CompleteMiniGame();
        }

        private int FindClosestAvailableLineEndpoint(
            Vector2 pointerPosition,
            out Vector2 selectedStart,
            out Vector2 selectedEnd)
        {
            selectedStart = Vector2.zero;
            selectedEnd = Vector2.zero;
            int selectedIndex = -1;
            float closestDistance = startTolerance;

            for (int i = 0; i < cutLineImages.Length; i++)
            {
                if (_completedLines[i] == true || cutLineImages[i] == null)
                    continue;

                RectTransform lineRect = cutLineImages[i].rectTransform;
                Vector2 center = lineRect.anchoredPosition;
                Vector2 halfDirection = ResolveLineHalfDirection(lineRect);
                Vector2 firstEndpoint = center - halfDirection;
                Vector2 secondEndpoint = center + halfDirection;
                float firstDistance = Vector2.Distance(pointerPosition, firstEndpoint);
                float secondDistance = Vector2.Distance(pointerPosition, secondEndpoint);

                if (firstDistance < closestDistance)
                {
                    closestDistance = firstDistance;
                    selectedIndex = i;
                    selectedStart = firstEndpoint;
                    selectedEnd = secondEndpoint;
                }

                if (secondDistance < closestDistance)
                {
                    closestDistance = secondDistance;
                    selectedIndex = i;
                    selectedStart = secondEndpoint;
                    selectedEnd = firstEndpoint;
                }
            }

            return selectedIndex;
        }

        private static Vector2 ResolveLineHalfDirection(RectTransform lineRect)
        {
            float angleRadians = lineRect.localEulerAngles.z * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(-Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));
            return direction * (lineRect.rect.height * 0.5f);
        }

        private void UpdateActiveDrag(Vector2 pointerPosition)
        {
            if (_activeLineLengthSquared <= Mathf.Epsilon)
                return;

            Vector2 line = _activeEnd - _activeStart;
            float progress = Vector2.Dot(pointerPosition - _activeStart, line) / _activeLineLengthSquared;
            _maximumProgress = Mathf.Max(_maximumProgress, progress);

            float clampedProgress = Mathf.Clamp01(progress);
            Vector2 closestPoint = _activeStart + line * clampedProgress;
            _deviationSum += Vector2.Distance(pointerPosition, closestPoint);
            _deviationSampleCount++;

            if (knifeImage != null)
            {
                knifeImage.gameObject.SetActive(true);
                knifeImage.rectTransform.anchoredPosition = pointerPosition;
            }
        }

        private void CompleteMiniGame()
        {
            Action<CookingMiniGameResult> completed = _completed;
            if (completed == null)
                return;

            float averagePrecision = _completedLineCount > 0 ? _precisionSum / _completedLineCount : 0f;
            float score = Mathf.Clamp01(averagePrecision - _mistakeCount * mistakePenalty);
            CookingMiniGameGrade grade = ResolveGrade(score);
            CookingMiniGameResult result = new CookingMiniGameResult(
                CookingMiniGameType.Slicing,
                grade,
                score,
                ResolveQualityDelta(grade),
                BuildFeedbackText(grade));

            _completed = null;
            ResetActiveDrag();
            completed.Invoke(result);
        }

        private CookingMiniGameGrade ResolveGrade(float score)
        {
            if (score >= perfectThreshold)
                return CookingMiniGameGrade.Perfect;
            if (score >= goodThreshold)
                return CookingMiniGameGrade.Good;
            if (score >= normalThreshold)
                return CookingMiniGameGrade.Normal;

            return CookingMiniGameGrade.Bad;
        }

        private int ResolveQualityDelta(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return perfectQualityDelta;
                case CookingMiniGameGrade.Good:
                    return goodQualityDelta;
                case CookingMiniGameGrade.Bad:
                    return badQualityDelta;
                default:
                    return normalQualityDelta;
            }
        }

        private static string BuildFeedbackText(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return "절단선에 맞춰 매우 정교하게 썰었습니다.";
                case CookingMiniGameGrade.Good:
                    return "재료를 고르게 썰었습니다.";
                case CookingMiniGameGrade.Bad:
                    return "칼질이 거칠어 재료 크기가 고르지 않습니다.";
                default:
                    return "재료를 무난하게 썰었습니다.";
            }
        }

        private bool TryGetLocalPointerPosition(PointerEventData eventData, out Vector2 localPosition)
        {
            localPosition = Vector2.zero;
            return interactionArea != null
                   && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                       interactionArea,
                       eventData.position,
                       eventData.pressEventCamera,
                       out localPosition);
        }

        private bool IsActivePointer(PointerEventData eventData)
        {
            return eventData != null
                   && _activeLineIndex >= 0
                   && eventData.pointerId == _activePointerId;
        }

        private void RegisterMistake()
        {
            _mistakeCount++;
            SetText(instructionField, "절단선 끝에서 시작해 선을 따라 드래그하세요.");
            RefreshProgressText();
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

        private void RefreshProgressText()
        {
            int totalCount = _completedLines != null ? _completedLines.Length : 0;
            SetText(progressField, $"절단 {_completedLineCount} / {totalCount}   실수 {_mistakeCount}");
        }

        private void ResetInteractionState()
        {
            _completedLines = null;
            _completedLineCount = 0;
            _mistakeCount = 0;
            _precisionSum = 0f;
            ResetActiveDrag();
        }

        private void ResetActiveDrag()
        {
            _activeLineIndex = -1;
            _activePointerId = int.MinValue;
            _activeLineLengthSquared = 0f;
            _maximumProgress = 0f;
            _deviationSum = 0f;
            _deviationSampleCount = 0;
            if (knifeImage != null)
                knifeImage.gameObject.SetActive(false);
        }

        private void EnsureReferences()
        {
            if (interactionArea == null)
                interactionArea = transform as RectTransform;

            if (_fontAsset != null)
                SetFontAsset(_fontAsset);
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
