using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Runtime;

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
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeSO recipe = recipes[i];
                if (recipe != null
                    && recipe.HasRequiredPreparationMethods == false
                    && recipe.MatchesIngredients(selectedIngredients))
                    return recipe;
            }

            return null;
        }

        public RecipeSO FindRecipeByPreparedIngredients(IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            RecipeSO bestRecipe = null;
            int bestScore = int.MinValue;

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
                }
            }

            return bestScore >= 0 ? bestRecipe : null;
        }

        public List<string> BuildValidationMessages()
        {
            List<string> messages = new List<string>();
            AddDuplicateIdMessages(messages, categories, category => category != null ? category.CategoryId : string.Empty, "category");
            AddDuplicateIdMessages(messages, ingredientCategories, category => category != null ? category.CategoryId : string.Empty, "ingredient category");
            AddDuplicateIdMessages(messages, tags, tag => tag != null ? tag.TagId : string.Empty, "tag");
            AddDuplicateIdMessages(messages, preparationMethods, method => method != null ? method.MethodId : string.Empty, "preparation method");
            AddDuplicateIdMessages(messages, ingredients, ingredient => ingredient != null ? ingredient.IngredientId : string.Empty, "ingredient");
            AddDuplicateIdMessages(messages, recipes, recipe => recipe != null ? recipe.RecipeId : string.Empty, "recipe");
            return messages;
        }

        private static void AddDuplicateIdMessages<T>(
            ICollection<string> messages,
            IReadOnlyList<T> values,
            System.Func<T, string> getId,
            string label)
        {
            HashSet<string> seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            HashSet<string> duplicates = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < values.Count; i++)
            {
                string id = getId(values[i]);
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (seen.Add(id) == false)
                    duplicates.Add(id);
            }

            foreach (string duplicate in duplicates)
                messages.Add($"Duplicate {label} id: {duplicate}");
        }
    }
}
