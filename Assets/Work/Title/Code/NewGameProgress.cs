using UnityEngine;
using Work.Cook.Code.Runtime.Systems;
using Work.Dispatch.Code.Runtime;
using Work.NPC.Code.Runtime;
using Work.TimeSystem;

namespace Work.Title
{
    public static class NewGameProgress
    {
        /// <summary>
        /// Reset gameplay saves before the destination scene initializes its services.
        /// Settings such as master volume belong to the player, not the game session.
        /// </summary>
        public static void ResetSavedProgress()
        {
            PlayerPrefs.DeleteKey(NpcEncounterHistory.DefaultSaveKey);
            PlayerPrefs.DeleteKey(GameTimeRepository.DefaultSaveKey);
            PlayerPrefs.DeleteKey(DispatchRepository.DefaultSaveKey);
            PlayerPrefs.DeleteKey(CookingKnowledgeStore.DefaultSaveKey);
            PlayerPrefs.DeleteKey(CookingRewardWallet.DefaultSaveKey);
            PlayerPrefs.Save();
        }
    }
}
