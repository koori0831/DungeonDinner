using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
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
            int qualityScore = CalculateQualityScore(session);
            List<FoodTagSO> tags = BuildTags(recipe, session);
            DishFormationStatus formationStatus = recipe != null
                ? DishFormationStatus.Formed
                : DishFormationStatus.Unformed;
            DishVariantStatus variantStatus = recipeMatch.IsVariant
                ? DishVariantStatus.Variant
                : DishVariantStatus.Base;
            DishOddity oddity = disgusting.IsBizarre ? DishOddity.Bizarre : DishOddity.Normal;
            DishSafety safety = HasPoison(session) ? DishSafety.Dangerous : DishSafety.Safe;
            DishCraftGrade craftGrade = DetermineCraftGrade(session, qualityScore);
            string displayName = _dishNameBuilder.BuildName(
                recipe,
                craftGrade,
                preparedIngredients,
                oddity == DishOddity.Bizarre,
                formationStatus == DishFormationStatus.Formed);

            List<string> reasons = new List<string>();
            if (recipeMatch.IsMatched == false && string.IsNullOrWhiteSpace(recipeMatch.Reason) == false)
                reasons.Add(recipeMatch.Reason);
            AddReasons(reasons, disgusting.Reasons);
            AddSafetyReasons(reasons, session);

            return new DishResult(
                displayName,
                recipe,
                recipe != null ? recipe.Category : null,
                tags,
                formationStatus,
                variantStatus,
                oddity,
                safety,
                craftGrade,
                qualityScore,
                session?.SessionId,
                recipeMatch.TargetRecipe,
                recipeMatch.IsTargetRecipeMatched,
                recipeMatch.VariantIdentity,
                preparedIngredients,
                reasons);
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

                    AddTags(tags, prepared.AddedTags);
                    RemoveTags(tags, prepared.RemoveTags);
                }
            }

            return tags;
        }

        private static int CalculateQualityScore(CookingSession session)
        {
            if (session == null)
                return 0;

            int qualityScore = 0;
            for (int i = 0; i < session.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = session.PreparedIngredients[i];
                if (prepared != null)
                    qualityScore += prepared.QualityDelta;
            }

            return qualityScore;
        }

        private static DishCraftGrade DetermineCraftGrade(CookingSession session, int qualityScore)
        {
            if (session == null)
                return DishCraftGrade.Bad;

            if (qualityScore >= 2)
                return DishCraftGrade.Perfect;
            if (qualityScore >= 1)
                return DishCraftGrade.Good;
            if (qualityScore < 0)
                return DishCraftGrade.Bad;

            return DishCraftGrade.Normal;
        }

        private static bool HasPoison(CookingSession session)
        {
            if (session == null)
                return false;

            for (int i = 0; i < session.PreparedIngredients.Count; i++)
            {
                if (session.PreparedIngredients[i]?.AddsPoison == true)
                    return true;
            }

            return false;
        }

        private static void AddSafetyReasons(ICollection<string> reasons, CookingSession session)
        {
            if (reasons == null || session == null)
                return;

            for (int i = 0; i < session.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = session.PreparedIngredients[i];
                if (prepared?.AddsPoison == true)
                    reasons.Add($"{GetIngredientName(prepared)} preparation added poison.");
            }
        }

        private static string GetIngredientName(PreparedIngredientState prepared)
        {
            return prepared?.Ingredient != null ? prepared.Ingredient.DisplayName : "Unknown ingredient";
        }

        private static void AddReasons(ICollection<string> target, IReadOnlyList<string> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i]) == false && target.Contains(source[i]) == false)
                    target.Add(source[i]);
            }
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
