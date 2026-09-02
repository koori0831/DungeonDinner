using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    public readonly struct DispatchWorkload
    {
        public int Amount { get; }
        public int AmountPerBatch { get; }
        public int TimePerBatch { get; }

        public DispatchWorkload(int amount, int amountPerBatch, int timePerBatch)
        {
            Amount = amount;
            AmountPerBatch = amountPerBatch;
            TimePerBatch = timePerBatch;
        }
    }

    public readonly struct DispatchDurationResult
    {
        public int GatherTime { get; }
        public int RequiredTime { get; }

        public DispatchDurationResult(int gatherTime, int requiredTime)
        {
            GatherTime = gatherTime;
            RequiredTime = requiredTime;
        }
    }

    public sealed class DispatchDurationCalculator
    {
        public DispatchDurationResult Calculate(
            int baseTravelTime,
            int timeMultiplierPercent,
            IReadOnlyList<DispatchWorkload> workloads)
        {
            if (workloads == null || workloads.Count == 0)
            {
                throw new ArgumentException("파견 작업량이 필요합니다.", nameof(workloads));
            }

            int gatherTime = 0;
            for (int i = 0; i < workloads.Count; i++)
            {
                DispatchWorkload workload = workloads[i];
                if (workload.Amount <= 0 || workload.AmountPerBatch <= 0 || workload.TimePerBatch <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(workloads), "작업량 수치는 모두 1 이상이어야 합니다.");
                }

                int batchCount = Mathf.CeilToInt(workload.Amount / (float)workload.AmountPerBatch);
                gatherTime = checked(gatherTime + batchCount * workload.TimePerBatch);
            }

            int baseTime = checked(Mathf.Max(0, baseTravelTime) + gatherTime);
            int multiplier = Mathf.Clamp(timeMultiplierPercent, 50, 200);
            int requiredTime = Mathf.Max(1, Mathf.CeilToInt(baseTime * multiplier / 100f));
            return new DispatchDurationResult(gatherTime, requiredTime);
        }

        public DispatchDurationResult Calculate(
            DispatchRegionSO region,
            DispatchNpcRule npcRule,
            IReadOnlyList<DispatchDraftRequest> requests)
        {
            List<DispatchWorkload> workloads = new List<DispatchWorkload>(requests.Count);
            for (int i = 0; i < requests.Count; i++)
            {
                DispatchDraftRequest request = requests[i];
                if (region.TryFindMaterial(request.ItemId, out DispatchMaterialRule materialRule) == false)
                {
                    throw new InvalidOperationException($"파견 재료 규칙을 찾을 수 없습니다: {request.ItemId}");
                }

                workloads.Add(new DispatchWorkload(
                    request.Amount,
                    materialRule.AmountPerBatch,
                    materialRule.TimePerBatch));
            }

            return Calculate(region.BaseTravelTime, npcRule.TimeMultiplierPercent, workloads);
        }
    }
}
