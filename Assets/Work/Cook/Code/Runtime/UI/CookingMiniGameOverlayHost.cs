using System;
using System.Collections;
using TMPro;
using UnityEngine;
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

        [Header("HUD")]
        [SerializeField] private RectTransform hudRoot;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI instructionField;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private Button cancelButton;

        [Header("Result")]
        [SerializeField] private CanvasGroup resultCanvasGroup;
        [SerializeField] private TextMeshProUGUI resultField;
        [SerializeField] private Image resultBackground;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;

        private CookingGamePanel _owner;
        private CookingMiniGameOverlaySettingsSO _settings;
        private Coroutine _resultRoutine;
        private Vector2 _targetFrameOriginalPosition;
        private Vector2 _targetFrameOriginalSize;
        private float _lastProgressTime;
        private bool _targetFrameLayoutCached;
        private bool _isRunning;
        private bool _hintShown;

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
            ResolveIngredientAnchor();

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
                cancelButton.onClick.AddListener(HandleCancelClicked);
            }

            SetFontAsset(fontAsset);
            SetResultVisible(false);
            CacheTargetFrameLayout();
            SetIngredientVisible(false);
            ApplyDimColor();
        }

        public void SetFontAsset(TMP_FontAsset fontAsset)
        {
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
        }

        public bool Begin(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (ingredient == null || option == null || targetFrame == null || maskImage == null)
                return false;

            StopResultRoutine();
            CacheTargetFrameLayout();
            ResetTargetFrame();
            AlignTargetFrameToIngredientAnchor();
            maskImage.sprite = ingredient.IconSprite;
            maskImage.preserveAspect = true;
            SetIngredientVisible(true);

            SetText(titleField, option.DisplayName);
            SetText(instructionField, "재료 위의 가이드를 따라 조작하세요.");
            SetText(statusField, string.Empty);
            SetResultVisible(false);
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
            PlayClip(_settings != null ? _settings.ActionClip : null);
        }

        public void PlayMistakeFeedback()
        {
            PlayClip(_settings != null ? _settings.MistakeClip : null);
            TryVibrate();
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
            if (cancelButton != null)
                cancelButton.interactable = false;
            SetResultVisible(false);
            ResetTargetFrame();
            SetIngredientVisible(false);
        }

        private void OnEnable()
        {
            if (_isRunning == true)
                RefreshFocusLayout();
        }

        private void LateUpdate()
        {
            if (_isRunning == false)
                return;

            AlignTargetFrameToIngredientAnchor();
            RefreshFocusLayout();

            if (_hintShown == false && Time.unscaledTime - _lastProgressTime >= 10f)
            {
                _hintShown = true;
                SetStatus("빛나는 가이드를 따라 조작하세요");
            }
        }

        private IEnumerator ShowResultRoutine(CookingMiniGameResult result, Action completed)
        {
            _isRunning = false;
            if (cancelButton != null)
                cancelButton.interactable = false;
            ResultShown?.Invoke(result);
            SetText(resultField, GetGradeLabel(result != null ? result.Grade : CookingMiniGameGrade.Bad));
            if (resultBackground != null)
                resultBackground.color = GetGradeColor(result != null ? result.Grade : CookingMiniGameGrade.Bad);
            SetResultVisible(true);
            AudioClip resultClip = null;
            if (_settings != null)
                resultClip = result != null && result.Grade == CookingMiniGameGrade.Bad
                    ? _settings.MistakeClip
                    : _settings.SuccessClip;
            PlayClip(resultClip);

            float duration = _settings != null ? _settings.ResultDisplayDuration : 0.6f;
            if (duration > 0f)
                yield return new WaitForSecondsRealtime(duration);

            SetResultVisible(false);
            ResetTargetFrame();
            SetIngredientVisible(false);
            _resultRoutine = null;
            completed?.Invoke();
        }

        private void ResolveIngredientAnchor()
        {
            if (ingredientAnchor != null)
                return;

            CookingWorkbenchView workbench = _owner != null
                ? _owner.GetComponentInChildren<CookingWorkbenchView>(true)
                : FindFirstObjectByType<CookingWorkbenchView>(FindObjectsInactive.Include);
            if (workbench != null)
                ingredientAnchor = workbench.IngredientAnchor;
        }

        private void AlignTargetFrameToIngredientAnchor()
        {
            ResolveIngredientAnchor();
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

        private void RefreshFocusLayout()
        {
            if (overlayRoot == null || targetFrame == null || maskImage == null)
                return;

            Vector3[] worldCorners = new Vector3[4];
            if (GetDisplayedIngredientWorldCorners(worldCorners) == false)
                return;

            Vector3 bottomLeft = overlayRoot.InverseTransformPoint(worldCorners[0]);
            Vector3 topRight = overlayRoot.InverseTransformPoint(worldCorners[2]);
            Vector2 size = new Vector2(
                Mathf.Max(1f, topRight.x - bottomLeft.x),
                Mathf.Max(1f, topRight.y - bottomLeft.y));
            Vector2 center = (Vector2)(bottomLeft + topRight) * 0.5f;

            if (hudRoot != null)
            {
                float hudY = Mathf.Min(overlayRoot.rect.yMax - 70f, center.y + (size.y * 0.5f) + 58f);
                hudRoot.anchoredPosition = new Vector2(center.x, hudY);
            }

            AlignFocusDimmers(bottomLeft, topRight);
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

            maskImage.enabled = visible;
            Mask mask = maskImage.GetComponent<Mask>();
            if (mask != null)
                mask.showMaskGraphic = visible;
            if (visible == false)
                maskImage.sprite = null;
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

            Color color = _settings != null ? _settings.FocusDimColor : new Color(0f, 0f, 0f, 0.18f);
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

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
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
                    return "S";
                case CookingMiniGameGrade.Good:
                    return "A";
                case CookingMiniGameGrade.Normal:
                    return "B";
                default:
                    return "C";
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
