using System.Collections.Generic;

namespace Work.MaterialAcquisition.Code.Common
{
    public readonly struct AcquisitionRewardResolveContext
    {
        public AcquisitionRewardResolveContext(
            AcquisitionRewardSourceType sourceType,
            string sourceId,
            float chanceMultiplier = 1f,
            float amountMultiplier = 1f,
            float rareChanceBonus = 0f,
            IReadOnlyList<string> bonusTags = null
        )
        {
            SourceType = sourceType;
            SourceId = sourceId;
            ChanceMultiplier = chanceMultiplier;
            AmountMultiplier = amountMultiplier;
            RareChanceBonus = rareChanceBonus;
            BonusTags = bonusTags;
        }

        public AcquisitionRewardSourceType SourceType { get; }
        public string SourceId { get; }
        public float ChanceMultiplier { get; }
        public float AmountMultiplier { get; }
        public float RareChanceBonus { get; }
        public IReadOnlyList<string> BonusTags { get; }
    }
}
