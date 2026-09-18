using System.Collections.Generic;
using UnityEngine;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    public sealed class DispatchEstimate
    {
        public int GatherTime { get; }
        public int RequiredTime { get; }
        public bool HasRareRewardChance { get; }
        public IReadOnlyList<DispatchResolvedRequest> Requests { get; }

        public DispatchEstimate(
            int gatherTime,
            int requiredTime,
            bool hasRareRewardChance,
            IReadOnlyList<DispatchResolvedRequest> requests)
        {
            GatherTime = gatherTime;
            RequiredTime = requiredTime;
            HasRareRewardChance = hasRareRewardChance;
            Requests = requests;
        }

        public static DispatchEstimate Build(
            DispatchDraft draft,
            DispatchRegionSO region,
            DispatchNpcRule npcRule,
            DispatchDurationCalculator durationCalculator)
        {
            DispatchDurationResult duration = durationCalculator.Calculate(region, npcRule, draft.Requests);
            List<DispatchResolvedRequest> requests = new List<DispatchResolvedRequest>(draft.Requests.Count);

            for (int i = 0; i < draft.Requests.Count; i++)
            {
                DispatchDraftRequest request = draft.Requests[i];
                region.TryFindMaterial(request.ItemId, out DispatchMaterialRule rule);

                int minimum = Mathf.Max(
                    1,
                    Mathf.FloorToInt(request.Amount * rule.MinYieldPercent / 100f));
                int maximum = Mathf.Max(
                    minimum,
                    Mathf.FloorToInt(request.Amount * rule.MaxYieldPercent / 100f));

                requests.Add(new DispatchResolvedRequest(
                    request.ItemId,
                    request.Amount,
                    minimum,
                    maximum));
            }

            return new DispatchEstimate(
                duration.GatherTime,
                duration.RequiredTime,
                region.RareRewards != null && region.RareRewards.HasRewards,
                requests);
        }
    }
}
