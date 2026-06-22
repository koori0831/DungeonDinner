using System;
using UnityEngine;
using Work.Items.Code;

namespace Work.Dispatch.Code.Data
{
    /// <summary>
    /// 파견 완료 시 지급할 아이템과 수량 데이터
    /// </summary>
    [Serializable]
    public sealed class DispatchRewardEntry
    {
        private const int MIN_AMOUNT = 1;

        [SerializeField]
        private ItemDataSO item;

        [SerializeField]
        [Min(MIN_AMOUNT)]
        private int amount = MIN_AMOUNT;

        /// <summary>
        /// 지급할 아이템 데이터
        /// </summary>
        public ItemDataSO Item => item;

        /// <summary>
        /// 지급할 아이템 수량
        /// </summary>
        public int Amount => Mathf.Max(MIN_AMOUNT, amount);

        /// <summary>
        /// 지급 가능한 보상 데이터 여부
        /// </summary>
        public bool IsValid => item != null && Amount > 0;

        /// <summary>
        /// 인벤토리에 추가할 아이템 스택 생성
        /// </summary>
        /// <returns>아이템 스택 값</returns>
        public InventoryItemStack CreateItemStack()
        {
            if (IsValid == false)
            {
                return default;
            }

            return new InventoryItemStack(item, Amount);
        }

        /// <summary>
        /// UI 표시용 보상 텍스트 생성
        /// </summary>
        /// <returns>보상 텍스트</returns>
        public string BuildDisplayText()
        {
            string itemName = item != null ? item.DisplayName : "Missing Item";
            return $"{itemName} x{Amount}";
        }
    }
}
