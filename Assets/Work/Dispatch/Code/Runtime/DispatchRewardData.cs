using System;
using UnityEngine;

namespace Work.Dispatch.Code.Runtime
{
    [Serializable]
    public sealed class DispatchRewardData
    {
        public string ItemId;
        public int GrantedAmount;
        public int RemainingAmount;
        public bool IsRare;

        public DispatchRewardData()
        {
        }

        public DispatchRewardData(string itemId, int amount, bool isRare)
        {
            ItemId = itemId;
            GrantedAmount = Mathf.Max(0, amount);
            RemainingAmount = GrantedAmount;
            IsRare = isRare;
        }
    }
}
