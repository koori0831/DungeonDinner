using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "Recipe", menuName = "Dungeon Dinner/Cooking/Recipe")]
    public sealed class RecipeSO : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private FoodCategorySO category;
        [SerializeField] private List<FoodTagSO> baseTags = new List<FoodTagSO>();
        [SerializeField] private List<RecipeIngredientRequirement> requiredIngredients = new List<RecipeIngredientRequirement>();
        [SerializeField] private List<RecipePreparationRule> perfectPreparationRules = new List<RecipePreparationRule>();

        public string RecipeId => recipeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? recipeId : displayName;
        public string Description => description;
        public FoodCategorySO Category => category;
        public IReadOnlyList<FoodTagSO> BaseTags => baseTags;
        public IReadOnlyList<RecipeIngredientRequirement> RequiredIngredients => requiredIngredients;
        public IReadOnlyList<RecipePreparationRule> PerfectPreparationRules => perfectPreparationRules;

        public bool MatchesIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null || ingredients.Count != requiredIngredients.Count)
                return false;

            bool[] used = new bool[ingredients.Count];
            for (int requirementIndex = 0; requirementIndex < requiredIngredients.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[requirementIndex];
                bool matched = false;

                for (int ingredientIndex = 0; ingredientIndex < ingredients.Count; ingredientIndex++)
                {
                    if (used[ingredientIndex])
                        continue;

                    if (requirement != null && requirement.IsMatchedBy(ingredients[ingredientIndex]))
                    {
                        used[ingredientIndex] = true;
                        matched = true;
                        break;
                    }
                }

                if (matched == false)
                    return false;
            }

            return true;
        }

        public bool IsPerfectPreparation(IngredientSO ingredient, PreparationMethodSO method)
        {
            if (ingredient == null || method == null)
                return false;

            for (int i = 0; i < perfectPreparationRules.Count; i++)
            {
                RecipePreparationRule rule = perfectPreparationRules[i];
                if (rule != null
                    && rule.PerfectMethod == method
                    && IsRequirementIngredientMatchedBy(rule.Ingredient, ingredient))
                    return true;
            }

            return false;
        }

        public bool IsRequirementIngredientMatchedBy(IngredientSO requirementIngredient, IngredientSO candidate)
        {
            if (requirementIngredient == null || candidate == null)
                return false;

            if (candidate == requirementIngredient)
                return true;

            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[i];
                if (requirement != null
                    && requirement.Ingredient == requirementIngredient
                    && requirement.IsMatchedBy(candidate))
                    return true;
            }

            return false;
        }

        public bool HasPerfectPreparationRules => perfectPreparationRules.Count > 0;
    }
}
