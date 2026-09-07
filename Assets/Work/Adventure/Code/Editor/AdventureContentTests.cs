using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Work.Adventure.Code.DialogMethod;
using Work.Adventure.Code.UI;
using Work.Cook.Code.Data;
using Work.Core.EventBus;
using Work.Items.Code;
using Work.Players.Code.Inventory;
using Object = UnityEngine.Object;

namespace Work.Adventure.Code.Editor
{
    public sealed class AdventureContentTests
    {
        private const string DialogPath = "Assets/Work/Adventure/SO/Dialog/";
        private const string ItemPath = "Assets/Work/Items/SO/";
        private static readonly string[] EventNames =
        {
            "Find_HolySwordSalt", "Meet_MushroomBarber", "Meet_AdventurerTrade", "Find_BoxSlime",
            "Find_SaltGoddess", "Find_BrokenGoddessRepair", "Find_CoconutCrab", "Meet_SproutSlime"
        };

        [MenuItem("Tools/Dungeon Dinner/Adventure/Validate Added Events %#F8")]
        public static void ValidateAddedEvents()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("플레이 모드를 종료한 뒤 검증을 실행하세요.");
            var tests = new AdventureContentTests();
            tests.Events_HaveValidReferencesAndFinishEveryBranch();
            tests.Events_AreRegisteredOnceInBothScenes();
            tests.Trades_ConsumeToolsAndGrantExpectedIngredients();
            tests.Repair_RejectsMissingIngredientAndConsumesExactlyOnce();
            tests.Repair_RejectsInsufficientQuantityWithoutPartialConsumption();
            tests.RandomBoxReward_AddsExactlyTheLoggedIngredients();
            tests.CoconutCrab_IsRegisteredCookingIngredient();
            File.WriteAllText("Temp/AdventureEditModeValidation.txt", DateTime.Now.ToString("O") + "\nPASS: all 7 AdventureContentTests checks in Unity Editor.\n");
            Debug.Log("Adventure validation passed: 8 events, 24 choices, references, costs, inventory rewards and crab ingredient.");
        }

        [Test]
        public void Events_HaveValidReferencesAndFinishEveryBranch()
        {
            foreach (string name in EventNames)
            {
                AdventureEventSO asset = LoadEvent(name);
                Assert.That(asset.options.Count, Is.EqualTo(3), name);
                Assert.That(asset.dialogDatas.Count, Is.GreaterThan(0), name);
                ValidateLines(asset.dialogDatas);
                foreach (Options option in asset.options)
                {
                    Assert.That(option, Is.Not.Null, name);
                    Assert.That(option.OptionName, Is.Not.Empty, name);
                    ValidateLines(option.ResultdialogDatas);
                    Assert.That(option.ResultdialogDatas.Last().method.OfType<DeleteAllImageEvent>().Any(), Is.True, name);
                    foreach (AdventureReward reward in option.rewardMethod)
                        Assert.That(reward, Is.Not.Null, name);
                    if (option is LockedOption locked)
                        Assert.That(locked.KeyItem, Is.Not.Null, name);
                }
                var serialized = new SerializedObject(asset);
                SerializedProperty property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && !property.propertyPath.StartsWith("m_"))
                        Assert.That(property.objectReferenceValue, Is.Not.Null, name + ": " + property.propertyPath);
                }
            }
        }

        [Test]
        public void Events_AreRegisteredOnceInBothScenes()
        {
            foreach (string scene in new[] { "Assets/Work/Adventure/Scene/AdventureTestScene.unity", "Assets/Work/Cook/Scene/CookTestScene.unity" })
            {
                string text = File.ReadAllText(scene);
                foreach (string name in EventNames)
                {
                    string guid = AssetDatabase.AssetPathToGUID(DialogPath + name + ".asset");
                    Assert.That(text.Split(new[] { guid }, StringSplitOptions.None).Length - 1, Is.EqualTo(1), scene + ": " + name);
                }
            }
        }

        [Test]
        public void Trades_ConsumeToolsAndGrantExpectedIngredients()
        {
            AdventureEventSO trade = LoadEvent("Meet_AdventurerTrade");
            for (int i = 0; i < 2; i++)
            {
                var option = trade.options[i] as LockedOption;
                Assert.That(option, Is.Not.Null);
                Assert.That(option.IsUseItemOption, Is.True);
                Assert.That(option.IsUnLockOption, Is.False);
                var received = new Dictionary<ItemDataSO, int>();
                Action<InventoryItemAddRequestedEvent> handler = evt => received[evt.Item] = received.GetValueOrDefault(evt.Item) + evt.Amount;
                Bus<InventoryItemAddRequestedEvent>.Events += handler;
                try
                {
                    foreach (AdventureReward reward in option.rewardMethod) reward.GetReward();
                    Assert.That(received.Count, Is.EqualTo(2));
                    Assert.That(received.Values.Sum(), Is.EqualTo(3));
                }
                finally { Bus<InventoryItemAddRequestedEvent>.Events -= handler; }
            }
        }

        [Test]
        public void Repair_RejectsMissingIngredientAndConsumesExactlyOnce()
        {
            var option = (IngredientLockedOption)LoadEvent("Find_BrokenGoddessRepair").options[0];
            var owner = new GameObject("Adventure inventory test");
            try
            {
                var inventory = owner.AddComponent<PlayerInventoryModule>();
                Assert.That(option.CanSelect(null), Is.False);
                Assert.That(option.TryConsume(inventory), Is.False);
                inventory.AddItem(option.RequiredIngredient, 1);
                Assert.That(option.CanSelect(inventory), Is.True);
                Assert.That(option.TryConsume(inventory), Is.True);
                Assert.That(inventory.GetItemAmount(option.RequiredIngredient), Is.Zero);
                Assert.That(option.TryConsume(inventory), Is.False);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void Repair_RejectsInsufficientQuantityWithoutPartialConsumption()
        {
            var option = new IngredientLockedOption();
            var ingredient = AssetDatabase.LoadAssetAtPath<IngredientItemDataSO>(ItemPath + "TempCookingTest/TempSlimeMucusIngredientItem.asset");
            SetField(option, "<RequiredIngredient>k__BackingField", ingredient);
            SetField(option, "<RequiredAmount>k__BackingField", 2);
            var owner = new GameObject("Adventure inventory quantity test");
            try
            {
                var inventory = owner.AddComponent<PlayerInventoryModule>();
                inventory.AddItem(ingredient, 1);
                Assert.That(option.TryConsume(inventory), Is.False);
                Assert.That(inventory.GetItemAmount(ingredient), Is.EqualTo(1));
                inventory.AddItem(ingredient, 2);
                Assert.That(option.TryConsume(inventory), Is.True);
                Assert.That(inventory.GetItemAmount(ingredient), Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void RandomBoxReward_AddsExactlyTheLoggedIngredients()
        {
            var ingredient = AssetDatabase.LoadAssetAtPath<IngredientItemDataSO>(ItemPath + "TempCookingTest/TempSlimeMucusIngredientItem.asset");
            var root = new GameObject("Random reward test", typeof(RectTransform));
            var template = new GameObject("Reward image", typeof(RectTransform), typeof(Image));
            int added = 0, logged = 0;
            Action<InventoryItemAddRequestedEvent> onAdd = evt => { Assert.That(evt.Item, Is.SameAs(ingredient)); added += evt.Amount; };
            Action<OnPlusLogCreateEvent> onLog = evt => logged++;
            Bus<InventoryItemAddRequestedEvent>.Events += onAdd;
            Bus<OnPlusLogCreateEvent>.Events += onLog;
            var randomState = UnityEngine.Random.state;
            try
            {
                var reward = new RandomIngredientEvent();
                SetField(reward, "randomItemList", new List<ImageAndItem> { new ImageAndItem { itemDataSO = ingredient, imagePrefab = template.GetComponent<Image>() } });
                reward.Init((RectTransform)root.transform);
                reward.RaiseEvent();
                Assert.That(added, Is.InRange(2, 5));
                Assert.That(logged, Is.EqualTo(added));
                Assert.That(root.transform.childCount, Is.EqualTo(added));
            }
            finally
            {
                UnityEngine.Random.state = randomState;
                Bus<InventoryItemAddRequestedEvent>.Events -= onAdd;
                Bus<OnPlusLogCreateEvent>.Events -= onLog;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(template);
            }
        }

        [Test]
        public void CoconutCrab_IsRegisteredCookingIngredient()
        {
            var item = AssetDatabase.LoadAssetAtPath<IngredientItemDataSO>(ItemPath + "CoconutCrabMeatIngredientItem.asset");
            Assert.That(item, Is.Not.Null);
            Assert.That(item.IsValidIngredientItem, Is.True);
            Assert.That(item.Icon, Is.Not.Null);
            Assert.That(item.Ingredient.FindPreparationOption("roast"), Is.Not.Null);
            Assert.That(item.Ingredient.FindPreparationOption("parboil"), Is.Not.Null);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(ItemPath + "ItemCatalog.asset");
            Assert.That(catalog.TryFindItem(item.ItemId, out ItemDataSO found), Is.True);
            Assert.That(found, Is.SameAs(item));
        }

        private static AdventureEventSO LoadEvent(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AdventureEventSO>(DialogPath + name + ".asset");
            Assert.That(asset, Is.Not.Null, name);
            return asset;
        }

        private static void ValidateLines(List<AdventrueDialogData> lines)
        {
            Assert.That(lines.Count, Is.GreaterThan(0));
            foreach (var line in lines)
            {
                Assert.That(line.Context, Is.Not.Empty);
                foreach (var method in line.method) Assert.That(method, Is.Not.Null);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
