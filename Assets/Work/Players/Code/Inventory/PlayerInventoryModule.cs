using System;
using UnityEngine;
using Work.Core.EventBus;
using Work.Entities.Code;
using Work.Items.Code;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 플레이어가 보유한 아이템 슬롯과 수량을 관리하는 인벤토리 모듈
    /// </summary>
    public sealed class PlayerInventoryModule : MonoBehaviour, IEntityModule
    {
        private const int MIN_SLOT_CAPACITY = 1;
        private const int DEFAULT_SLOT_CAPACITY = 24;

        [SerializeField]
        [Min(MIN_SLOT_CAPACITY)]
        private int slotCapacity = DEFAULT_SLOT_CAPACITY;

        [SerializeField]
        private InventorySlot[] slots = new InventorySlot[DEFAULT_SLOT_CAPACITY];

        [Header("Last Add Result")]
        [SerializeField]
        private int lastAddedAmount;

        [SerializeField]
        private int lastRemainingAmount;

        private bool _isSubscribedToItemEvents;
        private InventoryItemStack[] _snapshotItemStacks = new InventoryItemStack[DEFAULT_SLOT_CAPACITY];

        /// <summary>
        /// 인벤토리 내용이 변경될 때 발생하는 이벤트
        /// </summary>
        public event Action<PlayerInventoryModule> InventoryChanged;

        /// <summary>
        /// 현재 슬롯 수
        /// </summary>
        public int SlotCapacity => slots != null ? slots.Length : slotCapacity;

        /// <summary>
        /// 마지막 추가 요청의 실제 추가 수량
        /// </summary>
        public int LastAddedAmount => lastAddedAmount;

        /// <summary>
        /// 마지막 추가 요청의 미추가 수량
        /// </summary>
        public int LastRemainingAmount => lastRemainingAmount;

        private void Awake()
        {
            EnsureSlots();
        }

        private void OnEnable()
        {
            SubscribeItemEvents();
        }

        private void OnDisable()
        {
            UnsubscribeItemEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeItemEvents();
        }

        /// <summary>
        /// 모듈 소유자 초기화
        /// </summary>
        /// <param name="entity">모듈 소유 엔티티</param>
        public void Initialize(Entity entity)
        {
            EnsureSlots();
        }

        /// <summary>
        /// 지정한 인덱스의 슬롯 조회
        /// </summary>
        /// <param name="index">조회할 슬롯 인덱스</param>
        /// <returns>슬롯 데이터</returns>
        public InventorySlot GetSlot(int index)
        {
            EnsureSlots();

            if (index < 0 || index >= slots.Length)
            {
                return null;
            }

            return slots[index];
        }

        /// <summary>
        /// 아이템 단일 스택 추가
        /// </summary>
        /// <param name="item">추가할 아이템 데이터</param>
        /// <param name="amount">추가할 수량</param>
        /// <returns>추가 처리 결과</returns>
        public InventoryAddResult AddItem(ItemDataSO item, int amount)
        {
            EnsureSlots();

            InventoryAddResult result = AddItemInternal(item, amount);
            lastAddedAmount = result.AddedAmount;
            lastRemainingAmount = result.RemainingAmount;

            if (result.AddedAmount > 0)
            {
                RaiseInventoryChanged();
            }

            return result;
        }

        /// <summary>
        /// 아이템 스택 배열을 한 번의 변경 알림으로 추가
        /// </summary>
        /// <param name="itemStacks">추가할 아이템 스택 배열</param>
        /// <param name="startIndex">추가를 시작할 배열 인덱스</param>
        /// <param name="count">처리할 스택 수</param>
        /// <returns>묶음 추가 처리 결과</returns>
        public InventoryBatchAddResult AddItems(InventoryItemStack[] itemStacks, int startIndex, int count)
        {
            return AddItems(itemStacks, startIndex, count, null, 0);
        }

        /// <summary>
        /// 아이템 스택 배열을 한 번의 변경 알림으로 추가하고 개별 추가 결과를 저장
        /// </summary>
        /// <param name="itemStacks">추가할 아이템 스택 배열</param>
        /// <param name="startIndex">추가를 시작할 배열 인덱스</param>
        /// <param name="count">처리할 스택 수</param>
        /// <param name="addResults">개별 추가 결과 저장 버퍼</param>
        /// <param name="addResultStartIndex">개별 추가 결과 저장 시작 인덱스</param>
        /// <returns>묶음 추가 처리 결과</returns>
        public InventoryBatchAddResult AddItems(
            InventoryItemStack[] itemStacks,
            int startIndex,
            int count,
            InventoryAddResult[] addResults,
            int addResultStartIndex
        )
        {
            EnsureSlots();

            if (itemStacks == null || startIndex < 0 || count <= 0 || startIndex >= itemStacks.Length)
            {
                lastAddedAmount = 0;
                lastRemainingAmount = 0;
                return default;
            }

            int validCount = Mathf.Min(count, itemStacks.Length - startIndex);
            int requestedStackCount = 0;
            int fullyAddedStackCount = 0;
            int addedAmount = 0;
            int remainingAmount = 0;
            int addResultIndex = Mathf.Max(0, addResultStartIndex);

            for (int i = 0; i < validCount; i++)
            {
                InventoryItemStack itemStack = itemStacks[startIndex + i];

                if (itemStack.IsValid == false)
                {
                    continue;
                }

                requestedStackCount++;
                InventoryAddResult result = AddItemInternal(itemStack.Item, itemStack.Amount);

                if (addResults != null && addResultIndex < addResults.Length)
                {
                    addResults[addResultIndex] = result;
                }

                addResultIndex++;
                addedAmount += result.AddedAmount;
                remainingAmount += result.RemainingAmount;

                if (result.IsFullyAdded == true)
                {
                    fullyAddedStackCount++;
                }
            }

            lastAddedAmount = addedAmount;
            lastRemainingAmount = remainingAmount;

            if (addedAmount > 0)
            {
                RaiseInventoryChanged();
            }

            return new InventoryBatchAddResult(requestedStackCount, fullyAddedStackCount, addedAmount, remainingAmount);
        }

        /// <summary>
        /// 지정한 아이템 수량 제거
        /// </summary>
        /// <param name="item">제거할 아이템 데이터</param>
        /// <param name="amount">제거할 수량</param>
        /// <returns>실제로 제거된 수량</returns>
        public int RemoveItem(ItemDataSO item, int amount)
        {
            EnsureSlots();

            if (item == null || amount <= 0)
            {
                return 0;
            }

            int remainingAmount = amount;
            int removedAmount = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];

                if (slot.Contains(item) == false)
                {
                    continue;
                }

                int slotRemovedAmount = slot.Remove(remainingAmount);
                removedAmount += slotRemovedAmount;
                remainingAmount -= slotRemovedAmount;

                if (remainingAmount <= 0)
                {
                    break;
                }
            }

            if (removedAmount > 0)
            {
                RaiseInventoryChanged();
            }

            return removedAmount;
        }

        /// <summary>
        /// 지정한 아이템 보유 수량 조회
        /// </summary>
        /// <param name="item">조회할 아이템 데이터</param>
        /// <returns>보유 수량</returns>
        public int GetItemAmount(ItemDataSO item)
        {
            EnsureSlots();

            if (item == null)
            {
                return 0;
            }

            int totalAmount = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];

                if (slot.Contains(item) == false)
                {
                    continue;
                }

                totalAmount += slot.Amount;
            }

            return totalAmount;
        }

        private InventoryAddResult AddItemInternal(ItemDataSO item, int amount)
        {
            if (item == null || amount <= 0)
            {
                int invalidRemainingAmount = Mathf.Max(0, amount);
                return new InventoryAddResult(item, amount, 0, invalidRemainingAmount);
            }

            int remainingAmount = amount;
            int addedAmount = 0;

            AddToExistingSlots(item, ref remainingAmount, ref addedAmount);

            if (remainingAmount > 0)
            {
                AddToEmptySlots(item, ref remainingAmount, ref addedAmount);
            }

            return new InventoryAddResult(item, amount, addedAmount, remainingAmount);
        }

        private void AddToExistingSlots(ItemDataSO item, ref int remainingAmount, ref int addedAmount)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];

                if (slot.IsEmpty == true)
                {
                    continue;
                }

                if (slot.CanAccept(item) == false)
                {
                    continue;
                }

                int slotAddedAmount = slot.Add(item, remainingAmount);
                addedAmount += slotAddedAmount;
                remainingAmount -= slotAddedAmount;

                if (remainingAmount <= 0)
                {
                    break;
                }
            }
        }

        private void AddToEmptySlots(ItemDataSO item, ref int remainingAmount, ref int addedAmount)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];

                if (slot.IsEmpty == false)
                {
                    continue;
                }

                int slotAddedAmount = slot.Add(item, remainingAmount);
                addedAmount += slotAddedAmount;
                remainingAmount -= slotAddedAmount;

                if (remainingAmount <= 0)
                {
                    break;
                }
            }
        }

        private void EnsureSlots()
        {
            slotCapacity = Mathf.Max(MIN_SLOT_CAPACITY, slotCapacity);

            if (slots == null || slots.Length != slotCapacity)
            {
                InventorySlot[] newSlots = new InventorySlot[slotCapacity];
                int copyCount = slots != null ? Mathf.Min(slots.Length, newSlots.Length) : 0;

                for (int i = 0; i < copyCount; i++)
                {
                    newSlots[i] = slots[i];
                }

                slots = newSlots;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = new InventorySlot();
                }

                slots[i].Validate();
            }
        }

        private void RaiseInventoryChanged()
        {
            Action<PlayerInventoryModule> handler = InventoryChanged;

            if (handler != null)
            {
                handler.Invoke(this);
            }

            Bus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
        }

        private void SubscribeItemEvents()
        {
            if (_isSubscribedToItemEvents == true)
            {
                return;
            }

            Bus<InventoryItemAddRequestedEvent>.Events += HandleAddRequested;
            Bus<InventoryItemsAddRequestedEvent>.Events += HandleAddItemsRequested;
            Bus<InventoryItemRemoveRequestedEvent>.Events += HandleRemoveRequested;
            Bus<InventorySnapshotRequestedEvent>.Events += HandleSnapshotRequested;
            _isSubscribedToItemEvents = true;
        }

        private void UnsubscribeItemEvents()
        {
            if (_isSubscribedToItemEvents == false)
            {
                return;
            }

            Bus<InventoryItemAddRequestedEvent>.Events -= HandleAddRequested;
            Bus<InventoryItemsAddRequestedEvent>.Events -= HandleAddItemsRequested;
            Bus<InventoryItemRemoveRequestedEvent>.Events -= HandleRemoveRequested;
            Bus<InventorySnapshotRequestedEvent>.Events -= HandleSnapshotRequested;
            _isSubscribedToItemEvents = false;
        }

        private void HandleAddRequested(InventoryItemAddRequestedEvent request)
        {
            AddItem(request.Item, request.Amount);
        }

        private void HandleAddItemsRequested(InventoryItemsAddRequestedEvent request)
        {
            AddItems(
                request.ItemStacks,
                request.StartIndex,
                request.Count);
        }

        private void HandleRemoveRequested(InventoryItemRemoveRequestedEvent request)
        {
            RemoveItem(request.Item, request.Amount);
        }

        private void HandleSnapshotRequested(InventorySnapshotRequestedEvent request)
        {
            EnsureSlots();
            EnsureSnapshotBuffer();

            int stackCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty == true)
                {
                    continue;
                }

                _snapshotItemStacks[stackCount] = new InventoryItemStack(slot.Item, slot.Amount);
                stackCount++;
            }

            Bus<InventorySnapshotPublishedEvent>.Raise(new InventorySnapshotPublishedEvent(_snapshotItemStacks, stackCount));
        }

        private void EnsureSnapshotBuffer()
        {
            if (_snapshotItemStacks != null && _snapshotItemStacks.Length >= slots.Length)
            {
                return;
            }

            _snapshotItemStacks = new InventoryItemStack[slots.Length];
        }

        private void OnValidate()
        {
            EnsureSlots();
        }
    }
}
