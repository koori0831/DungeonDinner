using System;
using System.Collections.Generic;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Runtime.Systems
{
    public sealed class CookingRecipeStartPlanBuilder
    {
        private readonly IReadOnlyList<IngredientSO> _catalogIngredients;
        private readonly Func<IngredientSO, int> _quantityProvider;
        private readonly CookingKnowledgeStore _knowledgeStore;

        public CookingRecipeStartPlanBuilder(
            IReadOnlyList<IngredientSO> catalogIngredients,
            Func<IngredientSO, int> quantityProvider,
            CookingKnowledgeStore knowledgeStore)
        {
            _catalogIngredients = catalogIngredients ?? Array.Empty<IngredientSO>();
            _quantityProvider = quantityProvider;
            _knowledgeStore = knowledgeStore;
        }

        public CookingRecipeStartPlan BuildBase(RecipeSO recipe)
        {
            return Build(recipe, null);
        }

        public CookingRecipeStartPlan BuildVariant(
            RecipeSO recipe,
            CookingRecipeVariantKnowledgeSnapshot variant)
        {
            return Build(recipe, variant);
        }

        private CookingRecipeStartPlan Build(
            RecipeSO recipe,
            CookingRecipeVariantKnowledgeSnapshot variant)
        {
            List<RecipeRequirementPlan> requirements = BuildRequirements(recipe);
            List<IngredientSO> candidates = BuildAllCandidates(requirements);
            List<PlannedIngredientOccurrence> presets = new List<PlannedIngredientOccurrence>();
            List<PlannedPreparation> recommendations = new List<PlannedPreparation>();
            List<IngredientShortage> shortages = new List<IngredientShortage>();

            bool legacyUnavailable = variant != null && variant.CanReplay == false;
            if (variant != null && variant.CanReplay)
                BuildVariantPresets(recipe, variant, presets, recommendations, shortages);
            else
                BuildBasePresets(requirements, presets, shortages);

            BuildDefaultPreparationRecommendations(recipe, requirements, recommendations);
            return new CookingRecipeStartPlan(
                recipe,
                variant?.VariantId,
                requirements,
                candidates,
                presets,
                recommendations,
                shortages,
                legacyUnavailable);
        }

        private List<RecipeRequirementPlan> BuildRequirements(RecipeSO recipe)
        {
            List<RecipeRequirementPlan> plans = new List<RecipeRequirementPlan>();
            if (recipe == null)
                return plans;
            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null)
                    continue;
                List<IngredientSO> candidates = new List<IngredientSO>();
                AddUnique(candidates, requirement.Ingredient);
                AddUnique(candidates, requirement.Alternatives);
                for (int alternativeIndex = 0; alternativeIndex < requirement.AlternativeOptions.Count; alternativeIndex++)
                    AddUnique(candidates, requirement.AlternativeOptions[alternativeIndex]?.Ingredient);
                for (int ingredientIndex = 0; ingredientIndex < _catalogIngredients.Count; ingredientIndex++)
                {
                    IngredientSO ingredient = _catalogIngredients[ingredientIndex];
                    if (requirement.IsMatchedBy(ingredient))
                        AddUnique(candidates, ingredient);
                }
                plans.Add(new RecipeRequirementPlan(requirement, candidates));
            }
            return plans;
        }

        private static List<IngredientSO> BuildAllCandidates(IReadOnlyList<RecipeRequirementPlan> requirements)
        {
            List<IngredientSO> candidates = new List<IngredientSO>();
            for (int i = 0; i < requirements.Count; i++)
                AddUnique(candidates, requirements[i].Candidates);
            return candidates;
        }

        private void BuildBasePresets(
            IReadOnlyList<RecipeRequirementPlan> requirements,
            ICollection<PlannedIngredientOccurrence> presets,
            ICollection<IngredientShortage> shortages)
        {
            Dictionary<IngredientSO, int> reserved = new Dictionary<IngredientSO, int>();
            for (int i = 0; i < requirements.Count; i++)
            {
                RecipeIngredientRequirement requirement = requirements[i].Requirement;
                if (requirement == null || requirement.RecipeDefining == false || requirement.MinCount <= 0)
                    continue;

                IngredientSO preset = requirement.Ingredient;
                if (preset == null)
                {
                    List<IngredientSO> ownedCandidates = new List<IngredientSO>();
                    for (int candidateIndex = 0; candidateIndex < requirements[i].Candidates.Count; candidateIndex++)
                    {
                        IngredientSO candidate = requirements[i].Candidates[candidateIndex];
                        if (GetUnreservedQuantity(candidate, reserved) > 0)
                            ownedCandidates.Add(candidate);
                    }
                    if (ownedCandidates.Count == 1)
                        preset = ownedCandidates[0];
                }

                if (preset == null)
                    continue;
                int available = GetUnreservedQuantity(preset, reserved);
                int selected = Math.Min(requirement.MinCount, available);
                for (int occurrence = 0; occurrence < selected; occurrence++)
                    presets.Add(new PlannedIngredientOccurrence(requirement.RequirementId, preset, occurrence));
                Reserve(reserved, preset, selected);
                if (selected < requirement.MinCount)
                {
                    shortages.Add(new IngredientShortage(
                        requirement.RequirementId,
                        preset,
                        requirement.MinCount,
                        selected));
                }
            }
        }

        private void BuildVariantPresets(
            RecipeSO recipe,
            CookingRecipeVariantKnowledgeSnapshot variant,
            ICollection<PlannedIngredientOccurrence> presets,
            ICollection<PlannedPreparation> recommendations,
            ICollection<IngredientShortage> shortages)
        {
            Dictionary<IngredientSO, int> reserved = new Dictionary<IngredientSO, int>();
            Dictionary<string, int> occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < variant.ReplayComponents.Count; i++)
            {
                VariantComponentRecord component = variant.ReplayComponents[i];
                IngredientSO ingredient = FindIngredient(component?.ingredientId);
                if (ingredient == null)
                    continue;
                string occurrenceKey = (component.requirementId ?? string.Empty) + "|" + ingredient.IngredientId;
                occurrences.TryGetValue(occurrenceKey, out int occurrence);
                occurrences[occurrenceKey] = occurrence + 1;

                int available = GetUnreservedQuantity(ingredient, reserved);
                if (available <= 0)
                {
                    shortages.Add(new IngredientShortage(component.requirementId, ingredient, 1, 0));
                    continue;
                }

                presets.Add(new PlannedIngredientOccurrence(component.requirementId, ingredient, occurrence));
                Reserve(reserved, ingredient, 1);
                IngredientPreparationOption option = ingredient.FindPreparationOption(component.preparationOptionId);
                if (option != null)
                {
                    recommendations.Add(new PlannedPreparation(
                        component.requirementId,
                        ingredient,
                        occurrence,
                        option,
                        CookingPreparationRecommendationKind.ReplayedVariant));
                }
            }
        }

        private void BuildDefaultPreparationRecommendations(
            RecipeSO recipe,
            IReadOnlyList<RecipeRequirementPlan> requirements,
            ICollection<PlannedPreparation> recommendations)
        {
            if (recipe == null)
                return;
            for (int requirementIndex = 0; requirementIndex < requirements.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = requirements[requirementIndex].Requirement;
                for (int candidateIndex = 0; candidateIndex < requirements[requirementIndex].Candidates.Count; candidateIndex++)
                {
                    IngredientSO ingredient = requirements[requirementIndex].Candidates[candidateIndex];
                    if (HasRecommendation(recommendations, requirement.RequirementId, ingredient))
                        continue;

                    IngredientPreparationOption option = FindKnownPerfectOption(recipe, ingredient);
                    CookingPreparationRecommendationKind kind = CookingPreparationRecommendationKind.KnownPerfect;
                    if (option == null)
                    {
                        option = FindSoleAllowedOption(requirement, ingredient);
                        kind = CookingPreparationRecommendationKind.SoleRecipeAllowed;
                    }
                    if (option != null)
                    {
                        recommendations.Add(new PlannedPreparation(
                            requirement.RequirementId,
                            ingredient,
                            -1,
                            option,
                            kind));
                    }
                }
            }
        }

        private IngredientPreparationOption FindKnownPerfectOption(RecipeSO recipe, IngredientSO ingredient)
        {
            for (int i = 0; i < recipe.PerfectPreparationRules.Count; i++)
            {
                RecipePreparationRule rule = recipe.PerfectPreparationRules[i];
                if (rule == null || rule.Ingredient != ingredient || rule.PerfectMethod == null)
                    continue;
                IngredientPreparationOption option = ingredient.FindPreparationOption(rule.PerfectMethod);
                if (option != null && _knowledgeStore != null
                                   && _knowledgeStore.IsPreparationEffectKnown(ingredient, option))
                    return option;
            }
            return null;
        }

        private static IngredientPreparationOption FindSoleAllowedOption(
            RecipeIngredientRequirement requirement,
            IngredientSO ingredient)
        {
            if (requirement == null || ingredient == null || requirement.HasRequiredPreparationMethods == false)
                return null;
            PreparationMethodSO onlyMethod = null;
            int methodCount = 0;
            for (int i = 0; i < requirement.RequiredPreparationMethods.Count; i++)
            {
                PreparationMethodSO method = requirement.RequiredPreparationMethods[i];
                if (method == null || method == onlyMethod)
                    continue;
                onlyMethod = method;
                methodCount++;
            }
            if (methodCount == 0 && requirement.RequiredPreparationMethod != null)
            {
                onlyMethod = requirement.RequiredPreparationMethod;
                methodCount = 1;
            }
            return methodCount == 1 ? ingredient.FindPreparationOption(onlyMethod) : null;
        }

        private static bool HasRecommendation(
            IEnumerable<PlannedPreparation> recommendations,
            string requirementId,
            IngredientSO ingredient)
        {
            foreach (PlannedPreparation recommendation in recommendations)
            {
                if (recommendation?.Ingredient == ingredient
                    && string.Equals(recommendation.RequirementId, requirementId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private IngredientSO FindIngredient(string ingredientId)
        {
            for (int i = 0; i < _catalogIngredients.Count; i++)
            {
                IngredientSO ingredient = _catalogIngredients[i];
                if (ingredient != null
                    && string.Equals(ingredient.IngredientId, ingredientId, StringComparison.OrdinalIgnoreCase))
                    return ingredient;
            }
            return null;
        }

        private int GetUnreservedQuantity(IngredientSO ingredient, IDictionary<IngredientSO, int> reserved)
        {
            if (ingredient == null)
                return 0;
            int owned = Math.Max(0, _quantityProvider != null ? _quantityProvider(ingredient) : 0);
            reserved.TryGetValue(ingredient, out int used);
            return Math.Max(0, owned - used);
        }

        private static void Reserve(IDictionary<IngredientSO, int> reserved, IngredientSO ingredient, int amount)
        {
            if (ingredient == null || amount <= 0)
                return;
            reserved.TryGetValue(ingredient, out int used);
            reserved[ingredient] = used + amount;
        }

        private static void AddUnique(ICollection<IngredientSO> target, IngredientSO ingredient)
        {
            if (target != null && ingredient != null && target.Contains(ingredient) == false)
                target.Add(ingredient);
        }

        private static void AddUnique(ICollection<IngredientSO> target, IReadOnlyList<IngredientSO> ingredients)
        {
            if (target == null || ingredients == null)
                return;
            for (int i = 0; i < ingredients.Count; i++)
                AddUnique(target, ingredients[i]);
        }
    }
}
