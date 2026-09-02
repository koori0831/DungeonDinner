using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Dispatch.Code.Data
{
    [Serializable]
    public sealed class DispatchRareRewardTable
    {
        [SerializeField, Range(0f, 100f)] private float chancePerGatherTime = 2f;
        [SerializeField, Range(0f, 100f)] private float maximumChance = 20f;
        [SerializeField] private List<DispatchRareRewardRule> rewards = new List<DispatchRareRewardRule>();

        public float ChancePerGatherTime => Mathf.Clamp(chancePerGatherTime, 0f, 100f);
        public float MaximumChance => Mathf.Clamp(maximumChance, 0f, 100f);
        public IReadOnlyList<DispatchRareRewardRule> Rewards => rewards;
        public bool HasRewards => rewards != null && rewards.Count > 0;
    }
}
