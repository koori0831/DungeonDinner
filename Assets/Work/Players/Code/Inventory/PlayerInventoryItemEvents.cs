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
            ItemDataSO Item,
            int Amount,
            InventoryItemAddRequestResult Result
        ) : IEvent;

        /// <summary>
        /// 아이템 스택 배열 추가 요청
        /// </summary>
        public readonly record struct InventoryItemsAddRequestedEvent(
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
            ItemDataSO Item,
            int Amount,
            InventoryItemRemoveRequestResult Result
        ) : IEvent;

        /// <summary>
        /// 아이템 보유 수량 조회 요청
        /// </summary>
        public readonly record struct InventoryItemAmountRequestedEvent(
            ItemDataSO Item,
            InventoryItemAmountRequestResult Result
        ) : IEvent;

        /// <summary>
        /// 플레이어 인벤토리 내용 변경 알림
        /// </summary>
        public readonly record struct InventoryChangedEvent : IEvent;

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
            ItemDataSO item,
            int amount,
            out bool handled,
            out string reason)
        {
            InventoryItemAddRequestResult result = new InventoryItemAddRequestResult(item, amount);
            Bus<InventoryItemAddRequestedEvent>.Raise(new InventoryItemAddRequestedEvent(item, amount, result));
            handled = result.Handled;
            reason = result.Reason;
            return result.Result;
        }

        /// <summary>
        /// 아이템 스택 배열 추가 요청 발행
        /// </summary>
        public static InventoryBatchAddResult RequestAddItems(
            InventoryItemStack[] itemStacks,
            int startIndex,
            int count,
            InventoryAddResult[] addResults,
            int addResultStartIndex,
            out bool handled,
            out string reason)
        {
            InventoryItemsAddRequestResult result = new InventoryItemsAddRequestResult();
            Bus<InventoryItemsAddRequestedEvent>.Raise(new InventoryItemsAddRequestedEvent(
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
            ItemDataSO item,
            int amount,
            out bool handled,
            out string reason)
        {
            InventoryItemRemoveRequestResult result = new InventoryItemRemoveRequestResult();
            Bus<InventoryItemRemoveRequestedEvent>.Raise(new InventoryItemRemoveRequestedEvent(item, amount, result));
            handled = result.Handled;
            reason = result.Reason;
            return result.RemovedAmount;
        }

        /// <summary>
        /// 아이템 보유 수량 조회 요청 발행
        /// </summary>
        public static int RequestItemAmount(
            ItemDataSO item,
            out bool handled,
            out string reason)
        {
            InventoryItemAmountRequestResult result = new InventoryItemAmountRequestResult();
            Bus<InventoryItemAmountRequestedEvent>.Raise(new InventoryItemAmountRequestedEvent(item, result));
            handled = result.Handled;
            reason = result.Reason;
            return result.Amount;
        }
    }
}
