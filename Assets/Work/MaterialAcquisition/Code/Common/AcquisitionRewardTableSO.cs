using System.Collections.Generic;
using UnityEngine;

namespace Work.MaterialAcquisition.Code.Common
{
    [CreateAssetMenu(
        fileName = "AcquisitionRewardTable",
        menuName = "Dungeon Dinner/Material Acquisition/Reward Table"
    )]
    public sealed class AcquisitionRewardTableSO : ScriptableObject
    {
        private const int MIN_ROLL_COUNT = 0;

        [SerializeField]
        private string tableId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private List<AcquisitionRewardEntry> entries = new List<AcquisitionRewardEntry>();

        [SerializeField]
        [Min(MIN_ROLL_COUNT)]
        private int minRollCount = 1;

        [SerializeField]
        [Min(MIN_ROLL_COUNT)]
        private int maxRollCount = 1;

        [SerializeField]
        private bool allowDuplicateItems = true;

        [SerializeField]
        private AcquisitionRewardTableMode mode = AcquisitionRewardTableMode.ChanceEach;

        public string TableId => string.IsNullOrWhiteSpace(tableId) == false ? tableId : name;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) == false ? displayName : name;
        public IReadOnlyList<AcquisitionRewardEntry> Entries => entries;
        public int MinRollCount => Mathf.Max(MIN_ROLL_COUNT, minRollCount);
        public int MaxRollCount => Mathf.Max(MinRollCount, maxRollCount);
        public bool AllowDuplicateItems => allowDuplicateItems;
        public AcquisitionRewardTableMode Mode => mode;

        private void OnValidate()
        {
            minRollCount = Mathf.Max(MIN_ROLL_COUNT, minRollCount);
            maxRollCount = Mathf.Max(minRollCount, maxRollCount);

            if (entries == null)
            {
                entries = new List<AcquisitionRewardEntry>();
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                entries[i]?.Validate();
            }
        }
    }
}
