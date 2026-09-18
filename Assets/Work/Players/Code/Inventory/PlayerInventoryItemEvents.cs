using Work.Core.EventBus;
using Work.Items.Code;

namespace Work.Players.Code.Inventory
{
    /// <summary>
    /// 인벤토리에 아이템 추가 요청
    /// </summary>
    public readonly record struct InventoryItemAddRequestedEvent(
        ItemDataSO Item,
        int Amount
    ) : IEvent;

    /// <summary>
    /// 인벤토리에 아이템 묶음 추가 요청
    /// </summary>
    public readonly record struct InventoryItemsAddRequestedEvent(
        InventoryItemStack[] ItemStacks,
        int StartIndex,
        int Count
    ) : IEvent;

    /// <summary>
    /// 인벤토리에서 아이템 제거 요청
    /// </summary>
    public readonly record struct InventoryItemRemoveRequestedEvent(
        ItemDataSO Item,
        int Amount
    ) : IEvent;

    /// <summary>
    /// 인벤토리 스냅샷 요청
    /// </summary>
    public readonly record struct InventorySnapshotRequestedEvent : IEvent;

    /// <summary>
    /// 현재 인벤토리 스냅샷 발행
    /// </summary>
    public readonly record struct InventorySnapshotPublishedEvent(
        InventoryItemStack[] ItemStacks,
        int Count
    ) : IEvent;

    /// <summary>
    /// 인벤토리 내용 변경 알림
    /// </summary>
    public readonly record struct InventoryChangedEvent : IEvent;
}
