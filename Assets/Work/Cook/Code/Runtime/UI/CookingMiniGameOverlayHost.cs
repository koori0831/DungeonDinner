using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 미니게임 중에만 선택 재료를 표시하고 입력 차단, 포커스 딤, HUD와 결과 연출을 제공한다.
    /// </summary>
    public sealed class CookingMiniGameOverlayHost : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private RectTransform targetFrame;
        [SerializeField] private RectTransform ingredientAnchor;
        [SerializeField] private RectTransform controllerContainer;
        [SerializeField] private Image maskImage;
        [SerializeField] private Image[] focusDimmers;
        [SerializeField, Min(0f)] private float targetPadding = 24f;
        [SerializeField, Min(0f)] private float actionHudGap = 24f;

        [Header("HUD")]
        [SerializeField] private RectTransform hudRoot;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private Button cancelButton;

        [Header("Action HUD")]
        [SerializeField] private RectTransform actionHudRoot;
        [SerializeField] private RectTransform progressGaugeRoot;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image targetBand;
        [SerializeField] private Image targetMarker;
        [SerializeField] private TextMeshProUGUI progressField;
        [SerializeField] private TextMeshProUGUI gestureField;
        [SerializeField] private RectTransform timerGaugeRoot;
        [SerializeField] private Image timerFill;

        [Header("Mistake Toast")]
        [SerializeField] private CanvasGroup mistakeCanvasGroup;
        [SerializeField] private TextMeshProUGUI mistakeField;
        [SerializeField, Min(0.1f)] private float mistakeDisplayDuration = 1.2f;

        [Header("Result")]
        [SerializeField] private CanvasGroup resultCanvasGroup;
        [SerializeField] private TextMeshProUGUI resultField;
        [SerializeField] private TextMeshProUGUI resultScoreField;
        [SerializeField] private TextMeshProUGUI resultReasonField;
        [SerializeField] private Image resultBackground;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool useTemporaryFeedbackAudio = true;

        [Header("Synchronized Presentation")]
        [SerializeField] private CookingWorkbenchView synchronizedWorkbenchView;
        [SerializeField] private CookingPreparationHandView synchronizedHandView;
        [SerializeField] private CookingActivePreparationSlotView synchronizedActiveSlotView;
        [SerializeField] private GameObject[] preparationTextObjects;

        private CookingGamePanel _owner;
        private CookingWorkbenchView _workbenchView;
        private CookingPreparationHandView _preparationHandView;
        private CookingActivePreparationSlotView _activeSlotView;
        private IngredientSO _activeIngredient;
        private IngredientPreparationOption _activeOption;
        private CookingMiniGameOverlaySettingsSO _settings;
        private Coroutine _resultRoutine;
        private Coroutine _mistakeRoutine;
        private Coroutine _ingredientFeedbackRoutine;
        private Image _ingredientVisualImage;
        private Vector2 _targetFrameOriginalPosition;
        private Vector2 _targetFrameOriginalSize;
        private Vector3 _ingredientBaseScale;
        private Quaternion _ingredientBaseRotation;
        private float _lastProgressTime;
        private bool _targetFrameLayoutCached;
        private bool _ingredientTransformCached;
        private bool _ingredientFlipped;
        private bool _isRunning;
        private bool _hintShown;
        private int _presentationSyncFramesRemaining;
        private float _lastActionFeedbackTime = float.MinValue;
        private float _lastMistakeFeedbackTime = float.MinValue;
        private bool[] _preparationTextWasActive;
        private bool _preparationTextHidden;
        private bool _hudLayoutDirty = true;
        private RectTransform _shiftedWorkbench;
        private Vector2 _workbenchOriginalPosition;

        private const float PanelPadding = 16f;
        private const float ScreenPadding = 24f;

        private const float ActionFeedbackCooldown = 0.08f;
        private const float MistakeFeedbackCooldown = 0.18f;
        private static AudioClip _temporaryActionClip;
        private static AudioClip _temporarySuccessClip;
        private static AudioClip _temporaryMistakeClip;

        private enum TemporaryFeedbackTone
        {
            Action,
            Success,
            Mistake
        }

        public RectTransform ControllerContainer => controllerContainer;
        public RectTransform TargetFrame => targetFrame;
        public bool IsRunning => _isRunning;
        public event Action<CookingMiniGameResult> ResultShown;

        public void Initialize(
            CookingGamePanel owner,
            CookingMiniGameOverlaySettingsSO settings,
            TMP_FontAsset fontAsset)
        {
            _owner = owner;
            _settings = settings;

            if (overlayRoot == null)
                overlayRoot = transform as RectTransform;
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            ResolvePresentationPeers();

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
                cancelButton.onClick.AddListener(HandleCancelClicked);
            }

            SetFontAsset(fontAsset);
            SetResultVisible(false);
            StopMistakeRoutine();
            ResetActionHud();
            CacheTargetFrameLayout();
            CacheIngredientTransform();
            ResetIngredientTransform();
            SetIngredientVisible(false);
            ApplyDimColor();
        }

        public void SetFontAsset(TMP_FontAsset fontAsset)
        {
            _hudLayoutDirty = true;
            if (fontAsset == null)
                return;

            if (titleField != null)
                titleField.font = fontAsset;
            if (instructionField != null)
                instructionField.font = fontAsset;
            if (statusField != null)
                statusField.font = fontAsset;
            if (resultField != null)
                resultField.font = fontAsset;
            if (resultScoreField != null)
                resultScoreField.font = fontAsset;
            if (resultReasonField != null)
                resultReasonField.font = fontAsset;
            if (progressField != null)
            {
                progressField.font = fontAsset;
                progressField.fontSize = Mathf.Max(progressField.fontSize, 20f);
            }
            if (gestureField != null)
            {
                gestureField.font = fontAsset;
                gestureField.fontSize = Mathf.Max(gestureField.fontSize, 22f);
            }
            if (mistakeField != null)
            {
                mistakeField.font = fontAsset;
                mistakeField.fontSize = Mathf.Max(mistakeField.fontSize, 21f);
            }
        }

        public bool Begin(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (ingredient == null || option == null || targetFrame == null || maskImage == null)
                return false;

            StopResultRoutine();
            _activeIngredient = ingredient;
            _activeOption = option;
            _presentationSyncFramesRemaining = 2;
            ResolvePresentationPeers();
            SetPreparationTextHidden(true);
            CacheTargetFrameLayout();
            ResetIngredientTransform();
            ResetTargetFrame();
            AlignTargetFrameToIngredientAnchor();
            SynchronizePreparationPresentation();
            EnsureIngredientVisualImage();
            Sprite ingredientSprite = CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
            maskImage.sprite = ingredientSprite;
            maskImage.preserveAspect = true;
            if (_ingredientVisualImage != null)
            {
                _ingredientVisualImage.sprite = ingredientSprite;
                _ingredientVisualImage.preserveAspect = true;
            }
            SetIngredientVisible(true);

            SetText(titleField, option.DisplayName);
            SetText(instructionField, "재료 위의 가이드를 따라 조작하세요.");
            SetText(statusField, string.Empty);
            SetResultVisible(false);
            StopMistakeRoutine();
            ResetActionHud();
            _lastProgressTime = Time.unscaledTime;
            _hintShown = false;
            _isRunning = true;
            if (cancelButton != null)
                cancelButton.interactable = true;
            RefreshFocusLayout();
            return true;
        }

        public void SetInstruction(string text)
        {
            SetText(instructionField, text);
        }

        public void SetStatus(string text)
        {
            SetText(statusField, text);
        }

        public void MarkProgress()
        {
            _lastProgressTime = Time.unscaledTime;
            _hintShown = false;
        }

        public void PlayActionFeedback()
        {
            if (Time.unscaledTime - _lastActionFeedbackTime < ActionFeedbackCooldown)
                return;

            _lastActionFeedbackTime = Time.unscaledTime;
            PlayClip(_settings != null ? _settings.ActionClip : null, TemporaryFeedbackTone.Action);
        }

        public void PlayMistakeFeedback()
        {
            if (Time.unscaledTime - _lastMistakeFeedbackTime < MistakeFeedbackCooldown)
                return;

            _lastMistakeFeedbackTime = Time.unscaledTime;
            PlayClip(_settings != null ? _settings.MistakeClip : null, TemporaryFeedbackTone.Mistake);
            TryVibrate();
        }

        public bool IsIngredientHit(PointerEventData eventData)
        {
            if (_isRunning == false || eventData == null || maskImage == null || maskImage.enabled == false)
                return false;

            RectTransform rectTransform = maskImage.rectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint) == false)
            {
                return false;
            }

            Rect hitRect = rectTransform.rect;
            Sprite sprite = maskImage.sprite;
            if (maskImage.preserveAspect == true && sprite != null && sprite.rect.height > 0f)
                hitRect = CalculatePreservedAspectRect(hitRect, sprite.rect.size);
            return hitRect.Contains(localPoint);
        }

        public void PlayIngredientClickFeedback()
        {
            EnsureIngredientVisualImage();
            if (_ingredientVisualImage == null || _ingredientVisualImage.enabled == false)
                return;

            CacheIngredientTransform();
            StopIngredientFeedback(false);
            Quaternion rotation = GetIngredientStableRotation();
            _ingredientFeedbackRoutine = StartCoroutine(AnimateIngredientFeedback(rotation, rotation, 0.14f, false));
        }

        public void PlayIngredientFlipFeedback()
        {
            EnsureIngredientVisualImage();
            if (_ingredientVisualImage == null || _ingredientVisualImage.enabled == false)
                return;

            CacheIngredientTransform();
            StopIngredientFeedback(false);
            Quaternion startRotation = GetIngredientStableRotation();
            _ingredientFlipped = !_ingredientFlipped;
            _ingredientFeedbackRoutine = StartCoroutine(AnimateIngredientFeedback(
                startRotation,
                GetIngredientStableRotation(),
                0.2f,
                true));
        }

        public void ConfigureActionHud(string gestureText, bool showProgress, bool showTarget, bool showTimer)
        {
            if (actionHudRoot != null)
                actionHudRoot.gameObject.SetActive(true);
            if (progressGaugeRoot != null)
                progressGaugeRoot.gameObject.SetActive(showProgress || showTarget);
            if (progressFill != null)
                progressFill.gameObject.SetActive(showProgress || showTarget);
            if (targetBand != null)
                targetBand.gameObject.SetActive(showTarget);
            if (targetMarker != null)
                targetMarker.gameObject.SetActive(showTarget);
            if (timerGaugeRoot != null)
                timerGaugeRoot.gameObject.SetActive(showTimer);

            SetText(gestureField, gestureText);
            SetText(progressField, string.Empty);
            SetHorizontalFill(progressFill, 0f);
            SetHorizontalFill(timerFill, 1f);
        }

        public void SetGesture(string text)
        {
            SetText(gestureField, text);
        }

        public void SetProgress(float normalizedValue, string label)
        {
            SetHorizontalFill(progressFill, normalizedValue);
            if (targetBand != null)
                targetBand.gameObject.SetActive(false);
            if (targetMarker != null)
                targetMarker.gameObject.SetActive(false);
            SetText(progressField, label);
        }

        public void SetTargetState(float normalizedValue, float targetMin, float targetMax, string label)
        {
            float minimum = Mathf.Clamp01(Mathf.Min(targetMin, targetMax));
            float maximum = Mathf.Clamp01(Mathf.Max(targetMin, targetMax));
            SetHorizontalFill(progressFill, normalizedValue);
            SetHorizontalRange(targetBand, minimum, maximum);
            SetHorizontalMarker(targetMarker, normalizedValue);
            if (targetBand != null)
                targetBand.gameObject.SetActive(true);
            if (targetMarker != null)
                targetMarker.gameObject.SetActive(true);
            SetText(progressField, label);
        }

        public void SetTimer(float remaining, float duration)
        {
            float normalized = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
            SetHorizontalFill(timerFill, normalized);
        }

        public void ShowMistake(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                text = "입력 가이드를 다시 확인하세요.";

            if (_mistakeRoutine != null)
                StopCoroutine(_mistakeRoutine);
            _mistakeRoutine = StartCoroutine(ShowMistakeRoutine(text));
            PlayMistakeFeedback();
        }

        public void PlayResult(CookingMiniGameResult result, Action completed)
        {
            StopResultRoutine();
            _resultRoutine = StartCoroutine(ShowResultRoutine(result, completed));
        }

        public void EndImmediate()
        {
            StopResultRoutine();
            _isRunning = false;
            _presentationSyncFramesRemaining = 0;
            _activeIngredient = null;
            _activeOption = null;
            if (cancelButton != null)
                cancelButton.interactable = false;
            SetResultVisible(false);
            StopMistakeRoutine();
            ResetActionHud();
            ResetTargetFrame();
            SetIngredientVisible(false);
            RestoreWorkbenchPosition();
            SetPreparationTextHidden(false);
        }

        private void OnEnable()
        {
            if (_isRunning == true)
            {
                SetPreparationTextHidden(true);
                SynchronizePreparationPresentation();
                RefreshFocusLayout();
            }
        }

        private void OnDisable()
        {
            ResetIngredientTransform();
            RestoreWorkbenchPosition();
            SetPreparationTextHidden(false);
        }

        private void LateUpdate()
        {
            if (_isRunning == false)
                return;

            if (_presentationSyncFramesRemaining > 0)
            {
                _presentationSyncFramesRemaining--;
                SynchronizePreparationPresentation();
            }

            AlignTargetFrameToIngredientAnchor();
            RefreshFocusLayout();

            if (_hintShown == false && Time.unscaledTime - _lastProgressTime >= 10f)
            {
                _hintShown = true;
                ShowMistake("입력 가이드를 다시 확인하세요.");
            }
        }

        private IEnumerator ShowResultRoutine(CookingMiniGameResult result, Action completed)
        {
            _isRunning = false;
            if (cancelButton != null)
                cancelButton.interactable = false;
            StopMistakeRoutine();
            ResetActionHud();
            ResultShown?.Invoke(result);
            CookingMiniGameGrade grade = result != null ? result.Grade : CookingMiniGameGrade.Bad;
            SetText(resultField, GetGradeLabel(grade));
            SetText(resultScoreField, result != null ? $"정확도 {Mathf.RoundToInt(result.Score * 100f)}%" : "정확도 0%");
            SetText(resultReasonField, result != null ? result.FeedbackText : "조리 결과를 확인하세요.");
            Color resultTextColor = new Color(0.08f, 0.055f, 0.035f, 1f);
            if (resultField != null)
                resultField.color = resultTextColor;
            if (resultScoreField != null)
                resultScoreField.color = resultTextColor;
            if (resultReasonField != null)
                resultReasonField.color = resultTextColor;
            if (resultBackground != null)
                resultBackground.color = GetGradeColor(grade);
            SetResultVisible(true);
            AudioClip resultClip = null;
            if (_settings != null)
                resultClip = result != null && result.Grade == CookingMiniGameGrade.Bad
                    ? _settings.MistakeClip
                    : _settings.SuccessClip;
            PlayClip(resultClip, result != null && result.Grade == CookingMiniGameGrade.Bad
                ? TemporaryFeedbackTone.Mistake
                : TemporaryFeedbackTone.Success);

            RectTransform resultRoot = resultCanvasGroup != null
                ? resultCanvasGroup.transform as RectTransform
                : null;
            if (resultCanvasGroup != null)
                resultCanvasGroup.alpha = 0f;
            if (resultRoot != null)
                resultRoot.localScale = Vector3.one * 0.88f;

            const float revealDuration = 0.16f;
            float revealElapsed = 0f;
            while (revealElapsed < revealDuration)
            {
                revealElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(revealElapsed / revealDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                if (resultCanvasGroup != null)
                    resultCanvasGroup.alpha = eased;
                if (resultRoot != null)
                    resultRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, eased);
                yield return null;
            }

            float duration = _settings != null ? _settings.ResultDisplayDuration : 2f;
            if (duration > 0f)
                yield return new WaitForSecondsRealtime(duration);

            const float fadeDuration = 0.14f;
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                if (resultCanvasGroup != null)
                    resultCanvasGroup.alpha = 1f - Mathf.Clamp01(fadeElapsed / fadeDuration);
                yield return null;
            }

            SetResultVisible(false);
            ResetTargetFrame();
            SetIngredientVisible(false);
            _resultRoutine = null;
            RestoreWorkbenchPosition();
            SetPreparationTextHidden(false);
            completed?.Invoke();
        }

        private void ResolvePresentationPeers()
        {
            if (_workbenchView == null)
                _workbenchView = synchronizedWorkbenchView;
            if (_workbenchView == null && _owner != null)
                _workbenchView = _owner.GetComponentInChildren<CookingWorkbenchView>(true);
            if (_workbenchView == null)
                _workbenchView = FindFirstObjectByType<CookingWorkbenchView>(FindObjectsInactive.Include);

            if (_preparationHandView == null)
                _preparationHandView = synchronizedHandView;
            if (_preparationHandView == null && _owner != null)
                _preparationHandView = _owner.GetComponentInChildren<CookingPreparationHandView>(true);
            if (_preparationHandView == null)
                _preparationHandView = FindFirstObjectByType<CookingPreparationHandView>(FindObjectsInactive.Include);

            if (_activeSlotView == null)
                _activeSlotView = synchronizedActiveSlotView;
            if (_activeSlotView == null && _owner != null)
                _activeSlotView = _owner.GetComponentInChildren<CookingActivePreparationSlotView>(true);
            if (_activeSlotView == null)
                _activeSlotView = FindFirstObjectByType<CookingActivePreparationSlotView>(FindObjectsInactive.Include);

            if (ingredientAnchor == null && _workbenchView != null)
                ingredientAnchor = _workbenchView.IngredientAnchor;
        }

        private void SynchronizePreparationPresentation()
        {
            if (_activeIngredient == null || _activeOption == null)
                return;

            ResolvePresentationPeers();
            _workbenchView?.ShowInteractionStarted(_activeIngredient, _activeOption);
            _activeSlotView?.BindInProgress(_activeOption);
            _preparationHandView?.ShowMiniGameState();
        }

        private void SetPreparationTextHidden(bool hidden)
        {
            if (_preparationTextHidden == hidden)
                return;

            if (hidden)
            {
                if ((preparationTextObjects == null || preparationTextObjects.Length == 0) && _workbenchView != null)
                {
                    Transform workbench = _workbenchView.transform;
                    preparationTextObjects = new[]
                    {
                        workbench.Find("IngredientName")?.gameObject,
                        workbench.Find("Instruction")?.gameObject,
                        workbench.parent != null ? workbench.parent.Find("Title")?.gameObject : null
                    };
                }

                if (preparationTextObjects == null)
                    return;
                _preparationTextWasActive = new bool[preparationTextObjects.Length];
            }

            for (int i = 0; i < preparationTextObjects.Length; i++)
            {
                GameObject textObject = preparationTextObjects[i];
                if (textObject == null)
                    continue;
                if (hidden)
                    _preparationTextWasActive[i] = textObject.activeSelf;
                textObject.SetActive(hidden == false && _preparationTextWasActive[i]);
            }
            _preparationTextHidden = hidden;
        }

        private void RestoreWorkbenchPosition()
        {
            if (_shiftedWorkbench != null)
                _shiftedWorkbench.anchoredPosition = _workbenchOriginalPosition;
            _shiftedWorkbench = null;
        }

        private void AlignTargetFrameToIngredientAnchor()
        {
            ResolvePresentationPeers();
            if (ingredientAnchor == null || targetFrame == null || targetFrame.parent == null)
                return;

            RectTransform targetParent = targetFrame.parent as RectTransform;
            if (targetParent == null)
                return;

            Vector3[] worldCorners = new Vector3[4];
            ingredientAnchor.GetWorldCorners(worldCorners);
            Vector3 bottomLeft = targetParent.InverseTransformPoint(worldCorners[0]);
            Vector3 topRight = targetParent.InverseTransformPoint(worldCorners[2]);
            Vector2 center = (Vector2)(bottomLeft + topRight) * 0.5f;
            Vector2 size = new Vector2(
                Mathf.Max(1f, topRight.x - bottomLeft.x) + targetPadding * 2f,
                Mathf.Max(1f, topRight.y - bottomLeft.y) + targetPadding * 2f);
            if (_targetFrameLayoutCached)
                size = Vector2.Max(size, _targetFrameOriginalSize);

            targetFrame.anchorMin = targetFrame.anchorMax = new Vector2(0.5f, 0.5f);
            targetFrame.pivot = new Vector2(0.5f, 0.5f);
            targetFrame.anchoredPosition = center - targetParent.rect.center;
            targetFrame.sizeDelta = size;
        }

        private void CacheTargetFrameLayout()
        {
            if (_targetFrameLayoutCached == true || targetFrame == null)
                return;

            _targetFrameOriginalPosition = targetFrame.anchoredPosition;
            _targetFrameOriginalSize = targetFrame.sizeDelta;
            _targetFrameLayoutCached = true;
        }

        private void ResetTargetFrame()
        {
            if (targetFrame == null || _targetFrameLayoutCached == false)
                return;

            targetFrame.anchoredPosition = _targetFrameOriginalPosition;
            targetFrame.sizeDelta = _targetFrameOriginalSize;
        }

        private void CacheIngredientTransform()
        {
            EnsureIngredientVisualImage();
            if (_ingredientTransformCached == true || _ingredientVisualImage == null)
                return;

            _ingredientBaseScale = _ingredientVisualImage.rectTransform.localScale;
            _ingredientBaseRotation = _ingredientVisualImage.rectTransform.localRotation;
            _ingredientTransformCached = true;
        }

        private void ResetIngredientTransform()
        {
            StopIngredientFeedback(true);
        }

        private void StopIngredientFeedback(bool resetFlipState)
        {
            if (_ingredientFeedbackRoutine != null)
            {
                StopCoroutine(_ingredientFeedbackRoutine);
                _ingredientFeedbackRoutine = null;
            }

            if (resetFlipState)
                _ingredientFlipped = false;
            ApplyIngredientStableTransform();
        }

        private void ApplyIngredientStableTransform()
        {
            EnsureIngredientVisualImage();
            if (_ingredientVisualImage == null)
                return;

            CacheIngredientTransform();
            _ingredientVisualImage.rectTransform.localScale = _ingredientBaseScale;
            _ingredientVisualImage.rectTransform.localRotation = GetIngredientStableRotation();
        }

        private Quaternion GetIngredientStableRotation()
        {
            return _ingredientBaseRotation * Quaternion.Euler(0f, _ingredientFlipped ? 180f : 0f, 0f);
        }

        private IEnumerator AnimateIngredientFeedback(
            Quaternion startRotation,
            Quaternion endRotation,
            float duration,
            bool flip)
        {
            RectTransform rectTransform = _ingredientVisualImage != null
                ? _ingredientVisualImage.rectTransform
                : null;
            if (rectTransform == null)
            {
                _ingredientFeedbackRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                float scale = flip
                    ? Mathf.Lerp(1f, 0.82f, pulse)
                    : Mathf.Lerp(1f, 1.08f, pulse);
                rectTransform.localScale = Vector3.Scale(_ingredientBaseScale, new Vector3(scale, scale, 1f));
                rectTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, progress);
                yield return null;
            }

            ApplyIngredientStableTransform();
            _ingredientFeedbackRoutine = null;
        }

        private void RefreshFocusLayout()
        {
            if (overlayRoot == null || targetFrame == null || maskImage == null)
                return;

            if (_hudLayoutDirty)
            {
                RefreshHudTextLayout();
                _hudLayoutDirty = false;
            }

            Rect workBounds = GetWorkBounds();
            float headerHeight = hudRoot != null ? hudRoot.rect.height : 0f;
            float actionHeight = actionHudRoot != null ? actionHudRoot.rect.height : 0f;
            RectTransform mistakeRoot = mistakeCanvasGroup != null
                ? mistakeCanvasGroup.transform as RectTransform : null;
            float toastHeight = mistakeRoot != null ? mistakeRoot.rect.height * 1.04f : 0f;
            float minimumCenter = overlayRoot.rect.yMin + ScreenPadding + actionHudGap
                                  + actionHeight + PanelPadding + toastHeight + workBounds.height * 0.5f;
            float maximumCenter = overlayRoot.rect.yMax - ScreenPadding - actionHudGap
                                  - headerHeight - workBounds.height * 0.5f;

            // Translate the workbench only when needed; its size and all input tolerances stay unchanged.
            if (minimumCenter <= maximumCenter)
            {
                float shift = Mathf.Clamp(workBounds.center.y, minimumCenter, maximumCenter) - workBounds.center.y;
                if (Mathf.Abs(shift) > 0.01f)
                {
                    RectTransform workbench = _workbenchView != null ? _workbenchView.transform as RectTransform : null;
                    if (workbench != null)
                    {
                        if (_shiftedWorkbench == null)
                        {
                            _shiftedWorkbench = workbench;
                            _workbenchOriginalPosition = workbench.anchoredPosition;
                        }
                        workbench.position += overlayRoot.TransformVector(new Vector3(0f, shift));
                        AlignTargetFrameToIngredientAnchor();
                    }
                    else if (ingredientAnchor == null)
                        targetFrame.position += overlayRoot.TransformVector(new Vector3(0f, shift));
                    workBounds = GetWorkBounds();
                }
            }

            if (hudRoot != null)
                PlacePanel(hudRoot, workBounds.center.x, workBounds.yMax + actionHudGap + headerHeight * 0.5f);
            if (actionHudRoot != null)
                PlacePanel(actionHudRoot, workBounds.center.x, workBounds.yMin - actionHudGap - actionHeight * 0.5f);
            if (mistakeRoot != null)
                PlacePanel(mistakeRoot, workBounds.center.x,
                    workBounds.yMin - actionHudGap - actionHeight - PanelPadding - toastHeight * 0.5f);

            Vector3[] worldCorners = new Vector3[4];
            if (GetDisplayedIngredientWorldCorners(worldCorners) == false)
                return;

            Vector3 bottomLeft = overlayRoot.InverseTransformPoint(worldCorners[0]);
            Vector3 topRight = overlayRoot.InverseTransformPoint(worldCorners[2]);

            AlignFocusDimmers(bottomLeft, topRight);
        }

        private Rect GetWorkBounds()
        {
            Rect bounds = GetOverlayRect(targetFrame);
            if (_workbenchView != null && _workbenchView.transform is RectTransform workbench)
            {
                Rect board = GetOverlayRect(workbench);
                bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, board.xMin), Mathf.Min(bounds.yMin, board.yMin),
                    Mathf.Max(bounds.xMax, board.xMax), Mathf.Max(bounds.yMax, board.yMax));
            }
            // The knife pivots at its tip, so its pulsing handle extends past either cut endpoint.
            // Reserve that space independently of the current pointer to avoid moving the board while dragging.
            if (_activeOption != null && _activeOption.MiniGameType == CookingMiniGameType.Slicing)
            {
                bounds.yMin -= 80f;
                bounds.yMax += 80f;
            }
            return bounds;
        }

        private Rect GetOverlayRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 minimum = overlayRoot.InverseTransformPoint(corners[0]);
            Vector2 maximum = minimum;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = overlayRoot.InverseTransformPoint(corners[i]);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        private void PlacePanel(RectTransform panel, float x, float y)
        {
            Rect bounds = GetOverlayRect(panel);
            float halfWidth = bounds.width * 0.5f;
            x = Mathf.Clamp(x, overlayRoot.rect.xMin + ScreenPadding + halfWidth,
                overlayRoot.rect.xMax - ScreenPadding - halfWidth);
            panel.position += overlayRoot.TransformVector(new Vector2(x, y) - bounds.center);
        }

        private void RefreshHudTextLayout()
        {
            if (hudRoot != null)
            {
                float buttonWidth = cancelButton != null ? ((RectTransform)cancelButton.transform).rect.width : 0f;
                float width = hudRoot.rect.width - PanelPadding * 3f - buttonWidth;
                float x = -(buttonWidth + PanelPadding) * 0.5f;
                float top = PanelPadding;
                LayoutTextRow(titleField, x, width, 36f, PanelPadding, ref top);
                LayoutTextRow(instructionField, x, width, 48f, PanelPadding, ref top);
                LayoutTextRow(statusField, x, width, 28f, PanelPadding, ref top);
                hudRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top);
                if (cancelButton != null)
                {
                    RectTransform button = (RectTransform)cancelButton.transform;
                    button.anchoredPosition = new Vector2(hudRoot.rect.width * 0.5f - PanelPadding - buttonWidth * 0.5f, 0f);
                }
            }

            if (actionHudRoot != null)
            {
                float width = actionHudRoot.rect.width - PanelPadding * 2f;
                float top = PanelPadding;
                LayoutTextRow(gestureField, 0f, width, 52f, 8f, ref top);
                LayoutRow(progressGaugeRoot, 0f, width, 22f, 8f, ref top);
                LayoutTextRow(progressField, 0f, width, 28f, 8f, ref top);
                LayoutRow(timerGaugeRoot, 0f, width, 8f, 8f, ref top);
                actionHudRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(176f, top + 8f));
            }

            if (mistakeCanvasGroup != null && mistakeField != null)
            {
                RectTransform toast = (RectTransform)mistakeCanvasGroup.transform;
                float width = toast.rect.width - PanelPadding * 2f;
                float top = PanelPadding;
                LayoutTextRow(mistakeField, 0f, width, 42f, PanelPadding, ref top);
                toast.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top);
            }
        }

        private static void LayoutTextRow(TextMeshProUGUI field, float x, float width, float minimumHeight,
            float gap, ref float top)
        {
            if (field == null)
                return;
            field.textWrappingMode = TextWrappingModes.Normal;
            float height = Mathf.Max(minimumHeight, Mathf.Ceil(field.GetPreferredValues(field.text, width, Mathf.Infinity).y));
            LayoutRow(field.rectTransform, x, width, height, gap, ref top);
        }

        private static void LayoutRow(RectTransform row, float x, float width, float height, float gap, ref float top)
        {
            if (row == null)
                return;
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(x, -top);
            row.sizeDelta = new Vector2(width, height);
            top += height + gap;
        }

        private bool GetDisplayedIngredientWorldCorners(Vector3[] worldCorners)
        {
            if (worldCorners == null || worldCorners.Length < 4 || maskImage == null || maskImage.enabled == false)
                return false;

            RectTransform rectTransform = maskImage.rectTransform;
            Rect fittedRect = rectTransform.rect;
            Sprite sprite = maskImage.sprite;
            if (maskImage.preserveAspect == true && sprite != null && sprite.rect.height > 0f)
                fittedRect = CalculatePreservedAspectRect(fittedRect, sprite.rect.size);

            worldCorners[0] = rectTransform.TransformPoint(new Vector3(fittedRect.xMin, fittedRect.yMin));
            worldCorners[1] = rectTransform.TransformPoint(new Vector3(fittedRect.xMin, fittedRect.yMax));
            worldCorners[2] = rectTransform.TransformPoint(new Vector3(fittedRect.xMax, fittedRect.yMax));
            worldCorners[3] = rectTransform.TransformPoint(new Vector3(fittedRect.xMax, fittedRect.yMin));
            return true;
        }

        private void SetIngredientVisible(bool visible)
        {
            if (maskImage == null)
                return;

            if (visible)
                EnsureIngredientVisualImage();
            if (visible == false)
                ResetIngredientTransform();
            maskImage.enabled = visible;
            Mask mask = maskImage.GetComponent<Mask>();
            if (mask != null)
                mask.showMaskGraphic = false;
            if (_ingredientVisualImage != null)
                _ingredientVisualImage.enabled = visible;
            if (visible == false)
            {
                maskImage.sprite = null;
                if (_ingredientVisualImage != null)
                    _ingredientVisualImage.sprite = null;
            }
        }

        private void EnsureIngredientVisualImage()
        {
            if (_ingredientVisualImage != null || maskImage == null)
                return;

            GameObject visualObject = new GameObject(
                "IngredientFeedbackVisual",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            visualObject.layer = maskImage.gameObject.layer;

            RectTransform visualRect = visualObject.GetComponent<RectTransform>();
            visualRect.SetParent(maskImage.rectTransform, false);
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.anchoredPosition = Vector2.zero;
            visualRect.sizeDelta = Vector2.zero;
            visualRect.localScale = Vector3.one;
            visualRect.localRotation = Quaternion.identity;
            visualRect.SetAsFirstSibling();

            _ingredientVisualImage = visualObject.GetComponent<Image>();
            _ingredientVisualImage.material = maskImage.material;
            _ingredientVisualImage.color = maskImage.color;
            _ingredientVisualImage.type = maskImage.type;
            _ingredientVisualImage.preserveAspect = true;
            _ingredientVisualImage.raycastTarget = false;
            _ingredientVisualImage.maskable = true;
            _ingredientTransformCached = false;

            Mask mask = maskImage.GetComponent<Mask>();
            if (mask != null)
                mask.showMaskGraphic = false;
        }

        private static Rect CalculatePreservedAspectRect(Rect container, Vector2 contentSize)
        {
            if (container.width <= 0f || container.height <= 0f || contentSize.x <= 0f || contentSize.y <= 0f)
                return container;

            float contentAspect = contentSize.x / contentSize.y;
            float containerAspect = container.width / container.height;
            if (contentAspect > containerAspect)
            {
                float height = container.width / contentAspect;
                container.y += (container.height - height) * 0.5f;
                container.height = height;
            }
            else
            {
                float width = container.height * contentAspect;
                container.x += (container.width - width) * 0.5f;
                container.width = width;
            }

            return container;
        }

        private void AlignFocusDimmers(Vector2 bottomLeft, Vector2 topRight)
        {
            if (focusDimmers == null || focusDimmers.Length < 4 || overlayRoot == null)
                return;

            Rect bounds = overlayRoot.rect;
            SetRect(focusDimmers[0], bounds.xMin, bottomLeft.x, bounds.yMin, bounds.yMax);
            SetRect(focusDimmers[1], topRight.x, bounds.xMax, bounds.yMin, bounds.yMax);
            SetRect(focusDimmers[2], bottomLeft.x, topRight.x, topRight.y, bounds.yMax);
            SetRect(focusDimmers[3], bottomLeft.x, topRight.x, bounds.yMin, bottomLeft.y);
        }

        private static void SetRect(Image image, float xMin, float xMax, float yMin, float yMax)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }

        private void ApplyDimColor()
        {
            if (focusDimmers == null)
                return;

            Color color = _settings != null ? _settings.FocusDimColor : new Color(0f, 0f, 0f, 0.5f);
            for (int i = 0; i < focusDimmers.Length; i++)
            {
                if (focusDimmers[i] != null)
                    focusDimmers[i].color = color;
            }
        }

        private void SetResultVisible(bool visible)
        {
            if (resultCanvasGroup == null)
                return;

            resultCanvasGroup.alpha = visible ? 1f : 0f;
            resultCanvasGroup.interactable = false;
            resultCanvasGroup.blocksRaycasts = false;
        }

        private IEnumerator ShowMistakeRoutine(string text)
        {
            SetText(mistakeField, text);
            SetMistakeVisible(true);

            RectTransform mistakeRoot = mistakeCanvasGroup != null
                ? mistakeCanvasGroup.transform as RectTransform
                : null;
            if (mistakeCanvasGroup != null)
                mistakeCanvasGroup.alpha = 0f;
            if (mistakeRoot != null)
                mistakeRoot.localScale = Vector3.one * 0.9f;

            const float revealDuration = 0.12f;
            float revealElapsed = 0f;
            while (revealElapsed < revealDuration)
            {
                revealElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(revealElapsed / revealDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                if (mistakeCanvasGroup != null)
                    mistakeCanvasGroup.alpha = eased;
                if (mistakeRoot != null)
                    mistakeRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1.04f, eased);
                yield return null;
            }
            if (mistakeRoot != null)
                mistakeRoot.localScale = Vector3.one;

            float holdDuration = Mathf.Max(0.1f, mistakeDisplayDuration - revealDuration - 0.18f);
            yield return new WaitForSecondsRealtime(holdDuration);

            float elapsed = 0f;
            const float fadeDuration = 0.18f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (mistakeCanvasGroup != null)
                    mistakeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            SetMistakeVisible(false);
            if (mistakeRoot != null)
                mistakeRoot.localScale = Vector3.one;
            _mistakeRoutine = null;
        }

        private void StopMistakeRoutine()
        {
            if (_mistakeRoutine != null)
            {
                StopCoroutine(_mistakeRoutine);
                _mistakeRoutine = null;
            }
            SetMistakeVisible(false);
            if (mistakeCanvasGroup != null)
                mistakeCanvasGroup.transform.localScale = Vector3.one;
        }

        private void SetMistakeVisible(bool visible)
        {
            if (mistakeCanvasGroup == null)
                return;

            mistakeCanvasGroup.alpha = visible ? 1f : 0f;
            mistakeCanvasGroup.interactable = false;
            mistakeCanvasGroup.blocksRaycasts = false;
        }

        private void ResetActionHud()
        {
            if (actionHudRoot != null)
                actionHudRoot.gameObject.SetActive(false);
            SetText(progressField, string.Empty);
            SetText(gestureField, string.Empty);
            SetHorizontalFill(progressFill, 0f);
            SetHorizontalFill(timerFill, 1f);
            if (targetBand != null)
                targetBand.gameObject.SetActive(false);
            if (targetMarker != null)
                targetMarker.gameObject.SetActive(false);
        }

        private static void SetHorizontalFill(Image image, float normalizedValue)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(normalizedValue), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetHorizontalRange(Image image, float minimum, float maximum)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(Mathf.Clamp01(minimum), 0f);
            rect.anchorMax = new Vector2(Mathf.Clamp01(maximum), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetHorizontalMarker(Image image, float normalizedValue)
        {
            if (image == null)
                return;

            float value = Mathf.Clamp01(normalizedValue);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(value, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void PlayClip(AudioClip clip, TemporaryFeedbackTone fallbackTone)
        {
            if (audioSource == null)
                return;

            AudioClip resolvedClip = clip;
            if (resolvedClip == null && useTemporaryFeedbackAudio == true)
                resolvedClip = GetOrCreateTemporaryClip(fallbackTone);
            if (resolvedClip != null)
                audioSource.PlayOneShot(resolvedClip);
        }

        private static AudioClip GetOrCreateTemporaryClip(TemporaryFeedbackTone tone)
        {
            switch (tone)
            {
                case TemporaryFeedbackTone.Action:
                    return _temporaryActionClip != null
                        ? _temporaryActionClip
                        : _temporaryActionClip = CreateTemporaryTone("Temp Cooking Action", 0.045f, 660f, 760f, 0.045f);
                case TemporaryFeedbackTone.Success:
                    return _temporarySuccessClip != null
                        ? _temporarySuccessClip
                        : _temporarySuccessClip = CreateTemporaryTone("Temp Cooking Success", 0.18f, 520f, 920f, 0.065f);
                default:
                    return _temporaryMistakeClip != null
                        ? _temporaryMistakeClip
                        : _temporaryMistakeClip = CreateTemporaryTone("Temp Cooking Mistake", 0.13f, 240f, 150f, 0.075f);
            }
        }

        private static AudioClip CreateTemporaryTone(
            string clipName,
            float duration,
            float startFrequency,
            float endFrequency,
            float volume)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = sampleCount > 1 ? (float)i / (sampleCount - 1) : 1f;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += frequency * Mathf.PI * 2f / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * progress) * (1f - progress * 0.45f);
                samples[i] = Mathf.Sin(phase) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private void TryVibrate()
        {
            if (_settings == null || _settings.EnableHaptics == false)
                return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private void HandleCancelClicked()
        {
            _owner?.CancelActiveMiniGame();
        }

        private void StopResultRoutine()
        {
            if (_resultRoutine == null)
                return;

            StopCoroutine(_resultRoutine);
            _resultRoutine = null;
        }

        private Color GetGradeColor(CookingMiniGameGrade grade)
        {
            Color success = _settings != null ? _settings.SuccessColor : new Color(0.38f, 0.9f, 0.45f, 0.95f);
            Color mistake = _settings != null ? _settings.MistakeColor : new Color(1f, 0.3f, 0.2f, 0.95f);
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return success;
                case CookingMiniGameGrade.Good:
                    return Color.Lerp(success, Color.yellow, 0.35f);
                case CookingMiniGameGrade.Normal:
                    return new Color(1f, 0.67f, 0.2f, 0.95f);
                default:
                    return mistake;
            }
        }

        private static string GetGradeLabel(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return "S · 완벽";
                case CookingMiniGameGrade.Good:
                    return "A · 좋음";
                case CookingMiniGameGrade.Normal:
                    return "B · 보통";
                default:
                    return "C · 미흡";
            }
        }

        private void SetText(TextMeshProUGUI field, string text)
        {
            string value = text ?? string.Empty;
            if (field == null || field.text == value)
                return;
            field.text = value;
            _hudLayoutDirty = true;
        }
    }
}
