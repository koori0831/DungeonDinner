using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 요리 결과의 재료별 손질 내역을 구조화해 표시한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookingPreparedIngredientRowView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI ingredientNameField;
        [SerializeField] private TextMeshProUGUI methodField;
        [SerializeField] private TextMeshProUGUI gradeField;
        [SerializeField] private TextMeshProUGUI feedbackField;
        [SerializeField] private RectTransform effectRoot;
        [SerializeField] private CookingEffectChipView effectChipTemplate;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;
        [SerializeField, Min(0f)] private float minHeight = 92f;
        [SerializeField, Min(0f)] private float verticalPadding = 18f;

        public void SetPresentationSettings(CookingUiPresentationSettingsSO value)
        {
            presentationSettings = value;
            ApplyFont();
        }

        public void Bind(CookingPreparedIngredientPresentationModel model, Sprite icon)
        {
            EnsureReferences();
            if (model == null)
            {
                gameObject.SetActive(false);
                return;
            }

            SetText(ingredientNameField, model.IngredientName);
            SetText(methodField, model.MethodName);
            SetText(gradeField, model.GradeName);
            SetText(feedbackField, model.Feedback);
            SetActive(feedbackField, string.IsNullOrWhiteSpace(model.Feedback) == false);
            SetText(descriptionField, string.Empty);
            SetActive(descriptionField, false);

            if (gradeField != null)
                gradeField.color = GetGradeColor(model);

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }

            RebuildEffects(model);
            ApplyFont();
            RefreshLayoutHeight();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 이전 씬/프리팹과의 호환을 위한 문자열 바인딩.
        /// </summary>
        public void Bind(string description, Sprite icon)
        {
            EnsureReferences();
            SetActive(descriptionField, true);
            SetText(descriptionField, description);
            SetActive(ingredientNameField, false);
            SetActive(methodField, false);
            SetActive(gradeField, false);
            SetActive(feedbackField, false);
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }
            RefreshLayoutHeight();
        }

        private void RebuildEffects(CookingPreparedIngredientPresentationModel model)
        {
            if (effectRoot == null || effectChipTemplate == null)
                return;

            for (int i = effectRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = effectRoot.GetChild(i);
                if (child == effectChipTemplate.transform)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            for (int i = 0; i < model.EffectLabels.Count; i++)
            {
                CookingEffectChipView chip = Instantiate(effectChipTemplate, effectRoot);
                chip.gameObject.name = $"EffectChip{i + 1}";
                chip.Bind(model.EffectLabels[i], presentationSettings);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(effectRoot);
        }

        private Color GetGradeColor(CookingPreparedIngredientPresentationModel model)
        {
            if (model.Grade.HasValue == false)
                return presentationSettings != null ? presentationSettings.SecondaryTextColor : Color.gray;

            switch (model.Grade.Value)
            {
                case CookingMiniGameGrade.Perfect:
                    return presentationSettings != null ? presentationSettings.PositiveColor : Color.yellow;
                case CookingMiniGameGrade.Good:
                    return new Color(0.46f, 0.78f, 0.42f, 1f);
                case CookingMiniGameGrade.Normal:
                    return presentationSettings != null ? presentationSettings.SecondaryTextColor : Color.gray;
                default:
                    return presentationSettings != null ? presentationSettings.NegativeColor : Color.red;
            }
        }

        private void RefreshLayoutHeight()
        {
            EnsureReferences();
            if (layoutElement == null)
                return;

            float feedbackHeight = feedbackField != null && feedbackField.gameObject.activeSelf
                ? feedbackField.GetPreferredValues(feedbackField.text, feedbackField.rectTransform.rect.width, 0f).y
                : 0f;
            float preferredHeight = Mathf.Max(minHeight, minHeight + feedbackHeight + verticalPadding);
            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = preferredHeight;

            RectTransform rowRect = transform as RectTransform;
            if (rowRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
        }

        private void EnsureReferences()
        {
            if (descriptionField == null)
                descriptionField = GetComponentInChildren<TextMeshProUGUI>(true);
            if (layoutElement == null)
                layoutElement = GetComponent<LayoutElement>();
        }

        private void ApplyFont()
        {
            if (presentationSettings?.FontAsset == null)
                return;

            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].font = presentationSettings.FontAsset;
            }
        }

        private static void SetText(TextMeshProUGUI field, string value)
        {
            if (field != null)
                field.text = value ?? string.Empty;
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
                component.gameObject.SetActive(active);
        }
    }
}
