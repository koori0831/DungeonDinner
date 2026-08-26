using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingGrindingMiniGameView : CookingOverlayMiniGameController,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image pestleImage;
        [SerializeField] private Image[] particleImages;
        [SerializeField] private float innerRadiusRatio = 0.18f;
        [SerializeField] private float outerRadiusRatio = 0.48f;

        private Vector2 _lastDirection;
        private float _accumulatedAngle;
        private float _continuitySum;
        private int _continuitySamples;
        private int _insideSamples;
        private int _totalSamples;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Grinding;
}
        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Grinding, completed) == false)
                return false;

            _accumulatedAngle = 0f;
            _continuitySum = 0f;
            _continuitySamples = 0;
            _insideSamples = 0;
            _totalSamples = 0;
            if (pestleImage != null)
                pestleImage.gameObject.SetActive(false);
            SetParticleScale(1f);
            Host.SetInstruction("절구 안에서 막자를 원형으로 네 바퀴 돌리세요.");
            Host.SetStatus("원 가장자리를 따라 돌리기");
            return true;
        }

        public override void CancelMiniGame()
        {
            base.CancelMiniGame();
            if (pestleImage != null)
                pestleImage.gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Completion == null || TryCapturePointer(eventData) == false
                || TryGetLocalPosition(eventData, out Vector2 point) == false)
            {
                ReleasePointer();
                return;
            }

            _lastDirection = point.normalized;
            MovePestle(point);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false || TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;

            Rect rect = ((RectTransform)transform).rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float normalizedRadius = point.magnitude / Mathf.Max(1f, radius);
            bool insideRing = normalizedRadius >= innerRadiusRatio && normalizedRadius <= outerRadiusRatio;
            _totalSamples++;
            if (insideRing)
                _insideSamples++;

            Vector2 direction = point.normalized;
            float delta = Vector2.SignedAngle(_lastDirection, direction);
            if (Mathf.Abs(delta) <= 45f && insideRing)
            {
                _accumulatedAngle += Mathf.Abs(delta);
                _continuitySum += 1f - Mathf.Abs(Mathf.Abs(delta) - 10f) / 35f;
                _continuitySamples++;
                MarkProgress();
            }
            else if (Mathf.Abs(delta) > 60f)
            {
                RegisterMistake();
            }

            _lastDirection = direction;
            MovePestle(point);
            float rotations = _accumulatedAngle / 360f;
            SetParticleScale(Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(rotations / 4f)));
            Host.SetStatus($"회전 {Mathf.Min(4, Mathf.FloorToInt(rotations) + 1)}/4");
            if (rotations >= 4f)
            {
                float rhythm = _continuitySamples > 0 ? Mathf.Clamp01(_continuitySum / _continuitySamples) : 0f;
                float inside = (float)_insideSamples / Mathf.Max(1, _totalSamples);
                Finish(CookingMiniGameType.Grinding, rhythm * 0.6f + inside * 0.4f,
                    "막자를 일정하게 돌려 재료를 곱게 빻았습니다.");
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;
            ReleasePointer();
            if (pestleImage != null)
                pestleImage.gameObject.SetActive(false);
        }

        private void MovePestle(Vector2 point)
        {
            if (pestleImage == null)
                return;
            pestleImage.gameObject.SetActive(true);
            pestleImage.rectTransform.anchoredPosition = point;
        }

        private void SetParticleScale(float scale)
        {
            if (particleImages == null)
                return;
            for (int i = 0; i < particleImages.Length; i++)
            {
                if (particleImages[i] != null)
                    particleImages[i].rectTransform.localScale = Vector3.one * scale;
            }
        }
    }
}
