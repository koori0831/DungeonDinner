using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Items.Code;

namespace Work.Dispatch.Code.Data
{
    [CreateAssetMenu(fileName = "DispatchCatalog", menuName = "Dispatch/Catalog")]
    public sealed class DispatchCatalogSO : ScriptableObject
    {
        [SerializeField] private ItemCatalogSO itemCatalog;
        [SerializeField] private List<DispatchRegionSO> regions = new List<DispatchRegionSO>();
        [SerializeField] private List<DispatchNpcRule> npcRules = new List<DispatchNpcRule>();
        [SerializeField, Min(1)] private int maxMaterialTypes = 3;

        public ItemCatalogSO ItemCatalog => itemCatalog;
        public IReadOnlyList<DispatchRegionSO> Regions => regions;
        public IReadOnlyList<DispatchNpcRule> NpcRules => npcRules;
        public int MaxMaterialTypes => Mathf.Max(1, maxMaterialTypes);

        public bool TryFindRegion(string regionId, out DispatchRegionSO region)
        {
            if (string.IsNullOrWhiteSpace(regionId) == false)
            {
                for (int i = 0; i < regions.Count; i++)
                {
                    DispatchRegionSO candidate = regions[i];
                    if (candidate != null
                        && string.Equals(candidate.RegionId, regionId, StringComparison.OrdinalIgnoreCase))
                    {
                        region = candidate;
                        return true;
                    }
                }
            }

            region = null;
            return false;
        }

        public bool TryFindNpcRule(string npcId, out DispatchNpcRule npcRule)
        {
            if (string.IsNullOrWhiteSpace(npcId) == false)
            {
                for (int i = 0; i < npcRules.Count; i++)
                {
                    DispatchNpcRule candidate = npcRules[i];
                    if (candidate != null
                        && string.Equals(candidate.NpcId, npcId, StringComparison.OrdinalIgnoreCase))
                    {
                        npcRule = candidate;
                        return true;
                    }
                }
            }

            npcRule = null;
            return false;
        }

        public List<string> BuildValidationMessages()
        {
            List<string> messages = new List<string>();
            HashSet<string> regionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> npcIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (itemCatalog == null)
            {
                messages.Add("Dispatch catalog has no ItemCatalogSO.");
            }

            for (int i = 0; i < regions.Count; i++)
            {
                DispatchRegionSO region = regions[i];
                if (region == null || string.IsNullOrWhiteSpace(region.RegionId))
                {
                    messages.Add($"Dispatch region entry {i} has no valid RegionId.");
                    continue;
                }

                if (regionIds.Add(region.RegionId) == false)
                {
                    messages.Add($"Duplicate dispatch RegionId: {region.RegionId}");
                }

                ValidateRegionMaterials(region, messages);
            }

            for (int i = 0; i < npcRules.Count; i++)
            {
                DispatchNpcRule npcRule = npcRules[i];
                if (npcRule == null || string.IsNullOrWhiteSpace(npcRule.NpcId))
                {
                    messages.Add($"Dispatch NPC rule {i} has no NpcId.");
                    continue;
                }

                if (npcIds.Add(npcRule.NpcId) == false)
                {
                    messages.Add($"Duplicate dispatch NpcId: {npcRule.NpcId}");
                }
            }

            return messages;
        }

        private void ValidateRegionMaterials(DispatchRegionSO region, ICollection<string> messages)
        {
            HashSet<string> itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < region.Materials.Count; i++)
            {
                DispatchMaterialRule material = region.Materials[i];
                if (material == null || string.IsNullOrWhiteSpace(material.ItemId))
                {
                    messages.Add($"Dispatch material is missing in region {region.RegionId}, entry {i}.");
                    continue;
                }

                if (itemIds.Add(material.ItemId) == false)
                {
                    messages.Add($"Duplicate material {material.ItemId} in region {region.RegionId}.");
                }

                if (material.MinYieldPercent > material.MaxYieldPercent)
                {
                    messages.Add($"Invalid yield range for {material.ItemId} in region {region.RegionId}.");
                }

                if (itemCatalog != null && itemCatalog.TryFindItem(material.ItemId, out _) == false)
                {
                    messages.Add($"ItemCatalogSO does not contain {material.ItemId}.");
                }
            }
        }
    }
}
