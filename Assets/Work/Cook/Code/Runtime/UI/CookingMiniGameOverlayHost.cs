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
    /// 실제 조리대 재료의 표시 영역을 추적하며 입력 차단, 포커스 딤, HUD와 결과 연출을 제공한다.
    /// </summary>
    public sealed class CookingMiniGameOverlayHost : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private RectTransform targetFrame;
        [SerializeField] private RectTransform controllerContainer;
        [SerializeField] private Image maskImage;
        [SerializeField] private Image[] focusDimmers;

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
        private CookingWorkbenchView _workbench;
        private CookingMiniGameOverlaySettingsSO _settings;
        private Coroutine _resultRoutine;
        private float _lastProgressTime;
        private bool _isRunning;
        private bool _hintShown;
        private bool _trackIngredient = true;

        public RectTransform ControllerContainer => controllerContainer;
        public RectTransform TargetFrame => targetFrame;
        public bool IsRunning => _isRunning;

        public void Initialize(
            CookingGamePanel owner,
            CookingWorkbenchView workbench,
            CookingMiniGameOverlaySettingsSO settings,
            TMP_FontAsset fontAsset)
        {
            _owner = owner;
            _workbench = workbench;
            _settings = settings;

            if (overlayRoot == null)
                overlayRoot = transform as RectTransform;
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
                cancelButton.onClick.AddListener(HandleCancelClicked);
            }

            SetFontAsset(fontAsset);
            SetResultVisible(false);
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
            if (_workbench == null || ingredient == null || option == null)
                return false;

            StopResultRoutine();
            _workbench.EnterMiniGameFocus();
            if (maskImage != null)
            {
                maskImage.sprite = _workbench.IngredientSprite;
                maskImage.preserveAspect = true;
            }

            SetText(titleField, option.DisplayName);
            SetText(instructionField, "재료 위의 가이드를 따라 조작하세요.");
            SetText(statusField, string.Empty);
            SetResultVisible(false);
            _lastProgressTime = Time.unscaledTime;
            _hintShown = false;
            _isRunning = true;
            _trackIngredient = true;
            AlignToIngredient();
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

        public void BeginIngredientDrag()
        {
            _trackIngredient = false;
        }

        public void MoveIngredient(Vector2 screenPosition, Camera eventCamera)
        {
            _workbench?.SetFocusedIngredientScreenPosition(screenPosition, eventCamera);
        }

        public void EndIngredientDrag(bool keepAtCurrentPosition)
        {
            if (keepAtCurrentPosition == false)
            {
                _workbench?.ResetFocusedIngredientPosition();
                _trackIngredient = true;
                AlignToIngredient();
            }
        }

        public void EndImmediate()
        {
            StopResultRoutine();
            _isRunning = false;
            _trackIngredient = true;
            SetResultVisible(false);
            _workbench?.ExitMiniGameFocus();
        }

        private void OnEnable()
        {
            if (_isRunning == true)
                AlignToIngredient();
        }

        private void LateUpdate()
        {
            if (_isRunning == false)
                return;

            if (_trackIngredient == true)
                AlignToIngredient();
            if (_hintShown == false && Time.unscaledTime - _lastProgressTime >= 10f)
            {
                _hintShown = true;
                SetStatus("빛나는 가이드를 따라 조작하세요");
            }
        }

        private IEnumerator ShowResultRoutine(CookingMiniGameResult result, Action completed)
        {
            _isRunning = false;
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
            _workbench?.ExitMiniGameFocus();
            _resultRoutine = null;
            completed?.Invoke();
        }

        private void AlignToIngredient()
        {
            if (overlayRoot == null || targetFrame == null || _workbench == null)
                return;

            Vector3[] worldCorners = new Vector3[4];
            if (_workbench.GetDisplayedIngredientWorldCorners(worldCorners) == false)
                return;

            Vector3 bottomLeft = overlayRoot.InverseTransformPoint(worldCorners[0]);
            Vector3 topRight = overlayRoot.InverseTransformPoint(worldCorners[2]);
            Vector2 size = new Vector2(
                Mathf.Max(1f, topRight.x - bottomLeft.x),
                Mathf.Max(1f, topRight.y - bottomLeft.y));
            Vector2 center = (Vector2)(bottomLeft + topRight) * 0.5f;

            targetFrame.anchorMin = targetFrame.anchorMax = new Vector2(0.5f, 0.5f);
            targetFrame.pivot = new Vector2(0.5f, 0.5f);
            targetFrame.anchoredPosition = center;
            targetFrame.sizeDelta = size;

            if (hudRoot != null)
            {
                float hudY = Mathf.Min(overlayRoot.rect.yMax - 70f, center.y + (size.y * 0.5f) + 58f);
                hudRoot.anchoredPosition = new Vector2(center.x, hudY);
            }

            AlignFocusDimmers(bottomLeft, topRight);
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
                    return "Perfect";
                case CookingMiniGameGrade.Good:
                    return "Good";
                case CookingMiniGameGrade.Normal:
                    return "Normal";
                default:
                    return "Bad";
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
