using System.Collections.Generic;
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
            {
                return new RecipeMatchResult(
                    RecipeMatchStatus.NoMatch,
                    null,
                    null,
                    null,
                    CookingVariantIdentity.Base,
                    "Cooking session is missing.");
            }

            RecipeSO targetRecipe = session.SelectedRecipe;
            IReadOnlyList<RecipeSO> recipes = _dataProvider?.GetRecipes();
            if (recipes == null || recipes.Count == 0)
            {
                return new RecipeMatchResult(
                    RecipeMatchStatus.NoMatch,
                    null,
                    targetRecipe,
                    null,
                    CookingVariantIdentity.Base,
                    "Recipe catalog is missing or empty.");
            }

            List<CandidateMatch> topMatches = new List<CandidateMatch>();
            int bestScore = int.MinValue;
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeSO recipe = recipes[i];
                if (recipe == null)
                    continue;

                RecipePreparedMatchResult preparedMatch =
                    recipe.MatchPreparedIngredients(session.PreparedIngredients);
                if (preparedMatch.Status == RecipeMatchStatus.NoMatch)
                    continue;

                int score = recipe.CalculateMatchSpecificityScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    topMatches.Clear();
                    topMatches.Add(new CandidateMatch(recipe, preparedMatch));
                }
                else if (score == bestScore)
                {
                    topMatches.Add(new CandidateMatch(recipe, preparedMatch));
                }
            }

            if (topMatches.Count == 0)
            {
                return new RecipeMatchResult(
                    RecipeMatchStatus.NoMatch,
                    null,
                    targetRecipe,
                    null,
                    CookingVariantIdentity.Base,
                    "Prepared ingredients did not match any authored recipe.");
            }

            if (topMatches.Count > 1)
            {
                return new RecipeMatchResult(
                    RecipeMatchStatus.Ambiguous,
                    null,
                    targetRecipe,
                    null,
                    CookingVariantIdentity.Base,
                    "Prepared ingredients tie between multiple authored recipes.");
            }

            CandidateMatch winner = topMatches[0];
            if (winner.PreparedMatch.Status == RecipeMatchStatus.Ambiguous)
            {
                return new RecipeMatchResult(
                    RecipeMatchStatus.Ambiguous,
                    null,
                    targetRecipe,
                    winner.PreparedMatch.Bindings,
                    CookingVariantIdentity.Base,
                    winner.PreparedMatch.Reason);
            }

            CookingVariantIdentity variantIdentity = CookingVariantIdentityBuilder.Build(
                winner.Recipe,
                winner.PreparedMatch.Bindings);
            string reason = winner.Recipe == targetRecipe
                ? "Prepared ingredients matched the selected target recipe."
                : "Prepared ingredients matched a different authored recipe; the selected recipe was only a target.";
            return new RecipeMatchResult(
                RecipeMatchStatus.Matched,
                winner.Recipe,
                targetRecipe,
                winner.PreparedMatch.Bindings,
                variantIdentity,
                reason);
        }

        private sealed class CandidateMatch
        {
            public RecipeSO Recipe { get; }
            public RecipePreparedMatchResult PreparedMatch { get; }

            public CandidateMatch(RecipeSO recipe, RecipePreparedMatchResult preparedMatch)
            {
                Recipe = recipe;
                PreparedMatch = preparedMatch;
            }
        }
    }
}
