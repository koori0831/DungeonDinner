using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 조리 중 확인하는 주문 명세서 표시
    /// </summary>
    public sealed class CookingOrderNoteView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI bodyField;

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            if (titleField != null)
                titleField.font = value;
            if (bodyField != null)
                bodyField.font = value;
        }

        public void Refresh(CookingGamePanel gamePanel)
        {
            SetText(titleField, "주문 명세서");

            if (gamePanel == null)
            {
                SetText(bodyField, "주문 정보를 불러올 수 없습니다.");
                return;
            }

            CookingGameSnapshot snapshot = gamePanel.CurrentSnapshot;
            NpcDishMatchReport report = snapshot != null ? snapshot.CurrentNpcMatchReport : null;
            NpcOrderContext order = report != null ? report.Order : null;
            StringBuilder builder = new StringBuilder();

            if (order != null)
            {
                builder.AppendLine($"손님: {ValueOrNone(order.NpcId)}");
                AppendList(builder, "필수", order.RequiredTags);
                AppendList(builder, "선호", order.PreferredTags);
                AppendList(builder, "회피", order.AvoidTags);
                AppendList(builder, "위험", order.DisgustingTags);
            }
            else
            {
                builder.AppendLine("현재 NPC 주문 단서를 결과 비교 전까지 제한적으로 표시합니다.");
            }

            if (snapshot != null)
            {
                string recipeName = snapshot.SelectedRecipe != null ? snapshot.SelectedRecipe.DisplayName : "자유 조리";
                builder.AppendLine();
                builder.AppendLine($"레시피: {recipeName}");
                builder.AppendLine($"진행: {snapshot.PreparedIngredientCount} / {snapshot.SelectedIngredientCount}");
            }

            SetText(bodyField, builder.ToString());
        }

        private static void AppendList(StringBuilder builder, string label, IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return;

            builder.Append(label);
            builder.Append(": ");
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                builder.Append(values[i]);
            }
            builder.AppendLine();
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) == true ? "-" : value;
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text ?? string.Empty;
        }
    }
}
