using System;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Dispatch.Code.Data
{
    [Serializable]
    public sealed class DispatchMaterialRule
    {
        [SerializeField] private IngredientItemDataSO item;
        [SerializeField, Min(1)] private int maxRequestAmount = 10;
        [SerializeField, Min(1)] private int amountPerBatch = 2;
        [SerializeField, Min(1)] private int timePerBatch = 1;
        [SerializeField, Range(0, 100)] private int minYieldPercent = 60;
        [SerializeField, Range(0, 100)] private int maxYieldPercent = 100;

        public IngredientItemDataSO Item => item;
        public string ItemId => item != null ? item.ItemId : string.Empty;
        public int MaxRequestAmount => Mathf.Max(1, maxRequestAmount);
        public int AmountPerBatch => Mathf.Max(1, amountPerBatch);
        public int TimePerBatch => Mathf.Max(1, timePerBatch);
        public int MinYieldPercent => Mathf.Clamp(minYieldPercent, 0, 100);
        public int MaxYieldPercent => Mathf.Clamp(maxYieldPercent, MinYieldPercent, 100);
    }
}
