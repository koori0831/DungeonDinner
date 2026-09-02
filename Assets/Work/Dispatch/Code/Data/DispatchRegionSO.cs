using System;
using System.Collections.Generic;
using Assets.Work.Adventure.Code;
using UnityEngine;

namespace Work.Dispatch.Code.Data
{
    [CreateAssetMenu(fileName = "DispatchRegion", menuName = "Dispatch/Region")]
    public sealed class DispatchRegionSO : ScriptableObject
    {
        [SerializeField] private MapInfoSO region;
        [SerializeField] private Sprite illustration;
        [SerializeField, Min(0)] private int baseTravelTime = 1;
        [SerializeField] private List<DispatchMaterialRule> materials = new List<DispatchMaterialRule>();
        [SerializeField] private DispatchRareRewardTable rareRewards = new DispatchRareRewardTable();

        public MapInfoSO Region => region;
        public string RegionId => region != null ? region.RegionId : string.Empty;
        public string DisplayName => region != null ? region.MapName : string.Empty;
        public Sprite Illustration => illustration;
        public int BaseTravelTime => Mathf.Max(0, baseTravelTime);
        public IReadOnlyList<DispatchMaterialRule> Materials => materials;
        public DispatchRareRewardTable RareRewards => rareRewards;

        public bool TryFindMaterial(string itemId, out DispatchMaterialRule materialRule)
        {
            if (string.IsNullOrWhiteSpace(itemId) == false)
            {
                for (int i = 0; i < materials.Count; i++)
                {
                    DispatchMaterialRule candidate = materials[i];
                    if (candidate != null
                        && string.Equals(candidate.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        materialRule = candidate;
                        return true;
                    }
                }
            }

            materialRule = null;
            return false;
        }
    }
}
