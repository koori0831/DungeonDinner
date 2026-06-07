using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class DishResultBuilder : IDishResultBuilder
    {
        private readonly IRecipeMatcher _recipeMatcher;
        private readonly IDisgustingRuleEvaluator _disgustingRuleEvaluator;
        private readonly IDishNameBuilder _dishNameBuilder;

        public DishResultBuilder(
            IRecipeMatcher recipeMatcher,
            IDisgustingRuleEvaluator disgustingRuleEvaluator = null,
            IDishNameBuilder dishNameBuilder = null)
        {
            _recipeMatcher = recipeMatcher;
            _disgustingRuleEvaluator = disgustingRuleEvaluator ?? new DisgustingRuleEvaluator();
            _dishNameBuilder = dishNameBuilder ?? new DishNameBuilder();
        }

        public DishResult Build(CookingSession session)
        {
            RecipeMatchResult recipeMatch = _recipeMatcher != null
                ? _recipeMatcher.Match(session)
                : new RecipeMatchResult(session?.SelectedRecipe, "No recipe matcher was provided.");

            DisgustingEvaluation disgusting = _disgustingRuleEvaluator.Evaluate(session, recipeMatch);
            RecipeSO recipe = recipeMatch.Recipe;
            List<PreparedIngredientState> preparedIngredients = CopyPreparedIngredients(session);

            if (disgusting.IsDisgusting)
            {
                return new DishResult(
                    "괴식",
                    recipe,
                    recipe != null ? recipe.Category : null,
                    BuildTags(recipe, session),
                    DishQuality.Disgusting,
                    true,
                    recipeMatch.IsMatched,
                    preparedIngredients,
                    disgusting.Reasons);
            }

            List<FoodTagSO> tags = BuildTags(recipe, session);
            DishQuality quality = DetermineQuality(recipe, session);
            string displayName = _dishNameBuilder.BuildName(recipe, quality, preparedIngredients, false);

            return new DishResult(
                displayName,
                recipe,
                recipe != null ? recipe.Category : null,
                tags,
                quality,
                false,
                recipeMatch.IsMatched,
                preparedIngredients,
                new List<string> { recipeMatch.Reason });
        }

        private static List<PreparedIngredientState> CopyPreparedIngredients(CookingSession session)
        {
            List<PreparedIngredientState> preparedIngredients = new List<PreparedIngredientState>();
            if (session == null)
                return preparedIngredients;

            for (int i = 0; i < session.PreparedIngredients.Count; i++)
                preparedIngredients.Add(session.PreparedIngredients[i]);

            return preparedIngredients;
        }

        private static List<FoodTagSO> BuildTags(RecipeSO recipe, CookingSession session)
        {
            List<FoodTagSO> tags = new List<FoodTagSO>();

            if (recipe != null)
                AddTags(tags, recipe.BaseTags);

            if (session != null)
            {
                for (int i = 0; i < session.SelectedIngredients.Count; i++)
                {
                    IngredientSO ingredient = session.SelectedIngredients[i];
                    if (ingredient != null)
                        AddTags(tags, ingredient.BaseTags);
                }

                for (int i = 0; i < session.PreparedIngredients.Count; i++)
                {
                    PreparedIngredientState prepared = session.PreparedIngredients[i];
                    if (prepared == null)
                        continue;

                    AddTags(tags, prepared.AddTags);
                    RemoveTags(tags, prepared.RemoveTags);
                }
            }

            return tags;
        }

        private static DishQuality DetermineQuality(RecipeSO recipe, CookingSession session)
        {
            if (recipe == null || session == null)
                return DishQuality.Disgusting;

            bool hasNegativeQuality = false;
            bool hasAlteration = false;
            for (int i = 0; i < session.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = session.PreparedIngredients[i];
                if (prepared == null)
                    continue;

                if (prepared.QualityDelta < 0)
                    hasNegativeQuality = true;

                if (prepared.QualityDelta != 0 || string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                    hasAlteration = true;

                if (prepared.AddTags.Count > 0 || prepared.RemoveTags.Count > 0)
                    hasAlteration = true;
            }

            if (recipe.HasPerfectPreparationRules && ArePerfectRulesSatisfied(recipe, session) && hasNegativeQuality == false)
                return DishQuality.Perfect;

            return hasAlteration ? DishQuality.Altered : DishQuality.Normal;
        }

        private static bool ArePerfectRulesSatisfied(RecipeSO recipe, CookingSession session)
        {
            for (int ruleIndex = 0; ruleIndex < recipe.PerfectPreparationRules.Count; ruleIndex++)
            {
                RecipePreparationRule rule = recipe.PerfectPreparationRules[ruleIndex];
                if (rule == null || rule.Ingredient == null || rule.PerfectMethod == null)
                    return false;

                bool matched = false;
                for (int preparedIndex = 0; preparedIndex < session.PreparedIngredients.Count; preparedIndex++)
                {
                    PreparedIngredientState prepared = session.PreparedIngredients[preparedIndex];
                    if (prepared != null && rule.IsSatisfiedBy(prepared.Ingredient, prepared.Method))
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched == false)
                    return false;
            }

            return true;
        }

        private static void AddTags(ICollection<FoodTagSO> target, IReadOnlyList<FoodTagSO> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                FoodTagSO tag = source[i];
                if (tag != null && target.Contains(tag) == false)
                    target.Add(tag);
            }
        }

        private static void RemoveTags(ICollection<FoodTagSO> target, IReadOnlyList<FoodTagSO> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                FoodTagSO tag = source[i];
                if (tag != null)
                    target.Remove(tag);
            }
        }
    }
}
