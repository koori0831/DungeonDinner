using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonDinner.Npc.PlayModeTests
{
    [Category("CookingBusinessFlow")]
    public sealed class CookingBusinessFlowPlayModeTests
    {
        private const string MainPath = "Assets/MainScene.unity";
        private const string HistoryKey = "DungeonDinner.NpcEncounterHistory";
        private const string TimeKey = "DungeonDinner.GameTime";
        private const string DispatchKey = "DungeonDinner.Dispatch";
        private const string WalletKey = "DungeonDinner.CookingRewardBalance";
        private static readonly string[] StringKeys =
        {
            HistoryKey, TimeKey, DispatchKey, "DungeonDinner.CookingKnowledge"
        };

        private readonly Dictionary<string, string> _savedStrings = new Dictionary<string, string>();
        private bool _hadWallet;
        private int _savedWallet;
        private float _timeScale;
        private MonoBehaviour _flow;
        private MonoBehaviour _director;
        private MonoBehaviour _time;
        private MonoBehaviour _menu;
        private Button _nextBusiness;

        [SetUp]
        public void PreservePreferences()
        {
            _savedStrings.Clear();
            foreach (string key in StringKeys)
                if (PlayerPrefs.HasKey(key)) _savedStrings.Add(key, PlayerPrefs.GetString(key));
            _hadWallet = PlayerPrefs.HasKey(WalletKey);
            _savedWallet = PlayerPrefs.GetInt(WalletKey);
            _timeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [UnityTearDown]
        public IEnumerator RestorePreferences()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("CookingBusinessFlowCleanup");
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
            PlayerPrefs.Save();
            Time.timeScale = _timeScale;
        }

        [UnityTest]
        public IEnumerator CompletedBusiness_OneNextBusinessClickStartsCustomerAcrossDayBoundary()
        {
            // Cover a three-unit wait, a one-unit wait, an exact day boundary and overflow.
            foreach (int initialTime in new[] { 0, 2, 3, 5 })
            {
                yield return LoadAndCloseCompletedBusiness(initialTime);
                int expectedTime = Math.Max(6, initialTime + 3);

                _nextBusiness.onClick.Invoke();
                Assert.That(Property<int>(_time, "TotalElapsedTime"), Is.EqualTo(expectedTime),
                    $"Next business used the wrong waiting time after closing at {initialTime + 3}.");
                yield return new WaitForSecondsRealtime(0.8f);
                AssertBusinessStarted(expectedTime);
            }
        }

        [UnityTest]
        public IEnumerator PreparationTime_NextBusinessWaitsOnlyForRemainingTime()
        {
            foreach (int preparationTime in new[] { 2, 4 })
            {
                yield return LoadAndCloseCompletedBusiness(0);
                AdvancePreparationTime(preparationTime);

                _nextBusiness.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.8f);
                AssertBusinessStarted(Math.Max(6, 3 + preparationTime));
            }
        }

        [UnityTest]
        public IEnumerator NextBusiness_RepeatedClicksKeepTheSameCustomerAndTime()
        {
            yield return LoadAndCloseCompletedBusiness(0);
            _nextBusiness.onClick.Invoke();
            _nextBusiness.onClick.Invoke();
            _nextBusiness.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.8f);
            AssertBusinessStarted(6);

            MonoBehaviour runner = FindBehaviour("NpcConversationRunner");
            string npcId = Property<string>(runner, "CurrentNpcId");
            _nextBusiness.onClick.Invoke();
            yield return null;
            AssertBusinessStarted(6);
            Assert.That(Property<string>(runner, "CurrentNpcId"), Is.EqualTo(npcId));
        }

        private IEnumerator LoadAndCloseCompletedBusiness(int elapsedTime)
        {
            string record = "{\"npcId\":\"Odin\",\"eventId\":\"OldVisit\",\"regionId\":\"MossCave\",\"day\":1}";
            PlayerPrefs.SetString(HistoryKey,
                "{\"lastEncounterDay\":1,\"recentEncounters\":[" + record + "," + record + "," + record + "]}");
            PlayerPrefs.SetString(TimeKey, "{\"TotalElapsedTime\":" + elapsedTime + "}");
            PlayerPrefs.DeleteKey(DispatchKey);

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(MainPath);
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0), MainPath + " must be enabled in Build Settings.");
            void ConfigureFixture(Scene loaded, LoadSceneMode mode)
            {
                var preparation = FindBehaviour("PreparationManager");
                preparation.GetType().GetField("startWithAdventure", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(preparation, false);
            }
            SceneManager.sceneLoaded += ConfigureFixture;
            yield return SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            SceneManager.sceneLoaded -= ConfigureFixture;
            yield return null;

            _flow = FindBehaviour("CookingBusinessFlowController");
            _director = FindBehaviour("NpcEncounterDirector");
            _time = FindBehaviour("GameTimeService");
            _menu = FindBehaviour("PreparationMenu");
            _nextBusiness = _menu.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "GO_NextBusiness");

            var inventory = FindBehaviour("PlayerInventoryModule");
            var item = Resources.FindObjectsOfTypeAll<ScriptableObject>().First(o => o.GetType().Name == "IngredientItemDataSO");
            inventory.GetType().GetMethod("AddItem").Invoke(inventory, new object[] { item, 20 });
            _flow.GetType().GetField("_businessClosed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_flow, false);
            _flow.GetType().GetMethod("StartNextCustomer").Invoke(_flow, null);
            Assert.That(Property<int>(_director, "EncountersStartedToday"), Is.EqualTo(3));
            Button close = Field<Button>(_flow, "closeShopButton");
            Assert.That(close.gameObject.activeInHierarchy, Is.True);
            close.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(Property<int>(_time, "TotalElapsedTime"), Is.EqualTo(elapsedTime + 3));
            Assert.That(Field<bool>(_flow, "_businessClosed"), Is.True);
            Assert.That(Field<RectTransform>(_menu, "root").anchoredPosition.x,
                Is.EqualTo(Field<float>(_menu, "offset_x")).Within(0.1f));
        }

        private void AdvancePreparationTime(int amount)
        {
            MethodInfo advance = _time.GetType().GetMethod("AdvanceTime");
            Type activityType = advance.GetParameters()[1].ParameterType;
            advance.Invoke(_time, new[] { (object)amount, Enum.Parse(activityType, "Adventure") });
        }

        private void AssertBusinessStarted(int expectedTime)
        {
            Assert.That(Property<int>(_time, "TotalElapsedTime"), Is.EqualTo(expectedTime));
            Assert.That(Property<int>(_director, "CurrentDay"), Is.EqualTo(expectedTime / 6 + 1));
            Assert.That(Property<int>(_director, "EncountersStartedToday"), Is.EqualTo(1));
            Assert.That(Property<bool>(FindBehaviour("NpcConversationRunner"), "HasActiveConversation"), Is.True);
            Assert.That(Property<object>(FindBehaviour("CookingGamePanel"), "CurrentScreen").ToString(),
                Is.EqualTo("NpcConversation"));
            Assert.That(Field<bool>(_flow, "_businessClosed"), Is.False);
            Assert.That(Field<Button>(_flow, "closeShopButton").gameObject.activeInHierarchy, Is.False,
                "Next business must not show the close-shop button again without serving a customer.");
            Assert.That(Field<RectTransform>(_flow, "actionRoot").gameObject.activeInHierarchy, Is.False);
            Assert.That(Field<RectTransform>(_menu, "root").anchoredPosition.x,
                Is.EqualTo(Field<float>(_menu, "_hide_x")).Within(0.1f));
        }

        private static MonoBehaviour FindBehaviour(string typeName)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Single(behaviour => behaviour != null && behaviour.GetType().Name == typeName);
        }

        private static T Property<T>(object target, string name)
        {
            return (T)target.GetType().GetProperty(name).GetValue(target);
        }

        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
