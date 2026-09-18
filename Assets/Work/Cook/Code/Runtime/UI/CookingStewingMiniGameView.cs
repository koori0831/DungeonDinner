using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingStewingMiniGameView : CookingOverlayMiniGameController,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image actionIndicator;
        [SerializeField] private Image wasteZone;

        private int _step;
        private Vector2 _dragStart;
        private Vector2 _lastDirection;
        private float _stirAngle;
        private float _accuracySum;
        private float _startedTime;
        private bool _stepAdvancedDuringDrag;
        private CookingMiniGameOverlayProfile _profile;

        public override void Initialize(CookingMiniGameOverlayHost host, CookingMiniGameOverlaySettingsSO settings)
        {
            base.Initialize(host, settings);
            ApplySprite(actionIndicator, settings != null ? settings.FoamDiscardSprite : null);
        }

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Stewing;
        }

        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Stewing, completed) == false)
                return false;

            _step = 0;
            _stirAngle = 0f;
            _accuracySum = 0f;
            _startedTime = Time.unscaledTime;
            _profile = GetProfile(CookingMiniGameType.Stewing);
            if (wasteZone != null)
                wasteZone.gameObject.SetActive(false);
            ConfigureHud(CookingGesture.DragRight, true, false, true);
            SetProgress(0f);
            SetTimer(_profile.Duration, _profile.Duration);
            RefreshStep();
            return true;
        }

        private void Update()
        {
            if (Completion == null || _profile == null)
                return;

            float elapsed = Time.unscaledTime - _startedTime;
            SetTimer(Mathf.Max(0f, _profile.Duration - elapsed), _profile.Duration);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Completion == null || TryCapturePointer(eventData) == false
                || TryGetLocalPosition(eventData, out _dragStart) == false)
            {
                ReleasePointer();
                return;
            }

            _lastDirection = _dragStart.normalized;
            _stepAdvancedDuringDrag = false;
            if (actionIndicator != null)
                actionIndicator.rectTransform.anchoredPosition = _dragStart;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false || TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;

            if (actionIndicator != null)
                actionIndicator.rectTransform.anchoredPosition = point;

            if (_step == 1)
            {
                Vector2 direction = point.normalized;
                float delta = Mathf.Abs(Vector2.SignedAngle(_lastDirection, direction));
                if (delta <= 45f)
                {
                    _stirAngle += delta;
                    MarkProgress();
                    SetProgress((1f + Mathf.Clamp01(_stirAngle / 360f)) / 3f);
                }
                _lastDirection = direction;
                if (_stirAngle >= 360f)
                {
                    _accuracySum += Mathf.Clamp01(1f - Mathf.Abs(_stirAngle - 360f) / 180f);
                    _stepAdvancedDuringDrag = true;
                    AdvanceStep();
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;
            TryGetLocalPosition(eventData, out Vector2 end);
            ReleasePointer();
            if (_stepAdvancedDuringDrag == true)
                return;

            Rect rect = ((RectTransform)transform).rect;
            Vector2 delta = end - _dragStart;
            if (_step == 0)
            {
                float target = rect.width * 0.42f;
                float score = 1f - Mathf.Abs(delta.x - target) / Mathf.Max(1f, target);
                if (delta.x > rect.width * 0.25f)
                {
                    _accuracySum += Mathf.Clamp01(score);
                    AdvanceStep();
                }
                else
                    RegisterMistake();
            }
            else if (_step == 2)
            {
                bool reachedWaste = wasteZone != null && wasteZone.gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(wasteZone.rectTransform, eventData.position, eventData.pressEventCamera);
                if (reachedWaste)
                {
                    float directionScore = Vector2.Dot(delta.normalized, new Vector2(0.78f, 0.62f));
                    _accuracySum += Mathf.Clamp01(directionScore);
                    AdvanceStep();
                }
                else
                    RegisterMistake();
            }
        }

        private void AdvanceStep()
        {
            _step++;
            MarkProgress();
            SetProgress(Mathf.Clamp01((float)_step / 3f));
            if (_step >= 3)
            {
                float elapsed = Time.unscaledTime - _startedTime;
                float speed = 1f - Mathf.InverseLerp(_profile.Duration * 0.65f, _profile.Duration * 1.2f, elapsed);
                float accuracy = Mathf.Clamp01(_accuracySum / 3f);
                Finish(CookingMiniGameType.Stewing, accuracy * 0.65f + speed * 0.35f,
                    "불과 국물 상태를 순서대로 조절했습니다.");
                return;
            }
            RefreshStep();
        }

        private void RefreshStep()
        {
            if (_step == 0)
            {
                SetGesture(CookingGesture.DragRight);
            }
            else if (_step == 1)
            {
                SetGesture(CookingGesture.Stir);
            }
            else
            {
                SetGesture(CookingGesture.Discard);
            }

            if (wasteZone != null)
                wasteZone.gameObject.SetActive(_step == 2);


        }
    }
}
