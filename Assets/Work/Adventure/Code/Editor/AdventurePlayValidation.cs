using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Work.Adventure.Code.UI;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;
using Work.Items.Code;
using Work.Players.Code.Inventory;
using Object = UnityEngine.Object;

namespace Work.Adventure.Code.Editor
{
    // Runs against the active play session; stopping Play Mode discards test inventory changes.
    public static class AdventurePlayValidation
    {
        private static IEnumerator run;
        private static double nextTick;
        private static readonly StringBuilder report = new StringBuilder();
        private static readonly List<string> errors = new List<string>();
        private const string ReportPath = "Temp/AdventurePlayValidation.txt";

        [MenuItem("Tools/Dungeon Dinner/Adventure/Validate All Choices In Play Mode %#F9")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || run != null)
                throw new InvalidOperationException("플레이 모드에서 검증을 한 번 실행하세요.");
            report.Clear();
            errors.Clear();
            Application.logMessageReceived += RecordError;
            report.AppendLine(DateTime.Now.ToString("O"));
            run = Validate();
            nextTick = 0;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextTick) return;
            try
            {
                if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode ended before validation completed.");
                if (run.MoveNext())
                {
                    nextTick = EditorApplication.timeSinceStartup + (run.Current is float seconds ? seconds : 0.03f);
                    return;
                }
                Require(errors.Count == 0, string.Join("\n", errors));
                report.AppendLine("PASS: 138 terminal paths across 45 events, including all 3 branching events; actual UI choices, missing-item locks, ingredient/tool costs, inventory rewards, dialog images and cleanup. No runtime errors, missing glyphs or missing-target tween warnings.");
                Debug.Log(report.ToString());
            }
            catch (Exception error)
            {
                report.AppendLine("FAIL: " + error);
                Debug.LogException(error);
            }
            File.WriteAllText(ReportPath, report.ToString());
            (run as IDisposable)?.Dispose();
            run = null;
            Application.logMessageReceived -= RecordError;
            EditorApplication.update -= Tick;
        }

        private static void RecordError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || (message.Contains("DOTWEEN") && message.Contains("missing/null")) || message.Contains("was not found in the"))
                errors.Add(message);
        }

        private static IEnumerator Validate()
        {
            var dialog = Object.FindFirstObjectByType<AdventureDialogUI>(FindObjectsInactive.Include);
            var inventory = Object.FindFirstObjectByType<PlayerInventoryModule>();
            Require(dialog != null && inventory != null, "Active adventure dialog and inventory required.");
            var optionUI = Field<OptionUI>(dialog, "optionUI");
            var imageRoot = Field<RectTransform>(dialog, "root");
            var goStop = Field<GoAndStopSelectUI>(dialog, "selectUI");
            Object.FindFirstObjectByType<MainUI>(FindObjectsInactive.Include)?.HideUI();
            Object.FindFirstObjectByType<PreparationMenu>(FindObjectsInactive.Include)?.HideUI();
            Object.FindFirstObjectByType<AdventureBackground>(FindObjectsInactive.Include).Enable();
            Object.FindFirstObjectByType<AdventureItemUI>(FindObjectsInactive.Include).Enable();
            dialog.gameObject.SetActive(true);
            Set(dialog, "time", 0.01f);
            Set(dialog, "characterInterval", 0.001f);
            var names = new[] { "Find_Box", "Find_HolySwordSalt", "Meet_MushroomBarber", "Meet_AdventurerTrade", "Find_BoxSlime", "Find_SaltGoddess", "Find_BrokenGoddessRepair", "Find_CoconutCrab", "Meet_SproutSlime", "Find_RopeCache", "Meet_LanternKeeper", "Find_CookKit", "Meet_SupplyPorter", "Meet_RopeWeaver", "Meet_BottleTrader", "Find_LedgePantry", "Meet_PitAdventurer", "Find_DarkNest", "Find_HotSpringBasket", "Find_StickyPool", "Find_RootCellar", "Find_HangingPantry", "Find_MossyStair", "Meet_LostMushroomChild", "Find_CrackedStoreroom", "Find_CrabSnare", "Find_TiltedSaltCart", "Find_ThornLunchbox", "Meet_CaughtApron", "Find_SeepingSaltWell", "Meet_LeakingPack", "Find_SlimeCurtain", "Meet_MushroomWaterer", "Find_SleepingCrab", "Find_CollapsedShelf", "Meet_SootyCook", "Find_GlowingCrack", "Find_DewMushrooms", "Meet_StatueCaretaker", "Find_SlimeTracks", "Find_SaltDrips", "Find_CrabMolting", "Find_StickyLatch", "Meet_MushroomSplinter", "Find_SlimePicnic" };
            var tools = new[] { "knife", "Hamer", "Rope", "Lantern", "Tongs", "CollectingBottle" }.Select(n => AssetDatabase.LoadAssetAtPath<AdventureItemSO>("Assets/Work/Adventure/SO/AdventureItem/" + n + ".asset")).ToArray();
            yield return 0.7f;
            foreach (string name in names)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AdventureEventSO>("Assets/Work/Adventure/SO/Dialog/" + name + ".asset");
                var paths = AdventureFlowTests.Paths(asset.options).ToArray();
                for (int index = 0; index < paths.Length; index++)
                {
                    var path = paths[index];
                    var option = path.Last();
                    if (option is LockedOption missingTool)
                        while (Have(missingTool.KeyItem)) Bus<OnRemoveAdventureItemEvent>.Raise(new OnRemoveAdventureItemEvent(missingTool.KeyItem));
                    if (option is IngredientLockedOption missingIngredient)
                        inventory.RemoveItem(missingIngredient.RequiredIngredient, inventory.GetItemAmount(missingIngredient.RequiredIngredient));
                    if (option is LockedOption || option is IngredientLockedOption)
                    {
                        int calls = 0;
                        var probe = Object.Instantiate(Field<OptionButtonUI>(optionUI, "optionButtonPrefab"), optionUI.transform);
                        probe.Init(option, _ => calls++);
                        var probeButton = Field<Button>(probe, "button");
                        Require(!probeButton.interactable, name + ": missing item did not lock choice");
                        probeButton.onClick.Invoke();
                        Require(calls == 0, name + ": locked choice callback fired");
                        Object.Destroy(probe.gameObject);
                        yield return 0.03f;
                    }
                    foreach (var tool in tools)
                        if (!Have(tool)) Bus<OnAddAdventureItemEvent>.Raise(new OnAddAdventureItemEvent(tool));
                    if (option is IngredientLockedOption cost)
                        inventory.AddItem(cost.RequiredIngredient, cost.RequiredAmount);
                    var before = Snapshot(inventory);
                    var manager = Object.FindFirstObjectByType<AdventureManager>();
                    var expectedTools = new Dictionary<string, int>(Field<Dictionary<string, int>>(manager, "_adventureItemDic"));
                    if (option is LockedOption usedTool && usedTool.IsUseItemOption) expectedTools[usedTool.KeyItem.ItemName]--;
                    if (name == "Find_Box" && option is LockedOption boxKnife && boxKnife.KeyItem.name == "knife")
                        expectedTools[boxKnife.KeyItem.ItemName]--;
                    goStop.Disable();
                    dialog.StartDialog(asset);
                    yield return 0.1f;
                    while (Field<int>(dialog, "_currentDialogIndex") != 0)
                    {
                        ValidateImages(imageRoot);
                        Field<Tween>(dialog, "_typingTween")?.Complete(true);
                        dialog.NextDialog();
                        yield return 0.03f;
                    }
                    yield return 0.35f;
                    var buttons = optionUI.GetComponentsInChildren<OptionButtonUI>();
                    Require(buttons.Length == asset.options.Count, name + ": wrong root button count");
                    foreach (var entry in buttons)
                    {
                        var label = Field<TextMeshProUGUI>(entry, "nameField");
                        label.ForceMeshUpdate();
                        var canvas = label.canvas;
                        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                        foreach (var character in label.textInfo.characterInfo.Take(label.textInfo.characterCount))
                        {
                            if (!character.isVisible) continue;
                            var left = RectTransformUtility.WorldToScreenPoint(camera, label.transform.TransformPoint(character.bottomLeft));
                            var right = RectTransformUtility.WorldToScreenPoint(camera, label.transform.TransformPoint(character.topRight));
                            Require(left.x >= 0 && right.x <= Screen.width, name + ": choice text outside screen: " + label.text);
                        }
                    }
                    for (int step = 0; step < path.Length - 1; step++)
                    {
                        var inquiry = Field<Button>(buttons.Single(b => Field<Options>(b, "_currentOption") == path[step]), "button");
                        inquiry.onClick.Invoke();
                        int firstIndex = Field<int>(dialog, "_currentDialogIndex");
                        inquiry.onClick.Invoke();
                        Require(Field<int>(dialog, "_currentDialogIndex") == firstIndex, name + ": repeated inquiry click");
                        yield return 0.03f;
                        while (Field<int>(dialog, "_currentDialogIndex") != 0)
                        {
                            Field<Tween>(dialog, "_typingTween")?.Complete(true);
                            dialog.NextDialog();
                            yield return 0.03f;
                        }
                        yield return 0.35f;
                        Require(!goStop.gameObject.activeSelf, name + ": event ended before follow-up choice");
                        Require(imageRoot.childCount > 0, name + ": branch images cleared too early");
                        buttons = optionUI.GetComponentsInChildren<OptionButtonUI>();
                        Require(buttons.Length == path[step].followUpOptions.Count, name + ": wrong follow-up button count");
                        Require(Field<Dictionary<string, int>>(manager, "_adventureItemDic").All(e => e.Value == expectedTools.GetValueOrDefault(e.Key)
                            + (option is LockedOption pending && pending.IsUseItemOption && pending.KeyItem.ItemName == e.Key ? 1 : 0)
                            + (name == "Find_Box" && option is LockedOption knife && knife.KeyItem.name == "knife" && knife.KeyItem.ItemName == e.Key ? 1 : 0)),
                            name + ": inquiry changed equipment");
                    }
                    var selected = Field<Button>(buttons.Single(b => Field<Options>(b, "_currentOption") == option), "button");
                    Require(selected.interactable, name + ": funded choice locked");
                    if (index == 0)
                    {
                        ScreenCapture.CaptureScreenshot("Temp/Adventure-" + name + ".png");
                        yield return 0.1f;
                    }
                    selected.onClick.Invoke();
                    var expected = new Dictionary<ItemDataSO, int>(before);
                    if (option is IngredientLockedOption payment)
                        expected[payment.RequiredIngredient] = expected.GetValueOrDefault(payment.RequiredIngredient) - payment.RequiredAmount;
                    foreach (var reward in option.rewardMethod)
                    {
                        if (reward is Work.Adventure.Code.Rewards.AdventureItemReward)
                        {
                            var gained = Field<AdventureItemSO>(reward, "itemSO");
                            expectedTools[gained.ItemName] = expectedTools.GetValueOrDefault(gained.ItemName) + 1;
                            continue;
                        }
                        var item = Field<ItemDataSO>(reward, "reward");
                        expected[item] = expected.GetValueOrDefault(item) + Field<int>(reward, "amount");
                    }
                    // A second invocation in the same frame must not consume or start the branch again.
                    int dialogIndex = Field<int>(dialog, "_currentDialogIndex");
                    selected.onClick.Invoke();
                    Require(Field<int>(dialog, "_currentDialogIndex") == dialogIndex, name + ": duplicate click changed dialog");
                    yield return 0.03f;
                    while (Field<int>(dialog, "_currentDialogIndex") != 0)
                    {
                        ValidateImages(imageRoot);
                        Field<Tween>(dialog, "_typingTween")?.Complete(true);
                        dialog.NextDialog();
                        yield return 0.03f;
                    }
                    yield return 0.1f;
                    if (name == "Find_Box" && option is LockedOption hammer && hammer.KeyItem.name != "knife")
                    {
                        var actual = Snapshot(inventory);
                        var randomMethod = option.ResultdialogDatas.SelectMany(d => d.method)
                            .OfType<Work.Adventure.Code.DialogMethod.RandomIngredientEvent>().Single();
                        var allowed = Field<List<Work.Adventure.Code.DialogMethod.ImageAndItem>>(randomMethod, "randomItemList")
                            .Select(i => i.itemDataSO).ToArray();
                        int gained = 0;
                        foreach (var entry in actual)
                        {
                            int delta = entry.Value - before.GetValueOrDefault(entry.Key);
                            Require(delta >= 0 && (delta == 0 || allowed.Contains(entry.Key)), "Box: invalid random ingredient");
                            gained += delta;
                        }
                        Require(gained >= 2 && gained <= 5, "Box: incorrect random reward quantity");
                        expected = actual;
                    }
                    foreach (var entry in expected)
                        Require(inventory.GetItemAmount(entry.Key) == entry.Value, name + ": inventory mismatch for " + entry.Key.DisplayName);
                    foreach (var entry in Snapshot(inventory))
                        Require(expected.GetValueOrDefault(entry.Key) == entry.Value, name + ": unexpected inventory reward");
                    foreach (var entry in expectedTools)
                        Require(Field<Dictionary<string, int>>(manager, "_adventureItemDic").GetValueOrDefault(entry.Key) == entry.Value, name + ": wrong equipment count: " + entry.Key);
                    var itemUI = Object.FindFirstObjectByType<AdventureItemUI>(FindObjectsInactive.Include);
                    var icons = Field<Dictionary<string, AdventureItemIconUI>>(itemUI, "adventureItemIconsDic");
                    foreach (var entry in expectedTools.Where(e => e.Value > 0))
                    {
                        Require(icons.ContainsKey(entry.Key), name + ": missing equipment icon");
                        Require(Field<TextMeshProUGUI>(icons[entry.Key], "countText").text == entry.Value.ToString(), name + ": wrong equipment icon count");
                    }
                    Require(imageRoot.childCount == 0, name + ": image cleanup incomplete");
                    Require(goStop.gameObject.activeSelf, name + ": continue/stop UI did not open");
                    dialog.NextDialog();
                    Require(Field<Dictionary<string, int>>(manager, "_adventureItemDic").All(e => e.Value == expectedTools.GetValueOrDefault(e.Key)), name + ": rewards repeated after end");
                    report.AppendLine("PASS " + name + " / " + string.Join(" -> ", path.Select(o => o.OptionName)));
                    File.WriteAllText(ReportPath, report.ToString());
                }
            }
            foreach (var delay in ValidateImmediateFollowUp(dialog, optionUI, goStop, tools[0])) yield return delay;
        }

        private static IEnumerable<float> ValidateImmediateFollowUp(AdventureDialogUI dialog, OptionUI ui, GoAndStopSelectUI goStop, AdventureItemSO knife)
        {
            while (Have(knife)) Bus<OnRemoveAdventureItemEvent>.Raise(new OnRemoveAdventureItemEvent(knife));
            var asset = ScriptableObject.CreateInstance<AdventureEventSO>();
            var parent = new Options();
            var child = new LockedOption();
            typeof(Options).GetField("<OptionName>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(parent, "도구를 건네받는다");
            typeof(Options).GetField("<OptionName>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(child, "도구를 사용한다");
            Set(child, "<KeyItem>k__BackingField", knife);
            Set(child, "<IsUseItemOption>k__BackingField", true);
            var reward = new Work.Adventure.Code.Rewards.AdventureItemReward();
            Set(reward, "itemSO", knife);
            parent.rewardMethod.Add(reward);
            parent.followUpOptions.Add(child);
            asset.options.Add(parent);
            try
            {
                dialog.StartDialog(asset);
                yield return 0.1f;
                var first = Field<Button>(ui.GetComponentsInChildren<OptionButtonUI>().Single(), "button");
                first.onClick.Invoke();
                first.onClick.Invoke();
                yield return 0.05f;
                Require(Have(knife), "Intermediate reward was not granted before follow-up");
                Require(!goStop.gameObject.activeSelf, "Empty intermediate text ended event");
                var next = Field<Button>(ui.GetComponentsInChildren<OptionButtonUI>().Single(), "button");
                Require(next.interactable, "Intermediate reward did not unlock next choice");
                next.onClick.Invoke();
                next.onClick.Invoke();
                yield return 0.1f;
                dialog.NextDialog();
                Require(!Have(knife), "Intermediate reward repeated or child cost not consumed");
                Require(ui.GetComponentsInChildren<OptionButtonUI>().Length == 0, "Empty terminal left stale buttons");
                Require(goStop.gameObject.activeSelf, "Empty terminal did not finish");
                report.AppendLine("PASS immediate follow-up: empty text, reward unlock, single consumption and duplicate-click protection");
            }
            finally { Object.Destroy(asset); }
        }

        private static void ValidateImages(RectTransform root)
        {
            foreach (var image in root.GetComponentsInChildren<Image>()) Require(image.sprite != null, "Missing event sprite");
        }
        private static Dictionary<ItemDataSO, int> Snapshot(PlayerInventoryModule inventory)
        {
            var result = new Dictionary<ItemDataSO, int>();
            for (int i = 0; i < inventory.SlotCapacity; i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty) result[slot.Item] = result.GetValueOrDefault(slot.Item) + slot.Amount;
            }
            return result;
        }
        private static bool Have(AdventureItemSO item) => Bus<OnHaveItemEvent, BoolenReturnValue>.Raise(new OnHaveItemEvent(item)).isTrue;
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
