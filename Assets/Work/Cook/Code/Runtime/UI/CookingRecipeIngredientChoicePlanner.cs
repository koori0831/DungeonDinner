using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    internal sealed class CookingRecipeIngredientChoicePlan
    {
        public CookingRecipeIngredientChoicePlan(
            List<IngredientSO> fixedIngredients,
            List<IngredientSO> choiceCandidates,
            int minChoiceCount,
            int maxChoiceCount)
        {
            FixedIngredients = fixedIngredients;
            ChoiceCandidates = choiceCandidates;
            MinChoiceCount = minChoiceCount;
            MaxChoiceCount = maxChoiceCount;
        }

        public IReadOnlyList<IngredientSO> FixedIngredients { get; }
        public IReadOnlyList<IngredientSO> ChoiceCandidates { get; }
        public int MinChoiceCount { get; }
        public int MaxChoiceCount { get; }
    }

    internal static class CookingRecipeIngredientChoicePlanner
    {
        public static bool TryBuild(
            RecipeSO recipe,
            IReadOnlyList<IngredientSO> availableIngredients,
            out CookingRecipeIngredientChoicePlan plan)
        {
            plan = null;

            if (recipe == null)
                return false;

            List<IngredientSO> fixedIngredients = new List<IngredientSO>();
            List<IngredientSO> choiceCandidates = new List<IngredientSO>();
            int minChoiceCount = 0;
            int maxChoiceCount = 0;

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null)
                    continue;

                List<IngredientSO> candidates = BuildRecipeRequirementCandidates(requirement, availableIngredients);
                if (RequiresPlayerChoice(requirement, candidates) == true)
                {
                    AddUnique(choiceCandidates, candidates);
                    minChoiceCount += requirement.MinCount;
                    if (requirement.HasMaxCount == true)
                        maxChoiceCount += requirement.MaxCount;
                    else
                        maxChoiceCount = 0;

                    continue;
                }

                int autoCount = requirement.MinCount > 1 ? requirement.MinCount : 1;
                if (candidates.Count > 0)
                {
                    IngredientSO fixedIngredient = requirement.Ingredient != null
                        ? requirement.Ingredient
                        : candidates[0];
                    for (int count = 0; count < autoCount; count++)
                        fixedIngredients.Add(fixedIngredient);
                }
            }

            if (choiceCandidates.Count == 0)
                return false;

            plan = new CookingRecipeIngredientChoicePlan(
                fixedIngredients,
                choiceCandidates,
                minChoiceCount,
                maxChoiceCount);
            return true;
        }

        private static List<IngredientSO> BuildRecipeRequirementCandidates(
            RecipeIngredientRequirement requirement,
            IReadOnlyList<IngredientSO> availableIngredients)
        {
            List<IngredientSO> candidates = new List<IngredientSO>();
            if (requirement == null)
                return candidates;

            if (availableIngredients != null)
            {
                for (int i = 0; i < availableIngredients.Count; i++)
                {
                    IngredientSO ingredient = availableIngredients[i];
                    if (ingredient != null && requirement.IsMatchedBy(ingredient) == true)
                        AddUnique(candidates, ingredient);
                }
            }

            if (candidates.Count == 0 && requirement.Ingredient != null)
                candidates.Add(requirement.Ingredient);

            return candidates;
        }

        private static bool RequiresPlayerChoice(
            RecipeIngredientRequirement requirement,
            IReadOnlyList<IngredientSO> candidates)
        {
            if (requirement == null || candidates == null)
                return false;

            if (requirement.RequiresChoice == false)
                return false;

            if (candidates.Count <= 1)
                return false;

            if (requirement.HasMaxCount == true && requirement.MinCount == requirement.MaxCount && candidates.Count <= requirement.MinCount)
                return false;

            return true;
        }

        private static void AddUnique(ICollection<IngredientSO> target, IngredientSO ingredient)
        {
            if (target == null || ingredient == null || target.Contains(ingredient) == true)
                return;

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
