using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonDinner.Npc.PlayModeTests
{
    [Category("NewGameStartup")]
    public sealed class NewGameStartupPlayModeTests
    {
        private const string TitlePath = "Assets/Work/Title/Scene/TitleScene.unity";
        private const string MainPath = "Assets/MainScene.unity";
        private const string HistoryKey = "DungeonDinner.NpcEncounterHistory";
        private const string TimeKey = "DungeonDinner.GameTime";
        private const string KnowledgeKey = "DungeonDinner.CookingKnowledge";
        private const string DispatchKey = "DungeonDinner.Dispatch";
        private const string WalletKey = "DungeonDinner.CookingRewardBalance";
        private const string VolumeKey = "GameSettings.MasterVolume";
        private static readonly string[] StringKeys = { HistoryKey, TimeKey, KnowledgeKey, DispatchKey };
        private readonly Dictionary<string, string> _savedStrings = new Dictionary<string, string>();
        private bool _hadWallet;
        private int _savedWallet;
        private bool _hadVolume;
        private float _savedVolume;
        private float _listenerVolume;
        private float _timeScale;

        [SetUp]
        public void PreservePreferences()
        {
            _savedStrings.Clear();
            foreach (string key in StringKeys)
                if (PlayerPrefs.HasKey(key)) _savedStrings.Add(key, PlayerPrefs.GetString(key));
            _hadWallet = PlayerPrefs.HasKey(WalletKey);
            _savedWallet = PlayerPrefs.GetInt(WalletKey);
            _hadVolume = PlayerPrefs.HasKey(VolumeKey);
            _savedVolume = PlayerPrefs.GetFloat(VolumeKey);
            _listenerVolume = AudioListener.volume;
            _timeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [UnityTearDown]
        public IEnumerator RestorePreferences()
        {
            // Unload gameplay before restoring saves so scene teardown cannot overwrite them.
            Scene previous = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("NewGameStartupCleanup");
            SceneManager.SetActiveScene(empty);
            if (previous.IsValid() && previous.isLoaded)
                yield return SceneManager.UnloadSceneAsync(previous);
            foreach (string key in StringKeys)
            {
                if (_savedStrings.TryGetValue(key, out string value)) PlayerPrefs.SetString(key, value);
                else PlayerPrefs.DeleteKey(key);
            }
            if (_hadWallet) PlayerPrefs.SetInt(WalletKey, _savedWallet);
            else PlayerPrefs.DeleteKey(WalletKey);
            if (_hadVolume) PlayerPrefs.SetFloat(VolumeKey, _savedVolume);
            else PlayerPrefs.DeleteKey(VolumeKey);
            PlayerPrefs.Save();
            AudioListener.volume = _listenerVolume;
            Time.timeScale = _timeScale;
        }

        [UnityTest]
        public IEnumerator TitleStart_WithCompletedSavedBusiness_StartsFreshOnEveryVisit()
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                yield return LoadScene(TitlePath);
                yield return new WaitForSecondsRealtime(0.75f);
                SeedCompletedBusiness(iteration == 2 ? 13 : 0);
                Invoke(FindBehaviour("TitleUIManager"), "StartGame");
                yield return WaitForMainScene();
                yield return new WaitForSecondsRealtime(0.85f);
                yield return WaitForAdventure();
                AssertFreshAdventure();
            }
        }

        [UnityTest]
        public IEnumerator TitleStart_DuringOpeningFade_CompletesAndStartsAdventure()
        {
            yield return LoadScene(TitlePath);
            // Start on the first frame, while FadeObject is still clearing the title.
            SeedCompletedBusiness(0);
            Invoke(FindBehaviour("TitleUIManager"), "StartGame");
            yield return WaitForMainScene();
            yield return new WaitForSecondsRealtime(0.85f);
            yield return WaitForAdventure();
                AssertFreshAdventure();
        }

        private static void SeedCompletedBusiness(int elapsedTime)
        {
            int day = elapsedTime / 6 + 1;
            string record = "{\"npcId\":\"Odin\",\"eventId\":\"OldVisit\",\"regionId\":\"MossCave\",\"day\":" + day + "}";
            PlayerPrefs.SetString(HistoryKey, "{\"lastEncounterDay\":" + day + ",\"recentEncounters\":[" + record + "," + record + "," + record + "]}");
            PlayerPrefs.SetString(TimeKey, "{\"TotalElapsedTime\":" + elapsedTime + "}");
            PlayerPrefs.SetString(KnowledgeKey, "{\"schemaVersion\":2,\"recipeRecords\":[{\"recipeId\":\"mushroom_soup_pane\"}]}");
            PlayerPrefs.SetString(DispatchKey, "{\"SaveVersion\":1,\"ReturnedReports\":[]}");
            PlayerPrefs.SetInt(WalletKey, 987);
            PlayerPrefs.SetFloat(VolumeKey, 0.37f);
            PlayerPrefs.Save();
        }

        private static IEnumerator LoadScene(string path)
        {
            int index = SceneUtility.GetBuildIndexByScenePath(path);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), path + " must be enabled in Build Settings.");
            yield return SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
        }

        private static IEnumerator WaitForMainScene()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().path != MainPath && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(MainPath),
                "Title scene transition did not finish; check the fade completion callback.");
            yield return null;
        }

        private static void AssertFreshAdventure()
        {
            var director = FindBehaviour("NpcEncounterDirector");
            var runner = FindBehaviour("NpcConversationRunner");
            var panel = FindBehaviour("CookingGamePanel");
            Assert.That(Property<int>(director, "CurrentDay"), Is.EqualTo(1));
            Assert.That(Property<int>(director, "EncountersStartedToday"), Is.Zero);
            Assert.That(Property<bool>(runner, "HasActiveConversation"), Is.False);
            Assert.That(Property<object>(panel, "CurrentScreen").ToString(), Is.EqualTo("None"));
            Assert.That(Property<bool>(FindBehaviour("AdventureManager"), "IsAdventureRunning"), Is.True);
            Assert.That(SceneBehaviours().Any(b => b.GetType().Name == "AdventureMapUI"), Is.False);
            var inventory = FindBehaviour("PlayerInventoryModule");
            int capacity = Property<int>(inventory, "SlotCapacity");
            for (int i = 0; i < capacity; i++)
            {
                object slot = inventory.GetType().GetMethod("GetSlot").Invoke(inventory, new object[] { i });
                if (slot != null) Assert.That(Property<int>(slot, "Amount"), Is.Zero);
            }
            Assert.That(Property<int>(FindBehaviour("GameTimeService"), "TotalElapsedTime"), Is.Zero);
            var wallet = FindBehaviour("CookingRewardWallet");
            Assert.That(Property<int>(wallet, "Balance"), Is.EqualTo(Property<int>(wallet, "StartingBalance")));
            Assert.That(Property<int>(FindBehaviour("CookingKnowledgeStore"), "DiscoveredRecipeCount"), Is.Zero);
            Assert.That(PlayerPrefs.HasKey(DispatchKey), Is.False);
            Assert.That(PlayerPrefs.GetFloat(VolumeKey), Is.EqualTo(.37f).Within(.001f));
        }

        private static IEnumerator WaitForAdventure()
        {
            float deadline = Time.realtimeSinceStartup + 20;
            while (Property<object>(FindBehaviour("AdventureManager"), "Phase").ToString() != "Dialog"
                   && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Property<object>(FindBehaviour("AdventureManager"), "Phase").ToString(), Is.EqualTo("Dialog"));
        }

        private static IEnumerable<MonoBehaviour> SceneBehaviours()
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Where(b => b != null);
        }

        private static MonoBehaviour FindBehaviour(string typeName)
        {
            MonoBehaviour result = SceneBehaviours().FirstOrDefault(b => b.GetType().Name == typeName);
            Assert.That(result, Is.Not.Null, typeName + " is missing from the active scene.");
            return result;
        }

        private static T Property<T>(object target, string name)
        {
            return (T)target.GetType().GetProperty(name).GetValue(target);
        }

        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, name + " was not found.");
            method.Invoke(target, null);
        }
    }
}
