using Work.Items.Code;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 인벤토리 아이템 추가 요청 결과
    /// </summary>
    public readonly struct InventoryAddResult
    {
        public readonly ItemDataSO Item;
        public readonly int RequestedAmount;
        public readonly int AddedAmount;
        public readonly int RemainingAmount;

        /// <summary>
        /// 아이템 추가 결과 생성
        /// </summary>
        /// <param name="item">추가 대상 아이템 데이터</param>
        /// <param name="requestedAmount">추가 요청 수량</param>
        /// <param name="addedAmount">실제 추가 수량</param>
        /// <param name="remainingAmount">남은 미추가 수량</param>
        public InventoryAddResult(ItemDataSO item, int requestedAmount, int addedAmount, int remainingAmount)
        {
            Item = item;
            RequestedAmount = requestedAmount;
            AddedAmount = addedAmount;
            RemainingAmount = remainingAmount;
        }

        /// <summary>
        /// 요청 수량 전체 추가 여부
        /// </summary>
        public bool IsFullyAdded => RequestedAmount > 0 && RemainingAmount <= 0;
    }
}
