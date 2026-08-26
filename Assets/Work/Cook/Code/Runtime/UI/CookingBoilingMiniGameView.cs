using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingBoilingMiniGameView : CookingOverlayMiniGameController,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image toolImage;
        [SerializeField] private Image plateZone;
        [SerializeField] private Image[] bubbleImages;
        [SerializeField] private Image cookTint;

        private CookingMiniGameOverlayProfile _profile;
        private float _startedTime;
        private Vector2 _dragStart;
        private Vector2 _lastPoint;
        private float _pathLength;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Boiling;
        }

        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Boiling, completed) == false)
                return false;

            _profile = GetProfile(CookingMiniGameType.Boiling);
            _startedTime = Time.unscaledTime;
            if (toolImage != null)
                toolImage.gameObject.SetActive(false);
            Host.SetInstruction("기포와 색을 보고 알맞게 익으면 재료를 접시로 끌어내세요.");
            Host.SetStatus("익힘 상태를 지켜보세요");
            RefreshVisual(0f);
            return true;
        }

        private void Update()
        {
            if (Completion == null)
                return;

            float elapsed = Time.unscaledTime - _startedTime;
            float doneness = Mathf.Clamp01(elapsed / _profile.Duration);
            RefreshVisual(doneness);
            if (elapsed >= Mathf.Max(_profile.Duration, _profile.MaximumDuration))
                Finish(CookingMiniGameType.Boiling, 0.12f, "재료를 제때 건져내지 못했습니다.");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Completion == null || TryCapturePointer(eventData) == false
                || TryGetLocalPosition(eventData, out _dragStart) == false)
            {
                ReleasePointer();
                return;
            }

            _lastPoint = _dragStart;
            _pathLength = 0f;
            Host.BeginIngredientDrag();
            Host.MoveIngredient(eventData.position, eventData.pressEventCamera);
            MoveTool(_dragStart);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false || TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;
            _pathLength += Vector2.Distance(_lastPoint, point);
            _lastPoint = point;
            Host.MoveIngredient(eventData.position, eventData.pressEventCamera);
            MoveTool(point);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;

            TryGetLocalPosition(eventData, out Vector2 end);
            ReleasePointer();
            if (toolImage != null)
                toolImage.gameObject.SetActive(false);

            bool onPlate = plateZone != null
                && RectTransformUtility.RectangleContainsScreenPoint(plateZone.rectTransform, eventData.position, eventData.pressEventCamera);
            if (onPlate == false)
            {
                Host.EndIngredientDrag(false);
                RegisterMistake("국자나 집게로 오른쪽 접시까지 옮기세요.");
                return;
            }

            Host.EndIngredientDrag(true);
            float straightDistance = Vector2.Distance(_dragStart, end);
            float extractionAccuracy = Mathf.Clamp01(straightDistance / Mathf.Max(straightDistance, _pathLength));
            float doneness = Mathf.Clamp01((Time.unscaledTime - _startedTime) / _profile.Duration);
            float score = CookingMiniGameScoring.ScoreBoiling(
                doneness, _profile.TargetMin, _profile.TargetMax, extractionAccuracy);
            Finish(CookingMiniGameType.Boiling, score, "재료를 알맞게 삶아 안정적으로 건져냈습니다.");
        }

        private void MoveTool(Vector2 point)
        {
            if (toolImage == null)
                return;
            toolImage.gameObject.SetActive(true);
            toolImage.rectTransform.anchoredPosition = point;
        }

        private void RefreshVisual(float doneness)
        {
            if (cookTint != null)
                cookTint.color = Color.Lerp(new Color(0.25f, 0.65f, 1f, 0.08f), new Color(1f, 0.72f, 0.22f, 0.42f), doneness);

            if (bubbleImages == null)
                return;
            for (int i = 0; i < bubbleImages.Length; i++)
            {
                if (bubbleImages[i] == null)
                    continue;
                float wave = 0.65f + Mathf.Sin(Time.unscaledTime * (3f + i) + i) * 0.25f;
                bubbleImages[i].color = new Color(0.8f, 0.95f, 1f, Mathf.Lerp(0.08f, 0.72f, doneness) * wave);
                bubbleImages[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.25f, doneness) * wave;
            }
        }
    }
}
