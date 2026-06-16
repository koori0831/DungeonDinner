using UnityEngine;
using Work.Combat.Code.Core;
using Work.Core.ObjectPool.RunTime;
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
        private PoolManagerSO lootPoolManager;

        [SerializeField]
        [Min(0f)]
        private float spawnRadius = 0.45f;

        [SerializeField]
        private float spawnHeightOffset = 0.15f;

        [SerializeField]
        [Min(MIN_VISUAL_SCALE)]
        private float fallbackVisualScale = 0.35f;

        [Header("Drop Arc")]
        [SerializeField]
        private bool playDropArc = true;

        [SerializeField]
        [Min(0f)]
        private float dropArcDistance = 1.25f;

        [SerializeField]
        [Min(0f)]
        private float dropArcHeight = 0.75f;

        [SerializeField]
        [Min(0.01f)]
        private float dropArcDuration = 0.45f;

        [SerializeField]
        [Min(0f)]
        private float dropStartHeightOffset = 0.05f;

        [SerializeField]
        [Min(0f)]
        private float groundRaycastStartHeight = 2f;

        [SerializeField]
        [Min(0.01f)]
        private float groundRaycastDistance = 6f;

        [SerializeField]
        private LayerMask groundLayerMask = ~0;

        [SerializeField]
        private bool logSpawns = true;

        [SerializeField]
        private int lastSpawnCount;

        private static Transform _sharedPoolRoot;

        /// <summary>
        /// 마지막 월드 드랍 생성 수
        /// </summary>
        public int LastSpawnCount => lastSpawnCount;

        private void Awake()
        {
            InitializeLootPool();
        }

        /// <summary>
        /// 드랍 결과 배열을 월드 루팅 아이템으로 생성
        /// </summary>
        /// <param name="dropResults">생성할 드랍 결과 배열</param>
        /// <param name="dropCount">처리할 드랍 결과 수</param>
        /// <returns>생성된 월드 루팅 아이템 수</returns>
        public int SpawnDrops(EnemyDropResult[] dropResults, int dropCount)
        {
            HitContext hitContext = default;
            return SpawnDropsInternal(dropResults, dropCount, false, in hitContext);
        }

        /// <summary>
        /// 피격 정보 기반으로 드랍 결과 배열을 월드 루팅 아이템으로 생성
        /// </summary>
        /// <param name="dropResults">생성할 드랍 결과 배열</param>
        /// <param name="dropCount">처리할 드랍 결과 수</param>
        /// <param name="hitContext">이번 피격 정보</param>
        /// <returns>생성된 월드 루팅 아이템 수</returns>
        public int SpawnDrops(EnemyDropResult[] dropResults, int dropCount, in HitContext hitContext)
        {
            return SpawnDropsInternal(dropResults, dropCount, true, in hitContext);
        }

        private int SpawnDropsInternal(EnemyDropResult[] dropResults, int dropCount, bool useHitContext, in HitContext hitContext)
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

                Vector3 spawnPosition = GetDropStartPosition(i, validCount, useHitContext, in hitContext);
                Vector3 landingPosition = GetDropLandingPosition(i, validCount, spawnPosition, useHitContext, in hitContext);
                WorldLootItem lootItem = CreateLootItem(dropResult, spawnPosition);

                if (lootItem == null)
                {
                    continue;
                }

                lootItem.Initialize(dropResult.Item, dropResult.Amount);

                if (useHitContext == true && playDropArc == true)
                {
                    lootItem.PlayDropArc(spawnPosition, landingPosition, dropArcHeight, dropArcDuration);
                }
                lastSpawnCount++;
            }

            LogSpawnResult();
            return lastSpawnCount;
        }

        private WorldLootItem CreateLootItem(in EnemyDropResult dropResult, Vector3 spawnPosition)
        {
            WorldLootItem pooledLootItem = CreatePooledLootItem(dropResult, spawnPosition);

            if (pooledLootItem != null)
            {
                return pooledLootItem;
            }

            return CreateFallbackLootItem(spawnPosition);
        }

        private WorldLootItem CreatePooledLootItem(in EnemyDropResult dropResult, Vector3 spawnPosition)
        {
            if (lootPoolManager == null || dropResult.Item == null)
            {
                return null;
            }

            PoolItemSO poolItem = dropResult.Item.WorldLootPoolItem;

            if (poolItem == null)
            {
                return null;
            }

            InitializeLootPool();
            WorldLootItem lootItem = lootPoolManager.Pop(poolItem) as WorldLootItem;

            if (lootItem == null)
            {
                return null;
            }

            lootItem.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            return lootItem;
        }

        private WorldLootItem CreateFallbackLootItem(Vector3 spawnPosition)
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

        private void InitializeLootPool()
        {
            if (lootPoolManager == null || lootPoolManager.IsInitialized == true)
            {
                return;
            }

            lootPoolManager.InitializePool(GetSharedPoolRoot());
        }

        private static Transform GetSharedPoolRoot()
        {
            if (_sharedPoolRoot != null)
            {
                return _sharedPoolRoot;
            }

            GameObject poolRootObject = new GameObject("WorldLootPoolRoot");
            _sharedPoolRoot = poolRootObject.transform;
            return _sharedPoolRoot;
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

        private Vector3 GetDropStartPosition(int index, int resultCount, bool useHitContext, in HitContext hitContext)
        {
            if (useHitContext == false || playDropArc == false)
            {
                return GetSpawnPosition(index, resultCount);
            }

            return hitContext.HitPoint + Vector3.up * dropStartHeightOffset;
        }

        private Vector3 GetDropLandingPosition(
            int index,
            int resultCount,
            Vector3 startPosition,
            bool useHitContext,
            in HitContext hitContext
        )
        {
            if (useHitContext == false || playDropArc == false)
            {
                return startPosition;
            }

            Vector3 dropDirection = GetDropDirection(hitContext.HitDirection);
            Vector3 sideOffset = GetDropSideOffset(index, resultCount, dropDirection);
            Vector3 candidatePosition = startPosition + dropDirection * dropArcDistance + sideOffset;
            return GetGroundedDropPosition(candidatePosition);
        }

        private Vector3 GetDropDirection(Vector3 hitDirection)
        {
            hitDirection.y = 0f;

            if (hitDirection.sqrMagnitude > 0.0001f)
            {
                return hitDirection.normalized;
            }

            Vector3 fallbackDirection = dropOrigin != null ? dropOrigin.forward : transform.forward;
            fallbackDirection.y = 0f;

            if (fallbackDirection.sqrMagnitude > 0.0001f)
            {
                return fallbackDirection.normalized;
            }

            return Vector3.forward;
        }

        private Vector3 GetDropSideOffset(int index, int resultCount, Vector3 dropDirection)
        {
            if (resultCount <= 1 || spawnRadius <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 sideDirection = Vector3.Cross(Vector3.up, dropDirection);

            if (sideDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            float centeredIndex = index - (resultCount - 1) * 0.5f;
            return sideDirection.normalized * (centeredIndex * spawnRadius);
        }

        private Vector3 GetGroundedDropPosition(Vector3 candidatePosition)
        {
            Vector3 rayStartPosition = candidatePosition + Vector3.up * groundRaycastStartHeight;
            float raycastDistance = groundRaycastStartHeight + groundRaycastDistance;

            if (Physics.Raycast(
                rayStartPosition,
                Vector3.down,
                out RaycastHit hit,
                raycastDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore
            ) == true)
            {
                return hit.point + Vector3.up * spawnHeightOffset;
            }

            candidatePosition.y = transform.position.y + spawnHeightOffset;
            return candidatePosition;
        }

        private void OnValidate()
        {
            spawnRadius = Mathf.Max(0f, spawnRadius);
            fallbackVisualScale = Mathf.Max(MIN_VISUAL_SCALE, fallbackVisualScale);
            dropArcDistance = Mathf.Max(0f, dropArcDistance);
            dropArcHeight = Mathf.Max(0f, dropArcHeight);
            dropArcDuration = Mathf.Max(0.01f, dropArcDuration);
            dropStartHeightOffset = Mathf.Max(0f, dropStartHeightOffset);
            groundRaycastStartHeight = Mathf.Max(0f, groundRaycastStartHeight);
            groundRaycastDistance = Mathf.Max(0.01f, groundRaycastDistance);
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
