using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class CookingEffectChipView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI labelField;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField, Min(24f)] private float horizontalPadding = 24f;

        public void Bind(string label, CookingUiPresentationSettingsSO settings)
        {
            string value = label ?? string.Empty;
            bool negative = value.Contains("혐오") || value.Contains("독성") || value.StartsWith("−");
            bool positive = value.StartsWith("+") || value.Contains("품질 +");
            Color color = negative
                ? settings != null ? settings.NegativeColor : new Color(0.75f, 0.2f, 0.15f, 1f)
                : positive
                    ? settings != null ? settings.PositiveColor : new Color(0.9f, 0.7f, 0.25f, 1f)
                    : settings != null ? settings.IronColor : new Color(0.25f, 0.25f, 0.25f, 1f);

            if (backgroundImage != null)
                backgroundImage.color = new Color(color.r, color.g, color.b, 0.9f);
            if (labelField != null)
            {
                labelField.text = value;
                if (settings?.FontAsset != null)
                    labelField.font = settings.FontAsset;
            }
            if (layoutElement != null && labelField != null)
                layoutElement.preferredWidth = Mathf.Max(64f, labelField.GetPreferredValues(value).x + horizontalPadding);

            gameObject.SetActive(true);
        }
    }
}
