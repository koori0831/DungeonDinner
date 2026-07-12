using System;
using UnityEngine;
using Work.Adventure.Code.UI;
using Work.Cook.Code.Data;
using Work.Core.EventBus;
using Work.Players.Code.Inventory;

namespace Work.Adventure.Code.AdventureEvents
{
    [Serializable]
    public class IngredientReward : AdventureReward
    {
        [SerializeField] private IngredientItemDataSO reward;
        [SerializeField] private int amount;

        public override void GetReward()
        {
            Bus<InventoryItemAddRequestedEvent>.Raise(new InventoryItemAddRequestedEvent(reward, amount));
            for (int i = 0; i < amount; i++)
                Bus<OnPlusLogCreateEvent>.Raise(new OnPlusLogCreateEvent(new ItemLogData(reward.DisplayName, ItemLogStatusEnum.Add, reward.Icon)));
        }
    }
}
