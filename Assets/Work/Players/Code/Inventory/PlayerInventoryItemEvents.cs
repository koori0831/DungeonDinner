using System;
using Work.Core.EventBus;
using Work.Items.Code;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 플레이어 인벤토리 아이템 입출력 요청 이벤트 모음
    /// </summary>
    public static class PlayerInventoryItemEvents
    {
        /// <summary>
        /// 아이템 단일 스택 추가 요청
        /// </summary>
        public readonly record struct InventoryItemAddRequestedEvent(
            PlayerInventoryModule Target,
            ItemDataSO Item,
            int Amount,
            InventoryItemAddRequestResult Result
        ) : IEvent;

        /// <summary>
        /// 아이템 스택 배열 추가 요청
        /// </summary>
        public readonly record struct InventoryItemsAddRequestedEvent(
            PlayerInventoryModule Target,
            InventoryItemStack[] ItemStacks,
            int StartIndex,
            int Count,
            InventoryAddResult[] AddResults,
            int AddResultStartIndex,
            InventoryItemsAddRequestResult Result
        ) : IEvent;

        /// <summary>
        /// 아이템 수량 제거 요청
        /// </summary>
        public readonly record struct InventoryItemRemoveRequestedEvent(
            PlayerInventoryModule Target,
            ItemDataSO Item,
            int Amount,
            InventoryItemRemoveRequestResult Result
        ) : IEvent;

        /// <summary>
        /// 아이템 보유 수량 조회 요청
        /// </summary>
        public readonly record struct InventoryItemAmountRequestedEvent(
            PlayerInventoryModule Target,
            ItemDataSO Item,
            InventoryItemAmountRequestResult Result
        ) : IEvent;

        /// <summary>
        /// 아이템 단일 스택 추가 요청 결과 수신자
        /// </summary>
        public sealed class InventoryItemAddRequestResult
        {
            public InventoryItemAddRequestResult(ItemDataSO item, int requestedAmount)
            {
                Result = new InventoryAddResult(item, requestedAmount, 0, Math.Max(0, requestedAmount));
                Reason = "Inventory item add request was not handled.";
            }

            public bool Handled { get; private set; }
            public InventoryAddResult Result { get; private set; }
            public string Reason { get; private set; }

            public void Complete(InventoryAddResult result)
            {
                Handled = true;
                Result = result;
                Reason = string.Empty;
            }
        }

        /// <summary>
        /// 아이템 스택 배열 추가 요청 결과 수신자
        /// </summary>
        public sealed class InventoryItemsAddRequestResult
        {
            public InventoryItemsAddRequestResult()
            {
                Result = default;
                Reason = "Inventory items add request was not handled.";
            }

            public bool Handled { get; private set; }
            public InventoryBatchAddResult Result { get; private set; }
            public string Reason { get; private set; }

            public void Complete(InventoryBatchAddResult result)
            {
                Handled = true;
                Result = result;
                Reason = string.Empty;
            }
        }

        /// <summary>
        /// 아이템 수량 제거 요청 결과 수신자
        /// </summary>
        public sealed class InventoryItemRemoveRequestResult
        {
            public InventoryItemRemoveRequestResult()
            {
                Reason = "Inventory item remove request was not handled.";
            }

            public bool Handled { get; private set; }
            public int RemovedAmount { get; private set; }
            public string Reason { get; private set; }

            public void Complete(int removedAmount)
            {
                Handled = true;
                RemovedAmount = Math.Max(0, removedAmount);
                Reason = string.Empty;
            }
        }

        /// <summary>
        /// 아이템 보유 수량 조회 요청 결과 수신자
        /// </summary>
        public sealed class InventoryItemAmountRequestResult
        {
            public InventoryItemAmountRequestResult()
            {
                Reason = "Inventory item amount request was not handled.";
            }

            public bool Handled { get; private set; }
            public int Amount { get; private set; }
            public string Reason { get; private set; }

            public void Complete(int amount)
            {
                Handled = true;
                Amount = Math.Max(0, amount);
                Reason = string.Empty;
            }
        }

        /// <summary>
        /// 아이템 단일 스택 추가 요청 발행
        /// </summary>
        public static InventoryAddResult RequestAddItem(
            PlayerInventoryModule target,
            ItemDataSO item,
            int amount,
            out bool handled,
            out string reason)
        {
            InventoryItemAddRequestResult result = new InventoryItemAddRequestResult(item, amount);
            if (target == null)
            {
                handled = result.Handled;
                reason = result.Reason;
                return result.Result;
            }

            PlayerInventoryEventBridge.EnsureBridge(target);
            Bus<InventoryItemAddRequestedEvent>.Raise(new InventoryItemAddRequestedEvent(target, item, amount, result));
            handled = result.Handled;
            reason = result.Reason;
            return result.Result;
        }

        /// <summary>
        /// 아이템 스택 배열 추가 요청 발행
        /// </summary>
        public static InventoryBatchAddResult RequestAddItems(
            PlayerInventoryModule target,
            InventoryItemStack[] itemStacks,
            int startIndex,
            int count,
            InventoryAddResult[] addResults,
            int addResultStartIndex,
            out bool handled,
            out string reason)
        {
            InventoryItemsAddRequestResult result = new InventoryItemsAddRequestResult();
            if (target == null)
            {
                handled = result.Handled;
                reason = result.Reason;
                return result.Result;
            }

            PlayerInventoryEventBridge.EnsureBridge(target);
            Bus<InventoryItemsAddRequestedEvent>.Raise(new InventoryItemsAddRequestedEvent(
                target,
                itemStacks,
                startIndex,
                count,
                addResults,
                addResultStartIndex,
                result));
            handled = result.Handled;
            reason = result.Reason;
            return result.Result;
        }

        /// <summary>
        /// 아이템 수량 제거 요청 발행
        /// </summary>
        public static int RequestRemoveItem(
            PlayerInventoryModule target,
            ItemDataSO item,
            int amount,
            out bool handled,
            out string reason)
        {
            InventoryItemRemoveRequestResult result = new InventoryItemRemoveRequestResult();
            if (target == null)
            {
                handled = result.Handled;
                reason = result.Reason;
                return result.RemovedAmount;
            }

            PlayerInventoryEventBridge.EnsureBridge(target);
            Bus<InventoryItemRemoveRequestedEvent>.Raise(new InventoryItemRemoveRequestedEvent(target, item, amount, result));
            handled = result.Handled;
            reason = result.Reason;
            return result.RemovedAmount;
        }

        /// <summary>
        /// 아이템 보유 수량 조회 요청 발행
        /// </summary>
        public static int RequestItemAmount(
            PlayerInventoryModule target,
            ItemDataSO item,
            out bool handled,
            out string reason)
        {
            InventoryItemAmountRequestResult result = new InventoryItemAmountRequestResult();
            if (target == null)
            {
                handled = result.Handled;
                reason = result.Reason;
                return result.Amount;
            }

            PlayerInventoryEventBridge.EnsureBridge(target);
            Bus<InventoryItemAmountRequestedEvent>.Raise(new InventoryItemAmountRequestedEvent(target, item, result));
            handled = result.Handled;
            reason = result.Reason;
            return result.Amount;
        }
    }
}
