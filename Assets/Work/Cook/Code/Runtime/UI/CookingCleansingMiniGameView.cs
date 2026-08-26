using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingCleansingMiniGameView : CookingOverlayMiniGameController,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image brushImage;
        [SerializeField] private Image[] stainImages;
        [SerializeField] private Color stainColor = new Color(0.28f, 0.15f, 0.05f, 0.78f);

        private float[] _stainAmounts;
        private int _remaining;
        private int _usefulSamples;
        private int _wastedSamples;
        private Vector2 _lastPoint;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Cleansing;
}
        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Cleansing, completed) == false
                || stainImages == null
                || stainImages.Length == 0)
            {
                return false;
            }

            _stainAmounts = new float[stainImages.Length];
            _remaining = stainImages.Length;
            _usefulSamples = 0;
            _wastedSamples = 0;
            for (int i = 0; i < stainImages.Length; i++)
            {
                _stainAmounts[i] = 1f;
                if (stainImages[i] != null)
                {
                    stainImages[i].gameObject.SetActive(true);
                    stainImages[i].color = stainColor;
                }
            }

            if (brushImage != null)
                brushImage.gameObject.SetActive(false);
            Host.SetInstruction("얼룩 위를 브러시로 문질러 씻어내세요.");
            RefreshStatus();
            return true;
        }

        public override void CancelMiniGame()
        {
            base.CancelMiniGame();
            if (brushImage != null)
                brushImage.gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Completion == null || TryCapturePointer(eventData) == false)
                return;
            if (TryGetLocalPosition(eventData, out _lastPoint) == false)
            {
                ReleasePointer();
                return;
            }
            MoveBrush(_lastPoint);
            Scrub(_lastPoint, 0.12f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false || TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;

            float distance = Vector2.Distance(_lastPoint, point);
            _lastPoint = point;
            MoveBrush(point);
            Scrub(point, Mathf.Clamp(distance / 90f, 0.05f, 0.22f));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;
            ReleasePointer();
            if (brushImage != null)
                brushImage.gameObject.SetActive(false);
        }

        private void Scrub(Vector2 point, float amount)
        {
            bool useful = false;
            for (int i = 0; i < stainImages.Length; i++)
            {
                Image stain = stainImages[i];
                if (stain == null || _stainAmounts[i] <= 0f)
                    continue;

                float radius = Mathf.Max(stain.rectTransform.rect.width, stain.rectTransform.rect.height) * 0.7f;
                if (Vector2.Distance(point, stain.rectTransform.anchoredPosition) > radius)
                    continue;

                useful = true;
                _stainAmounts[i] = Mathf.Max(0f, _stainAmounts[i] - amount);
                Color color = stainColor;
                color.a *= _stainAmounts[i];
                stain.color = color;
                if (_stainAmounts[i] <= 0f)
                {
                    stain.gameObject.SetActive(false);
                    _remaining--;
                    MarkProgress();
                    RefreshStatus();
                }
            }

            if (useful)
                _usefulSamples++;
            else
                _wastedSamples++;

            if (_remaining > 0)
                return;

            float efficiency = (float)_usefulSamples / Mathf.Max(1, _usefulSamples + _wastedSamples);
            float score = Mathf.Clamp01(0.35f + efficiency * 0.65f);
            Finish(CookingMiniGameType.Cleansing, score, "얼룩을 제거해 재료를 깨끗하게 씻었습니다.");
        }

        private void MoveBrush(Vector2 point)
        {
            if (brushImage == null)
                return;
            brushImage.gameObject.SetActive(true);
            brushImage.rectTransform.anchoredPosition = point;
        }

        private void RefreshStatus()
        {
            Host.SetStatus(_remaining > 0 ? $"남은 얼룩 {_remaining}개" : "깨끗해졌습니다");
        }
    }
}
