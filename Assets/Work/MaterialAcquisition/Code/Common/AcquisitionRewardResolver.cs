using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.MaterialAcquisition.Code.Common
{
    public sealed class AcquisitionRewardResolver
    {
        public AcquisitionRewardRoll[] Resolve(
            AcquisitionRewardTableSO table,
            IAcquisitionRandom random,
            AcquisitionRewardResolveContext context
        )
        {
            if (table == null || random == null || table.Entries == null)
            {
                return Array.Empty<AcquisitionRewardRoll>();
            }

            List<AcquisitionRewardRoll> rolls = new List<AcquisitionRewardRoll>();

            switch (table.Mode)
            {
                case AcquisitionRewardTableMode.AllGuaranteed:
                    AddAllValidEntries(table, context, random, rolls);
                    break;
                case AcquisitionRewardTableMode.WeightedPick:
                    AddWeightedEntries(table, context, random, rolls, false);
                    break;
                case AcquisitionRewardTableMode.ChanceEach:
                    AddChanceEntries(table, context, random, rolls);
                    break;
                case AcquisitionRewardTableMode.GuaranteedThenWeighted:
                    AddGuaranteedEntries(table, context, random, rolls);
                    AddWeightedEntries(table, context, random, rolls, true);
                    break;
            }

            return MergeRolls(rolls);
        }

        private static void AddAllValidEntries(
            AcquisitionRewardTableSO table,
            AcquisitionRewardResolveContext context,
            IAcquisitionRandom random,
            List<AcquisitionRewardRoll> rolls
        )
        {
            IReadOnlyList<AcquisitionRewardEntry> entries = table.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                TryAddRoll(table, entries[i], context, random, rolls);
            }
        }

        private static void AddGuaranteedEntries(
            AcquisitionRewardTableSO table,
            AcquisitionRewardResolveContext context,
            IAcquisitionRandom random,
            List<AcquisitionRewardRoll> rolls
        )
        {
            IReadOnlyList<AcquisitionRewardEntry> entries = table.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                AcquisitionRewardEntry entry = entries[i];
                if (entry != null && entry.Guaranteed == true)
                {
                    TryAddRoll(table, entry, context, random, rolls);
                }
            }
        }

        private static void AddChanceEntries(
            AcquisitionRewardTableSO table,
            AcquisitionRewardResolveContext context,
            IAcquisitionRandom random,
            List<AcquisitionRewardRoll> rolls
        )
        {
            IReadOnlyList<AcquisitionRewardEntry> entries = table.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                AcquisitionRewardEntry entry = entries[i];
                if (entry == null || entry.IsValid == false)
                {
                    continue;
                }

                if (entry.Guaranteed == true || random.RangeFloat01() <= GetEffectiveChance(entry, context))
                {
                    TryAddRoll(table, entry, context, random, rolls);
                }
            }
        }

        private static void AddWeightedEntries(
            AcquisitionRewardTableSO table,
            AcquisitionRewardResolveContext context,
            IAcquisitionRandom random,
            List<AcquisitionRewardRoll> rolls,
            bool skipGuaranteed
        )
        {
            List<AcquisitionRewardEntry> candidates = GetWeightedCandidates(table, skipGuaranteed);
            if (candidates.Count == 0)
            {
                return;
            }

            int rollCount = random.RangeInt(table.MinRollCount, table.MaxRollCount + 1);
            for (int i = 0; i < rollCount && candidates.Count > 0; i++)
            {
                int candidateIndex = PickWeightedEntry(random, candidates);
                if (candidateIndex < 0)
                {
                    return;
                }

                AcquisitionRewardEntry entry = candidates[candidateIndex];
                TryAddRoll(table, entry, context, random, rolls);

                if (table.AllowDuplicateItems == false)
                {
                    RemoveCandidatesWithSameItem(candidates, entry);
                }
            }
        }

        private static List<AcquisitionRewardEntry> GetWeightedCandidates(
            AcquisitionRewardTableSO table,
            bool skipGuaranteed
        )
        {
            List<AcquisitionRewardEntry> candidates = new List<AcquisitionRewardEntry>();
            IReadOnlyList<AcquisitionRewardEntry> entries = table.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                AcquisitionRewardEntry entry = entries[i];
                if (entry == null || entry.IsValid == false || entry.Weight <= 0)
                {
                    continue;
                }

                if (skipGuaranteed == true && entry.Guaranteed == true)
                {
                    continue;
                }

                candidates.Add(entry);
            }

            return candidates;
        }

        private static int PickWeightedEntry(IAcquisitionRandom random, IReadOnlyList<AcquisitionRewardEntry> candidates)
        {
            int[] weights = new int[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                weights[i] = candidates[i].Weight;
            }

            return random.PickWeighted(weights);
        }

        private static void RemoveCandidatesWithSameItem(
            List<AcquisitionRewardEntry> candidates,
            AcquisitionRewardEntry selectedEntry
        )
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].Item == selectedEntry.Item)
                {
                    candidates.RemoveAt(i);
                }
            }
        }

        private static float GetEffectiveChance(
            AcquisitionRewardEntry entry,
            AcquisitionRewardResolveContext context
        )
        {
            float chanceMultiplier = Mathf.Max(0f, context.ChanceMultiplier);
            float rareBonus = entry.Rarity >= AcquisitionRewardRarity.Rare
                ? Mathf.Max(0f, context.RareChanceBonus)
                : 0f;

            return Mathf.Clamp01(entry.Chance * chanceMultiplier + rareBonus);
        }

        private static bool TryAddRoll(
            AcquisitionRewardTableSO table,
            AcquisitionRewardEntry entry,
            AcquisitionRewardResolveContext context,
            IAcquisitionRandom random,
            List<AcquisitionRewardRoll> rolls
        )
        {
            if (entry == null || entry.IsValid == false)
            {
                return false;
            }

            int amount = random.RangeInt(entry.MinAmount, entry.MaxAmount + 1);
            amount = ApplyAmountMultiplier(amount, context.AmountMultiplier);
            if (amount <= 0)
            {
                return false;
            }

            rolls.Add(new AcquisitionRewardRoll(entry.Item, amount, entry.Rarity, table.TableId));
            return true;
        }

        private static int ApplyAmountMultiplier(int amount, float amountMultiplier)
        {
            if (amount <= 0)
            {
                return 0;
            }

            float multiplier = Mathf.Max(0f, amountMultiplier);
            int adjustedAmount = Mathf.RoundToInt(amount * multiplier);
            return Mathf.Max(1, adjustedAmount);
        }

        private static AcquisitionRewardRoll[] MergeRolls(List<AcquisitionRewardRoll> rolls)
        {
            List<AcquisitionRewardRoll> merged = new List<AcquisitionRewardRoll>();

            for (int i = 0; i < rolls.Count; i++)
            {
                AcquisitionRewardRoll roll = rolls[i];
                if (roll.IsValid == false)
                {
                    continue;
                }

                int existingIndex = FindMergeTarget(merged, roll);
                if (existingIndex < 0)
                {
                    merged.Add(roll);
                    continue;
                }

                AcquisitionRewardRoll existing = merged[existingIndex];
                merged[existingIndex] = new AcquisitionRewardRoll(
                    existing.Item,
                    existing.Amount + roll.Amount,
                    existing.Rarity,
                    existing.SourceTableId
                );
            }

            return merged.ToArray();
        }

        private static int FindMergeTarget(IReadOnlyList<AcquisitionRewardRoll> rolls, AcquisitionRewardRoll target)
        {
            for (int i = 0; i < rolls.Count; i++)
            {
                AcquisitionRewardRoll roll = rolls[i];
                if (roll.Item == target.Item
                    && roll.Rarity == target.Rarity
                    && string.Equals(roll.SourceTableId, target.SourceTableId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
