using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 보상 row 프리팹의 표시 연결
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DispatchRewardRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelField;

        public void Bind(string label, Sprite icon)
        {
            if (labelField == null)
            {
                labelField = GetComponentInChildren<TextMeshProUGUI>(true);
            }

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
        }
    }
}
