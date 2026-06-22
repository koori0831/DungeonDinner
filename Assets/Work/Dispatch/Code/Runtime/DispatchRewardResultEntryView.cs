using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Items.Code;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 보상 결과 행 프리팹의 표시 바인딩
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DispatchRewardResultEntryView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI itemNameField;
        [SerializeField] private TextMeshProUGUI addedAmountField;
        [SerializeField] private TextMeshProUGUI remainingAmountField;
        [SerializeField] private TextMeshProUGUI currentInventoryAmountField;
        [SerializeField] private TextMeshProUGUI summaryField;
        [SerializeField] private GameObject remainingStateObject;

        [Header("Text")]
        [SerializeField] private string missingItemText = "Missing Item";
        [SerializeField] private string addedPrefix = "+";
        [SerializeField] private string remainingPrefix = "미획득 ";
        [SerializeField] private string inventoryPrefix = "보유 ";

        /// <summary>
        /// 보상 결과 데이터 적용
        /// </summary>
        /// <param name="entry">표시할 보상 결과</param>
        public void Bind(DispatchRewardResultEntry entry)
        {
            ItemDataSO item = entry != null ? entry.Item : null;
            string itemName = item != null ? item.DisplayName : missingItemText;
            int addedAmount = entry != null ? entry.AddedAmount : 0;
            int remainingAmount = entry != null ? entry.RemainingAmount : 0;
            int currentInventoryAmount = entry != null ? entry.CurrentInventoryAmount : 0;

            if (iconImage != null)
            {
                iconImage.sprite = ItemIconUtility.ResolveIcon(item);
                iconImage.preserveAspect = true;
            }

            SetText(itemNameField, itemName);
            SetText(addedAmountField, $"{addedPrefix}{addedAmount}");
            SetText(remainingAmountField, $"{remainingPrefix}{remainingAmount}");
            SetText(currentInventoryAmountField, $"{inventoryPrefix}{currentInventoryAmount}");
            SetText(summaryField, BuildSummaryText(itemName, addedAmount, remainingAmount, currentInventoryAmount));
            SetActive(remainingStateObject, remainingAmount > 0);
        }

        private string BuildSummaryText(string itemName, int addedAmount, int remainingAmount, int currentInventoryAmount)
        {
            if (remainingAmount > 0)
            {
                return $"{itemName}  {addedPrefix}{addedAmount} / {remainingPrefix}{remainingAmount}  {inventoryPrefix}{currentInventoryAmount}";
            }

            return $"{itemName}  {addedPrefix}{addedAmount}  {inventoryPrefix}{currentInventoryAmount}";
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
            {
                field.text = text ?? string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
