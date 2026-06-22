using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 손질 선택 카드 프리팹의 표시, 효과 표시, 클릭 이벤트 바인딩
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookingPreparationOptionItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI iconField;
        [SerializeField] private TextMeshProUGUI nameField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private TextMeshProUGUI effectField;
        [SerializeField] private GameObject descriptionObject;
        [SerializeField] private GameObject effectObject;

        private UnityAction _clickAction;
        private bool _showEffect;

        /// <summary>
        /// 손질 카드 데이터 적용
        /// </summary>
        public void Bind(
            string iconText,
            string nameText,
            string descriptionText,
            string effectText,
            bool showEffect,
            UnityAction clickAction)
        {
            EnsureReferences();

            SetText(iconField, iconText);
            SetText(nameField, nameText);
            SetText(descriptionField, descriptionText);
            SetText(effectField, effectText);
            _showEffect = showEffect;
            SetActive(descriptionObject != null ? descriptionObject : descriptionField?.gameObject, false);
            SetActive(effectObject != null ? effectObject : effectField?.gameObject, showEffect == true);
            BindClick(clickAction);

            if (selectButton != null)
            {
                selectButton.interactable = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetActive(descriptionObject != null ? descriptionObject : descriptionField?.gameObject, true);
            SetActive(effectObject != null ? effectObject : effectField?.gameObject, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetActive(descriptionObject != null ? descriptionObject : descriptionField?.gameObject, false);
            SetActive(effectObject != null ? effectObject : effectField?.gameObject, _showEffect == true);
        }

        private void Reset()
        {
            selectButton = GetComponentInChildren<Button>(true);
            nameField = GetComponentInChildren<TextMeshProUGUI>(true);
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
            if (selectButton == null)
            {
                selectButton = GetComponentInChildren<Button>(true);
            }
        }

        private void BindClick(UnityAction clickAction)
        {
            if (selectButton == null)
            {
                return;
            }

            if (_clickAction != null)
            {
                selectButton.onClick.RemoveListener(_clickAction);
            }

            _clickAction = clickAction;
            if (_clickAction != null)
            {
                selectButton.onClick.AddListener(_clickAction);
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
