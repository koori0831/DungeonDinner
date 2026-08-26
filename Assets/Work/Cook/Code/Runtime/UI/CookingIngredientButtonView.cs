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
    public sealed class CookingIngredientButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelField;
        [SerializeField] private GameObject selectedMarker;

        private UnityAction _pointerEnterAction;
        private UnityAction _pointerExitAction;

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
            _pointerEnterAction = pointerEnterAction;
            _pointerExitAction = pointerExitAction;

            if (labelField != null)
            {
                labelField.text = label ?? string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
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
            if (_pointerEnterAction != null)
            {
                _pointerEnterAction.Invoke();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pointerExitAction != null)
            {
                _pointerExitAction.Invoke();
            }
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
        }
    }
}
