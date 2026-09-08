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
            "Find_SaltGoddess", "Find_BrokenGoddessRepair", "Find_CoconutCrab", "Meet_SproutSlime",
            "Find_RopeCache", "Meet_LanternKeeper", "Find_CookKit", "Meet_SupplyPorter", "Meet_RopeWeaver", "Meet_BottleTrader", "Find_LedgePantry", "Meet_PitAdventurer", "Find_DarkNest", "Find_HotSpringBasket", "Find_StickyPool", "Find_RootCellar", "Find_HangingPantry", "Find_MossyStair", "Meet_LostMushroomChild", "Find_CrackedStoreroom", "Find_CrabSnare", "Find_TiltedSaltCart", "Find_ThornLunchbox", "Meet_CaughtApron", "Find_SeepingSaltWell", "Meet_LeakingPack", "Find_SlimeCurtain", "Meet_MushroomWaterer", "Find_SleepingCrab", "Find_CollapsedShelf", "Meet_SootyCook", "Find_GlowingCrack", "Find_DewMushrooms", "Meet_StatueCaretaker", "Find_SlimeTracks", "Find_SaltDrips", "Find_CrabMolting", "Find_StickyLatch", "Meet_MushroomSplinter", "Find_SlimePicnic"
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
            tests.EquipmentExpansion_HasAcquisitionsAndMultipleUses();
            File.WriteAllText("Temp/AdventureEditModeValidation.txt", DateTime.Now.ToString("O") + "\nPASS: all 8 AdventureContentTests checks in Unity Editor.\n");
            Debug.Log("Adventure validation passed: 44 added events including nested choices, references, costs, inventory rewards and crab ingredient.");
        }


        [Test]
        public void EquipmentExpansion_HasAcquisitionsAndMultipleUses()
        {
            var events = EventNames.Select(LoadEvent).ToArray();
            foreach (var name in new[] { "Rope", "Lantern", "Tongs", "CollectingBottle" })
            {
                var item = AssetDatabase.LoadAssetAtPath<AdventureItemSO>("Assets/Work/Adventure/SO/AdventureItem/" + name + ".asset");
                Assert.That(item, Is.Not.Null, name);
                Assert.That(item.ItemIcon, Is.Not.Null, name);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Work/Adventure/Prefabs/Item/" + name + ".prefab");
                Assert.That(prefab.GetComponent<Image>().sprite, Is.EqualTo(item.ItemIcon), name);
                int acquisitions = events.Sum(e => e.options.Sum(o => o.rewardMethod
                    .OfType<Work.Adventure.Code.Rewards.AdventureItemReward>()
                    .Count(r => (AdventureItemSO)r.GetType().GetField("itemSO", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(r) == item)));
                int uses = events.Count(e => e.options.OfType<LockedOption>().Any(o => o.KeyItem == item));
                Assert.That(acquisitions, Is.GreaterThanOrEqualTo(2), name + ": acquisition paths");
                Assert.That(uses, Is.GreaterThanOrEqualTo(2), name + ": usable encounters");
            }
        }

        [Test]
        public void Events_HaveValidReferencesAndFinishEveryBranch()
        {
            foreach (string name in EventNames)
            {
                AdventureEventSO asset = LoadEvent(name);
                Assert.That(asset.options.Count, Is.GreaterThan(0), name);
                Assert.That(asset.dialogDatas.Count, Is.GreaterThan(0), name);
                ValidateLines(asset.dialogDatas);
                foreach (Options option in AdventureFlowTests.Paths(asset.options).SelectMany(p => p).Distinct())
                {
                    Assert.That(option, Is.Not.Null, name);
                    Assert.That(option.OptionName, Is.Not.Empty, name);
                    ValidateLines(option.ResultdialogDatas);
                    if (option.followUpOptions == null || option.followUpOptions.Count == 0)
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
                var option = AdventureFlowTests.Paths(trade.options).Select(p => p.Last()).OfType<LockedOption>().ToArray()[i];
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
            var ingredient = AssetDatabase.LoadAssetAtPath<IngredientItemDataSO>(ItemPath + "Ingredients/SlimeMucusIngredientItem.asset");
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
            var ingredient = AssetDatabase.LoadAssetAtPath<IngredientItemDataSO>(ItemPath + "Ingredients/SlimeMucusIngredientItem.asset");
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
