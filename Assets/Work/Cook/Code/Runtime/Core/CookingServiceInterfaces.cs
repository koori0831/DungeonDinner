using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public interface ICookingDataProvider
    {
        IReadOnlyList<RecipeSO> GetRecipes();
        IReadOnlyList<IngredientSO> GetIngredients();
        IReadOnlyList<IngredientPreparationOption> GetPreparationOptions(IngredientSO ingredient);
        RecipeSO FindRecipeByIngredients(IReadOnlyList<IngredientSO> ingredients);
        RecipeSO FindRecipeByPreparedIngredients(IReadOnlyList<PreparedIngredientState> preparedIngredients);
    }

    public interface IRecipeMatcher
    {
        RecipeMatchResult Match(CookingSession session);
    }

    public interface IDisgustingRuleEvaluator
    {
        DisgustingEvaluation Evaluate(CookingSession session, RecipeMatchResult recipeMatch);
    }

    public interface IDishNameBuilder
    {
        string BuildName(
            RecipeSO recipe,
            DishCraftGrade craftGrade,
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            bool isBizarre,
            bool isFormed);
    }

    public interface IDishResultBuilder
    {
        DishResult Build(CookingSession session);
    }
}
