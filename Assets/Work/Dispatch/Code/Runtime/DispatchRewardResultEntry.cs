using Work.Items.Code;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 보상 아이템별 인벤토리 지급 결과
    /// </summary>
    public sealed class DispatchRewardResultEntry
    {
        /// <summary>
        /// 지급 대상 아이템
        /// </summary>
        public ItemDataSO Item { get; }

        /// <summary>
        /// 지급 요청 수량
        /// </summary>
        public int RequestedAmount { get; }

        /// <summary>
        /// 실제 인벤토리에 추가된 수량
        /// </summary>
        public int AddedAmount { get; }

        /// <summary>
        /// 인벤토리에 추가되지 못한 수량
        /// </summary>
        public int RemainingAmount { get; }

        /// <summary>
        /// 지급 처리 후 현재 인벤토리 보유 수량
        /// </summary>
        public int CurrentInventoryAmount { get; }

        /// <summary>
        /// 파견 보상 결과 항목 생성
        /// </summary>
        public DispatchRewardResultEntry(
            ItemDataSO item,
            int requestedAmount,
            int addedAmount,
            int remainingAmount,
            int currentInventoryAmount)
        {
            Item = item;
            RequestedAmount = requestedAmount;
            AddedAmount = addedAmount;
            RemainingAmount = remainingAmount;
            CurrentInventoryAmount = currentInventoryAmount;
        }
    }
}
