using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 손질 선택 카드 프리팹의 표시와 입력 연결
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookingPreparationOptionCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI iconTextField;
        [SerializeField] private TextMeshProUGUI nameField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private TextMeshProUGUI effectField;
        [SerializeField] private CanvasGroup descriptionGroup;
        [SerializeField] private CanvasGroup effectGroup;
        [SerializeField] private RectTransform hoverVisualRoot;
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;

        private bool _showDetailsOnHover = true;
        private CookingPreparationTooltipView _tooltipView;
        private Tween _hoverTween;
        private Vector2 _restingPosition;
        private Vector3 _restingScale = Vector3.one;
        private string _displayName = string.Empty;
        private string _description = string.Empty;
        private string _effect = string.Empty;

        public RectTransform HoverVisualRoot => hoverVisualRoot;

        private void Awake()
        {
            EnsureReferences();
            CaptureRestingTransform();
        }

        private void OnDisable()
        {
            _tooltipView?.Hide(this);
            _hoverTween?.Kill();
            _hoverTween = null;
            RestoreImmediate();
        }

        public void SetPresentation(
            CookingUiPresentationSettingsSO settings,
            CookingPreparationTooltipView tooltipView)
        {
            presentationSettings = settings;
            _tooltipView = tooltipView;
            CaptureRestingTransform();
        }

        public void Bind(
            string iconText,
            Sprite icon,
            string displayName,
            string description,
            string effect,
            string buttonLabel,
            bool showDetailsOnHover,
            UnityAction selectAction)
        {
            EnsureReferences();
            _showDetailsOnHover = showDetailsOnHover;
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;
            _effect = effect ?? string.Empty;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }

            if (iconTextField != null)
            {
                iconTextField.text = icon != null ? string.Empty : iconText ?? string.Empty;
                iconTextField.gameObject.SetActive(icon == null);
            }

            SetText(nameField, displayName);
            SetText(descriptionField, description);
            SetText(effectField, effect);

            SetDetailsVisible(showDetailsOnHover == false);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                if (selectAction != null)
                {
                    selectButton.onClick.AddListener(selectAction);
                }

                TextMeshProUGUI buttonLabelField = selectButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (buttonLabelField != null)
                {
                    buttonLabelField.text = buttonLabel ?? string.Empty;
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_showDetailsOnHover == true)
            {
                AnimateHover(true);
                if (_tooltipView != null)
                    _tooltipView.Show(this, _displayName, _description, _effect);
                else
                    SetDetailsVisible(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_showDetailsOnHover == true)
            {
                AnimateHover(false);
                if (_tooltipView != null)
                    _tooltipView.Hide(this);
                else
                    SetDetailsVisible(false);
            }
        }

        private void AnimateHover(bool hovered)
        {
            EnsureReferences();
            if (hoverVisualRoot == null)
                return;

            if (hovered)
                CaptureRestingTransform();

            float offset = presentationSettings != null ? presentationSettings.CardHoverOffset : 20f;
            float scale = presentationSettings != null ? presentationSettings.CardHoverScale : 1.04f;
            float duration = presentationSettings != null ? presentationSettings.CardHoverDuration : 0.14f;
            Vector2 targetPosition = hovered ? _restingPosition + Vector2.up * offset : _restingPosition;
            Vector3 targetScale = hovered ? _restingScale * scale : _restingScale;

            _hoverTween?.Kill();
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(hoverVisualRoot.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutQuad));
            sequence.Join(hoverVisualRoot.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
            _hoverTween = sequence;
        }

        private void CaptureRestingTransform()
        {
            EnsureReferences();
            if (hoverVisualRoot == null)
                return;

            _restingPosition = hoverVisualRoot.anchoredPosition;
            _restingScale = hoverVisualRoot.localScale;
        }

        private void RestoreImmediate()
        {
            if (hoverVisualRoot == null)
                return;

            hoverVisualRoot.anchoredPosition = _restingPosition;
            hoverVisualRoot.localScale = _restingScale;
        }

        private void SetDetailsVisible(bool visible)
        {
            SetCanvasGroupVisible(descriptionGroup, visible);
            SetCanvasGroupVisible(effectGroup, visible);
        }

        private static void SetCanvasGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible == true ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
            {
                field.text = text ?? string.Empty;
            }
        }

        private void EnsureReferences()
        {
            if (selectButton == null)
            {
                selectButton = GetComponentInChildren<Button>(true);
            }

            if (hoverVisualRoot == null)
                hoverVisualRoot = transform as RectTransform;
        }
    }
}
