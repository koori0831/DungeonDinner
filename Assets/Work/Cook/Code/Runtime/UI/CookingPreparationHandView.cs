using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Systems;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 현재 재료에 가능한 손질 카드 손패 표시
    /// </summary>
    public sealed class CookingPreparationHandView : MonoBehaviour
    {
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private CookingPreparationOptionCardView preparationOptionCardPrefab;
        [SerializeField] private CanvasGroup cardGroup;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private string noOptionText = "이 재료에는 등록된 손질법이 없습니다.";
        [SerializeField] private string noOptionButtonText = "그대로 진행";
        [SerializeField] private string unknownEffectText = "아직 결과를 모릅니다.";
        [SerializeField] private string knownEffectTitleText = "확인한 효과";

        private CookingKnowledgeStore _knowledgeStore;

        public void Initialize(CookingGamePanel owner, CookingKnowledgeStore knowledge, TMP_FontAsset defaultFontAsset)
        {
            _knowledgeStore = knowledge;
            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);
            EnsureReferences();
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            ApplyFontToExistingTexts();
        }

        public void Rebuild(
            IngredientSO ingredient,
            IReadOnlyList<IngredientPreparationOption> options,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            EnsureReferences();
            ClearChildren(cardRoot);
            SetInteractable(true);

            if (cardRoot == null || ingredient == null)
                return;

            if (options == null || options.Count == 0)
            {
                CreateNoOptionCard(ingredient, selected);
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                if (option == null)
                    continue;

                CreatePreparationCard(ingredient, option, i, selected);
            }
        }

        public void SetInteractable(bool interactable)
        {
            EnsureReferences();
            if (cardGroup == null)
                return;

            cardGroup.alpha = interactable == true ? 1f : 0.45f;
            cardGroup.interactable = interactable;
            cardGroup.blocksRaycasts = interactable;
        }

        private void CreateNoOptionCard(
            IngredientSO ingredient,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            if (preparationOptionCardPrefab == null)
            {
                Debug.LogError("CookingPreparationHandView preparationOptionCardPrefab is missing.", this);
                return;
            }

            CookingPreparationOptionCardView view = Instantiate(preparationOptionCardPrefab, cardRoot);
            view.Bind(
                string.Empty,
                null,
                noOptionButtonText,
                noOptionText,
                string.Empty,
                "선택",
                false,
                () => selected?.Invoke(ingredient, null));
            ApplyFont(view.gameObject);
        }

        private void CreatePreparationCard(
            IngredientSO ingredient,
            IngredientPreparationOption option,
            int index,
            Action<IngredientSO, IngredientPreparationOption> selected)
        {
            if (preparationOptionCardPrefab == null)
            {
                Debug.LogError("CookingPreparationHandView preparationOptionCardPrefab is missing.", this);
                return;
            }

            CookingPreparationOptionCardView view = Instantiate(preparationOptionCardPrefab, cardRoot);
            Sprite icon = option != null && option.Method != null ? option.Method.IconSprite : null;
            view.Bind(
                BuildOptionIconText(index, option),
                icon,
                option.DisplayName,
                BuildOptionDescription(option),
                BuildKnownEffectText(ingredient, option),
                "선택",
                true,
                () => selected?.Invoke(ingredient, option));
            ApplyFont(view.gameObject);
        }

        private string BuildKnownEffectText(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (option == null)
                return unknownEffectText;

            if (_knowledgeStore == null || _knowledgeStore.IsPreparationEffectKnown(ingredient, option) == false)
                return unknownEffectText;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(knownEffectTitleText);

            if (option.QualityDelta != 0)
                builder.AppendLine($"품질 변화: {option.QualityDelta:+#;-#;0}");

            AppendTags(builder, "추가 태그", option.AddTags);
            AppendTags(builder, "제거 태그", option.RemoveTags);

            if (string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
                builder.AppendLine($"이름 변화: {option.ResultNameModifier}");

            if (option.CausesDisgusting == true)
                builder.AppendLine("괴식 위험이 있습니다.");

            if (option.AddsPoison == true)
                builder.AppendLine("독성이 추가됩니다.");

            return builder.Length > knownEffectTitleText.Length + 1
                ? builder.ToString()
                : $"{knownEffectTitleText}\n특별한 변화 없음";
        }

        private static string BuildOptionDescription(IngredientPreparationOption option)
        {
            if (option == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(option.Description) == false)
                return option.Description;

            if (option.Method != null && string.IsNullOrWhiteSpace(option.Method.Description) == false)
                return option.Method.Description;

            return "이 방식으로 재료를 손질합니다.";
        }

        private static string BuildOptionIconText(int index, IngredientPreparationOption option)
        {
            if (option != null && option.Method != null && string.IsNullOrWhiteSpace(option.Method.MethodId) == false)
                return option.Method.MethodId.Substring(0, 1).ToUpperInvariant();

            return (index + 1).ToString();
        }

        private static void AppendTags(StringBuilder builder, string label, IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return;

            builder.Append(label);
            builder.Append(": ");
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == null)
                    continue;

                if (i > 0)
                    builder.Append(", ");
                builder.Append(tags[i].DisplayName);
            }

            builder.AppendLine();
        }

        private void EnsureReferences()
        {
            if (cardRoot == null)
                cardRoot = transform as RectTransform;
            if (cardGroup == null)
                cardGroup = GetComponent<CanvasGroup>();
            if (cardGroup == null)
                Debug.LogError("CookingPreparationHandView needs a CanvasGroup assigned or attached to the same GameObject.", this);
        }

        private void ApplyFontToExistingTexts()
        {
            ApplyFont(gameObject);
        }

        private void ApplyFont(GameObject target)
        {
            if (fontAsset == null || target == null)
                return;

            TextMeshProUGUI[] labels = target.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].font = fontAsset;
            }
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                child.SetParent(null, false);
                if (Application.isPlaying == true)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
