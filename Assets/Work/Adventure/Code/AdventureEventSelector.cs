using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Work.Adventure.Code.DialogMethod;
using Work.Adventure.Code.Rewards;

namespace Work.Adventure.Code
{
    // Counts opportunities offered, not rewards accepted by the player.
    public sealed class AdventureEventSelector
    {
        public int EventsWithoutSupply { get; private set; }

        public AdventureEventSO Select(IEnumerable<AdventureEventSO> source, AdventureEventSO previous,
            int toolCount, Func<AdventureItemSO, bool> hasItem, Func<Options, bool> canSelect,
            float supplyChance, int maxWithoutSupply, float missingToolWeight, Func<float> random)
        {
            var all = source.Where(e => e != null).Distinct().ToList();
            if (all.Count == 0) return null;
            var candidates = all.Count > 1 ? all.Where(e => e != previous).ToList() : all;
            var supplies = candidates.ToDictionary(e => e, e => AvailableItems(e, canSelect));
            var supply = candidates.Where(e => supplies[e].Count > 0).ToList();
            var ordinary = candidates.Where(e => supplies[e].Count == 0).ToList();
            toolCount = Mathf.Max(0, toolCount);
            // A stocked inventory must not receive forced supplies every fourth encounter.
            bool guarantee = toolCount <= 1 && EventsWithoutSupply >= Mathf.Max(1, maxWithoutSupply) * 2;
            bool chooseSupply = supply.Count > 0 && (toolCount == 0 || ordinary.Count == 0
                || guarantee || random() < SupplyProbability(toolCount, supplyChance));
            var pool = chooseSupply ? supply : ordinary.Count > 0 ? ordinary : candidates;
            float Weight(AdventureEventSO e) => chooseSupply
                ? supplies[e].Any(item => !hasItem(item)) ? Mathf.Max(1f, missingToolWeight) : 1f
                : toolCount >= 2 ? UsageWeight(e.options, canSelect, toolCount >= 4, new HashSet<Options>()) : 1f;
            float roll = Mathf.Clamp01(random()) * pool.Sum(Weight);
            var selected = pool[pool.Count - 1];
            foreach (var e in pool)
            {
                roll -= Weight(e);
                if (roll < 0) { selected = e; break; }
            }
            EventsWithoutSupply = supplies[selected].Count > 0 ? 0
                : Math.Min(EventsWithoutSupply + 1, Mathf.Max(1, maxWithoutSupply) * 2);
            return selected;
        }

        public static float SupplyProbability(int toolCount, float baseChance)
        {
            if (toolCount <= 0) return 1f;
            float[] multipliers = { 1f, 0.7f, 0.45f, 0.28f, 0.18f, 0.12f, 0.07f };
            return Mathf.Clamp01(baseChance) * multipliers[Mathf.Min(toolCount, 6)];
        }

        private static float UsageWeight(IEnumerable<Options> options, Func<Options, bool> canSelect,
            bool preferConsumption, HashSet<Options> visited)
        {
            float weight = 1f;
            if (options == null) return weight;
            foreach (var option in options)
            {
                if (option == null || !visited.Add(option) || !canSelect(option)) continue;
                if (option is LockedOption locked && !locked.IsUnLockOption)
                    weight = Mathf.Max(weight, preferConsumption && locked.IsUseItemOption ? 3f : 2f);
                weight = Mathf.Max(weight, UsageWeight(option.followUpOptions, canSelect, preferConsumption, visited));
            }
            return weight;
        }

        public static HashSet<AdventureItemSO> AvailableItems(AdventureEventSO e, Func<Options, bool> canSelect)
        {
            var result = new HashSet<AdventureItemSO>();
            AddDialogItems(e.dialogDatas, result);
            var visited = new HashSet<Options>();
            void Visit(IEnumerable<Options> options)
            {
                if (options == null) return;
                foreach (var option in options)
                {
                    if (option == null || !visited.Add(option) || !canSelect(option)) continue;
                    foreach (var reward in option.rewardMethod.OfType<AdventureItemReward>())
                        if (reward.Item != null) result.Add(reward.Item);
                    AddDialogItems(option.ResultdialogDatas, result);
                    Visit(option.followUpOptions);
                }
            }
            Visit(e.options);
            return result;
        }

        private static void AddDialogItems(IEnumerable<AdventrueDialogData> lines, HashSet<AdventureItemSO> result)
        {
            foreach (var line in lines)
                foreach (var method in line.method.OfType<AddAdventureItem>())
                    if (method.Item != null) result.Add(method.Item);
        }
    }
}
