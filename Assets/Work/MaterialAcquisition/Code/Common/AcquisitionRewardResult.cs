using System.Collections.Generic;

namespace Work.MaterialAcquisition.Code.Common
{
    public sealed class AcquisitionRewardResult
    {
        private readonly List<AcquisitionRewardResultEntry> entries;

        public AcquisitionRewardResult(
            AcquisitionRewardSourceType sourceType,
            string sourceId,
            int seed,
            IReadOnlyList<AcquisitionRewardResultEntry> entries
        )
        {
            SourceType = sourceType;
            SourceId = sourceId;
            Seed = seed;
            this.entries = entries != null
                ? new List<AcquisitionRewardResultEntry>(entries)
                : new List<AcquisitionRewardResultEntry>();

            for (int i = 0; i < this.entries.Count; i++)
            {
                AcquisitionRewardResultEntry entry = this.entries[i];
                RequestedTotalAmount += entry.RequestedAmount;
                GrantedTotalAmount += entry.GrantedAmount;
                RemainingTotalAmount += entry.RemainingAmount;
                HasRareReward |= entry.IsRare;
                HasNewDiscovery |= entry.IsNewDiscovery;
            }

            HasAnyReward = GrantedTotalAmount > 0;
        }

        public AcquisitionRewardSourceType SourceType { get; }
        public string SourceId { get; }
        public int Seed { get; }
        public IReadOnlyList<AcquisitionRewardResultEntry> Entries => entries;
        public int RequestedTotalAmount { get; }
        public int GrantedTotalAmount { get; }
        public int RemainingTotalAmount { get; }
        public bool HasAnyReward { get; }
        public bool HasRareReward { get; }
        public bool HasNewDiscovery { get; }
    }
}
