using UnityEngine;
using Work.Items.Code;

namespace Work.Enemy.Code.Drops
{
    /// <summary>
    /// 계산된 적 드랍 결과를 월드 루팅 아이템으로 생성
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyDropWorldSpawner : MonoBehaviour
    {
        private const float MIN_VISUAL_SCALE = 0.01f;

        [SerializeField]
        private Transform dropOrigin;

        [SerializeField]
        private WorldLootItem lootPrefab;

        [SerializeField]
        [Min(0f)]
        private float spawnRadius = 0.45f;

        [SerializeField]
        private float spawnHeightOffset = 0.15f;

        [SerializeField]
        [Min(MIN_VISUAL_SCALE)]
        private float fallbackVisualScale = 0.35f;

        [SerializeField]
        private bool logSpawns = true;

        [SerializeField]
        private int lastSpawnCount;

        /// <summary>
        /// 마지막 월드 드랍 생성 수
        /// </summary>
        public int LastSpawnCount => lastSpawnCount;

        /// <summary>
        /// 드랍 결과 배열을 월드 루팅 아이템으로 생성
        /// </summary>
        /// <param name="dropResults">생성할 드랍 결과 배열</param>
        /// <param name="dropCount">처리할 드랍 결과 수</param>
        /// <returns>생성된 월드 루팅 아이템 수</returns>
        public int SpawnDrops(EnemyDropResult[] dropResults, int dropCount)
        {
            lastSpawnCount = 0;

            if (dropResults == null || dropCount <= 0)
            {
                return 0;
            }

            int validCount = Mathf.Min(dropCount, dropResults.Length);

            for (int i = 0; i < validCount; i++)
            {
                EnemyDropResult dropResult = dropResults[i];

                if (dropResult.Item == null || dropResult.Amount <= 0)
                {
                    continue;
                }

                Vector3 spawnPosition = GetSpawnPosition(i, validCount);
                WorldLootItem lootItem = CreateLootItem(spawnPosition);

                if (lootItem == null)
                {
                    continue;
                }

                lootItem.Initialize(dropResult.Item, dropResult.Amount);
                lastSpawnCount++;
            }

            LogSpawnResult();
            return lastSpawnCount;
        }

        private WorldLootItem CreateLootItem(Vector3 spawnPosition)
        {
            if (lootPrefab != null)
            {
                return Instantiate(lootPrefab, spawnPosition, Quaternion.identity);
            }

            GameObject lootObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lootObject.transform.position = spawnPosition;
            lootObject.transform.localScale = Vector3.one * fallbackVisualScale;

            Collider lootCollider = lootObject.GetComponent<Collider>();

            if (lootCollider != null)
            {
                lootCollider.isTrigger = true;
            }

            return lootObject.AddComponent<WorldLootItem>();
        }

        private Vector3 GetSpawnPosition(int index, int resultCount)
        {
            Vector3 originPosition = dropOrigin != null ? dropOrigin.position : transform.position;
            Vector3 heightOffset = Vector3.up * spawnHeightOffset;

            if (resultCount <= 1 || spawnRadius <= 0f)
            {
                return originPosition + heightOffset;
            }

            float angle = 360f * index / resultCount;
            float radian = angle * Mathf.Deg2Rad;
            Vector3 horizontalOffset = new Vector3(Mathf.Cos(radian), 0f, Mathf.Sin(radian)) * spawnRadius;
            return originPosition + heightOffset + horizontalOffset;
        }

        private void OnValidate()
        {
            spawnRadius = Mathf.Max(0f, spawnRadius);
            fallbackVisualScale = Mathf.Max(MIN_VISUAL_SCALE, fallbackVisualScale);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogSpawnResult()
        {
            if (logSpawns == false || lastSpawnCount <= 0)
            {
                return;
            }

            Debug.Log($"{nameof(EnemyDropWorldSpawner)} spawned loot count={lastSpawnCount}", this);
        }
    }
}
