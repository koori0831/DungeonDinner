using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingDilutingMiniGameView : CookingOverlayMiniGameController,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image pitcherImage;
        [SerializeField] private Image pourStream;
        [SerializeField] private Image mixtureTint;

        private CookingMiniGameOverlayProfile _profile;
        private float _waterAmount;
        private float _spilledAmount;
        private bool _pouring;
        private bool _spilling;
        private bool _spillWarningShown;
        private Vector2 _pitcherHome;
        private float _startedTime;

        public override bool CanPlay(CookingMiniGameType miniGameType)
        {
            return miniGameType == CookingMiniGameType.Diluting;
        }

        public override bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed)
        {
            if (Begin(ingredient, option, CookingMiniGameType.Diluting, completed) == false)
                return false;

            _profile = GetProfile(CookingMiniGameType.Diluting);
            _waterAmount = 0f;
            _spilledAmount = 0f;
            _pouring = false;
            _spilling = false;
            _spillWarningShown = false;
            _startedTime = Time.unscaledTime;
            if (pitcherImage != null)
            {
                _pitcherHome = pitcherImage.rectTransform.anchoredPosition;
                pitcherImage.rectTransform.anchoredPosition = _pitcherHome;
            }
            if (pourStream != null)
                pourStream.gameObject.SetActive(false);
            RefreshVisual();
            Host.SetInstruction("물통을 용기 위로 드래그해 붓고, 원하는 농도에서 손을 떼세요.");
            Host.SetStatus("색이 부드러워질 때 멈추세요");
            ConfigureHud("물통을 용기 위로 드래그 · 적정 구간에서 놓기", false, true, true);
            RefreshHud();
            SetTimer(_profile.MaximumDuration, _profile.MaximumDuration);
            return true;
        }

        private void Update()
        {
            if (Completion == null)
                return;

            float elapsed = Time.unscaledTime - _startedTime;
            if (_profile.MaximumDuration > 0f)
            {
                SetTimer(Mathf.Max(0f, _profile.MaximumDuration - elapsed), _profile.MaximumDuration);
                if (elapsed >= _profile.MaximumDuration)
                {
                    CompletePour("시간 안에 적절한 농도로 맞추지 못했습니다.");
                    return;
                }
            }

            if (_pouring == false && _spilling == false)
                return;

            float flow = Time.unscaledDeltaTime / Mathf.Max(1f, _profile.Duration);
            if (_pouring)
                _waterAmount += flow;
            else
                _spilledAmount += flow;
            MarkProgress();
            RefreshVisual();
            RefreshHud();

            if (_waterAmount > _profile.TargetMax + _profile.PrimaryTolerance)
                CompletePour("물을 너무 많이 부어 농도가 지나치게 묽어졌습니다.");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Completion == null || TryCapturePointer(eventData) == false)
                return;
            UpdatePitcher(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;
            UpdatePitcher(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsActivePointer(eventData) == false)
                return;

            UpdatePitcher(eventData);
            ReleasePointer();
            _pouring = false;
            _spilling = false;
            if (pourStream != null)
                pourStream.gameObject.SetActive(false);

            if (_waterAmount >= _profile.TargetMin)
            {
                float total = Mathf.Max(0.0001f, _waterAmount + _spilledAmount);
                float spillRatio = _spilledAmount / total;
                string feedback = spillRatio > _profile.SecondaryTolerance
                    ? "농도는 맞췄지만 물을 용기 밖으로 흘렸습니다."
                    : _waterAmount > _profile.TargetMax
                        ? "물을 조금 많이 부어 목표보다 묽어졌습니다."
                        : "물의 양을 조절해 적절한 농도로 만들었습니다.";
                CompletePour(feedback);
                return;
            }

            if (pitcherImage != null)
                pitcherImage.rectTransform.anchoredPosition = _pitcherHome;
            Host.SetStatus("아직 진합니다 · 물을 더 부으세요");
            RefreshHud();
        }

        private void UpdatePitcher(PointerEventData eventData)
        {
            if (TryGetLocalPosition(eventData, out Vector2 point) == false)
                return;

            if (pitcherImage != null)
                pitcherImage.rectTransform.anchoredPosition = point;
            Rect rect = ((RectTransform)transform).rect;
            bool aboveVessel = Mathf.Abs(point.x) < rect.width * 0.24f && point.y > -rect.height * 0.05f;
            bool nearVessel = Mathf.Abs(point.x) < rect.width * 0.42f && point.y > -rect.height * 0.22f;
            _pouring = aboveVessel;
            _spilling = aboveVessel == false && nearVessel;
            if (pourStream != null)
                pourStream.gameObject.SetActive(_pouring || _spilling);
            if (_spilling && _spillWarningShown == false)
            {
                _spillWarningShown = true;
                RegisterMistake("물이 용기 밖으로 흐르고 있습니다.");
            }
            else if (_spilling == false)
            {
                _spillWarningShown = false;
            }
        }

        private void CompletePour(string feedback)
        {
            float total = Mathf.Max(0.0001f, _waterAmount + _spilledAmount);
            float spillRatio = _spilledAmount / total;
            float score = CookingMiniGameScoring.ScoreDiluting(
                _waterAmount, _profile.TargetMin, _profile.TargetMax, spillRatio);
            Finish(CookingMiniGameType.Diluting, score, feedback);
        }

        private void RefreshVisual()
        {
            if (mixtureTint == null)
                return;
            float amount = Mathf.Clamp01(_waterAmount);
            mixtureTint.color = Color.Lerp(
                new Color(0.55f, 0.24f, 0.08f, 0.62f),
                new Color(0.75f, 0.88f, 1f, 0.28f),
                amount);
        }

        private void RefreshHud()
        {
            string label;
            if (_spilling)
                label = "흘리는 중 · 용기 중앙으로 옮기세요";
            else if (_waterAmount < _profile.TargetMin)
                label = $"아직 진함 · {Mathf.RoundToInt(_waterAmount * 100f)}%";
            else if (_waterAmount <= _profile.TargetMax)
                label = "적정 농도 · 지금 손을 떼세요";
            else
                label = "너무 묽어지는 중 · 바로 멈추세요";

            SetTargetState(_waterAmount, _profile.TargetMin, _profile.TargetMax, label);
        }
    }
}
