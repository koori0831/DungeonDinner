using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 현재 작업 슬롯에 놓인 손질 카드 표시
    /// </summary>
    public sealed class CookingActivePreparationSlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private string emptyTitleText = "작업 슬롯";
        [SerializeField] private string emptyDescriptionText = "하단 카드에서 손질법을 선택하세요.";

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            if (titleField != null)
                titleField.font = value;
            if (descriptionField != null)
                descriptionField.font = value;
        }

        public void Clear()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            SetText(titleField, emptyTitleText);
            SetText(descriptionField, emptyDescriptionText);
        }

        public void Bind(IngredientPreparationOption option)
        {
            if (iconImage != null)
            {
                Sprite icon = option != null && option.Method != null ? option.Method.IconSprite : null;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }

            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(titleField, optionName);
            SetText(descriptionField, "선택한 카드가 작업 슬롯에 놓였습니다. 중앙 재료를 눌러 손질을 확정하세요.");
        }

        public void BindResult(IngredientPreparationOption option)
        {
            string optionName = option != null ? option.DisplayName : "그대로 사용";
            SetText(titleField, $"{optionName} 완료");
            SetText(descriptionField, "손질 결과가 저장되었습니다.");
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
