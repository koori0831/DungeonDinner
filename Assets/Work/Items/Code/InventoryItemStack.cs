namespace Work.Items.Code
{
    /// <summary>
    /// 아이템 데이터와 수량을 함께 전달하는 값
    /// </summary>
    public readonly struct InventoryItemStack
    {
        public readonly ItemDataSO Item;
        public readonly int Amount;

        /// <summary>
        /// 아이템 스택 생성
        /// </summary>
        /// <param name="item">아이템 데이터</param>
        /// <param name="amount">아이템 수량</param>
        public InventoryItemStack(ItemDataSO item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        /// <summary>
        /// 유효한 아이템 스택 여부
        /// </summary>
        public bool IsValid => Item != null && Amount > 0;
    }
}
