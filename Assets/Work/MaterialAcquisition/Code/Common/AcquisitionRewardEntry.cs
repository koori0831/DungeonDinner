using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Items.Code;

namespace Work.MaterialAcquisition.Code.Common
{
    [Serializable]
    public sealed class AcquisitionRewardEntry
    {
        private const int MIN_AMOUNT = 1;

        [SerializeField]
        private ItemDataSO item;

        [SerializeField]
        [Min(MIN_AMOUNT)]
        private int minAmount = MIN_AMOUNT;

        [SerializeField]
        [Min(MIN_AMOUNT)]
        private int maxAmount = MIN_AMOUNT;

        [SerializeField]
        [Range(0f, 1f)]
        private float chance = 1f;

        [SerializeField]
        [Min(0)]
        private int weight = 1;

        [SerializeField]
        private AcquisitionRewardRarity rarity = AcquisitionRewardRarity.Common;

        [SerializeField]
        private bool guaranteed;

        [SerializeField]
        private string previewGroupLabel;

        [SerializeField]
        private List<string> tags = new List<string>();

        public ItemDataSO Item => item;
        public int MinAmount => Mathf.Max(MIN_AMOUNT, minAmount);
        public int MaxAmount => Mathf.Max(MinAmount, maxAmount);
        public float Chance => Mathf.Clamp01(chance);
        public int Weight => Mathf.Max(0, weight);
        public AcquisitionRewardRarity Rarity => rarity;
        public bool Guaranteed => guaranteed;
        public string PreviewGroupLabel => previewGroupLabel;
        public IReadOnlyList<string> Tags => tags;
        public bool IsValid => item != null && MinAmount > 0 && MaxAmount >= MinAmount;

        public void Validate()
        {
            minAmount = Mathf.Max(MIN_AMOUNT, minAmount);
            maxAmount = Mathf.Max(minAmount, maxAmount);
            chance = Mathf.Clamp01(chance);
            weight = Mathf.Max(0, weight);

            if (tags == null)
            {
                tags = new List<string>();
            }
        }
    }
}
