using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    [Serializable]
    public class InfoDictionaryCategoryData
    {
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite MarkIcon { get; private set; }
        [field: SerializeField] public MarkerEnum Marker { get; private set; }
        [field: SerializeField] public ViewHaveInfoEnum ViewType { get; private set; }
        [field: SerializeField] public List<InfoDictionaryEntryData> Entries { get; private set; } = new List<InfoDictionaryEntryData>();

        public InfoDictionaryCategoryData()
        {
        }

        public InfoDictionaryCategoryData(
            string displayName,
            Sprite markIcon,
            MarkerEnum marker,
            ViewHaveInfoEnum viewType,
            IEnumerable<InfoDictionaryEntryData> entries)
        {
            DisplayName = displayName;
            MarkIcon = markIcon;
            Marker = marker;
            ViewType = viewType;
            Entries = entries != null ? new List<InfoDictionaryEntryData>(entries) : new List<InfoDictionaryEntryData>();
        }
    }

    [Serializable]
    public class InfoDictionaryEntryData : IHaveDisplayNameInfo, IHaveIconInfo, IHaveDescriptionInfo
    {
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public string Description { get; private set; }

        public InfoDictionaryEntryData()
        {
        }

        public InfoDictionaryEntryData(string displayName, Sprite icon, string description)
        {
            DisplayName = displayName;
            Icon = icon;
            Description = description;
        }
    }
}
