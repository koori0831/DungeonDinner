using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Items.Code;
using Work.Players.Code.Inventory;

namespace Work.MaterialAcquisition.Code.Common
{
    public sealed class AcquisitionInventoryGateway : MonoBehaviour
    {
        [SerializeField]
        private PlayerInventoryModule inventoryModule;

        [SerializeField]
        private AcquisitionDiscoveryTracker discoveryTracker;

        public void SetInventoryModule(PlayerInventoryModule module)
        {
            inventoryModule = module;
        }

        public void SetDiscoveryTracker(AcquisitionDiscoveryTracker tracker)
        {
            discoveryTracker = tracker;
        }

        public AcquisitionRewardResult Grant(
            AcquisitionRewardSourceType sourceType,
            string sourceId,
            int seed,
            IReadOnlyList<AcquisitionRewardRoll> rolls
        )
        {
            List<AcquisitionRewardRoll> validRolls = GetValidRolls(rolls);
            if (validRolls.Count == 0)
            {
                return new AcquisitionRewardResult(
                    sourceType,
                    sourceId,
                    seed,
                    new List<AcquisitionRewardResultEntry>()
                );
            }

            EnsureReferences();

            if (inventoryModule == null)
            {
                return BuildMissingInventoryResult(sourceType, sourceId, seed, validRolls);
            }

            bool[] alreadyDiscovered = SnapshotDiscoveryState(validRolls);
            InventoryItemStack[] itemStacks = BuildItemStacks(validRolls);
            InventoryAddResult[] addResults = new InventoryAddResult[validRolls.Count];

            inventoryModule.AddItems(itemStacks, 0, itemStacks.Length, addResults, 0);

            List<AcquisitionRewardResultEntry> entries = new List<AcquisitionRewardResultEntry>(validRolls.Count);
            HashSet<string> discoveredDuringGrant = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < validRolls.Count; i++)
            {
                AcquisitionRewardRoll roll = validRolls[i];
                InventoryAddResult addResult = addResults[i];
                int currentAmount = inventoryModule.GetItemAmount(addResult.Item);
                bool isNewDiscovery = TryMarkNewDiscovery(
                    roll,
                    addResult.AddedAmount,
                    alreadyDiscovered[i],
                    discoveredDuringGrant
                );

                entries.Add(
                    new AcquisitionRewardResultEntry(
                        addResult.Item,
                        addResult.RequestedAmount,
                        addResult.AddedAmount,
                        addResult.RemainingAmount,
                        currentAmount,
                        roll.Rarity,
                        isNewDiscovery,
                        roll.SourceTableId
                    )
                );
            }

            return new AcquisitionRewardResult(sourceType, sourceId, seed, entries);
        }

        private static List<AcquisitionRewardRoll> GetValidRolls(IReadOnlyList<AcquisitionRewardRoll> rolls)
        {
            List<AcquisitionRewardRoll> validRolls = new List<AcquisitionRewardRoll>();
            if (rolls == null)
            {
                return validRolls;
            }

            for (int i = 0; i < rolls.Count; i++)
            {
                if (rolls[i].IsValid == true)
                {
                    validRolls.Add(rolls[i]);
                }
            }

            return validRolls;
        }

        private static InventoryItemStack[] BuildItemStacks(IReadOnlyList<AcquisitionRewardRoll> rolls)
        {
            InventoryItemStack[] itemStacks = new InventoryItemStack[rolls.Count];
            for (int i = 0; i < rolls.Count; i++)
            {
                itemStacks[i] = new InventoryItemStack(rolls[i].Item, rolls[i].Amount);
            }

            return itemStacks;
        }

        private bool[] SnapshotDiscoveryState(IReadOnlyList<AcquisitionRewardRoll> rolls)
        {
            bool[] alreadyDiscovered = new bool[rolls.Count];
            for (int i = 0; i < rolls.Count; i++)
            {
                alreadyDiscovered[i] = IsAlreadyDiscovered(rolls[i]);
            }

            return alreadyDiscovered;
        }

        private bool IsAlreadyDiscovered(AcquisitionRewardRoll roll)
        {
            IngredientSO ingredient = GetIngredient(roll.Item);
            if (ingredient == null)
            {
                return true;
            }

            if (discoveryTracker != null && discoveryTracker.IsDiscovered(ingredient) == true)
            {
                return true;
            }

            return inventoryModule != null && inventoryModule.GetItemAmount(roll.Item) > 0;
        }

        private bool TryMarkNewDiscovery(
            AcquisitionRewardRoll roll,
            int grantedAmount,
            bool alreadyDiscovered,
            HashSet<string> discoveredDuringGrant
        )
        {
            if (grantedAmount <= 0 || alreadyDiscovered == true)
            {
                return false;
            }

            IngredientSO ingredient = GetIngredient(roll.Item);
            if (ingredient == null)
            {
                return false;
            }

            string discoveryId = GetDiscoveryId(ingredient);
            if (string.IsNullOrWhiteSpace(discoveryId) == true)
            {
                return false;
            }

            if (discoveredDuringGrant != null && discoveredDuringGrant.Add(discoveryId) == false)
            {
                return false;
            }

            if (discoveryTracker == null)
            {
                return true;
            }

            return discoveryTracker.MarkDiscovered(ingredient);
        }

        private static IngredientSO GetIngredient(ItemDataSO item)
        {
            if (item is IngredientItemDataSO ingredientItem && ingredientItem.Ingredient != null)
            {
                return ingredientItem.Ingredient;
            }

            return null;
        }

        private static string GetDiscoveryId(IngredientSO ingredient)
        {
            if (ingredient == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(ingredient.IngredientId) == false)
            {
                return ingredient.IngredientId.Trim();
            }

            return ingredient.name;
        }

        private static AcquisitionRewardResult BuildMissingInventoryResult(
            AcquisitionRewardSourceType sourceType,
            string sourceId,
            int seed,
            IReadOnlyList<AcquisitionRewardRoll> rolls
        )
        {
            List<AcquisitionRewardResultEntry> entries = new List<AcquisitionRewardResultEntry>(rolls.Count);
            for (int i = 0; i < rolls.Count; i++)
            {
                AcquisitionRewardRoll roll = rolls[i];
                entries.Add(
                    new AcquisitionRewardResultEntry(
                        roll.Item,
                        roll.Amount,
                        0,
                        roll.Amount,
                        0,
                        roll.Rarity,
                        false,
                        roll.SourceTableId
                    )
                );
            }

            return new AcquisitionRewardResult(sourceType, sourceId, seed, entries);
        }

        private void EnsureReferences()
        {
            if (inventoryModule == null)
            {
                inventoryModule = FindFirstObjectByType<PlayerInventoryModule>();
            }

            if (discoveryTracker == null)
            {
                discoveryTracker = FindFirstObjectByType<AcquisitionDiscoveryTracker>();
            }
        }
    }
}
