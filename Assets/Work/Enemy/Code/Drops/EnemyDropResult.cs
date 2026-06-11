namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 적 피격으로 계산된 드랍 결과
    /// </summary>
    public readonly struct EnemyDropResult
    {
        public readonly EnemyDropItemSO Item;
        public readonly int Amount;

        /// <summary>
        /// 드랍 결과 생성
        /// </summary>
        /// <param name="item">드랍 아이템</param>
        /// <param name="amount">드랍 수량</param>
        public EnemyDropResult(EnemyDropItemSO item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }
}
