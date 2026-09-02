using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "CookingDataCatalog", menuName = "Dungeon Dinner/Cooking/Data Catalog")]
    public sealed class CookingDataCatalogSO : ScriptableObject
    {
        [SerializeField] private List<FoodCategorySO> categories = new List<FoodCategorySO>();
        [SerializeField] private List<IngredientCategorySO> ingredientCategories = new List<IngredientCategorySO>();
        [SerializeField] private List<FoodTagSO> tags = new List<FoodTagSO>();
        [SerializeField] private List<PreparationMethodSO> preparationMethods = new List<PreparationMethodSO>();
        [SerializeField] private List<IngredientSO> ingredients = new List<IngredientSO>();
        [SerializeField] private List<RecipeSO> recipes = new List<RecipeSO>();

        public IReadOnlyList<FoodCategorySO> Categories => categories;
        public IReadOnlyList<IngredientCategorySO> IngredientCategories => ingredientCategories;
        public IReadOnlyList<FoodTagSO> Tags => tags;
        public IReadOnlyList<PreparationMethodSO> PreparationMethods => preparationMethods;
        public IReadOnlyList<IngredientSO> Ingredients => ingredients;
        public IReadOnlyList<RecipeSO> Recipes => recipes;

        public RecipeSO FindRecipeById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return null;

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeSO recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.RecipeId, recipeId, System.StringComparison.OrdinalIgnoreCase))
                    return recipe;
            }

            return null;
        }

        public RecipeSO FindRecipeByIngredients(IReadOnlyList<IngredientSO> selectedIngredients)
        {
            RecipeSO bestRecipe = null;
            int bestScore = int.MinValue;
            bool tied = false;
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeSO recipe = recipes[i];
                if (recipe != null
                    && recipe.HasRequiredPreparationMethods == false
                    && recipe.MatchesIngredients(selectedIngredients))
                {
                    int score = recipe.CalculateMatchSpecificityScore();
                    if (score > bestScore)
                    {
                        bestRecipe = recipe;
                        bestScore = score;
                        tied = false;
                    }
                    else if (score == bestScore)
                        tied = true;
                }
            }

            return tied ? null : bestRecipe;
        }

        public RecipeSO FindRecipeByPreparedIngredients(IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            RecipeSO bestRecipe = null;
            int bestScore = int.MinValue;
            bool tied = false;

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeSO recipe = recipes[i];
                if (recipe == null)
                    continue;

                int score = recipe.CalculateMatchScore(preparedIngredients);
                if (score > bestScore)
                {
                    bestRecipe = recipe;
                    bestScore = score;
                    tied = false;
                }
                else if (score >= 0 && score == bestScore)
                    tied = true;
            }

            return bestScore >= 0 && tied == false ? bestRecipe : null;
        }

        public List<string> BuildValidationMessages()
        {
            List<string> messages = new List<string>();
            CookingDataValidationReport report = new CookingDataValidationService().ValidateCatalog(this);
            for (int i = 0; i < report.Issues.Count; i++)
                messages.Add(report.Issues[i].ToString());
            return messages;
        }
    }
}
