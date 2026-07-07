using UnityEngine;
using Work.Core.EventBus;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 인벤토리 아이템 입출력 이벤트를 실제 플레이어 인벤토리 모듈 호출로 연결
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryEventBridge : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryModule inventoryModule;
        [SerializeField] private bool searchInventoryInParents = true;
        [SerializeField] private bool searchInventoryInChildren = true;

        private bool _isSubscribed;

        /// <summary>
        /// 지정 인벤토리에 이벤트 브릿지가 없으면 생성
        /// </summary>
        public static PlayerInventoryEventBridge EnsureBridge(PlayerInventoryModule inventory)
        {
            if (inventory == null)
            {
                return null;
            }

            PlayerInventoryEventBridge bridge = inventory.GetComponent<PlayerInventoryEventBridge>();
            if (bridge == null)
            {
                bridge = inventory.gameObject.AddComponent<PlayerInventoryEventBridge>();
            }

            bridge.SetInventoryModule(inventory);
            bridge.ResolveInventoryModule();
            bridge.Subscribe();
            return bridge;
        }

        /// <summary>
        /// 이벤트 요청을 처리할 인벤토리 모듈 지정
        /// </summary>
        public void SetInventoryModule(PlayerInventoryModule inventory)
        {
            if (inventoryModule == inventory)
            {
                return;
            }

            inventoryModule = inventory;
        }

        private void OnEnable()
        {
            ResolveInventoryModule();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void ResolveInventoryModule()
        {
            if (inventoryModule != null)
            {
                return;
            }

            inventoryModule = GetComponent<PlayerInventoryModule>();
            if (inventoryModule != null)
            {
                return;
            }

            if (searchInventoryInParents == true)
            {
                inventoryModule = GetComponentInParent<PlayerInventoryModule>();
                if (inventoryModule != null)
                {
                    return;
                }
            }

            if (searchInventoryInChildren == true)
            {
                inventoryModule = GetComponentInChildren<PlayerInventoryModule>();
            }
        }

        private void Subscribe()
        {
            if (_isSubscribed == true)
            {
                return;
            }

            Bus<PlayerInventoryItemEvents.InventoryItemAddRequestedEvent>.Events += HandleAddRequested;
            Bus<PlayerInventoryItemEvents.InventoryItemsAddRequestedEvent>.Events += HandleAddItemsRequested;
            Bus<PlayerInventoryItemEvents.InventoryItemRemoveRequestedEvent>.Events += HandleRemoveRequested;
            Bus<PlayerInventoryItemEvents.InventoryItemAmountRequestedEvent>.Events += HandleAmountRequested;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            Bus<PlayerInventoryItemEvents.InventoryItemAddRequestedEvent>.Events -= HandleAddRequested;
            Bus<PlayerInventoryItemEvents.InventoryItemsAddRequestedEvent>.Events -= HandleAddItemsRequested;
            Bus<PlayerInventoryItemEvents.InventoryItemRemoveRequestedEvent>.Events -= HandleRemoveRequested;
            Bus<PlayerInventoryItemEvents.InventoryItemAmountRequestedEvent>.Events -= HandleAmountRequested;
            _isSubscribed = false;
        }

        private void HandleAddRequested(PlayerInventoryItemEvents.InventoryItemAddRequestedEvent request)
        {
            if (CanHandle(request.Target) == false || request.Result == null)
            {
                return;
            }

            request.Result.Complete(inventoryModule.AddItem(request.Item, request.Amount));
        }

        private void HandleAddItemsRequested(PlayerInventoryItemEvents.InventoryItemsAddRequestedEvent request)
        {
            if (CanHandle(request.Target) == false || request.Result == null)
            {
                return;
            }

            request.Result.Complete(inventoryModule.AddItems(
                request.ItemStacks,
                request.StartIndex,
                request.Count,
                request.AddResults,
                request.AddResultStartIndex));
        }

        private void HandleRemoveRequested(PlayerInventoryItemEvents.InventoryItemRemoveRequestedEvent request)
        {
            if (CanHandle(request.Target) == false || request.Result == null)
            {
                return;
            }

            request.Result.Complete(inventoryModule.RemoveItem(request.Item, request.Amount));
        }

        private void HandleAmountRequested(PlayerInventoryItemEvents.InventoryItemAmountRequestedEvent request)
        {
            if (CanHandle(request.Target) == false || request.Result == null)
            {
                return;
            }

            request.Result.Complete(inventoryModule.GetItemAmount(request.Item));
        }

        private bool CanHandle(PlayerInventoryModule target)
        {
            return inventoryModule != null && target != null && target == inventoryModule;
        }
    }
}
