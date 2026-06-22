using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 지도 포인트 프리팹의 표시와 클릭 이벤트 바인딩
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DispatchPointButtonView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI rewardField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private GameObject invalidStateObject;

        private UnityAction _clickAction;

        /// <summary>
        /// 포인트 데이터와 선택 콜백 적용
        /// </summary>
        /// <param name="point">표시할 파견 포인트</param>
        /// <param name="clickAction">클릭 시 실행할 콜백</param>
        /// <param name="interactable">버튼 상호작용 가능 여부</param>
        public void Bind(DispatchPointSO point, UnityAction clickAction, bool interactable)
        {
            EnsureReferences();

            SetText(titleField, point != null ? point.DisplayName : string.Empty);
            SetText(rewardField, point != null ? point.BuildRewardSummaryText() : string.Empty);
            SetText(descriptionField, point != null ? point.Description : string.Empty);

            if (iconImage != null)
            {
                iconImage.sprite = point != null ? point.Icon : null;
                iconImage.gameObject.SetActive(iconImage.sprite != null);
            }

            SetActive(invalidStateObject, point == null || point.HasValidReward == false);
            BindClick(clickAction);

            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void Reset()
        {
            button = GetComponent<Button>();
            iconImage = GetComponentInChildren<Image>(true);
            titleField = GetComponentInChildren<TextMeshProUGUI>(true);
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
