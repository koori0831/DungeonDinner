using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 재료 선택 반복 버튼 프리팹의 표시와 입력 연결
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookingIngredientButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelField;
        [SerializeField] private GameObject selectedMarker;
        [SerializeField] private RectTransform hoverVisualRoot;
        [SerializeField, Min(1f)] private float hoverScale = 1.08f;
        [SerializeField, Min(0f)] private float hoverDuration = 0.12f;

        private UnityAction _pointerEnterAction;
        private UnityAction _pointerExitAction;
        private ScrollRect _parentScrollRect;
        private Vector3 _hoverBaseScale = Vector3.one;

        public Button Button => button;

        public void Bind(
            string label,
            Sprite icon,
            bool selected,
            bool interactable,
            UnityAction clickAction,
            UnityAction pointerEnterAction,
            UnityAction pointerExitAction)
        {
            EnsureReferences();
            ResetHoverVisual();
            _pointerEnterAction = pointerEnterAction;
            _pointerExitAction = pointerExitAction;

            if (labelField != null)
            {
                labelField.text = label ?? string.Empty;
                labelField.color = new Color(0.26f, 0.16f, 0.09f);
                labelField.alignment = TextAlignmentOptions.Center;
                labelField.enableAutoSizing = true;
                labelField.fontSizeMin = 13;
                labelField.fontSizeMax = 17;
                labelField.textWrappingMode = TextWrappingModes.Normal;
                labelField.rectTransform.anchorMin = Vector2.zero;
                labelField.rectTransform.anchorMax = Vector2.one;
                labelField.rectTransform.offsetMin = new Vector2(6, 6);
                labelField.rectTransform.offsetMax = new Vector2(-6, -76);
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
                iconImage.rectTransform.anchorMin = iconImage.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                iconImage.rectTransform.anchoredPosition = new Vector2(0, -40);
                iconImage.rectTransform.sizeDelta = new Vector2(64, 64);
            }
            var rowSize = GetComponent<LayoutElement>();
            if (rowSize != null)
            {
                rowSize.minHeight = rowSize.preferredHeight = 112;
                rowSize.flexibleHeight = 0;
            }

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(selected == true);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (clickAction != null)
                {
                    button.onClick.AddListener(clickAction);
                }

                button.interactable = interactable;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AnimateHoverVisual(_hoverBaseScale * hoverScale);

            if (_pointerEnterAction != null)
            {
                _pointerEnterAction.Invoke();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateHoverVisual(_hoverBaseScale);

            if (_pointerExitAction != null)
            {
                _pointerExitAction.Invoke();
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            EnsureReferences();
            if (_parentScrollRect != null)
                _parentScrollRect.OnScroll(eventData);
        }

        private void OnDisable()
        {
            ResetHoverVisual();
        }

        private void AnimateHoverVisual(Vector3 targetScale)
        {
            if (hoverVisualRoot == null)
                return;

            hoverVisualRoot.DOKill();
            if (hoverDuration <= 0f)
            {
                hoverVisualRoot.localScale = targetScale;
                return;
            }

            hoverVisualRoot.DOScale(targetScale, hoverDuration).SetEase(Ease.OutQuad);
        }

        private void ResetHoverVisual()
        {
            if (hoverVisualRoot == null)
                return;

            hoverVisualRoot.DOKill();
            hoverVisualRoot.localScale = _hoverBaseScale;
        }

        private void EnsureReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (labelField == null)
            {
                labelField = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (hoverVisualRoot == null && iconImage != null)
                hoverVisualRoot = iconImage.rectTransform;

            if (hoverVisualRoot != null)
                _hoverBaseScale = Vector3.one;

            if (_parentScrollRect == null)
                _parentScrollRect = GetComponentInParent<ScrollRect>();
        }
    }
}
