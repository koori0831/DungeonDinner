using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.Integration
{
    public sealed class CookingCatalogIngredientSource : MonoBehaviour, ICookingIngredientSource, ICookingIngredientQuantitySource
    {
        [SerializeField] private CookingDataCatalogSO fallbackCatalog;
        [SerializeField] private bool preferRunnerCatalog = true;
        [SerializeField] private bool includeManualIngredients;
        [SerializeField] private List<IngredientSO> manualIngredients = new List<IngredientSO>();
        [SerializeField] private List<IngredientStack> manualIngredientStacks = new List<IngredientStack>();
        [SerializeField, Min(0)] private int defaultCatalogIngredientQuantity = 1;

        public event Action IngredientsChanged;
        public string SourceName => "카탈로그 재료";

        public IReadOnlyList<IngredientSO> GetAvailableIngredients(CookingGamePanel owner, CookingFlowRunner runner)
        {
            IReadOnlyList<IngredientSO> baseIngredients = GetBaseIngredients(owner, runner);
            if (includeManualIngredients == false)
                return baseIngredients ?? Array.Empty<IngredientSO>();

            List<IngredientSO> merged = new List<IngredientSO>();
            AddUnique(merged, baseIngredients);
            AddUnique(merged, manualIngredients);
            AddUnique(merged, manualIngredientStacks);
            return merged;
        }

        public int GetAvailableIngredientQuantity(
            IngredientSO ingredient,
            CookingGamePanel owner,
            CookingFlowRunner runner)
        {
            if (ingredient == null)
                return 0;

            int manualQuantity = FindManualQuantity(ingredient);
            if (manualQuantity >= 0)
                return manualQuantity;

            return ContainsIngredient(GetBaseIngredients(owner, runner), ingredient)
                ? Mathf.Max(0, defaultCatalogIngredientQuantity)
                : 0;
        }

        public void SetFallbackCatalog(CookingDataCatalogSO value)
        {
            if (fallbackCatalog == value)
                return;

            fallbackCatalog = value;
            NotifyIngredientsChanged();
        }

        public void NotifyIngredientsChanged()
        {
            IngredientsChanged?.Invoke();
        }

        private IReadOnlyList<IngredientSO> GetBaseIngredients(CookingGamePanel owner, CookingFlowRunner runner)
        {
            CookingFlowRunner resolvedRunner = runner != null ? runner : owner != null ? owner.FlowRunner : null;
            if (preferRunnerCatalog && resolvedRunner != null && resolvedRunner.Ingredients.Count > 0)
                return resolvedRunner.Ingredients;

            if (fallbackCatalog != null)
                return fallbackCatalog.Ingredients;

            return resolvedRunner != null ? resolvedRunner.Ingredients : Array.Empty<IngredientSO>();
        }

        private static void AddUnique(ICollection<IngredientSO> target, IReadOnlyList<IngredientSO> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                IngredientSO ingredient = source[i];
                if (ingredient != null && target.Contains(ingredient) == false)
                    target.Add(ingredient);
            }
        }

        private static void AddUnique(ICollection<IngredientSO> target, IReadOnlyList<IngredientStack> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                IngredientSO ingredient = source[i]?.Ingredient;
                if (ingredient != null && target.Contains(ingredient) == false)
                    target.Add(ingredient);
            }
        }

        private int FindManualQuantity(IngredientSO ingredient)
        {
            for (int i = 0; i < manualIngredientStacks.Count; i++)
            {
                IngredientStack stack = manualIngredientStacks[i];
                if (stack != null && stack.Ingredient == ingredient)
                    return stack.Quantity;
            }

            return manualIngredients.Contains(ingredient) ? 1 : -1;
        }

        private static bool ContainsIngredient(IReadOnlyList<IngredientSO> ingredients, IngredientSO ingredient)
        {
            if (ingredients == null || ingredient == null)
                return false;

            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] == ingredient)
                    return true;
            }

            return false;
        }

        [Serializable]
        private sealed class IngredientStack
        {
            [SerializeField] private IngredientSO ingredient;
            [SerializeField, Min(0)] private int quantity = 1;

            public IngredientSO Ingredient => ingredient;
            public int Quantity => Mathf.Max(0, quantity);
        }
    }
}
