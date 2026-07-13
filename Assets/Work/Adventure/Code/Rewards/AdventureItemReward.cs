using System;
using UnityEngine;
using Work.Adventure.Code.UI;
using Work.Core.EventBus;

namespace Work.Adventure.Code.Rewards
{
    [Serializable]
    public class AdventureItemReward : AdventureReward
    {
        [SerializeField] private AdventureItemSO itemSO;

        public override void GetReward()
        {
            Bus<OnAddAdventureItemEvent>.Raise(new OnAddAdventureItemEvent(itemSO));
            Bus<OnPlusLogCreateEvent>.Raise(new OnPlusLogCreateEvent(new ItemLogData(itemSO.ItemName, ItemLogStatusEnum.Add, itemSO.ItemIcon)));
        }
    }
}
