using TMPro;
using UnityEngine;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 요리 결과 손질 내역 항목 프리팹의 텍스트 바인딩
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookingPreparedIngredientEntryView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI descriptionField;

        /// <summary>
        /// 손질 내역 텍스트 적용
        /// </summary>
        /// <param name="description">표시할 설명</param>
        public void Bind(string description)
        {
            if (descriptionField != null)
            {
                descriptionField.text = description ?? string.Empty;
            }
        }

        private void Reset()
        {
            descriptionField = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
