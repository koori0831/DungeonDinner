using Work.Items.Code;

namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 적 피격으로 계산된 드랍 결과
    /// </summary>
    public readonly struct EnemyDropResult
    {
        public readonly ItemDataSO Item;
        public readonly int Amount;

        /// <summary>
        /// 드랍 결과 생성
        /// </summary>
        /// <param name="item">드랍 아이템</param>
        /// <param name="amount">드랍 수량</param>
        public EnemyDropResult(ItemDataSO item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        /// <summary>
        /// 인벤토리 추가용 공용 아이템 스택으로 변환
        /// </summary>
        /// <returns>아이템 스택 값</returns>
        public InventoryItemStack ToInventoryItemStack()
        {
            return new InventoryItemStack(Item, Amount);
        }
    }
}
