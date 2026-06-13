using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingDataProvider : ICookingDataProvider
    {
        private static readonly IReadOnlyList<RecipeSO> EmptyRecipes = new List<RecipeSO>();
        private static readonly IReadOnlyList<IngredientSO> EmptyIngredients = new List<IngredientSO>();
        private static readonly IReadOnlyList<IngredientPreparationOption> EmptyPreparationOptions =
            new List<IngredientPreparationOption>();

        private readonly CookingDataCatalogSO _catalog;

        public CookingDataProvider(CookingDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public IReadOnlyList<RecipeSO> GetRecipes()
        {
            return _catalog != null ? _catalog.Recipes : EmptyRecipes;
        }

        public IReadOnlyList<IngredientSO> GetIngredients()
        {
            return _catalog != null ? _catalog.Ingredients : EmptyIngredients;
        }

        public IReadOnlyList<IngredientPreparationOption> GetPreparationOptions(IngredientSO ingredient)
        {
            return ingredient != null ? ingredient.PreparationOptions : EmptyPreparationOptions;
        }

        public RecipeSO FindRecipeByIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            return _catalog != null ? _catalog.FindRecipeByIngredients(ingredients) : null;
        }

        public RecipeSO FindRecipeByPreparedIngredients(IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            return _catalog != null ? _catalog.FindRecipeByPreparedIngredients(preparedIngredients) : null;
        }
    }
}
