using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime
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

        private bool _showDetailsOnHover = true;

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
                SetDetailsVisible(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_showDetailsOnHover == true)
            {
                SetDetailsVisible(false);
            }
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
        }
    }
}
