#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Work.Adventure.Code;
using Work.Adventure.Code.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;
using Work.NPC.Code.Runtime;
using Work.Players.Code.Inventory;
using Work.TimeSystem;
using Work.UtillUI.Code;

namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>Opt-in development-player regression driver. Inputs travel through the Input System and EventSystem.</summary>
    public sealed partial class PlaytestFeedbackProbe : MonoBehaviour
    {
        public static Action<int, int> EditorResolution;
        public static Action<bool> EditorCompleted;
        private readonly Dictionary<string, string> _saved = new Dictionary<string, string>();
        private static readonly string[] SaveKeys = { "DungeonDinner.NpcEncounterHistory", "DungeonDinner.GameTime", "DungeonDinner.CookingKnowledge", "DungeonDinner.Dispatch" };
        private readonly List<string> _errors = new List<string>();
        private Mouse _mouse;
        private Keyboard _keyboard;
        private bool _hadWallet;
        private int _wallet;
        private string _output;
        [SerializeField] private GameObject standaloneOverlayPrefab;
        private CookingMiniGameRouterView _routerOverride;
        private CookingGamePanel _panel;
        private int _checks;
        private RenderTexture _captureTarget;
        private int _captureWidth = 1920, _captureHeight = 1080;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInDevelopmentPlayer()
        {
            if (!Application.isEditor && System.Environment.GetCommandLineArgs().Contains("-feedback-qa"))
            {
                var probe = FindFirstObjectByType<PlaytestFeedbackProbe>(FindObjectsInactive.Include);
                if (probe == null) probe = new GameObject("PlaytestFeedbackProbe").AddComponent<PlaytestFeedbackProbe>();
                probe.gameObject.SetActive(true);
                probe.BeginValidation();
            }
        }

        public void BeginValidation()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            _output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "FeedbackQA", Application.isEditor ? "Editor" : "Windows"));
            Directory.CreateDirectory(_output);
            File.WriteAllText(Path.Combine(_output, "report.txt"), DateTime.Now.ToString("O") + "\nRUNNING\n");
            foreach (string key in SaveKeys) if (PlayerPrefs.HasKey(key)) _saved[key] = PlayerPrefs.GetString(key);
            _hadWallet = PlayerPrefs.HasKey("DungeonDinner.CookingRewardBalance");
            _wallet = PlayerPrefs.GetInt("DungeonDinner.CookingRewardBalance");
            _mouse = InputSystem.AddDevice<Mouse>("FeedbackMouse");
            _keyboard = InputSystem.AddDevice<Keyboard>("FeedbackKeyboard");
            Application.logMessageReceived += OnLog;
            SceneManager.sceneLoaded += OnLoaded;
            StartCoroutine(Guard(Run()));
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                _errors.Add(message + "\n" + stack);
        }

        private IEnumerator Guard(IEnumerator work)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(work);
            Exception failure = null;
            while (stack.Count > 0)
            {
                object next = null;
                try
                {
                    if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                    next = stack.Peek().Current;
                }
                catch (Exception ex) { failure = ex; break; }
                if (next is IEnumerator nested) stack.Push(nested);
                else yield return next;
            }
            if (failure != null) { Write("FAIL " + failure); yield return Capture("failure"); }
            foreach (string error in _errors) Write("RUNTIME ERROR " + error);
            bool passed = failure == null && _errors.Count == 0;
            Write((passed ? "PASS" : "FAIL") + " checks=" + _checks);
            // Unload gameplay before restoring preferences, including teardown saves.
            var cleanup = SceneManager.CreateScene("FeedbackCleanup");
            SceneManager.SetActiveScene(cleanup);
            var oldScenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s != cleanup).ToArray();
            foreach (var scene in oldScenes) yield return SceneManager.UnloadSceneAsync(scene);
            foreach (string key in SaveKeys)
                if (_saved.TryGetValue(key, out string value)) PlayerPrefs.SetString(key, value); else PlayerPrefs.DeleteKey(key);
            if (_hadWallet) PlayerPrefs.SetInt("DungeonDinner.CookingRewardBalance", _wallet); else PlayerPrefs.DeleteKey("DungeonDinner.CookingRewardBalance");
            PlayerPrefs.Save();
            Application.logMessageReceived -= OnLog;
            SceneManager.sceneLoaded -= OnLoaded;
            if (_captureTarget != null) { _captureTarget.Release(); Destroy(_captureTarget); }
            InputSystem.RemoveDevice(_mouse); InputSystem.RemoveDevice(_keyboard);
            if (Application.isEditor) EditorCompleted?.Invoke(passed); else Application.Quit(passed ? 0 : 1);
        }

        private IEnumerator Run()
        {
            yield return SetResolution(1920, 1080);
            yield return Unblocked();
            yield return Capture("01-title");
            var start = FindObjectsByType<TitleUIButton>(FindObjectsSortMode.None).Single(b => b.Action.ToString() == "Start");
            yield return Click(start.GetComponent<RectTransform>());
            yield return Until(() => SceneManager.GetActiveScene().name == "MainScene", 15, "Title → MainScene");
            _panel = Find<CookingGamePanel>();
            if (System.Environment.GetCommandLineArgs().Contains("-feedback-dispatch-only"))
            {
                yield return DispatchVisualChecks();
                yield break;
            }
            var adventure = Find<AdventureManager>();
            var inventory = Find<PlayerInventoryModule>();
            Check(Stock(inventory) == 0, "new game starts with zero ingredients");
            Check(!Find<NpcConversationRunner>().HasActiveConversation, "first customer waits for adventure");
            yield return Until(() => adventure.Phase == AdventurePhase.Dialog, 20, "first adventure dialog opens without map");
            yield return Unblocked();
            yield return Capture("02-first-adventure");
            var dialog = Find<AdventureDialogUI>();
            for (int i = 0; i < 30 && !dialog.IsAwaitingChoice; i++) yield return KeyPress(Key.Space);
            yield return Until(() => dialog.IsAwaitingChoice, 10, "first harvest choice");
            yield return Unblocked();
            var option = FindObjectsByType<OptionButtonUI>(FindObjectsSortMode.None).First(b => b.GetComponentInChildren<Button>().interactable);
            yield return Click(option.GetComponentInChildren<Button>().transform as RectTransform);
            for (int i = 0; i < 30 && adventure.Phase != AdventurePhase.Choice; i++) yield return KeyPress(Key.Space);
            yield return Until(() => adventure.Phase == AdventurePhase.Choice, 10, "first harvest completion");
            yield return Unblocked();
            Check(Stock(inventory) == 6, "six first-harvest ingredients, one each");
            Check(Enumerable.Range(0, inventory.SlotCapacity).Select(inventory.GetSlot).Where(s => s != null && s.Amount > 0).All(s => s.Amount == 1), "first harvest quantities");
            for (int i = 0; i < 16; i++) yield return KeyPress(Key.Space);
            Check(adventure.Phase == AdventurePhase.Choice && Stock(inventory) == 6, "space spam cannot retreat or duplicate harvest");
            yield return Capture("03-first-harvest");
            yield return Click(Field<Button>(Find<GoAndStopSelectUI>(), "stopButton").transform as RectTransform);
            yield return Until(() => !adventure.IsAdventureRunning && adventure.Phase == AdventurePhase.Inactive, 15, "return to preparation");
            yield return Unblocked();
            Check(Find<GameTimeService>().TotalElapsedTime == 1, "retreat charges time once");
            yield return Capture("04-preparation");
            yield return Click(Find<PreparationMenu>().GetComponentsInChildren<Button>(true).Single(b => b.name == "GO_NextBusiness").transform as RectTransform);
            yield return Until(() => Find<NpcConversationRunner>().HasActiveConversation, 8, "first business can start before any close");
            yield return Unblocked();
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture("05-order");
            float conversationDeadline = Time.realtimeSinceStartup + 45f;
            while (Time.realtimeSinceStartup < conversationDeadline && _panel.CurrentScreen != CookingGameScreenState.Inventory)
            {
                yield return KeyPress(Key.Space);
                var ready = Field<Button>(Find<NpcQuestionPanel>(), "skipButton");
                if (!Find<NpcConversationRunner>().IsPlaying && ready != null && ready.IsActive() && ready.IsInteractable())
                    yield return Click(ready.transform as RectTransform);
            }
            yield return Until(() => _panel.CurrentScreen == CookingGameScreenState.Inventory, 5, "conversation leads to ingredient bag");
            yield return BagChecks();
            // Exercise the regular bag, preparation, result and service UI with an experimental dish.
            var ingredient = _panel.FlowRunner.Catalog.Ingredients.First(i => i.IngredientId == "flat_mushroom");
            var bag = Find<CookingIngredientSelectionView>();
            _panel.ToggleIngredientSelection(ingredient);
            yield return Click(Field<Button>(bag, "confirmButton").transform as RectTransform);
            yield return Until(() => _panel.CurrentScreen == CookingGameScreenState.Preparation, 5, "bag confirms preparation");
            var preparation = ingredient.PreparationOptions.First(o => o.MiniGameType == CookingMiniGameType.Chopping);
            Check(_panel.SelectPreparation(ingredient, preparation), "regular cooking starts chopping");
            yield return Solve(ingredient, preparation, false);
            yield return Until(() => _panel.CurrentScreen == CookingGameScreenState.Result, 8, "cooking result opens");
            yield return new WaitForSecondsRealtime(1.3f);
            var resultView = Find<CookingResultView>();
            Check(_panel.GetCurrentDishResult() != null, "dish result created");
            foreach (var size in Resolutions())
            {
                yield return SetResolution(size.x, size.y);
                yield return Capture("07-result-" + size.x + "x" + size.y);
                CheckInside(resultView.transform as RectTransform, resultView.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform, "result inside canvas");
            }
            yield return SetResolution(1920, 1080);
            yield return Click(Field<Button>(resultView, "detailsToggleButton").transform as RectTransform);
            Check(resultView.DetailsOpen, "result details expand");
            yield return Capture("08-result-details");
            yield return Click(Field<Button>(resultView, "handToNpcButton").transform as RectTransform);
            var flow = Find<CookingBusinessFlowController>();
            for (int i = 0; i < 65 && !Field<Button>(flow, "closeShopButton").gameObject.activeInHierarchy; i++)
            {
                yield return KeyPress(Key.Space);
                if (_panel.CurrentScreen == CookingGameScreenState.Result && !resultView.IsRevealing)
                {
                    var hand = Field<Button>(resultView, "handToNpcButton");
                    if (hand.interactable) yield return Click(hand.transform as RectTransform);
                }
            }
            Check(Field<Button>(flow, "closeShopButton").gameObject.activeInHierarchy, "early close offered after serving");
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture("09-post-service");
            yield return Click(Field<Button>(flow, "closeShopButton").transform as RectTransform);
            yield return Unblocked();
            yield return new WaitForSecondsRealtime(0.7f);
            yield return DispatchCapture();
            Find<PreparationManager>().SelectAdventure();
            yield return Until(() => adventure.Phase == AdventurePhase.Dialog, 20, "re-exploration starts");
            Check(Field<AdventureEventSO>(adventure, "_currentEvent") != Field<AdventureEventSO>(adventure, "firstHarvestEvent"), "first harvest is not repeated");
            yield return Capture("10-re-exploration");
            if (System.Environment.GetCommandLineArgs().Contains("-feedback-layout-only")) yield break;
            // Independent minigame matrix uses the scene's actual router and explicit test setup.
            adventure.enabled = false;
            Field<AdventureBackground>(adventure, "background").gameObject.SetActive(false);
            dialog.gameObject.SetActive(false);
            Find<PreparationManager>().StopAdventure();
            _panel.OpenPreparation();
            Find<PreparationMenu>().HideUI();
            yield return new WaitForSecondsRealtime(.6f);
            yield return Unblocked();
            yield return DragRecoveryChecks();
            yield return MiniGameMatrix();
            Check(standaloneOverlayPrefab != null, "standalone prefab supplied by development build");
            var standalone = Instantiate(standaloneOverlayPrefab, _panel.MiniGameView.GetComponentInParent<Canvas>().rootCanvas.transform);
            standalone.SetActive(true);
            _routerOverride = standalone.GetComponentInChildren<CookingMiniGameRouterView>(true);
            _routerOverride.Initialize(_panel, _panel.FlowRunner);
            Write("STANDALONE PREFAB");
            yield return MiniGameMatrix();
            Destroy(standalone); _routerOverride = null;
            Check(_errors.Count == 0, "no runtime errors");
        }

        private IEnumerator DispatchCapture()
        {
            var menu = Find<PreparationMenu>();
            menu.HideUI();
            Find<PreparationManager>().SelectDispatch();
            var screen = Find<Work.Dispatch.Code.UI.DispatchScreenPresenter>();
            var document = screen.GetComponent<UnityEngine.UIElements.UIDocument>();
            var original = document.panelSettings;
            var settings = Instantiate(original);
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            target.Create(); settings.targetTexture = target;
            document.panelSettings = settings;
            var root = new GameObject("DispatchCaptureSurface", typeof(RectTransform), typeof(Canvas), typeof(RawImage));
            root.transform.SetParent(_panel.MiniGameView.GetComponentInParent<Canvas>().rootCanvas.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Canvas>().overrideSorting = true; root.GetComponent<Canvas>().sortingOrder = 20000;
            var graphic = root.GetComponent<RawImage>(); graphic.texture = target; graphic.raycastTarget = false;
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("09-dispatch-theme");
            document.panelSettings = original;
            Destroy(root); Destroy(settings); target.Release(); Destroy(target);
            screen.SendMessage("Close", SendMessageOptions.RequireReceiver);
            yield return Unblocked();
            yield return new WaitForSecondsRealtime(.6f);
        }

        private IEnumerator BagChecks()
        {
            var popup = Find<CookingIngredientBagPopup>();
            var window = Field<RectTransform>(popup, "window");
            var bag = window.GetComponent<CookingIngredientSelectionView>();
            foreach (var size in Resolutions())
            {
                yield return SetResolution(size.x, size.y);
                LogGuideGeometry();
                yield return Capture("06-bag-" + size.x + "x" + size.y);
                CheckInside(window, window.parent as RectTransform, "bag inside canvas at " + size);
                var history = _panel.NpcConversationView.transform as RectTransform;
                var order = Field<NpcOrderSlipPanel>(_panel.NpcRunner, "orderSlipPanel");
                Check(history.gameObject.activeInHierarchy, "conversation remains visible with bag at " + size);
                Check(order.gameObject.activeInHierarchy && Field<CanvasGroup>(order, "canvasGroup").alpha > .99f,
                    "order slip remains visible with bag at " + size);
                Check(Field<List<string>>(order, "_completedEntries").Count > 0, "heard order hints are recorded at " + size);
                Check(!ScreenRect(window).Overlaps(ScreenRect(history)), "bag leaves conversation readable at " + size);
                Check(!ScreenRect(window).Overlaps(ScreenRect(order.transform as RectTransform)), "bag leaves order readable at " + size);
            }
            yield return SetResolution(1920, 1080);
            var historyScroll = _panel.NpcConversationView.GetComponent<ScrollRect>();
            float beforeScroll = historyScroll.verticalNormalizedPosition;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = ScreenPoint(historyScroll.viewport), scroll = new Vector2(0, 120) });
            yield return Frames(3);
            Check(Mathf.Abs(historyScroll.verticalNormalizedPosition - beforeScroll) > .01f, "cooking conversation history scrolls with mouse wheel");
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = ScreenPoint(historyScroll.viewport) });
            historyScroll.verticalNormalizedPosition = 0f;
            _panel.SetIngredientSearchQuery("슬라임");
            var search = Field<TMP_InputField>(bag, "searchInputField");
            yield return Click(Field<Button>(popup, "collapseButton").transform as RectTransform);
            Check(!Field<CanvasGroup>(popup, "bodyGroup").blocksRaycasts, "collapsed bag releases body input");
            yield return Click(Field<Button>(popup, "openButton").transform as RectTransform);
            yield return Click(Field<Button>(popup, "closeButton").transform as RectTransform);
            Check(!Field<CanvasGroup>(popup, "windowGroup").blocksRaycasts, "closed bag releases input");
            yield return Click(Field<Button>(popup, "openButton").transform as RectTransform);
            Check(search.text == "슬라임", "bag retains search when folded/closed");
            _panel.SetIngredientSearchQuery("");
            var title = window.Find("TitleBar") as RectTransform;
            Vector2 home = window.anchoredPosition;
            yield return Drag(ScreenPoint(title), ScreenPoint(title) + new Vector2(-110, 60), 15);
            Check(Vector2.Distance(home, window.anchoredPosition) > 20, "title bar moves bag");
            yield return Drag(ScreenPoint(title), new Vector2(-150, Screen.height + 150), 15);
            CheckInside(window, window.parent as RectTransform, "dragged bag clamps to screen");
            window.anchoredPosition = home;
        }

private void LogGuideGeometry()
        {
            foreach (var guide in FindObjectsByType<Work.Cook.Code.Info.InfoDictionaryPanel>(FindObjectsSortMode.None))
                CheckInside(guide.transform as RectTransform, guide.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform, "guide inside canvas");
        }

        private static IEnumerable<Vector2Int> Resolutions()
        {
            return new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(1920,1200), new Vector2Int(2560,1440), new Vector2Int(3440,1440), new Vector2Int(1600,1200) };
        }

        private IEnumerator MiniGameMatrix()
        {
            var covered = new HashSet<CookingMiniGameType>();
            var catalog = _panel.FlowRunner.Catalog;
            foreach (var ingredient in catalog.Ingredients)
                foreach (var option in ingredient.PreparationOptions.Where(o => o.MiniGameType != CookingMiniGameType.None))
                {
                    covered.Add(option.MiniGameType);
                    yield return Solve(ingredient, option, true);
                }
            foreach (CookingMiniGameType type in Enum.GetValues(typeof(CookingMiniGameType)))
            {
                if (type == CookingMiniGameType.None || covered.Contains(type)) continue;
                // Exercise supported controllers that have no production ingredient assignment yet.
                var method = catalog.PreparationMethods.FirstOrDefault(m => m.MiniGameType == type);
                bool temporary = method == null;
                if (temporary)
                {
                    method = ScriptableObject.CreateInstance<PreparationMethodSO>();
                    typeof(PreparationMethodSO).GetField("miniGameType", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(method, type);
                }
                var option = new IngredientPreparationOption();
                typeof(IngredientPreparationOption).GetField("method", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(option, method);
                yield return Solve(catalog.Ingredients[0], option, true);
                covered.Add(type);
                if (temporary) Destroy(method);
            }
            Check(covered.Count == 10, "all ten minigame types completed");
        }

        private IEnumerator DragRecoveryChecks()
        {
            var ingredient = _panel.FlowRunner.Catalog.Ingredients.First(i => i.IngredientId == "rock_salt");
            var option = ingredient.PreparationOptions.First(o => o.MiniGameType == CookingMiniGameType.Grinding);
            var router = _panel.MiniGameView.GetComponentInChildren<CookingMiniGameRouterView>(true);
            _panel.MiniGameView.SetActive(true);
            int callbacks = 0;
            Check(router.StartMiniGame(ingredient, option, _ => callbacks++), "drag recovery fixture starts");
            yield return Unblocked();
            var controller = router.GetComponentInChildren<CookingGrindingMiniGameView>();
            var rect = (RectTransform)controller.transform;
            var home = ScreenPoint(rect, new Vector2(50, 0));
            yield return Press(home);
            yield return Move(home + new Vector2(0, 25), true);
            var visuals = Field<Image[]>(controller, "dragVisuals");
            Check(Field<int>(controller, "<ActivePointerId>k__BackingField") != int.MinValue, "pointer captured");
            Check(Field<Canvas[]>(controller, "_dragCanvases").All(c => c == null || c.enabled && c.overrideSorting), "drag graphics use upper canvas");
            var blocker = new GameObject("QA overlapping UI", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(_panel.MiniGameView.GetComponentInParent<Canvas>().rootCanvas.transform, false);
            var blockRect = (RectTransform)blocker.transform;
            blockRect.anchorMin = Vector2.zero; blockRect.anchorMax = Vector2.one;
            blockRect.offsetMin = blockRect.offsetMax = Vector2.zero;
            yield return Move(new Vector2(-40, -40), true);
            Check(Field<int>(controller, "<ActivePointerId>k__BackingField") != int.MinValue, "overlapping UI cannot steal active drag");
            yield return Release(new Vector2(-40, -40));
            Check(Field<int>(controller, "<ActivePointerId>k__BackingField") == int.MinValue, "outside release clears capture");
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null) Check(visuals[i].rectTransform.anchoredPosition == Field<Vector2[]>(controller, "_dragHomes")[i], "outside release restores tool");
            Destroy(blocker); yield return Frames(2);
            yield return Press(home); yield return Move(home + new Vector2(0, 25), true);
            controller.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            Check(Field<int>(controller, "<ActivePointerId>k__BackingField") == int.MinValue, "focus loss clears capture");
            yield return Release(home);
            router.CancelMiniGame();
            Check(callbacks == 0, "cancel does not deliver an obsolete result");
        }

        private IEnumerator Solve(IngredientSO ingredient, IngredientPreparationOption option, bool independent)
        {
            var router = _routerOverride != null ? _routerOverride : _panel.MiniGameView.GetComponentInChildren<CookingMiniGameRouterView>(true);
            CookingMiniGameResult result = null;
            int deliveries = 0;
            if (independent)
            {
                _panel.MiniGameView.SetActive(true);
                Check(router.StartMiniGame(ingredient, option, r => { result = r; deliveries++; }), "start " + ingredient.IngredientId + "/" + option.MiniGameType);
            }
            yield return Unblocked();
            yield return Frames(3);
            var controller = router.GetComponentsInChildren<CookingOverlayMiniGameController>().Single(c => c.gameObject.activeInHierarchy && c.CanPlay(option.MiniGameType));
            var rect = controller.transform as RectTransform;
            var host = router.GetComponentInChildren<CookingMiniGameOverlayHost>(true);
            if (!independent)
            {
                Check(_panel.NpcConversationView.activeInHierarchy, "conversation remains visible while preparing the order");
                var order = Field<NpcOrderSlipPanel>(_panel.NpcRunner, "orderSlipPanel");
                Check(Field<CanvasGroup>(order, "canvasGroup").alpha > .99f, "order remains visible during the mini-game");
            }
            var instruction = Field<TextMeshProUGUI>(host, "instructionField");
            Check(instruction != null && instruction.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(instruction.text),
                "readable instruction for " + option.MiniGameType);
            Check(instruction.fontSize >= 22f && !instruction.raycastTarget && instruction.GetComponentInParent<Mask>() == null,
                "instruction is legible and outside the ingredient mask");
            Canvas.ForceUpdateCanvases();
            instruction.ForceMeshUpdate();
            var instructionPaper = instruction.transform.parent.Find("PaperBackground").GetComponent<Image>();
            Check(instruction.canvasRenderer.absoluteDepth > instructionPaper.canvasRenderer.absoluteDepth,
                "instruction renders above the opaque paper");
            Check(instruction.textInfo.characterInfo.Take(instruction.textInfo.characterCount).Any(c => c.isVisible),
                "instruction generates visible glyphs");
            foreach (var text in host.GetComponentsInChildren<TMP_Text>())
                Check(text == instruction || string.IsNullOrEmpty(text.text) || System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^[\d\s%/.:,\-]+$"), "instruction or numeric minigame text: " + text.name);
            Write("INPUT " + ingredient.IngredientId + "/" + option.PreparationOptionId + " " + option.MiniGameType + " area=" + rect.rect);
            yield return Capture((independent ? "minigame-" : "cooking-active-order-") + (_routerOverride != null ? "standalone-" : "") + ingredient.IngredientId + "-" + option.MiniGameType);
            var profile = Field<CookingMiniGameOverlaySettingsSO>(router, "overlaySettings").GetProfile(option.MiniGameType);
            switch (option.MiniGameType)
            {
                case CookingMiniGameType.Chopping:
                    for (int i = 0; i < 20 && controller.gameObject.activeInHierarchy; i++)
                    {
                        int index = Field<int[]>(controller, "_order")[Field<int>(controller, "_orderIndex")];
                        yield return Click(Field<Image[]>(controller, "targetImages")[index].rectTransform);
                    }
                    break;
                case CookingMiniGameType.Slicing:
                    foreach (var line in Field<Image[]>(controller, "cutLineImages"))
                    {
                        var r = line.rectTransform;
                        Vector2 extent = r.rect.height >= r.rect.width ? new Vector2(0, r.rect.height * .5f) : new Vector2(r.rect.width * .5f, 0);
                        yield return Drag(ScreenPoint(r, r.rect.center - extent), ScreenPoint(r, r.rect.center + extent), 24);
                    }
                    break;
                case CookingMiniGameType.Grinding:
                    yield return Circle(rect, Mathf.Min(rect.rect.width, rect.rect.height) * .16f, 4.3f);
                    break;
                case CookingMiniGameType.Cleansing:
                    foreach (var stain in Field<Image[]>(controller, "stainImages"))
                    {
                        if (!controller.gameObject.activeInHierarchy) break;
                        Vector2 p = ScreenPoint(stain.rectTransform);
                        yield return Press(p);
                        for (int i = 0; i < 28 && stain.gameObject.activeInHierarchy; i++) yield return Move(p + new Vector2(i % 2 == 0 ? -12 : 12, 0), true);
                        yield return Release(p);
                    }
                    break;
                case CookingMiniGameType.Freezing:
                    foreach (var cell in Field<Image[]>(controller, "frostCells"))
                    {
                        if (!controller.gameObject.activeInHierarchy) break;
                        Vector2 p = ScreenPoint(cell.rectTransform);
                        yield return Press(p);
                        int index = Array.IndexOf(Field<Image[]>(controller, "frostCells"), cell);
                        for (int i = 0; i < 15 && Field<float[]>(controller, "_cells")[index] < .66f; i++)
                            yield return Move(p + new Vector2(i % 2 == 0 ? -16 : 16, 0), true);
                        yield return Release(p);
                    }
                    break;
                case CookingMiniGameType.Stewing:
                    string dragInstruction = instruction.text;
                    yield return Drag(ScreenPoint(rect, new Vector2(-rect.rect.width * .2f, 0)), ScreenPoint(rect, new Vector2(rect.rect.width * .22f, 0)), 20);
                    Check(instruction.text != dragInstruction, "stewing instruction advances to stirring");
                    string stirInstruction = instruction.text;
                    yield return Circle(rect, Mathf.Min(rect.rect.width, rect.rect.height) * .17f, 1.15f);
                    Check(instruction.text != stirInstruction && instruction.text.Contains("폐기"), "stewing instruction identifies the discard destination");
                    yield return Capture("stew-discard-" + ingredient.IngredientId);
                    yield return Drag(ScreenPoint(rect), ScreenPoint(Field<Image>(controller, "wasteZone").rectTransform), 25);
                    break;
                case CookingMiniGameType.Diluting:
                    Vector2 destination = ScreenPoint(Field<RectTransform>(controller, "pourZone"));
                    yield return Press(destination);
                    yield return Until(() => Field<float>(controller, "_waterAmount") >= (profile.TargetMin + profile.TargetMax) * .5f, profile.MaximumDuration, "pour into visible destination");
                    yield return Release(destination);
                    break;
                case CookingMiniGameType.Roasting:
                case CookingMiniGameType.Burning:
                    string flipInstruction = instruction.text;
                    yield return WaitElapsed(controller, profile.Duration * .32f);
                    yield return Click(Field<Image>(host, "maskImage").rectTransform);
                    Check(instruction.text != flipInstruction, "roasting instruction advances after the flip");
                    yield return WaitElapsed(controller, profile.Duration * (profile.TargetMin + profile.TargetMax) * .5f);
                    yield return Click(Field<Image>(host, "maskImage").rectTransform);
                    break;
                case CookingMiniGameType.Boiling:
                    yield return WaitElapsed(controller, profile.Duration * (profile.TargetMin + profile.TargetMax) * .5f);
                    yield return Click(Field<Image>(host, "maskImage").rectTransform);
                    break;
                default: throw new Exception("No actual-input solver for " + option.MiniGameType);
            }
            yield return Until(() => !controller.gameObject.activeInHierarchy, 3, "input completes " + option.MiniGameType);
            yield return Until(() => independent ? result != null : _panel.CurrentScreen != CookingGameScreenState.MiniGame, 8, "result delivered once " + option.MiniGameType);
            if (independent) Check(deliveries == 1, "one result callback");
            if (independent) Check(result.Grade != CookingMiniGameGrade.Bad, "playable " + ingredient.IngredientId + "/" + option.MiniGameType + " grade=" + result.Grade);
        }

        private IEnumerator WaitElapsed(object controller, float elapsed)
        {
            yield return Until(() => Time.unscaledTime - Field<float>(controller, "_startedTime") >= elapsed, elapsed + 3, "timing band");
        }
        private IEnumerator Circle(RectTransform rect, float radius, float turns)
        {
            Vector2 Point(float angle) => ScreenPoint(rect, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            yield return Press(Point(0));
            int steps = Mathf.CeilToInt(turns * 36);
            for (int i = 1; i <= steps; i++) yield return Move(Point(i * Mathf.PI / 18), true);
            yield return Release(Point(steps * Mathf.PI / 18));
        }
        private IEnumerator SetResolution(int width, int height)
        {
            if (Application.isEditor) EditorResolution?.Invoke(width, height);
            else Screen.SetResolution(Mathf.Min(width, 1920), Mathf.Min(height, 1080), FullScreenMode.Windowed);
            _captureWidth = width; _captureHeight = height;
            ConfigureCaptureTarget();
            yield return Frames(8);
            Write("RESOLUTION requested=" + width + "x" + height + " render=" + _captureTarget.width + "x" + _captureTarget.height);
        }
        private void OnLoaded(Scene scene, LoadSceneMode mode) => ConfigureCaptureTarget();
        private void ConfigureCaptureTarget()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).ToArray();
            var camera = Camera.main;
            if (camera == null) camera = cameras.FirstOrDefault(c => c.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Base);
            if (camera == null) return;
            if (_captureTarget == null || _captureTarget.width != _captureWidth || _captureTarget.height != _captureHeight)
            {
                if (_captureTarget != null) { _captureTarget.Release(); Destroy(_captureTarget); }
                _captureTarget = new RenderTexture(_captureWidth, _captureHeight, 24, RenderTextureFormat.ARGB32);
                _captureTarget.Create();
            }
            // Render the camera stack offscreen so hidden windows and monitor limits do not suppress captures.
            camera.targetTexture = _captureTarget;
            var uiCamera = cameras.FirstOrDefault(c => c.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay) ?? camera;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = uiCamera.nearClipPlane + .05f;
            }
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
                    canvas.planeDistance = canvas.worldCamera.nearClipPlane + .05f;
            var data = camera.GetUniversalAdditionalCameraData();
            if (uiCamera != camera && !data.cameraStack.Contains(uiCamera)) data.cameraStack.Add(uiCamera);
            Canvas.ForceUpdateCanvases();
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var camera = Camera.main;
            ConfigureCaptureTarget();
            Write("CAMERA " + camera.name + " stack=" + camera.GetUniversalAdditionalCameraData().cameraStack.Count);
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas))
                Write("CANVAS " + canvas.name + " mode=" + canvas.renderMode + " camera=" + (canvas.worldCamera != null ? canvas.worldCamera.name : "none") + " size=" + ((RectTransform)canvas.transform).rect);
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = _captureTarget };
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
            var previous = RenderTexture.active;
            RenderTexture.active = _captureTarget;
            Texture2D texture = new Texture2D(_captureTarget.width, _captureTarget.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture.Apply();
            var pixels = texture.GetPixels32();
            Write("CAPTURE " + name + " nonblack=" + pixels.Count(p => p.r > 10 || p.g > 10 || p.b > 10));
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(_output, name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }
        private IEnumerator Unblocked()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(10,10) });
            yield return Frames(3);
            yield return Until(() => !GameUiInput.IsBlocked, 15, "input unlocked");
        }
        private IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return Frames(2);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return Frames(3);
        }
        private IEnumerator Click(RectTransform target) { yield return Press(ScreenPoint(target)); yield return Release(ScreenPoint(target)); }
        private IEnumerator Press(Vector2 point) { yield return Move(point, false); yield return Move(point, true); }
        private IEnumerator Release(Vector2 point) { yield return Move(point, false); yield return Frames(2); }
        private IEnumerator Move(Vector2 point, bool down)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point, buttons = (ushort)(down ? 1 : 0) });
            yield return Frames(2);
        }
        private IEnumerator Drag(Vector2 from, Vector2 to, int steps)
        {
            yield return Press(from);
            for (int i = 1; i <= steps; i++) yield return Move(Vector2.Lerp(from, to, (float)i / steps), true);
            yield return Release(to);
        }
        private static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
        private IEnumerator Until(Func<bool> condition, float timeout, string label)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Check(condition(), label);
        }
        private static Vector2 ScreenPoint(RectTransform rect) => ScreenPoint(rect, rect.rect.center);
        private static Vector2 ScreenPoint(RectTransform rect, Vector2 local)
        {
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            return RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(local));
        }
        private static Rect ScreenRect(RectTransform rect)
        {
            Vector2 min = ScreenPoint(rect, rect.rect.min), max = ScreenPoint(rect, rect.rect.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void CheckInside(RectTransform rect, RectTransform parent, string label)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var bounds = parent.rect; bounds.xMin -= 2; bounds.yMin -= 2; bounds.xMax += 2; bounds.yMax += 2;
            Check(corners.All(c => bounds.Contains(parent.InverseTransformPoint(c))), label);
        }
        private static int Stock(PlayerInventoryModule inventory) => Enumerable.Range(0, inventory.SlotCapacity).Select(inventory.GetSlot).Where(s => s != null && s.Item is IngredientItemDataSO).Sum(s => s.Amount);
        private static T Find<T>() where T : UnityEngine.Object => FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        private static T Field<T>(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) return (T)field.GetValue(target);
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }
        private void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            _checks++; Write("OK " + label);
        }
        private void Write(string message) => File.AppendAllText(Path.Combine(_output, "report.txt"), message + "\n");
    }
}
#endif
