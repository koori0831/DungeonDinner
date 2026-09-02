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
        [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color pendingColor = new Color(1f, 0.86f, 0.35f, 1f);
        [SerializeField] private Color completedColor = new Color(0.38f, 0.9f, 0.45f, 0.82f);
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
            transform.SetAsLastSibling();
            RefreshLineVisuals();

            Host.SetInstruction("노란 절단선을 따라 끝까지 드래그하세요.");
            Host.SetStatus($"활성 절단선 · {cutLineImages.Length}개 남음");
            ConfigureHud("빛나는 칼 위치에서 반대쪽 끝까지 드래그", true, false, false);
            SetProgress(0f, $"절단 0/{cutLineImages.Length}");
            return true;
        }

        public override void CancelMiniGame()
        {
            base.CancelMiniGame();
            _activeLine = -1;
            if (knifeImage != null)
            {
                knifeImage.gameObject.SetActive(false);
                knifeImage.rectTransform.localScale = Vector3.one;
            }
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
                RegisterMistake("빛나는 칼 위치에서 드래그를 시작하세요.");
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
                SetProgress((float)_completedCount / _completedLines.Length,
                    $"절단 {_completedCount}/{_completedLines.Length}");
                RegisterMistake("선을 벗어났습니다. 끝점부터 다시 그어보세요.");
                ShowNextStartGuide();
                return;
            }

            _completedLines[finishedIndex] = true;
            _completedCount++;
            _precisionSum += Mathf.Clamp01(1f - averageDeviation / Mathf.Max(1f, tolerance));
            RefreshLineVisuals();
            MarkProgress();
            SetProgress((float)_completedCount / _completedLines.Length,
                $"절단 {_completedCount}/{_completedLines.Length}");

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
            int requiredLine = GetNextIncompleteLine();
            for (int i = 0; i < cutLineImages.Length; i++)
            {
                if (i != requiredLine || cutLineImages[i] == null)
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

        private int GetNextIncompleteLine()
        {
            if (_completedLines == null)
                return -1;

            for (int i = 0; i < _completedLines.Length; i++)
            {
                if (_completedLines[i] == false)
                    return i;
            }
            return -1;
        }

        private void RefreshLineVisuals()
        {
            if (cutLineImages == null)
                return;

            int active = GetNextIncompleteLine();
            for (int i = 0; i < cutLineImages.Length; i++)
            {
                if (cutLineImages[i] == null)
                    continue;

                cutLineImages[i].color = _completedLines != null && _completedLines[i]
                    ? completedColor
                    : i == active ? pendingColor : inactiveColor;
            }

            ShowNextStartGuide();
        }

        private void Update()
        {
            if (Completion == null || knifeImage == null || knifeImage.gameObject.activeSelf == false || _activeLine >= 0)
                return;

            float pulse = 1f + (Mathf.Sin(Time.unscaledTime * 7f) * 0.5f + 0.5f) * 0.12f;
            knifeImage.rectTransform.localScale = Vector3.one * pulse;
        }

        private void OnDisable()
        {
            if (knifeImage == null)
                return;

            knifeImage.gameObject.SetActive(false);
            knifeImage.rectTransform.localScale = Vector3.one;
        }

        private void ShowNextStartGuide()
        {
            if (knifeImage == null)
                return;

            int requiredLine = GetNextIncompleteLine();
            if (requiredLine < 0 || cutLineImages == null || requiredLine >= cutLineImages.Length
                || cutLineImages[requiredLine] == null)
            {
                knifeImage.gameObject.SetActive(false);
                knifeImage.rectTransform.localScale = Vector3.one;
                return;
            }

            RectTransform line = cutLineImages[requiredLine].rectTransform;
            Vector2 center = line.anchoredPosition;
            float radians = line.localEulerAngles.z * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
            float length = Mathf.Max(line.rect.width, line.rect.height);
            Vector2 start = center - direction * length * 0.5f;

            knifeImage.gameObject.SetActive(true);
            knifeImage.rectTransform.anchoredPosition = start;
            knifeImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            knifeImage.rectTransform.localScale = Vector3.one;
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
            if (_completedLines != null && _completedLines.Length > 0)
            {
                float totalProgress = (_completedCount + Mathf.Clamp01(_maximumProgress)) / _completedLines.Length;
                SetProgress(totalProgress, $"절단 {_completedCount}/{_completedLines.Length}");
            }
            if (knifeImage != null)
            {
                knifeImage.gameObject.SetActive(true);
                knifeImage.rectTransform.anchoredPosition = point;
                knifeImage.rectTransform.localScale = Vector3.one;
                knifeImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg - 90f);
            }
        }
    }
}
