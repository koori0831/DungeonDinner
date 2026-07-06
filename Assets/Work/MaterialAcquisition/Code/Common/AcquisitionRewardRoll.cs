using Work.Cook.Code.Data;
using Work.Items.Code;

namespace Work.MaterialAcquisition.Code.Common
{
    public readonly struct AcquisitionRewardRoll
    {
        public readonly ItemDataSO Item;
        public readonly int Amount;
        public readonly AcquisitionRewardRarity Rarity;
        public readonly string SourceTableId;
        public readonly bool IsRare;
        public readonly bool IsDiscoveryCandidate;

        public AcquisitionRewardRoll(
            ItemDataSO item,
            int amount,
            AcquisitionRewardRarity rarity,
            string sourceTableId
        )
        {
            Item = item;
            Amount = amount;
            Rarity = rarity;
            SourceTableId = sourceTableId;
            IsRare = rarity >= AcquisitionRewardRarity.Rare;
            IsDiscoveryCandidate = item is IngredientItemDataSO ingredientItem
                && ingredientItem.Ingredient != null;
        }

        public bool IsValid => Item != null && Amount > 0;
    }
}
