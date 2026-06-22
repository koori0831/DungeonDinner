using System.Collections.Generic;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 단일 파견 완료 보상 지급 결과
    /// </summary>
    public sealed class DispatchRewardResult
    {
        /// <summary>
        /// 완료된 파견 포인트
        /// </summary>
        public DispatchPointSO Point { get; }

        /// <summary>
        /// 아이템별 보상 지급 결과 목록
        /// </summary>
        public IReadOnlyList<DispatchRewardResultEntry> Entries { get; }

        /// <summary>
        /// 실제 인벤토리에 추가된 총수량
        /// </summary>
        public int AddedAmount { get; }

        /// <summary>
        /// 인벤토리에 추가되지 못한 총수량
        /// </summary>
        public int RemainingAmount { get; }

        /// <summary>
        /// 모든 보상이 지급되었는지 여부
        /// </summary>
        public bool IsFullyAdded => Entries.Count > 0 && RemainingAmount <= 0;

        /// <summary>
        /// 파견 완료 결과 생성
        /// </summary>
        public DispatchRewardResult(DispatchPointSO point, IReadOnlyList<DispatchRewardResultEntry> entries, int addedAmount, int remainingAmount)
        {
            Point = point;
            Entries = CopyEntries(entries);
            AddedAmount = addedAmount;
            RemainingAmount = remainingAmount;
        }

        private static IReadOnlyList<DispatchRewardResultEntry> CopyEntries(IReadOnlyList<DispatchRewardResultEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return new List<DispatchRewardResultEntry>();
            }

            List<DispatchRewardResultEntry> copy = new List<DispatchRewardResultEntry>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                {
                    copy.Add(entries[i]);
                }
            }

            return copy;
        }
    }
}
