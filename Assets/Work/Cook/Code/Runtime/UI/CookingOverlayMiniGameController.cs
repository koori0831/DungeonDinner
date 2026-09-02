using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    internal interface ICookingOverlayMiniGameController
    {
        Component Component { get; }
        void Initialize(CookingMiniGameOverlayHost host, CookingMiniGameOverlaySettingsSO settings);
        bool CanPlay(CookingMiniGameType miniGameType);
        bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed);
        void CancelMiniGame();
    }

    public abstract class CookingOverlayMiniGameController : MonoBehaviour, ICookingOverlayMiniGameController
    {
        protected CookingMiniGameOverlayHost Host { get; private set; }
        protected CookingMiniGameOverlaySettingsSO Settings { get; private set; }
        protected Action<CookingMiniGameResult> Completion { get; private set; }
        protected int ActivePointerId { get; set; } = int.MinValue;

        public Component Component => this;

        public virtual void Initialize(CookingMiniGameOverlayHost host, CookingMiniGameOverlaySettingsSO settings)
        {
            Host = host;
            Settings = settings;
        }

        public abstract bool CanPlay(CookingMiniGameType miniGameType);

        public abstract bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed);

        public virtual void CancelMiniGame()
        {
            Completion = null;
            ActivePointerId = int.MinValue;
        }

        protected bool Begin(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            CookingMiniGameType expectedType,
            Action<CookingMiniGameResult> completed)
        {
            if (ingredient == null
                || option == null
                || option.MiniGameType != expectedType
                || completed == null
                || Host == null)
            {
                return false;
            }

            Completion = completed;
            ActivePointerId = int.MinValue;
            return true;
        }

        protected CookingMiniGameOverlayProfile GetProfile(CookingMiniGameType type)
        {
            return Settings != null
                ? Settings.GetProfile(type)
                : CookingMiniGameOverlayProfile.CreateDefault(type);
        }

        protected bool TryGetLocalPosition(PointerEventData eventData, out Vector2 localPosition)
        {
            localPosition = Vector2.zero;
            RectTransform rect = transform as RectTransform;
            return rect != null
                   && eventData != null
                   && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                       rect,
                       eventData.position,
                       eventData.pressEventCamera,
                       out localPosition);
        }

        protected bool TryCapturePointer(PointerEventData eventData)
        {
            if (eventData == null)
                return false;

            int activePointerId = ActivePointerId;
            bool captured = CookingMiniGamePointerRules.TryCapture(ref activePointerId, eventData.pointerId);
            ActivePointerId = activePointerId;
            return captured;
        }

        protected bool IsActivePointer(PointerEventData eventData)
        {
            return eventData != null && CookingMiniGamePointerRules.IsActive(ActivePointerId, eventData.pointerId);
        }

        protected void ReleasePointer()
        {
            ActivePointerId = int.MinValue;
        }

        protected void MarkProgress()
        {
            Host?.MarkProgress();
            Host?.PlayActionFeedback();
        }

        protected void ConfigureHud(string gestureText, bool showProgress, bool showTarget, bool showTimer)
        {
            Host?.ConfigureActionHud(gestureText, showProgress, showTarget, showTimer);
        }

        protected void SetGesture(string text)
        {
            Host?.SetGesture(text);
        }

        protected void SetProgress(float normalizedValue, string label)
        {
            Host?.SetProgress(normalizedValue, label);
        }

        protected void SetTargetState(float normalizedValue, float targetMin, float targetMax, string label)
        {
            Host?.SetTargetState(normalizedValue, targetMin, targetMax, label);
        }

        protected void SetTimer(float remaining, float duration)
        {
            Host?.SetTimer(remaining, duration);
        }

        protected void RegisterMistake(string instruction = null)
        {
            Host?.ShowMistake(instruction);
        }

        protected void Finish(CookingMiniGameType type, float score, string feedbackText)
        {
            Action<CookingMiniGameResult> completed = Completion;
            if (completed == null)
                return;

            Completion = null;
            ActivePointerId = int.MinValue;
            CookingMiniGameGrade grade = CookingMiniGameUtility.ResolveGrade(score);
            completed.Invoke(CookingMiniGameUtility.CreateResult(type, grade, score, feedbackText));
        }
    }

    public static class CookingMiniGamePointerRules
    {
        public const int NoPointer = int.MinValue;

        public static bool TryCapture(ref int activePointerId, int requestedPointerId)
        {
            if (activePointerId != NoPointer)
                return false;

            activePointerId = requestedPointerId;
            return true;
        }

        public static bool IsActive(int activePointerId, int requestedPointerId)
        {
            return activePointerId != NoPointer && activePointerId == requestedPointerId;
        }
    }
}
