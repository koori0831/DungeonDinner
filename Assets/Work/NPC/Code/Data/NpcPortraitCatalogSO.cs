using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.NPC.Code.Data
{
    [Serializable]
    public sealed class NpcPortraitEntry
    {
        [SerializeField] private string npcId;
        [SerializeField] private Sprite portrait;

        public string NpcId => npcId;
        public Sprite Portrait => portrait;
    }

    [CreateAssetMenu(
        fileName = "NpcPortraitCatalog",
        menuName = "Dungeon Dinner/NPC/Portrait Catalog")]
    public sealed class NpcPortraitCatalogSO : ScriptableObject
    {
        public const string DefaultResourcePath = "NPCData/NpcPortraitCatalog";

        [SerializeField] private Sprite fallbackPortrait;
        [SerializeField] private List<NpcPortraitEntry> portraits = new List<NpcPortraitEntry>();

        public Sprite FallbackPortrait => fallbackPortrait;
        public IReadOnlyList<NpcPortraitEntry> Portraits => portraits;

        public static NpcPortraitCatalogSO LoadDefault()
        {
            return Resources.Load<NpcPortraitCatalogSO>(DefaultResourcePath);
        }

        public Sprite GetPortrait(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId) == false)
            {
                string normalizedId = npcId.Trim();
                for (int i = 0; i < portraits.Count; i++)
                {
                    NpcPortraitEntry entry = portraits[i];
                    if (entry != null
                        && entry.Portrait != null
                        && string.Equals(entry.NpcId?.Trim(), normalizedId, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Portrait;
                    }
                }
            }

            return fallbackPortrait;
        }
    }
}
