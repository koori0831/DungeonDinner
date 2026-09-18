using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    public readonly struct DispatchRareCandidate
    {
        public string ItemId { get; }
        public int Weight { get; }
        public int MinAmount { get; }
        public int MaxAmount { get; }

        public DispatchRareCandidate(string itemId, int weight, int minAmount, int maxAmount)
        {
            ItemId = itemId;
            Weight = Mathf.Max(1, weight);
            MinAmount = Mathf.Max(1, minAmount);
            MaxAmount = Mathf.Max(MinAmount, maxAmount);
        }
    }

    public readonly struct DispatchRareSettings
    {
        public float ChancePercent { get; }
        public IReadOnlyList<DispatchRareCandidate> Candidates { get; }

        public DispatchRareSettings(float chancePercent, IReadOnlyList<DispatchRareCandidate> candidates)
        {
            ChancePercent = Mathf.Clamp(chancePercent, 0f, 100f);
            Candidates = candidates;
        }
    }

    public sealed class DispatchOutcomeResolver
    {
        public List<DispatchRewardData> Resolve(
            IReadOnlyList<DispatchResolvedRequest> requests,
            DispatchRareSettings rareSettings,
            int randomSeed)
        {
            if (requests == null || requests.Count == 0)
            {
                throw new ArgumentException("파견 결과를 계산할 요청이 없습니다.", nameof(requests));
            }

            System.Random random = new System.Random(randomSeed);
            List<DispatchRewardData> rewards = new List<DispatchRewardData>(requests.Count + 1);

            for (int i = 0; i < requests.Count; i++)
            {
                DispatchResolvedRequest request = requests[i];
                int minimum = Mathf.Max(0, request.MinimumExpectedAmount);
                int maximum = Mathf.Max(minimum, request.MaximumExpectedAmount);
                int amount = random.Next(minimum, maximum + 1);
                rewards.Add(new DispatchRewardData(request.ItemId, amount, false));
            }

            if (ShouldGrantRareReward(random, rareSettings))
            {
                DispatchRareCandidate rare = PickWeighted(random, rareSettings.Candidates);
                int amount = random.Next(rare.MinAmount, rare.MaxAmount + 1);
                rewards.Add(new DispatchRewardData(rare.ItemId, amount, true));
            }

            return rewards;
        }

        public DispatchRareSettings BuildRareSettings(DispatchRareRewardTable table, int gatherTime)
        {
            if (table == null || table.HasRewards == false)
            {
                return new DispatchRareSettings(0f, Array.Empty<DispatchRareCandidate>());
            }

            float chance = Mathf.Min(table.MaximumChance, gatherTime * table.ChancePerGatherTime);
            List<DispatchRareCandidate> candidates = new List<DispatchRareCandidate>(table.Rewards.Count);

            for (int i = 0; i < table.Rewards.Count; i++)
            {
                DispatchRareRewardRule rule = table.Rewards[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.ItemId))
                {
                    continue;
                }

                candidates.Add(new DispatchRareCandidate(
                    rule.ItemId,
                    rule.Weight,
                    rule.MinAmount,
                    rule.MaxAmount));
            }

            return new DispatchRareSettings(chance, candidates);
        }

        private static bool ShouldGrantRareReward(System.Random random, DispatchRareSettings settings)
        {
            return settings.ChancePercent > 0f
                   && settings.Candidates != null
                   && settings.Candidates.Count > 0
                   && random.NextDouble() * 100d < settings.ChancePercent;
        }

        private static DispatchRareCandidate PickWeighted(
            System.Random random,
            IReadOnlyList<DispatchRareCandidate> candidates)
        {
            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight = checked(totalWeight + candidates[i].Weight);
            }

            int roll = random.Next(0, totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].Weight;
                if (roll < 0)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }
    }
}
