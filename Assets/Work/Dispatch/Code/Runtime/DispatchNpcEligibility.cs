using System.Collections.Generic;

namespace Work.Dispatch.Code.Runtime
{
    public readonly struct DispatchNpcEligibility
    {
        public bool NpcExists { get; }
        public int Affinity { get; }
        public IReadOnlyList<string> RegionIds { get; }

        public DispatchNpcEligibility(bool npcExists, int affinity, IReadOnlyList<string> regionIds)
        {
            NpcExists = npcExists;
            Affinity = affinity;
            RegionIds = regionIds;
        }

        public bool CanVisitRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId) || RegionIds == null)
            {
                return false;
            }

            for (int i = 0; i < RegionIds.Count; i++)
            {
                if (string.Equals(RegionIds[i], regionId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
