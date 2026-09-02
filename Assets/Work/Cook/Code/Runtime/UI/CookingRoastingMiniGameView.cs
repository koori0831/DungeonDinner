using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingRoastingMiniGameView : CookingOverlayMiniGameController,
        IPointerClickHandler
    {
        [SerializeField] private Image heatTint;
        [SerializeField] private Image flipIndicator;

        private CookingMiniGameType _type;
        private CookingMiniGameOverlayProfile _profile;
        private float _startedTime;
        private float _sideAExposure;
        private float _sideBExposure;
        private float _flipProgress;
        private bool _flipped;

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
            if (flipIndicator != null)
            {
                flipIndicator.gameObject.SetActive(true);
                flipIndicator.rectTransform.localRotation = Quaternion.identity;
                flipIndicator.rectTransform.localScale = Vector3.one;
            }
            Host.SetInstruction("재료를 한 번 눌러 뒤집고, 알맞게 익으면 다시 눌러 꺼내세요.");
            Host.SetStatus(_type == CookingMiniGameType.Burning ? "진한 그을음이 오를 때 꺼내세요" : "색과 연기를 살펴보세요");
            ConfigureHud("한 번 클릭해 뒤집기 · 적정 구간에서 다시 클릭", false, true, true);
            SetTargetState(0f, _profile.TargetMin, _profile.TargetMax, "덜 익음");
            float maximumDuration = Mathf.Max(_profile.Duration, _profile.MaximumDuration);
            SetTimer(maximumDuration, maximumDuration);
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

            float maximumDuration = Mathf.Max(_profile.Duration, _profile.MaximumDuration);
            SetTargetState(doneness, _profile.TargetMin, _profile.TargetMax, GetDonenessLabel(doneness));
            SetTimer(Mathf.Max(0f, maximumDuration - elapsed), maximumDuration);

            if (elapsed >= maximumDuration)
            {
                Finish(_type, 0.15f, "재료를 너무 오래 익혀 상태를 놓쳤습니다.");
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Completion == null)
                return;

            if (Host.IsIngredientHit(eventData) == false)
            {
                RegisterMistake("재료를 눌러주세요.");
                return;
            }

            if (_flipped == false)
            {
                _flipped = true;
                _flipProgress = Mathf.Clamp01((Time.unscaledTime - _startedTime) / _profile.Duration);
                if (flipIndicator != null)
                    flipIndicator.rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Host.PlayIngredientFlipFeedback();
                MarkProgress();
                Host.SetStatus("뒤집기 완료 · 적정 순간에 재료를 클릭하세요");
                SetGesture("적정 구간에서 재료를 다시 클릭");
                return;
            }

            Host.PlayIngredientClickFeedback();
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

            if (flipIndicator != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.06f;
                flipIndicator.rectTransform.localScale = Vector3.one * pulse;
            }
        }

        private string GetDonenessLabel(float doneness)
        {
            if (doneness < _profile.TargetMin)
                return _flipped ? "뒤집기 완료 · 아직 덜 익음" : "덜 익음 · 한 번 탭해 뒤집기";
            if (doneness <= _profile.TargetMax)
                return "적정 · 지금 재료를 클릭하세요";
            return _type == CookingMiniGameType.Burning ? "진한 그을음 · 지금 재료를 클릭하세요" : "과열 위험 · 바로 재료를 클릭하세요";
        }
    }
}
