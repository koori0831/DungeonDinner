using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 재료 선택 항목 프리팹의 표시, 선택 상태, 포인터 이벤트 바인딩
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookingIngredientButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelField;
        [SerializeField] private GameObject selectedStateObject;
        [SerializeField] private GameObject unavailableStateObject;

        private IngredientSO _ingredient;
        private UnityAction _clickAction;
        private Action<IngredientSO> _pointerEntered;
        private Action<IngredientSO> _pointerExited;

        /// <summary>
        /// 재료 선택 항목 데이터 적용
        /// </summary>
        public void Bind(
            IngredientSO ingredient,
            string label,
            Sprite icon,
            bool selected,
            bool interactable,
            UnityAction clickAction,
            Action<IngredientSO> pointerEntered,
            Action<IngredientSO> pointerExited)
        {
            EnsureReferences();

            _ingredient = ingredient;
            _pointerEntered = pointerEntered;
            _pointerExited = pointerExited;

            SetText(labelField, label);
            SetIcon(icon);
            SetActive(selectedStateObject, selected);
            SetActive(unavailableStateObject, interactable == false);
            BindClick(clickAction);

            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_ingredient != null && _pointerEntered != null)
            {
                _pointerEntered.Invoke(_ingredient);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_ingredient != null && _pointerExited != null)
            {
                _pointerExited.Invoke(_ingredient);
            }
        }

        private void Reset()
        {
            button = GetComponent<Button>();
            labelField = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDestroy()
        {
            BindClick(null);
        }

        private void EnsureReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void SetIcon(Sprite icon)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
            iconImage.preserveAspect = true;
        }

        private void BindClick(UnityAction clickAction)
        {
            if (button == null)
            {
                return;
            }

            if (_clickAction != null)
            {
                button.onClick.RemoveListener(_clickAction);
            }

            _clickAction = clickAction;
            if (_clickAction != null)
            {
                button.onClick.AddListener(_clickAction);
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
            {
                field.text = text ?? string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
