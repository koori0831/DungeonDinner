using System.Collections.Generic;
using UnityEngine;
using Work.Entities.Code;
using Work.Items.Code;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 플레이어 주변 월드 루팅 아이템을 자동으로 인벤토리에 수집
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLootCollector : MonoBehaviour, IEntityModule
    {
        private const float MIN_COLLECT_RADIUS = 0.01f;
        private const float MIN_COLLECT_INTERVAL = 0.01f;
        private const int MIN_LOOT_BUFFER_SIZE = 1;
        private const int DEFAULT_LOOT_BUFFER_SIZE = 16;

        [SerializeField]
        private PlayerInventoryModule inventoryModule;

        [SerializeField]
        private LayerMask lootLayerMask = ~0;

        [SerializeField]
        [Min(MIN_COLLECT_RADIUS)]
        private float collectRadius = 1.5f;

        [SerializeField]
        [Min(MIN_COLLECT_INTERVAL)]
        private float collectInterval = 0.05f;

        [SerializeField]
        [Min(MIN_LOOT_BUFFER_SIZE)]
        private int maxLootColliderCount = DEFAULT_LOOT_BUFFER_SIZE;

        [SerializeField]
        private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

        [SerializeField]
        private bool logLoots = true;

        [Header("Last Loot")]
        [SerializeField]
        private int lastCollectedAmount;

        [SerializeField]
        private int lastRemainingAmount;

        private float _nextCollectTime;
        private bool _loggedMissingInventory;
        private Collider[] _lootColliders;
        private WorldLootItem[] _lootItems;
        private InventoryItemStack[] _lootStacks;
        private InventoryAddResult[] _addResults;
        private readonly Dictionary<int, WorldLootItem> LOOT_BY_COLLIDER_ID = new Dictionary<int, WorldLootItem>();

        /// <summary>
        /// 마지막 자동 루팅으로 인벤토리에 들어간 수량
        /// </summary>
        public int LastCollectedAmount => lastCollectedAmount;

        /// <summary>
        /// 마지막 자동 루팅에서 인벤토리에 들어가지 못한 수량
        /// </summary>
        public int LastRemainingAmount => lastRemainingAmount;

        private void Awake()
        {
            EnsureBuffers();
            ResolveSceneReferences(null);
        }

        private void OnEnable()
        {
            _nextCollectTime = 0f;
        }

        private void Update()
        {
            if (Time.time < _nextCollectTime)
            {
                return;
            }

            _nextCollectTime = Time.time + collectInterval;
            CollectNearbyLoots();
        }

        private void OnDisable()
        {
            LOOT_BY_COLLIDER_ID.Clear();
        }

        /// <summary>
        /// 모듈 소유자 초기화
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티</param>
        public void Initialize(Entity entity)
        {
            ResolveSceneReferences(entity);
            EnsureBuffers();
        }

        /// <summary>
        /// 현재 수집 반경 안의 월드 루팅 아이템을 인벤토리에 추가
        /// </summary>
        /// <returns>인벤토리에 실제로 추가된 총수량</returns>
        public int CollectNearbyLoots()
        {
            ResetLastLootResult();
            ResolveSceneReferences(null);

            if (inventoryModule == null)
            {
                LogMissingInventoryModuleOnce();
                return 0;
            }

            EnsureBuffers();

            int lootCount = FindNearbyLootItems();

            if (lootCount <= 0)
            {
                return 0;
            }

            InventoryBatchAddResult batchResult = inventoryModule.AddItems(_lootStacks, 0, lootCount, _addResults, 0);
            lastCollectedAmount = batchResult.AddedAmount;
            lastRemainingAmount = batchResult.RemainingAmount;

            if (lastCollectedAmount > 0)
            {
                ConsumeCollectedLootItems(lootCount);
                LogLootResult();
            }

            ClearLootBuffers(lootCount);
            return lastCollectedAmount;
        }

        private int FindNearbyLootItems()
        {
            int colliderCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                collectRadius,
                _lootColliders,
                lootLayerMask,
                queryTriggerInteraction
            );

            int lootCount = 0;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider lootCollider = _lootColliders[i];

                if (lootCollider == null)
                {
                    continue;
                }

                WorldLootItem lootItem = GetCachedLootItem(lootCollider);

                if (lootItem == null || lootItem.IsLootable == false)
                {
                    continue;
                }

                if (ContainsLootItem(lootItem, lootCount) == true)
                {
                    continue;
                }

                _lootItems[lootCount] = lootItem;
                _lootStacks[lootCount] = lootItem.CreateItemStack();
                lootCount++;

                if (lootCount >= _lootItems.Length)
                {
                    break;
                }
            }

            return lootCount;
        }

        private void ConsumeCollectedLootItems(int lootCount)
        {
            for (int i = 0; i < lootCount; i++)
            {
                WorldLootItem lootItem = _lootItems[i];
                InventoryAddResult addResult = _addResults[i];

                if (lootItem == null || addResult.AddedAmount <= 0)
                {
                    continue;
                }

                lootItem.ConsumeAmount(addResult.AddedAmount);
            }
        }

        private WorldLootItem GetCachedLootItem(Collider lootCollider)
        {
            int colliderId = lootCollider.GetInstanceID();

            if (LOOT_BY_COLLIDER_ID.TryGetValue(colliderId, out WorldLootItem cachedLootItem) == true)
            {
                if (cachedLootItem != null)
                {
                    return cachedLootItem;
                }

                LOOT_BY_COLLIDER_ID.Remove(colliderId);
            }

            WorldLootItem lootItem = lootCollider.GetComponentInParent<WorldLootItem>();

            if (lootItem != null)
            {
                LOOT_BY_COLLIDER_ID.Add(colliderId, lootItem);
            }

            return lootItem;
        }

        private bool ContainsLootItem(WorldLootItem lootItem, int lootCount)
        {
            for (int i = 0; i < lootCount; i++)
            {
                if (_lootItems[i] == lootItem)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveSceneReferences(Entity entity)
        {
            if (inventoryModule != null)
            {
                _loggedMissingInventory = false;
                return;
            }

            if (entity != null && entity.TryGetModule(out PlayerInventoryModule entityInventoryModule, true) == true)
            {
                inventoryModule = entityInventoryModule;
                _loggedMissingInventory = false;
                return;
            }

            inventoryModule = GetComponent<PlayerInventoryModule>();

            if (inventoryModule != null)
            {
                _loggedMissingInventory = false;
            }
        }

        private void EnsureBuffers()
        {
            maxLootColliderCount = Mathf.Max(MIN_LOOT_BUFFER_SIZE, maxLootColliderCount);

            if (_lootColliders == null || _lootColliders.Length != maxLootColliderCount)
            {
                _lootColliders = new Collider[maxLootColliderCount];
                _lootItems = new WorldLootItem[maxLootColliderCount];
                _lootStacks = new InventoryItemStack[maxLootColliderCount];
                _addResults = new InventoryAddResult[maxLootColliderCount];
                LOOT_BY_COLLIDER_ID.Clear();
            }
        }

        private void ClearLootBuffers(int lootCount)
        {
            for (int i = 0; i < lootCount; i++)
            {
                _lootItems[i] = null;
                _lootStacks[i] = default;
                _addResults[i] = default;
            }
        }

        private void ResetLastLootResult()
        {
            lastCollectedAmount = 0;
            lastRemainingAmount = 0;
        }

        private void LogMissingInventoryModuleOnce()
        {
            if (_loggedMissingInventory == true)
            {
                return;
            }

            _loggedMissingInventory = true;
            LogMissingInventoryModule();
        }

        private void OnValidate()
        {
            collectRadius = Mathf.Max(MIN_COLLECT_RADIUS, collectRadius);
            collectInterval = Mathf.Max(MIN_COLLECT_INTERVAL, collectInterval);
            maxLootColliderCount = Mathf.Max(MIN_LOOT_BUFFER_SIZE, maxLootColliderCount);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectRadius);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogLootResult()
        {
            if (logLoots == false)
            {
                return;
            }

            Debug.Log($"{nameof(PlayerLootCollector)} collected amount={lastCollectedAmount}, remaining={lastRemainingAmount}", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingInventoryModule()
        {
            Debug.LogWarning($"{nameof(PlayerInventoryModule)} is missing. Auto loot skipped.", this);
        }
    }
}
