using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 조리 중 확인하는 구조화된 주문 명세서 표시.
    /// </summary>
    public sealed class CookingOrderNoteView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI npcNameField;
        [SerializeField] private TextMeshProUGUI recipeField;
        [SerializeField] private TextMeshProUGUI bodyField;
        [SerializeField] private TextMeshProUGUI emptyStateField;
        [SerializeField] private RectTransform chipRoot;
        [SerializeField] private CookingUiChipView chipTemplate;
        [SerializeField] private CookingUiPresentationSettingsSO presentationSettings;
        [SerializeField] private CookingDataCatalogSO fallbackCatalog;

        public void SetPresentationSettings(CookingUiPresentationSettingsSO value)
        {
            presentationSettings = value;
            ApplyFont();
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            SetFont(titleField, value);
            SetFont(npcNameField, value);
            SetFont(recipeField, value);
            SetFont(bodyField, value);
            SetFont(emptyStateField, value);
        }

        public void Refresh(CookingGamePanel gamePanel)
        {
            SetText(titleField, "주문서");

            CookingGameSnapshot snapshot = gamePanel?.CurrentSnapshot;
            NpcOrderContext order = ResolveOrder(gamePanel, snapshot);
            CookingDataCatalogSO catalog = gamePanel?.FlowRunner?.Catalog ?? fallbackCatalog;
            Func<string, string> npcNameResolver = gamePanel?.NpcRunner != null
                ? gamePanel.NpcRunner.GetNpcDisplayName
                : (Func<string, string>)null;
            CookingOrderPresentationModel model = CookingResultPresentationBuilder.BuildOrder(
                snapshot,
                order,
                catalog,
                npcNameResolver);

            BindModel(model);
        }

        private void BindModel(CookingOrderPresentationModel model)
        {
            bool hasOrder = model != null && model.HasOrder;
            SetText(npcNameField, hasOrder ? model.NpcName : string.Empty);
            SetText(recipeField, model != null ? $"요리 방향 · {model.RecipeName}" : string.Empty);
            SetActive(emptyStateField, hasOrder == false);
            SetText(emptyStateField, model?.EmptyMessage ?? "주문 정보를 불러올 수 없습니다.");

            if (chipRoot != null && chipTemplate != null)
            {
                RebuildChips(model?.Tags);
                SetText(bodyField, string.Empty);
                SetActive(bodyField, false);
            }
            else
            {
                SetActive(bodyField, true);
                SetText(bodyField, BuildFallbackText(model));
            }
        }

        private void RebuildChips(IReadOnlyList<CookingTagChipModel> chips)
        {
            ClearChipInstances();
            int count = chips?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                CookingUiChipView chip = Instantiate(chipTemplate, chipRoot);
                chip.gameObject.name = $"OrderTagChip{i + 1}";
                chip.Bind(chips[i], presentationSettings);
                chip.gameObject.SetActive(true);
            }

            if (chipRoot != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(chipRoot);
            }
        }

        private void ClearChipInstances()
        {
            if (chipRoot == null)
                return;

            for (int i = chipRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = chipRoot.GetChild(i);
                if (chipTemplate != null && child == chipTemplate.transform)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private static NpcOrderContext ResolveOrder(CookingGamePanel gamePanel, CookingGameSnapshot snapshot)
        {
            NpcOrderContext order = snapshot?.CurrentNpcMatchReport?.Order;
            if (order != null)
                return order;

            if (gamePanel?.NpcRunner != null
                && gamePanel.NpcRunner.TryGetCurrentOrderContext(out NpcOrderContext currentOrder))
            {
                return currentOrder;
            }

            return null;
        }

        private static string BuildFallbackText(CookingOrderPresentationModel model)
        {
            if (model == null || model.HasOrder == false)
                return model?.EmptyMessage ?? "주문 정보를 불러올 수 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(model.NpcName);
            builder.AppendLine($"요리 방향 · {model.RecipeName}");
            for (int i = 0; i < model.Tags.Count; i++)
                builder.AppendLine($"{BuildKindLabel(model.Tags[i].Kind)} · {model.Tags[i].DisplayName}");
            return builder.ToString();
        }

        private static string BuildKindLabel(CookingTagPresentationKind kind)
        {
            switch (kind)
            {
                case CookingTagPresentationKind.Required:
                    return "필수";
                case CookingTagPresentationKind.Preferred:
                    return "선호";
                case CookingTagPresentationKind.Avoid:
                    return "회피";
                default:
                    return "위험";
            }
        }

        private void ApplyFont()
        {
            if (presentationSettings?.FontAsset != null)
                SetFontAsset(presentationSettings.FontAsset);
        }

        private static void SetFont(TextMeshProUGUI field, TMP_FontAsset font)
        {
            if (field != null)
                field.font = font;
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null && target.gameObject.activeSelf != active)
                target.gameObject.SetActive(active);
        }
    }
}
