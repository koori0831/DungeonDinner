using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingRoastingMiniGameView : CookingOverlayMiniGameController,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image heatTint;
        [SerializeField] private Image flipIndicator;
        [SerializeField] private Image plateZone;

        private CookingMiniGameType _type;
        private CookingMiniGameOverlayProfile _profile;
        private float _startedTime;
        private float _sideAExposure;
        private float _sideBExposure;
        private float _flipProgress;
        private bool _flipped;
        private Vector2 _pointerStart;
        private Vector2 _currentPoint;
        private bool _dragging;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Roasting || miniGameType == CookingMiniGameType.Burning;
        }

        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (option == null || CanPlay(option.MiniGameType) == false
                || Begin(ingredient, option, option.MiniGameType, completed) == false)
            {
                return false;
            }

            _type = option.MiniGameType;
            _profile = GetProfile(_type);
            _startedTime = Time.unscaledTime;
            _sideAExposure = 0f;
            _sideBExposure = 0f;
            _flipProgress = 0f;
            _flipped = false;
            _dragging = false;
            if (flipIndicator != null)
                flipIndicator.gameObject.SetActive(true);
            Host.SetInstruction("재료를 한 번 눌러 뒤집고, 알맞게 익으면 가이드를 오른쪽 접시까지 드래그하세요.");
            Host.SetStatus(_type == CookingMiniGameType.Burning ? "진한 그을음이 오를 때 꺼내세요" : "색과 연기를 살펴보세요");
            RefreshVisual(0f);
            return true;
        }

        private void Update()
        {
            if (Completion == null)
                return;

            float elapsed = Time.unscaledTime - _startedTime;
            float doneness = Mathf.Clamp01(elapsed / _profile.Duration);
            if (_flipped)
                _sideBExposure += Time.unscaledDeltaTime;
            else
                _sideAExposure += Time.unscaledDeltaTime;
            RefreshVisual(doneness);

            if (elapsed >= Mathf.Max(_profile.Duration, _profile.MaximumDuration))
            {
                Finish(_type, 0.15f, "재료를 너무 오래 익혀 상태를 놓쳤습니다.");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Completion == null || TryCapturePointer(eventData) == false
                || TryGetLocalPosition(eventData, out _pointerStart) == false)
            {
                ReleasePointer();
                return;
            }

            _currentPoint = _pointerStart;
            _dragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false || TryGetLocalPosition(eventData, out _currentPoint) == false)
                return;

            if (Vector2.Distance(_pointerStart, _currentPoint) > 18f)
            {
                _dragging = true;
            }
            if (flipIndicator != null && _dragging)
                flipIndicator.rectTransform.anchoredPosition = _currentPoint;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;
            TryGetLocalPosition(eventData, out _currentPoint);
            ReleasePointer();

            if (_dragging == false)
            {
                if (_flipped)
                {
                    RegisterMistake("한 번만 뒤집고 익힘 상태를 기다리세요.");
                    return;
                }

                _flipped = true;
                _flipProgress = Mathf.Clamp01((Time.unscaledTime - _startedTime) / _profile.Duration);
                if (flipIndicator != null)
                    flipIndicator.rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                MarkProgress();
                Host.SetStatus("뒤집기 완료 · 접시로 꺼낼 순간을 고르세요");
                return;
            }

            bool onPlate = plateZone != null
                ? RectTransformUtility.RectangleContainsScreenPoint(plateZone.rectTransform, eventData.position, eventData.pressEventCamera)
                : _currentPoint.x > ((RectTransform)transform).rect.width * 0.25f;
            if (onPlate == false)
            {
                RegisterMistake("오른쪽 접시 영역까지 드래그해 꺼내세요.");
                return;
            }

            CompleteAtCurrentState();
        }

        private void CompleteAtCurrentState()
        {
            float elapsed = Time.unscaledTime - _startedTime;
            float doneness = Mathf.Clamp01(elapsed / _profile.Duration);
            float score = CookingMiniGameScoring.ScoreRoasting(
                doneness,
                _profile.TargetMin,
                _profile.TargetMax,
                _flipped ? _flipProgress : 0f,
                _sideAExposure,
                _sideBExposure);
            if (_flipped == false)
                score *= 0.65f;
            string feedback = _type == CookingMiniGameType.Burning
                ? "양면을 의도한 그을림 상태로 익혔습니다."
                : "양면을 고르게 익혀 적절한 순간에 꺼냈습니다.";
            Finish(_type, score, feedback);
        }

        private void RefreshVisual(float doneness)
        {
            if (heatTint != null)
            {
                Color color = Color.Lerp(new Color(1f, 0.65f, 0.2f, 0.08f), new Color(0.18f, 0.04f, 0f, 0.72f), doneness);
                heatTint.color = color;
            }

            if (flipIndicator != null && _dragging == false)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.06f;
                flipIndicator.rectTransform.localScale = Vector3.one * pulse;
            }
        }
    }
}
