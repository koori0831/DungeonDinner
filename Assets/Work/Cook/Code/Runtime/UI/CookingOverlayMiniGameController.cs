using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.UtillUI.Code;

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
        [SerializeField] private Image[] dragVisuals = Array.Empty<Image>();
        [SerializeField] private GameObject[] surfaceObjects = Array.Empty<GameObject>();
        private Canvas[] _dragCanvases;
        private Vector2[] _dragHomes;
        private Quaternion[] _dragRotations;
        protected CookingMiniGameOverlayHost Host { get; private set; }
        protected CookingMiniGameOverlaySettingsSO Settings { get; private set; }
        protected Action<CookingMiniGameResult> Completion { get; private set; }
        protected int ActivePointerId { get; set; } = int.MinValue;

        public Component Component => this;

        public virtual void Initialize(CookingMiniGameOverlayHost host, CookingMiniGameOverlaySettingsSO settings)
        {
            Host = host;
            Settings = settings;
            CacheDragVisuals();
            SetSurfaceVisible(false);
        }

        public abstract bool CanPlay(CookingMiniGameType miniGameType);

        public abstract bool StartMiniGame(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            Action<CookingMiniGameResult> completed);

        public virtual void CancelMiniGame()
        {
            Completion = null;
            CancelPointer();
            SetSurfaceVisible(false);
        }

        protected static void ApplySprite(Image image, Sprite sprite, bool preserveColor = false)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.preserveAspect = true;
            if (preserveColor == false)
                image.color = Color.white;
        }

        protected Image FindChildImage(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
                return null;

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.name == childName)
                    return image;
            }

            return null;
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

            CacheDragVisuals();
            SetSurfaceVisible(true);
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
            if (eventData == null || GameUiInput.IsBlocked)
                return false;

            int activePointerId = ActivePointerId;
            bool captured = CookingMiniGamePointerRules.TryCapture(ref activePointerId, eventData.pointerId);
            ActivePointerId = activePointerId;
            if (captured) SetDragSorting(true);
            return captured;
        }

        protected bool IsActivePointer(PointerEventData eventData)
        {
            return !GameUiInput.IsBlocked && eventData != null && CookingMiniGamePointerRules.IsActive(ActivePointerId, eventData.pointerId);
        }

        protected void ReleasePointer()
        {
            ActivePointerId = int.MinValue;
            SetDragSorting(false);
            if (_dragHomes != null)
                for (int i = 0; i < dragVisuals.Length; i++)
                    if (dragVisuals[i] != null)
                    {
                        dragVisuals[i].rectTransform.anchoredPosition = _dragHomes[i];
                        dragVisuals[i].rectTransform.localRotation = _dragRotations[i];
                    }
        }

        protected void MarkProgress()
        {
            Host?.MarkProgress();
            Host?.PlayActionFeedback();
        }

        protected void ConfigureHud(CookingGesture action, bool showProgress, bool showTarget, bool showTimer)
        {
            Host?.ConfigureActionHud(action, showProgress, showTarget, showTimer);
        }

        protected void SetGesture(CookingGesture action)
        {
            Host?.SetGesture(action);
        }

        protected void SetProgress(float normalizedValue, int completed = -1, int total = 0)
        {
            Host?.SetProgress(normalizedValue, completed, total);
        }

        protected void SetTargetState(float normalizedValue, float targetMin, float targetMax)
        {
            Host?.SetTargetState(normalizedValue, targetMin, targetMax);
        }

        protected void SetTimer(float remaining, float duration)
        {
            Host?.SetTimer(remaining, duration);
        }

        protected void RegisterMistake(CookingFeedbackState state = CookingFeedbackState.Mistake)
        {
            Host?.ShowMistake(state);
        }

        private void CacheDragVisuals()
        {
            if (_dragCanvases != null) return;
            _dragCanvases = new Canvas[dragVisuals.Length];
            _dragHomes = new Vector2[dragVisuals.Length];
            _dragRotations = new Quaternion[dragVisuals.Length];
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            for (int i = 0; i < dragVisuals.Length; i++)
            {
                var image = dragVisuals[i];
                if (image == null) continue;
                image.raycastTarget = false;
                image.maskable = false;
                _dragHomes[i] = image.rectTransform.anchoredPosition;
                _dragRotations[i] = image.rectTransform.localRotation;
                var canvas = image.GetComponent<Canvas>();
                if (canvas == null) canvas = image.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = (parentCanvas != null ? parentCanvas.rootCanvas.sortingOrder : 0) + 100;
                canvas.enabled = false;
                _dragCanvases[i] = canvas;
            }
        }

        private void SetDragSorting(bool value)
        {
            if (_dragCanvases == null) return;
            foreach (var canvas in _dragCanvases)
                if (canvas != null)
                {
                    canvas.enabled = value;
                    if (value) canvas.overrideSorting = true;
                }
        }

        private void LateUpdate() { if (ActivePointerId != int.MinValue) SetDragSorting(true); }

        private void SetSurfaceVisible(bool value)
        {
            foreach (var surface in surfaceObjects) if (surface != null) surface.SetActive(value);
        }

        protected virtual void OnPointerCancelled() { }

        private void CancelPointer()
        {
            ReleasePointer();
            if (_dragHomes != null)
                for (int i = 0; i < dragVisuals.Length; i++)
                    if (dragVisuals[i] != null)
                    {
                        dragVisuals[i].rectTransform.anchoredPosition = _dragHomes[i];
                        dragVisuals[i].rectTransform.localRotation = _dragRotations[i];
                    }
            OnPointerCancelled();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelPointer(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelPointer(); }
        protected virtual void OnDisable() { CancelPointer(); SetSurfaceVisible(false); }

        protected void Finish(CookingMiniGameType type, float score, string feedbackText)
        {
            Action<CookingMiniGameResult> completed = Completion;
            if (completed == null)
                return;

            Completion = null;
            ReleasePointer();
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
