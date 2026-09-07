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
                report.AppendLine("PASS: all 24 actual UI choices, missing-item locks, ingredient/tool costs, inventory rewards, dialog images and cleanup. No runtime errors or missing-target tween warnings.");
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
            if (type == LogType.Error || type == LogType.Exception || (message.Contains("DOTWEEN") && message.Contains("missing/null")))
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
            var names = new[] { "Find_HolySwordSalt", "Meet_MushroomBarber", "Meet_AdventurerTrade", "Find_BoxSlime", "Find_SaltGoddess", "Find_BrokenGoddessRepair", "Find_CoconutCrab", "Meet_SproutSlime" };
            var tools = new[] { "knife", "Hamer" }.Select(n => AssetDatabase.LoadAssetAtPath<AdventureItemSO>("Assets/Work/Adventure/SO/AdventureItem/" + n + ".asset")).ToArray();
            yield return 0.7f;
            foreach (string name in names)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AdventureEventSO>("Assets/Work/Adventure/SO/Dialog/" + name + ".asset");
                for (int index = 0; index < asset.options.Count; index++)
                {
                    var option = asset.options[index];
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
                    Require(buttons.Length == 3, name + ": expected three buttons");
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
                    var selected = Field<Button>(buttons[index], "button");
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
                    foreach (var entry in expected)
                        Require(inventory.GetItemAmount(entry.Key) == entry.Value, name + ": inventory mismatch for " + entry.Key.DisplayName);
                    foreach (var entry in Snapshot(inventory))
                        Require(expected.GetValueOrDefault(entry.Key) == entry.Value, name + ": unexpected inventory reward");
                    if (option is LockedOption locked)
                        Require(Have(locked.KeyItem) == !locked.IsUseItemOption, name + ": wrong tool consumption");
                    Require(imageRoot.childCount == 0, name + ": image cleanup incomplete");
                    Require(goStop.gameObject.activeSelf, name + ": continue/stop UI did not open");
                    report.AppendLine("PASS " + name + " / " + option.OptionName);
                    File.WriteAllText(ReportPath, report.ToString());
                }
            }
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
