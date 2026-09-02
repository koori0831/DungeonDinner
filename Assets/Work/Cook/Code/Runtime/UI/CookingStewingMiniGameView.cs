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
            ConfigureHud("노브를 오른쪽으로 드래그 →", true, false, true);
            SetProgress(0f, "단계 1/3");
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
                    SetProgress((1f + Mathf.Clamp01(_stirAngle / 360f)) / 3f,
                        $"단계 2/3 · 젓기 {Mathf.RoundToInt(Mathf.Clamp01(_stirAngle / 360f) * 100f)}%");
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
                    RegisterMistake("노브를 오른쪽 적정 위치까지 드래그하세요.");
            }
            else if (_step == 2)
            {
                bool reachedWaste = wasteZone != null
                    ? RectTransformUtility.RectangleContainsScreenPoint(wasteZone.rectTransform, eventData.position, eventData.pressEventCamera)
                    : delta.x > rect.width * 0.25f && delta.y > rect.height * 0.15f;
                if (reachedWaste)
                {
                    float directionScore = Vector2.Dot(delta.normalized, new Vector2(0.78f, 0.62f));
                    _accuracySum += Mathf.Clamp01(directionScore);
                    AdvanceStep();
                }
                else
                    RegisterMistake("거품을 오른쪽 위 폐기 영역으로 밀어내세요.");
            }
        }

        private void AdvanceStep()
        {
            _step++;
            MarkProgress();
            SetProgress(Mathf.Clamp01((float)_step / 3f), $"단계 {Mathf.Min(_step + 1, 3)}/3");
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
            string instruction;
            if (_step == 0)
            {
                instruction = "불 조절 노브를 오른쪽 적정 위치로 드래그하세요.";
                SetGesture("노브를 오른쪽으로 드래그 →");
            }
            else if (_step == 1)
            {
                instruction = "국물 중앙을 한 바퀴 원형으로 저으세요.";
                SetGesture("국물 중앙을 원형으로 한 바퀴 돌리기");
            }
            else
            {
                instruction = "거품을 오른쪽 위 폐기 영역으로 밀어내세요.";
                SetGesture("거품을 오른쪽 위로 밀기 ↗");
            }

            if (wasteZone != null)
                wasteZone.gameObject.SetActive(_step == 2);
            Host.SetInstruction(instruction);
            Host.SetStatus($"단계 {_step + 1}/3");
        }
    }
}
