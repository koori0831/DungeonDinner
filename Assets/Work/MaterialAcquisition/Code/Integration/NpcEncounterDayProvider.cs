using UnityEngine;
using Work.NPC.Code.Runtime;

namespace Work.MaterialAcquisition.Code.Integration
{
    [DisallowMultipleComponent]
    public sealed class NpcEncounterDayProvider : MonoBehaviour, IAcquisitionDayProvider
    {
        [SerializeField] private NpcEncounterDirector encounterDirector;

        public int CurrentDay
        {
            get
            {
                EnsureReferences();
                return encounterDirector != null ? encounterDirector.CurrentDay : 0;
            }
        }

        public string CurrentDayText
        {
            get
            {
                EnsureReferences();
                return encounterDirector != null ? encounterDirector.CurrentDateText : string.Empty;
            }
        }

        private void Awake()
        {
            EnsureReferences();
        }

        public void AdvanceDay()
        {
            EnsureReferences();

            if (encounterDirector == null)
            {
                Debug.LogWarning("NpcEncounterDayProvider cannot advance day because NpcEncounterDirector is missing.", this);
                return;
            }

            encounterDirector.AdvanceDay();
        }

        private void EnsureReferences()
        {
            if (encounterDirector == null)
                encounterDirector = FindFirstObjectByType<NpcEncounterDirector>();
        }
    }
}
