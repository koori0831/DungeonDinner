using UnityEngine;

namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 적 피격 시 계산할 드랍 후보 테이블
    /// </summary>
    [CreateAssetMenu(menuName = "Enemy/Drop/Table")]
    public sealed class EnemyDropTableSO : ScriptableObject
    {
        [SerializeField]
        private EnemyDropEntry[] entries;

        /// <summary>
        /// 드랍 테이블을 굴려 결과 배열에 저장
        /// </summary>
        /// <param name="results">드랍 결과 배열</param>
        /// <param name="startIndex">저장을 시작할 인덱스</param>
        /// <returns>추가된 드랍 결과 수</returns>
        public int RollDrops(EnemyDropResult[] results, int startIndex)
        {
            if (results == null || entries == null || startIndex >= results.Length)
            {
                return 0;
            }

            int dropCount = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                EnemyDropEntry entry = entries[i];

                if (entry == null)
                {
                    continue;
                }

                if (entry.TryCreateDropResult(out EnemyDropResult result) == false)
                {
                    continue;
                }

                int resultIndex = startIndex + dropCount;

                if (resultIndex >= results.Length)
                {
                    break;
                }

                results[resultIndex] = result;
                dropCount++;
            }

            return dropCount;
        }

        private void OnValidate()
        {
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                EnemyDropEntry entry = entries[i];

                if (entry == null)
                {
                    continue;
                }

                entry.Validate();
            }
        }
    }
}
