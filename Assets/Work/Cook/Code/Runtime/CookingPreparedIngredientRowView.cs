using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 요리 결과 손질 내역 row 프리팹의 표시 연결
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookingPreparedIngredientRowView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private Image iconImage;

        public void Bind(string description, Sprite icon)
        {
            if (descriptionField == null)
            {
                descriptionField = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (descriptionField != null)
            {
                descriptionField.text = description ?? string.Empty;
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
