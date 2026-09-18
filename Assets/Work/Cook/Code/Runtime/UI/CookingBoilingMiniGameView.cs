using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingBoilingMiniGameView : CookingOverlayMiniGameController,
        IPointerClickHandler
    {
        [SerializeField] private Image[] bubbleImages;
        [SerializeField] private Image cookTint;

        private CookingMiniGameOverlayProfile _profile;
        private float _startedTime;

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


            ConfigureHud(CookingGesture.Tap, false, true, true);
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
            RefreshVisual(doneness);
            float maximumDuration = Mathf.Max(_profile.Duration, _profile.MaximumDuration);
            SetTargetState(doneness, _profile.TargetMin, _profile.TargetMax);
            SetTimer(Mathf.Max(0f, maximumDuration - elapsed), maximumDuration);
            if (elapsed >= maximumDuration)
                Finish(CookingMiniGameType.Boiling, 0.12f, "재료를 제때 건져내지 못했습니다.");
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

            Host.PlayIngredientClickFeedback();
            float doneness = Mathf.Clamp01((Time.unscaledTime - _startedTime) / _profile.Duration);
            float score = CookingMiniGameScoring.ScoreBoiling(
                doneness, _profile.TargetMin, _profile.TargetMax);
            Finish(CookingMiniGameType.Boiling, score, "재료를 알맞게 삶아 안정적으로 건져냈습니다.");
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
