using System;
using UnityEngine;
using Work.Items.Code;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 플레이어 인벤토리의 단일 슬롯 데이터
    /// </summary>
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField]
        private ItemDataSO item;

        [SerializeField]
        [Min(0)]
        private int amount;

        /// <summary>
        /// 슬롯에 담긴 아이템 데이터
        /// </summary>
        public ItemDataSO Item => item;

        /// <summary>
        /// 슬롯에 담긴 아이템 수량
        /// </summary>
        public int Amount => amount;

        /// <summary>
        /// 빈 슬롯 여부
        /// </summary>
        public bool IsEmpty => item == null || amount <= 0;

        /// <summary>
        /// 지정한 아이템이 슬롯의 현재 아이템과 같은지 반환
        /// </summary>
        /// <param name="targetItem">비교할 아이템 데이터</param>
        /// <returns>동일 아이템 여부</returns>
        public bool Contains(ItemDataSO targetItem)
        {
            if (targetItem == null)
            {
                return false;
            }

            return item == targetItem && IsEmpty == false;
        }

        /// <summary>
        /// 지정한 아이템을 이 슬롯에 추가할 수 있는지 반환
        /// </summary>
        /// <param name="targetItem">추가할 아이템 데이터</param>
        /// <returns>추가 가능 여부</returns>
        public bool CanAccept(ItemDataSO targetItem)
        {
            if (targetItem == null)
            {
                return false;
            }

            if (IsEmpty == true)
            {
                return true;
            }

            if (item.CanStackWith(targetItem) == false)
            {
                return false;
            }

            return amount < item.MaxStackAmount;
        }

        /// <summary>
        /// 슬롯에 아이템 수량 추가
        /// </summary>
        /// <param name="targetItem">추가할 아이템 데이터</param>
        /// <param name="requestedAmount">추가 요청 수량</param>
        /// <returns>실제로 추가된 수량</returns>
        public int Add(ItemDataSO targetItem, int requestedAmount)
        {
            if (targetItem == null || requestedAmount <= 0)
            {
                return 0;
            }

            if (IsEmpty == true)
            {
                item = targetItem;
                amount = 0;
            }
            else if (item.CanStackWith(targetItem) == false)
            {
                return 0;
            }

            int capacity = item.MaxStackAmount - amount;

            if (capacity <= 0)
            {
                return 0;
            }

            int addedAmount = Mathf.Min(capacity, requestedAmount);
            amount += addedAmount;
            return addedAmount;
        }

        /// <summary>
        /// 슬롯에서 아이템 수량 제거
        /// </summary>
        /// <param name="requestedAmount">제거 요청 수량</param>
        /// <returns>실제로 제거된 수량</returns>
        public int Remove(int requestedAmount)
        {
            if (IsEmpty == true || requestedAmount <= 0)
            {
                return 0;
            }

            int removedAmount = Mathf.Min(amount, requestedAmount);
            amount -= removedAmount;

            if (amount <= 0)
            {
                Clear();
            }

            return removedAmount;
        }

        /// <summary>
        /// 슬롯 비우기
        /// </summary>
        public void Clear()
        {
            item = null;
            amount = 0;
        }

        /// <summary>
        /// 직렬화된 슬롯 데이터 정규화
        /// </summary>
        public void Validate()
        {
            if (item == null || amount <= 0)
            {
                Clear();
                return;
            }

            amount = Mathf.Clamp(amount, 1, item.MaxStackAmount);
        }
    }
}
