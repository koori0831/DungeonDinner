using System;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Core.EventBus;
using Work.Players.Code.Inventory;

namespace Work.Adventure.Code.AdventureEvents
{
    [Serializable]
    public class IngredientReward : AdventureReward
    {
        [SerializeField] private IngredientItemDataSO reward;

        public override void GetReward()
        {
            //Bus<InventoryAddResult>.Raise(new InventoryAddResult());
        }
    }
}
