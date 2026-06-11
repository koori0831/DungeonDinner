using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 적 피격 드랍 테이블의 단일 후보 항목
    /// </summary>
    [Serializable]
    public sealed class EnemyDropEntry
    {
        private const int MIN_AMOUNT = 1;

        [SerializeField]
        private EnemyDropItemSO item;

        [SerializeField]
        [Min(MIN_AMOUNT)]
        private int minAmount = 1;

        [SerializeField]
        [Min(MIN_AMOUNT)]
        private int maxAmount = 1;

        [SerializeField]
        [Range(0f, 1f)]
        private float dropChance = 1f;

        /// <summary>
        /// 드랍 후보 데이터 정규화
        /// </summary>
        public void Validate()
        {
            minAmount = Mathf.Max(MIN_AMOUNT, minAmount);
            maxAmount = Mathf.Max(minAmount, maxAmount);
            dropChance = Mathf.Clamp01(dropChance);
        }

        /// <summary>
        /// 확률과 수량 범위 기반 드랍 결과 생성 시도
        /// </summary>
        /// <param name="result">생성된 드랍 결과</param>
        /// <returns>드랍 발생 여부</returns>
        public bool TryCreateDropResult(out EnemyDropResult result)
        {
            result = default;

            if (item == null || dropChance <= 0f)
            {
                return false;
            }

            if (UnityEngine.Random.value > dropChance)
            {
                return false;
            }

            int amount = UnityEngine.Random.Range(minAmount, maxAmount + 1);
            result = new EnemyDropResult(item, amount);
            return true;
        }
    }
}
