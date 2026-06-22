using System.Collections.Generic;
using UnityEngine;
using Work.Core.EventBus;
using Work.Entities.Code;
using Work.Items.Code;
using static Work.Items.Code.WorldLootEvents;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 감지 이벤트로 들어온 월드 루팅 아이템을 플레이어 인벤토리에 수집
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLootCollector : MonoBehaviour, IEntityModule
    {
        [SerializeField]
        private PlayerInventoryModule inventoryModule;

        [SerializeField]
        private CharacterController collectorController;

        [SerializeField]
        private bool logLoots = true;

        [Header("Last Loot")]
        [SerializeField]
        private int lastCollectedAmount;

        [SerializeField]
        private int lastRemainingAmount;

        private bool _loggedMissingInventory;
        private bool _loggedMissingController;
        private bool _isSubscribedToLootEvents;
        private bool _isCollecting;
        private PlayerInventoryModule _subscribedInventoryModule;
        private readonly List<WorldLootItem> NEARBY_LOOT_ITEMS = new List<WorldLootItem>();

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
            ResolveSceneReferences(null);
        }

        private void OnEnable()
        {
            ResolveSceneReferences(null);
            SubscribeLootEvents();
            SubscribeInventory();
        }

        private void OnDisable()
        {
            UnsubscribeLootEvents();
            UnsubscribeInventory();
            NEARBY_LOOT_ITEMS.Clear();
            _isCollecting = false;
        }

        /// <summary>
        /// 모듈 소유자 초기화
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티</param>
        public void Initialize(Entity entity)
        {
            ResolveSceneReferences(entity);

            if (isActiveAndEnabled == true)
            {
                SubscribeInventory();
            }
        }

        /// <summary>
        /// 현재 감지 후보에 있는 월드 루팅 아이템을 인벤토리에 추가
        /// </summary>
        /// <returns>인벤토리에 실제로 추가된 총수량</returns>
        public int CollectNearbyLoots()
        {
            ResetLastLootResult();
            ResolveSceneReferences(null);
            SubscribeInventory();

            if (inventoryModule == null)
            {
                LogMissingInventoryModuleOnce();
                return 0;
            }

            if (collectorController == null)
            {
                LogMissingControllerOnce();
                return 0;
            }

            if (NEARBY_LOOT_ITEMS.Count <= 0)
            {
                return 0;
            }

            if (_isCollecting == true)
            {
                return 0;
            }

            _isCollecting = true;

            try
            {
                for (int i = NEARBY_LOOT_ITEMS.Count - 1; i >= 0; i--)
                {
                    WorldLootItem lootItem = NEARBY_LOOT_ITEMS[i];

                    if (lootItem == null || lootItem.IsLootable == false)
                    {
                        RemoveLootItemAt(i, lootItem);
                        continue;
                    }

                    InventoryItemStack itemStack = lootItem.CreateItemStack();

                    if (itemStack.IsValid == false)
                    {
                        RemoveLootItemAt(i, lootItem);
                        continue;
                    }

                    InventoryAddResult addResult = inventoryModule.AddItem(itemStack.Item, itemStack.Amount);
                    lastCollectedAmount += addResult.AddedAmount;
                    lastRemainingAmount += addResult.RemainingAmount;

                    if (addResult.AddedAmount > 0)
                    {
                        lootItem.ConsumeAmount(addResult.AddedAmount);
                    }

                    if (lootItem == null || lootItem.IsLootable == false)
                    {
                        RemoveLootItem(lootItem);
                    }
                }
            }
            finally
            {
                _isCollecting = false;
            }

            if (lastCollectedAmount > 0)
            {
                LogLootResult();
            }

            return lastCollectedAmount;
        }

        private void HandleWorldLootDetected(WorldLootDetectedEvent evt)
        {
            ResolveSceneReferences(null);

            if (IsMatchingCollector(evt.CollectorController) == false)
            {
                return;
            }

            WorldLootItem lootItem = evt.LootItem;

            if (lootItem == null || lootItem.IsLootable == false)
            {
                return;
            }

            if (ContainsLootItem(lootItem) == false)
            {
                NEARBY_LOOT_ITEMS.Add(lootItem);
            }

            CollectNearbyLoots();
        }

        private void HandleWorldLootLost(WorldLootLostEvent evt)
        {
            ResolveSceneReferences(null);

            if (IsMatchingCollector(evt.CollectorController) == false)
            {
                return;
            }

            if (_isCollecting == true)
            {
                return;
            }

            RemoveLootItem(evt.LootItem);
        }

        private void HandleInventoryChanged(PlayerInventoryModule changedInventoryModule)
        {
            if (changedInventoryModule != inventoryModule)
            {
                return;
            }

            if (_isCollecting == true)
            {
                return;
            }

            CollectNearbyLoots();
        }

        private bool IsMatchingCollector(CharacterController targetController)
        {
            if (targetController == null)
            {
                return false;
            }

            if (collectorController == null)
            {
                ResolveSceneReferences(null);

                if (collectorController == null)
                {
                    LogMissingControllerOnce();
                    return false;
                }
            }

            return targetController == collectorController;
        }

        private bool ContainsLootItem(WorldLootItem lootItem)
        {
            for (int i = 0; i < NEARBY_LOOT_ITEMS.Count; i++)
            {
                if (NEARBY_LOOT_ITEMS[i] == lootItem)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveLootItem(WorldLootItem lootItem)
        {
            for (int i = NEARBY_LOOT_ITEMS.Count - 1; i >= 0; i--)
            {
                if (NEARBY_LOOT_ITEMS[i] != lootItem)
                {
                    continue;
                }

                NEARBY_LOOT_ITEMS.RemoveAt(i);
            }
        }

        private void RemoveLootItemAt(int index, WorldLootItem expectedLootItem)
        {
            if (index < 0 || index >= NEARBY_LOOT_ITEMS.Count)
            {
                return;
            }

            if (NEARBY_LOOT_ITEMS[index] != expectedLootItem)
            {
                return;
            }

            NEARBY_LOOT_ITEMS.RemoveAt(index);
        }

        private void ResolveSceneReferences(Entity entity)
        {
            ResolveInventoryModule(entity);
            ResolveCollectorController(entity);
        }

        private void ResolveInventoryModule(Entity entity)
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

            if (inventoryModule == null)
            {
                inventoryModule = GetComponentInParent<PlayerInventoryModule>();
            }

            if (inventoryModule != null)
            {
                _loggedMissingInventory = false;
            }
        }

        private void ResolveCollectorController(Entity entity)
        {
            if (collectorController != null)
            {
                _loggedMissingController = false;
                return;
            }

            if (entity != null)
            {
                collectorController = entity.GetComponent<CharacterController>();

                if (collectorController != null)
                {
                    _loggedMissingController = false;
                    return;
                }
            }

            collectorController = GetComponent<CharacterController>();

            if (collectorController == null)
            {
                collectorController = GetComponentInParent<CharacterController>();
            }
            if (collectorController != null)
            {
                _loggedMissingController = false;
            }
        }

        private void ResetLastLootResult()
        {
            lastCollectedAmount = 0;
            lastRemainingAmount = 0;
        }

        private void SubscribeLootEvents()
        {
            if (_isSubscribedToLootEvents == true)
            {
                return;
            }

            Bus<WorldLootDetectedEvent>.Events += HandleWorldLootDetected;
            Bus<WorldLootLostEvent>.Events += HandleWorldLootLost;
            _isSubscribedToLootEvents = true;
        }

        private void UnsubscribeLootEvents()
        {
            if (_isSubscribedToLootEvents == false)
            {
                return;
            }

            Bus<WorldLootDetectedEvent>.Events -= HandleWorldLootDetected;
            Bus<WorldLootLostEvent>.Events -= HandleWorldLootLost;
            _isSubscribedToLootEvents = false;
        }

        private void SubscribeInventory()
        {
            if (_subscribedInventoryModule == inventoryModule)
            {
                return;
            }

            UnsubscribeInventory();

            if (inventoryModule == null)
            {
                return;
            }
            inventoryModule.InventoryChanged += HandleInventoryChanged;
            _subscribedInventoryModule = inventoryModule;
        }

        private void UnsubscribeInventory()
        {
            if (_subscribedInventoryModule == null)
            {
                return;
            }

            _subscribedInventoryModule.InventoryChanged -= HandleInventoryChanged;
            _subscribedInventoryModule = null;
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

        private void LogMissingControllerOnce()
        {
            if (_loggedMissingController == true)
            {
                return;
            }

            _loggedMissingController = true;
            LogMissingController();
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogMissingController()
        {
            Debug.LogWarning($"{nameof(CharacterController)} is missing. Auto loot event skipped.", this);
        }
    }
}
