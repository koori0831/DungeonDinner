using Work.Items.Code;

namespace Work.MaterialAcquisition.Code.Common
{
    public readonly struct AcquisitionRewardResultEntry
    {
        public readonly ItemDataSO Item;
        public readonly int RequestedAmount;
        public readonly int GrantedAmount;
        public readonly int RemainingAmount;
        public readonly int CurrentInventoryAmount;
        public readonly AcquisitionRewardRarity Rarity;
        public readonly bool IsRare;
        public readonly bool IsNewDiscovery;
        public readonly string SourceId;

        public AcquisitionRewardResultEntry(
            ItemDataSO item,
            int requestedAmount,
            int grantedAmount,
            int remainingAmount,
            int currentInventoryAmount,
            AcquisitionRewardRarity rarity,
            bool isNewDiscovery,
            string sourceId
        )
        {
            Item = item;
            RequestedAmount = requestedAmount;
            GrantedAmount = grantedAmount;
            RemainingAmount = remainingAmount;
            CurrentInventoryAmount = currentInventoryAmount;
            Rarity = rarity;
            IsRare = rarity >= AcquisitionRewardRarity.Rare;
            IsNewDiscovery = isNewDiscovery;
            SourceId = sourceId;
        }
    }
}
