using System;
using UnityEngine;
using Work.Core.EventBus;

namespace Work.Adventure.Code.Rewards
{
    [Serializable]
    public class AdventureItemReward : AdventureReward
    {
        [SerializeField] private AdventureItemSO itemSO;

        public override void GetReward()
        {
            Bus<OnAddAdventureItem>.Raise(new OnAddAdventureItem(itemSO));
        }
    }
}
