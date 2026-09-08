using System;
using UnityEditor;
using UnityEngine;

namespace Work.Adventure.Code.Editor
{
    public static class AdventureEquipmentInstaller
    {
        // Merge into the open scene without saving or replacing any other scene edits.
        [MenuItem("Tools/Dungeon Dinner/Adventure/Register Equipment In Open Scene %#F7")]
        public static void RegisterInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("플레이 모드를 종료한 뒤 등록하세요.");
            var names = new[] { "Find_RopeCache", "Meet_LanternKeeper", "Find_CookKit", "Meet_SupplyPorter",
                "Meet_RopeWeaver", "Meet_BottleTrader", "Find_LedgePantry", "Meet_PitAdventurer",
                "Find_DarkNest", "Find_HotSpringBasket", "Find_StickyPool", "Find_RootCellar", "Find_HangingPantry", "Find_MossyStair", "Meet_LostMushroomChild", "Find_CrackedStoreroom", "Find_CrabSnare", "Find_TiltedSaltCart", "Find_ThornLunchbox", "Meet_CaughtApron", "Find_SeepingSaltWell", "Meet_LeakingPack", "Find_SlimeCurtain", "Meet_MushroomWaterer", "Find_SleepingCrab", "Find_CollapsedShelf", "Meet_SootyCook", "Find_GlowingCrack", "Find_DewMushrooms", "Meet_StatueCaretaker", "Find_SlimeTracks", "Find_SaltDrips", "Find_CrabMolting", "Find_StickyLatch", "Meet_MushroomSplinter", "Find_SlimePicnic" };
            foreach (var manager in UnityEngine.Object.FindObjectsByType<AdventureManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(manager);
                var list = serialized.FindProperty("eventList");
                foreach (var name in names)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<AdventureEventSO>("Assets/Work/Adventure/SO/Dialog/" + name + ".asset");
                    if (asset == null) throw new InvalidOperationException("Missing event: " + name);
                    bool exists = false;
                    for (int i = 0; i < list.arraySize; i++) exists |= list.GetArrayElementAtIndex(i).objectReferenceValue == asset;
                    if (exists) continue;
                    int index = list.arraySize;
                    list.InsertArrayElementAtIndex(index);
                    list.GetArrayElementAtIndex(index).objectReferenceValue = asset;
                }
                serialized.ApplyModifiedProperties();
            }
            Debug.Log("Equipment events merged into open scene. Other edits preserved; scene not saved.");
        }
    }
}
