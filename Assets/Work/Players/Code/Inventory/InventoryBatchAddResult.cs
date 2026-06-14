namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 인벤토리 아이템 묶음 추가 요청 결과
    /// </summary>
    public readonly struct InventoryBatchAddResult
    {
        public readonly int RequestedStackCount;
        public readonly int FullyAddedStackCount;
        public readonly int AddedAmount;
        public readonly int RemainingAmount;

        /// <summary>
        /// 아이템 묶음 추가 결과 생성
        /// </summary>
        /// <param name="requestedStackCount">유효한 추가 요청 스택 수</param>
        /// <param name="fullyAddedStackCount">전체 추가된 스택 수</param>
        /// <param name="addedAmount">실제 추가 총수량</param>
        /// <param name="remainingAmount">남은 미추가 총수량</param>
        public InventoryBatchAddResult(int requestedStackCount, int fullyAddedStackCount, int addedAmount, int remainingAmount)
        {
            RequestedStackCount = requestedStackCount;
            FullyAddedStackCount = fullyAddedStackCount;
            AddedAmount = addedAmount;
            RemainingAmount = remainingAmount;
        }

        /// <summary>
        /// 유효한 요청 스택 전체 추가 여부
        /// </summary>
        public bool IsFullyAdded => RequestedStackCount > 0 && RemainingAmount <= 0;
    }
}
