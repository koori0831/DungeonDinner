using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Work.Adventure.Code.Rewards;
using Object = UnityEngine.Object;

namespace Work.Adventure.Code.Editor
{
    public sealed class AdventureFlowTests
    {
        public static IEnumerable<Options[]> Paths(List<Options> options)
        {
            IEnumerable<Options[]> Walk(List<Options> list, List<Options> prefix)
            {
                foreach (var option in list)
                {
                    Assert.That(option, Is.Not.Null);
                    Assert.That(prefix.Contains(option), Is.False, "Cyclic adventure choices");
                    var next = new List<Options>(prefix) { option };
                    if (option.followUpOptions == null || option.followUpOptions.Count == 0) yield return next.ToArray();
                    else foreach (var path in Walk(option.followUpOptions, next)) yield return path;
                }
            }
            return Walk(options, new List<Options>());
        }

        [MenuItem("Tools/Dungeon Dinner/Adventure/Validate Selection And Branches %#F6")]
        public static void Validate()
        {
            var tests = new AdventureFlowTests();
            tests.AllEvents_HaveReachableTerminalsAndConsistentTooltips();
            tests.Selection_BoostsEmptyInventoryAndGuaranteesSupply();
            tests.Selection_RejectsUnavailableSupplyAndHandlesSparseLists();
            tests.Selection_UsesPoolChanceAndMissingToolWeights();
            File.WriteAllText("Temp/AdventureFlowValidation.txt", DateTime.Now.ToString("O")
                + "\nPASS: all 55 events, 159 terminal paths, branch graph and tooltips; empty inventory, drought guarantee, unavailable costs, null/duplicate/single entries, 35% pool probability and missing-tool weighting.\n");
            Debug.Log("Adventure selection and branch validation passed.");
        }

        [Test]
        public void AllEvents_HaveReachableTerminalsAndConsistentTooltips()
        {
            var events = AssetDatabase.FindAssets("t:AdventureEventSO", new[] { "Assets/Work/Adventure/SO/Dialog" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AdventureEventSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.That(events.Length, Is.EqualTo(55));
            int count = 0;
            foreach (var e in events)
            {
                var paths = Paths(e.options).ToArray();
                Assert.That(paths.Length, Is.GreaterThan(0), e.name);
                count += paths.Length;
                foreach (var option in paths.SelectMany(p => p).Distinct())
                {
                    Assert.That(option.OptionName.EndsWith("다"), Is.True, e.name);
                    string expected = "필요한 아이템: 없음.";
                    if (option is LockedOption locked)
                    {
                        Assert.That(locked.KeyItem, Is.Not.Null);
                        expected = locked.IsUnLockOption ? $"선택 조건: {locked.KeyItem.ItemName} 미보유."
                            : $"필요한 아이템: {locked.KeyItem.ItemName} 1개." + (locked.IsUseItemOption ? " 선택 시 1개 소모됩니다." : "");
                        Assert.That(locked.LockTooltip, Is.EqualTo(expected));
                    }
                    if (option is IngredientLockedOption cost)
                    {
                        Assert.That(cost.RequiredIngredient, Is.Not.Null);
                        expected = $"필요한 아이템: {cost.RequiredIngredient.DisplayName} {cost.RequiredAmount}개. 선택 시 {cost.RequiredAmount}개 소모됩니다.";
                        Assert.That(cost.LockTooltip, Is.EqualTo(expected));
                    }
                    Assert.That(option.OptionTooltip, Is.EqualTo(expected), e.name);
                    if (option.followUpOptions?.Count > 0)
                        Assert.That(option.rewardMethod, Is.Empty, "Current inquiry branches must not award items");
                }
            }
            Assert.That(count, Is.EqualTo(159));
        }

        private readonly List<Object> created = new List<Object>();
        private AdventureEventSO Event(bool supply, AdventureItemSO item, bool locked = false)
        {
            var e = ScriptableObject.CreateInstance<AdventureEventSO>(); created.Add(e);
            Options option = locked ? new LockedOption() : new Options();
            if (supply)
            {
                var reward = new AdventureItemReward();
                typeof(AdventureItemReward).GetField("itemSO", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(reward, item);
                option.rewardMethod.Add(reward);
            }
            e.options.Add(option);
            return e;
        }
        private AdventureItemSO Item() { var i = ScriptableObject.CreateInstance<AdventureItemSO>(); created.Add(i); return i; }
        private void Cleanup() { foreach (var o in created) Object.DestroyImmediate(o); created.Clear(); }

        [Test]
        public void Selection_BoostsEmptyInventoryAndGuaranteesSupply()
        {
            try
            {
                var supply = Event(true, Item()); var ordinary = Event(false, null);
                var selector = new AdventureEventSelector();
                var pool = new[] { ordinary, supply };
                Assert.That(selector.Select(pool, null, false, _ => false, _ => true, 0, 3, 2, () => 0.99f), Is.SameAs(supply));
                for (int i = 0; i < 3; i++)
                    Assert.That(selector.Select(pool, null, true, _ => true, _ => true, 0, 3, 2, () => 0.99f), Is.SameAs(ordinary));
                Assert.That(selector.Select(pool, null, true, _ => true, _ => true, 0, 3, 2, () => 0.99f), Is.SameAs(supply));
                Assert.That(selector.EventsWithoutSupply, Is.Zero);
            }
            finally { Cleanup(); }
        }

        [Test]
        public void Selection_RejectsUnavailableSupplyAndHandlesSparseLists()
        {
            try
            {
                var locked = Event(true, Item(), true); var ordinary = Event(false, null);
                Assert.That(AdventureEventSelector.AvailableItems(locked, o => !(o is LockedOption)), Is.Empty);
                var selector = new AdventureEventSelector();
                Assert.That(selector.Select(new AdventureEventSO[] { null }, null, false, _ => false, _ => true, 1, 3, 2, () => 1), Is.Null);
                Assert.That(selector.Select(new[] { null, ordinary, ordinary }, ordinary, false, _ => false, _ => true, 1, 3, 2, () => 1), Is.SameAs(ordinary));
                Assert.That(selector.Select(new[] { locked, ordinary }, locked, true, _ => false, _ => true, 1, 3, 2, () => 1), Is.SameAs(ordinary));
                var parent = new Options(); parent.followUpOptions.Add(locked.options[0]); locked.options.Clear(); locked.options.Add(parent);
                Assert.That(AdventureEventSelector.AvailableItems(locked, o => !(o is LockedOption)), Is.Empty);
                Assert.That(AdventureEventSelector.AvailableItems(locked, _ => true).Count, Is.EqualTo(1));
            }
            finally { Cleanup(); }
        }

        [Test]
        public void Selection_UsesPoolChanceAndMissingToolWeights()
        {
            try
            {
                var missing = Item(); var owned = Item();
                var a = Event(true, missing); var b = Event(true, owned); var ordinary = Event(false, null);
                var rng = new System.Random(12345); int supplyCount = 0, missingCount = 0;
                for (int i = 0; i < 20000; i++)
                {
                    // Fresh history isolates the base probability from the separate drought guarantee.
                    var selected = new AdventureEventSelector().Select(new[] { a, b, ordinary }, null, true,
                        item => item == owned, _ => true, 0.35f, 3, 2, () => (float)rng.NextDouble());
                    if (selected != ordinary) supplyCount++;
                    if (selected == a) missingCount++;
                }
                Assert.That(supplyCount / 20000f, Is.InRange(0.33f, 0.37f));
                Assert.That(missingCount / (float)supplyCount, Is.InRange(0.64f, 0.69f));
            }
            finally { Cleanup(); }
        }
    }
}
