using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class CookingUiChipView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelField;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField, Min(24f)] private float horizontalPadding = 42f;
        [SerializeField, Min(42f)] private float minWidth = 72f;

        public void Bind(CookingTagChipModel model, CookingUiPresentationSettingsSO settings)
        {
            EnsureReferences();
            if (model == null)
            {
                gameObject.SetActive(false);
                return;
            }

            CookingTagVisual visual = settings != null ? settings.GetTagVisual(model.Kind) : null;
            Color color = settings != null
                ? settings.GetTagColor(model.Kind, model.Status)
                : Color.white;

            if (backgroundImage != null)
                backgroundImage.color = new Color(color.r, color.g, color.b, 0.92f);

            if (iconImage != null)
            {
                iconImage.sprite = visual?.Icon;
                iconImage.enabled = iconImage.sprite != null;
                iconImage.preserveAspect = true;
            }

            if (labelField != null)
            {
                labelField.text = model.DisplayName;
                if (settings?.FontAsset != null)
                    labelField.font = settings.FontAsset;
            }

            if (statusField != null)
            {
                statusField.text = BuildStatusSymbol(model.Kind, model.Status);
                if (settings?.FontAsset != null)
                    statusField.font = settings.FontAsset;
            }

            if (layoutElement != null && labelField != null)
            {
                float preferred = labelField.GetPreferredValues(model.DisplayName).x + horizontalPadding;
                layoutElement.minWidth = minWidth;
                layoutElement.preferredWidth = Mathf.Max(minWidth, preferred);
            }

            gameObject.SetActive(true);
        }

        private static string BuildStatusSymbol(
            CookingTagPresentationKind kind,
            CookingTagPresentationStatus status)
        {
            switch (status)
            {
                case CookingTagPresentationStatus.Matched:
                    return "O";
                case CookingTagPresentationStatus.Missing:
                    return "-";
                case CookingTagPresentationStatus.Triggered:
                    return "!";
            }

            switch (kind)
            {
                case CookingTagPresentationKind.Required:
                    return "*";
                case CookingTagPresentationKind.Preferred:
                    return "+";
                case CookingTagPresentationKind.Avoid:
                    return "X";
                default:
                    return "!";
            }
        }

        private void EnsureReferences()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            if (layoutElement == null)
                layoutElement = GetComponent<LayoutElement>();
        }
    }
}
