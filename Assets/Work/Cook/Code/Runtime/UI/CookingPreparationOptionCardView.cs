using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;

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
        [SerializeField] private Image selectedFrame;
        [SerializeField] private Outline selectedOutline;
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;

        private bool _showDetailsOnHover = true;
        private bool _inputEnabled = true;
        private CookingPreparationTooltipView _tooltipView;
        private UnityAction _selectAction;
        private Color _defaultButtonColor = Color.white;
        private bool _defaultButtonColorCached;
        private string _displayName = string.Empty;
        private string _description = string.Empty;
        private string _effect = string.Empty;

        public RectTransform HoverVisualRoot => hoverVisualRoot;
        // Fan 배치는 카드 프리팹 루트에 직렬화된 크기를 기준으로 해야 한다.
        // VisualRoot는 stretch 자식이므로 여기에 anchor/pivot을 덮어쓰면 크기가 0이 된다.
        public RectTransform LayoutRoot => transform as RectTransform;
        public event Action<CookingPreparationOptionCardView, bool> HoverChanged;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            _tooltipView?.Hide(this);
        }

        public void SetPresentation(
            CookingUiPresentationSettingsSO settings,
            CookingPreparationTooltipView tooltipView)
        {
            presentationSettings = settings;
            _tooltipView = tooltipView;
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
            _selectAction = selectAction;

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
                selectButton.onClick.AddListener(HandleSelectClicked);

                TextMeshProUGUI buttonLabelField = selectButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (buttonLabelField != null)
                {
                    buttonLabelField.text = buttonLabel ?? string.Empty;
                }
            }

            SetSelected(false);
            SetInputEnabled(true);
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (selectButton != null)
                selectButton.interactable = enabled && _selectAction != null;
        }

        public void SetSelected(bool selected)
        {
            EnsureReferences();
            if (selectedFrame != null)
                selectedFrame.enabled = selected;
            if (selectedOutline != null)
                selectedOutline.enabled = selected;

            if (selectButton?.image == null)
                return;

            CacheDefaultButtonColor();
            Color accent = presentationSettings != null
                ? presentationSettings.PositiveColor
                : new Color(0.95f, 0.74f, 0.27f, 1f);
            selectButton.image.color = selected
                ? Color.Lerp(_defaultButtonColor, accent, 0.22f)
                : _defaultButtonColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inputEnabled == false)
                return;

            HoverChanged?.Invoke(this, true);
            if (_showDetailsOnHover == true)
            {
                if (_tooltipView != null)
                    _tooltipView.Show(this, _displayName, _description, _effect);
                else
                    SetDetailsVisible(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverChanged?.Invoke(this, false);
            if (_showDetailsOnHover == true)
            {
                if (_tooltipView != null)
                    _tooltipView.Hide(this);
                else
                    SetDetailsVisible(false);
            }
        }

        private void HandleSelectClicked()
        {
            if (_inputEnabled == false)
                return;

            _selectAction?.Invoke();
        }

        private void CacheDefaultButtonColor()
        {
            if (_defaultButtonColorCached == true || selectButton?.image == null)
                return;

            _defaultButtonColor = selectButton.image.color;
            _defaultButtonColorCached = true;
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

            if (selectedFrame == null)
            {
                Transform selectedFrameTransform = transform.Find("SelectedFrame");
                if (selectedFrameTransform != null)
                    selectedFrame = selectedFrameTransform.GetComponent<Image>();
            }

            if (selectedOutline == null && selectButton != null)
                selectedOutline = selectButton.GetComponent<Outline>();

            CacheDefaultButtonColor();
        }
    }
}
