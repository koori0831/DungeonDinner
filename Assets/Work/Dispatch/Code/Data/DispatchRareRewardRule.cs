using System;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Dispatch.Code.Data
{
    [Serializable]
    public sealed class DispatchRareRewardRule
    {
        [SerializeField] private IngredientItemDataSO item;
        [SerializeField, Min(1)] private int weight = 1;
        [SerializeField, Min(1)] private int minAmount = 1;
        [SerializeField, Min(1)] private int maxAmount = 1;

        public IngredientItemDataSO Item => item;
        public string ItemId => item != null ? item.ItemId : string.Empty;
        public int Weight => Mathf.Max(1, weight);
        public int MinAmount => Mathf.Max(1, minAmount);
        public int MaxAmount => Mathf.Max(MinAmount, maxAmount);
    }
}
