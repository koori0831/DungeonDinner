using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Adventure.Code;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Core.EventBus;
using Work.Players.Code.Inventory;

namespace Work.Cook.Code.Runtime.Systems
{
    public sealed partial class CookingKnowledgeStore
    {
        private readonly HashSet<string> _discoveredEntryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _recordedResultSessionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<DishResult> _recordedResultObjects = new HashSet<DishResult>();
        private readonly HashSet<DishResult> _servedResultObjects = new HashSet<DishResult>();

        public static string IngredientEntryId(IngredientSO ingredient) => "ingredient:" + CookingKnowledgeKeyUtility.GetIngredientId(ingredient);
        public static string RecipeEntryId(RecipeSO recipe) => "recipe:" + CookingKnowledgeKeyUtility.GetRecipeId(recipe);

        public bool IsEntryDiscovered(string entryId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(entryId) && _discoveredEntryIds.Contains(entryId);
        }

        public bool DiscoverEntry(string entryId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(entryId) || !_discoveredEntryIds.Add(entryId.Trim())) return false;
            CommitChanges();
            return true;
        }

        public bool LearnFromResult(DishResult result)
        {
            EnsureInitialized();
            if (result == null || !_recordedResultObjects.Add(result)) return false;
            if (!string.IsNullOrWhiteSpace(result.CookingSessionId) && !_recordedResultSessionIds.Add(result.CookingSessionId)) return false;
            if (result.TargetRecipe != null) AddAttemptedRecipe(result.TargetRecipe);
            if (result.IsRecipeMatched && result.BaseRecipe != null)
            {
                AddAttemptedRecipe(result.BaseRecipe);
                AddRecipe(result.BaseRecipe);
                RecordRecipeCompletion(result, null, result.Tags);
                AddRecipeTags(result.BaseRecipe, result.Tags);
            }
            else _discoveredEntryIds.Add("incomplete_dish");
            if (result.PreparedIngredients != null)
                foreach (var prepared in result.PreparedIngredients)
                {
                    if (prepared?.Ingredient == null) continue;
                    _discoveredEntryIds.Add(IngredientEntryId(prepared.Ingredient));
                    AddTriedIngredient(prepared.Ingredient);
                    AddTriedPreparation(prepared.Ingredient, prepared.PreparationOption);
                    AddPreparationEffect(prepared.Ingredient, prepared.PreparationOption);
                }
            CommitChanges();
            return true;
        }

        private void OnEnable()
        {
            Bus<AdventureLineObservedEvent>.Events += ObserveAdventureLine;
            Bus<OnAddAdventureItemAfterEvent>.Events += ObserveTool;
            Bus<InventoryChangedEvent>.Events += ObserveInventory;
            Bus<CookingDishResultReadyEvent>.Events += ObserveDish;
        }

        private void OnDisable()
        {
            Bus<AdventureLineObservedEvent>.Events -= ObserveAdventureLine;
            Bus<OnAddAdventureItemAfterEvent>.Events -= ObserveTool;
            Bus<InventoryChangedEvent>.Events -= ObserveInventory;
            Bus<CookingDishResultReadyEvent>.Events -= ObserveDish;
        }

        private void ObserveAdventureLine(AdventureLineObservedEvent evt)
        {
            EnsureInitialized();
            bool changed = false;
            if (evt.Line?.DiscoveryEntryIds != null)
                foreach (var id in evt.Line.DiscoveryEntryIds)
                    if (!string.IsNullOrWhiteSpace(id)) changed |= _discoveredEntryIds.Add(id);
            if (changed) CommitChanges();
        }
        private void ObserveTool(OnAddAdventureItemAfterEvent evt)
        {
            if (evt.count > 0 && evt.itemSO != null) DiscoverEntry(evt.itemSO.DiscoveryEntryId);
        }
        private void ObserveDish(CookingDishResultReadyEvent evt) => LearnFromResult(evt.Result);
        private void ObserveInventory(InventoryChangedEvent evt)
        {
            EnsureInitialized();
            var inventory = FindFirstObjectByType<PlayerInventoryModule>();
            if (inventory == null) return;
            bool changed = false;
            for (int i = 0; i < inventory.SlotCapacity; i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot != null && slot.Amount > 0 && slot.Item is IngredientItemDataSO item && item.Ingredient != null)
                    changed |= _discoveredEntryIds.Add(IngredientEntryId(item.Ingredient));
            }
            if (changed) CommitChanges();
        }
    }
}
