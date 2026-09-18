using System;
using System.Collections.Generic;
using System.Linq;

namespace Work.Dispatch.Code.Runtime
{
    [Serializable]
    public sealed class DispatchJob
    {
        public string JobId;
        public string NpcId;
        public string RegionId;
        public List<DispatchResolvedRequest> Requests = new List<DispatchResolvedRequest>();
        public int StartedAtTotalTime;
        public int RequiredTime;
        public int CompleteAtTotalTime;
        public int RandomSeed;
        public DispatchState State;
        public List<DispatchRewardData> Rewards = new List<DispatchRewardData>();

        public bool IsCompleteAt(int totalElapsedTime)
        {
            return State == DispatchState.Active && totalElapsedTime >= CompleteAtTotalTime;
        }

        public bool HasRemainingRewards => Rewards != null && Rewards.Any(reward => reward != null && reward.RemainingAmount > 0);
    }
}
