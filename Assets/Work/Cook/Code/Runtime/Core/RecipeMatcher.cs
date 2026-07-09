using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class RecipeMatcher : IRecipeMatcher
    {
        private readonly ICookingDataProvider _dataProvider;

        public RecipeMatcher(ICookingDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public RecipeMatchResult Match(CookingSession session)
        {
            if (session == null)
                return new RecipeMatchResult(null, "Cooking session is missing.");

            if (session.SelectedRecipe != null)
                return new RecipeMatchResult(session.SelectedRecipe, "Selected recipe.");

            RecipeSO recipe = _dataProvider?.FindRecipeByPreparedIngredients(session.PreparedIngredients);
            if (recipe != null)
                return new RecipeMatchResult(recipe, "Prepared ingredients matched a known recipe.");

            recipe = _dataProvider?.FindRecipeByIngredients(session.SelectedIngredients);
            if (recipe != null)
                return new RecipeMatchResult(recipe, "Selected ingredients matched a known recipe without preparation requirements.");

            return new RecipeMatchResult(null, "Direct ingredients did not match any known recipe.");
        }
    }
}
