using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingSlicingMiniGameView : CookingOverlayMiniGameController,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image knifeImage;
        [SerializeField] private Image[] cutLineImages;
        [SerializeField] private Color pendingColor = new Color(1f, 0.86f, 0.35f, 0.72f);
        [SerializeField] private Color completedColor = new Color(0.38f, 0.9f, 0.45f, 0.9f);
        [SerializeField, Range(0f, 0.3f)] private float mistakePenalty = 0.1f;

        private bool[] _completedLines;
        private int _activeLine = -1;
        private int _completedCount;
        private int _mistakes;
        private Vector2 _lineStart;
        private Vector2 _lineEnd;
        private float _maximumProgress;
        private float _deviation;
        private int _samples;
        private float _precisionSum;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Slicing;
        }

        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Slicing, completed) == false
                || cutLineImages == null
                || cutLineImages.Length == 0)
            {
                return false;
            }

            _completedLines = new bool[cutLineImages.Length];
            _activeLine = -1;
            _completedCount = 0;
            _mistakes = 0;
            _precisionSum = 0f;
            for (int i = 0; i < cutLineImages.Length; i++)
            {
                if (cutLineImages[i] != null)
                    cutLineImages[i].color = pendingColor;
            }

            if (knifeImage != null)
                knifeImage.gameObject.SetActive(false);
            Host.SetInstruction("절단선의 끝에서 반대쪽 끝까지 드래그하세요.");
            Host.SetStatus("첫 번째 절단선을 따라 그으세요");
            return true;
        }

        public override void CancelMiniGame()
        {
            base.CancelMiniGame();
            _activeLine = -1;
            if (knifeImage != null)
                knifeImage.gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Completion == null || _activeLine >= 0 || TryCapturePointer(eventData) == false)
                return;
            if (TryGetLocalPosition(eventData, out Vector2 point) == false)
            {
                ReleasePointer();
                return;
            }

            CookingMiniGameOverlayProfile profile = GetProfile(CookingMiniGameType.Slicing);
            float tolerance = Mathf.Min(((RectTransform)transform).rect.width, ((RectTransform)transform).rect.height)
                              * profile.PrimaryTolerance;
            _activeLine = FindClosestEndpoint(point, tolerance, out _lineStart, out _lineEnd);
            if (_activeLine < 0)
            {
                ReleasePointer();
                _mistakes++;
                RegisterMistake("절단선 끝에서 드래그를 시작하세요.");
                return;
            }

            _maximumProgress = 0f;
            _deviation = 0f;
            _samples = 0;
            UpdateDrag(point);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false || TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;
            UpdateDrag(point);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;
            if (TryGetLocalPosition(eventData, out Vector2 point))
                UpdateDrag(point);

            CookingMiniGameOverlayProfile profile = GetProfile(CookingMiniGameType.Slicing);
            float tolerance = Mathf.Min(((RectTransform)transform).rect.width, ((RectTransform)transform).rect.height)
                              * profile.PrimaryTolerance;
            float averageDeviation = _samples > 0 ? _deviation / _samples : tolerance;
            int finishedIndex = _activeLine;
            bool successful = _maximumProgress >= 0.88f && averageDeviation <= tolerance;
            _activeLine = -1;
            ReleasePointer();
            if (knifeImage != null)
                knifeImage.gameObject.SetActive(false);

            if (successful == false)
            {
                _mistakes++;
                RegisterMistake("선을 벗어났습니다. 끝점부터 다시 그어보세요.");
                return;
            }

            _completedLines[finishedIndex] = true;
            _completedCount++;
            _precisionSum += Mathf.Clamp01(1f - averageDeviation / Mathf.Max(1f, tolerance));
            if (cutLineImages[finishedIndex] != null)
                cutLineImages[finishedIndex].color = completedColor;
            MarkProgress();

            if (_completedCount >= _completedLines.Length)
            {
                float score = Mathf.Clamp01(_precisionSum / _completedCount - _mistakes * mistakePenalty);
                Finish(CookingMiniGameType.Slicing, score, "절단선을 따라 재료를 고르게 썰었습니다.");
                return;
            }

            Host.SetStatus($"다음 절단선 · {_completedLines.Length - _completedCount}개 남음");
        }

        private int FindClosestEndpoint(Vector2 point, float tolerance, out Vector2 start, out Vector2 end)
        {
            int selected = -1;
            float closest = tolerance;
            start = end = Vector2.zero;
            for (int i = 0; i < cutLineImages.Length; i++)
            {
                if (_completedLines[i] || cutLineImages[i] == null)
                    continue;

                RectTransform line = cutLineImages[i].rectTransform;
                Vector2 center = line.anchoredPosition;
                float radians = line.localEulerAngles.z * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
                float length = Mathf.Max(line.rect.width, line.rect.height);
                Vector2 a = center - direction * length * 0.5f;
                Vector2 b = center + direction * length * 0.5f;
                float da = Vector2.Distance(point, a);
                float db = Vector2.Distance(point, b);
                if (da < closest)
                {
                    closest = da;
                    selected = i;
                    start = a;
                    end = b;
                }
                if (db < closest)
                {
                    closest = db;
                    selected = i;
                    start = b;
                    end = a;
                }
            }
            return selected;
        }

        private void UpdateDrag(Vector2 point)
        {
            Vector2 line = _lineEnd - _lineStart;
            float lengthSquared = line.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
                return;

            float progress = Vector2.Dot(point - _lineStart, line) / lengthSquared;
            _maximumProgress = Mathf.Max(_maximumProgress, progress);
            Vector2 nearest = _lineStart + line * Mathf.Clamp01(progress);
            _deviation += Vector2.Distance(point, nearest);
            _samples++;
            if (knifeImage != null)
            {
                knifeImage.gameObject.SetActive(true);
                knifeImage.rectTransform.anchoredPosition = point;
                knifeImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg - 90f);
            }
        }
    }
}
