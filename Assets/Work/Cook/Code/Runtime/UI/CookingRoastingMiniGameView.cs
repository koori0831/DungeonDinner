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

        public override void Initialize(CookingMiniGameOverlayHost host, CookingMiniGameOverlaySettingsSO settings)
        {
            base.Initialize(host, settings);
            ApplySprite(flipIndicator, settings != null ? settings.FlipSprite : null);
        }

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


            ConfigureHud(CookingGesture.Flip, false, true, true);
            SetTargetState(0f, _profile.TargetMin, _profile.TargetMax);
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
            SetTargetState(doneness, _profile.TargetMin, _profile.TargetMax);
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
                RegisterMistake();
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

                SetGesture(CookingGesture.Tap);
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


    }
}
