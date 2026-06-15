using System.Collections.Generic;

namespace Work.Cook.Code.Runtime
{
    public sealed class DisgustingRuleEvaluator : IDisgustingRuleEvaluator
    {
        public DisgustingEvaluation Evaluate(CookingSession session, RecipeMatchResult recipeMatch)
        {
            List<string> reasons = new List<string>();

            if (session == null)
            {
                reasons.Add("Cooking session is missing.");
                return new DisgustingEvaluation(true, reasons);
            }

            if (session.Mode == CookingMode.DirectIngredients && (recipeMatch == null || recipeMatch.IsMatched == false))
                reasons.Add("Selected ingredients do not form a known recipe.");

            if (session.SelectedIngredients.Count == 0)
                reasons.Add("No ingredients were selected.");

            for (int i = 0; i < session.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = session.PreparedIngredients[i];
                if (prepared == null)
                    continue;

                if (prepared.CausesDisgusting)
                    reasons.Add($"{GetIngredientName(prepared)} preparation causes disgusting result.");

                if (prepared.AddsPoison)
                    reasons.Add($"{GetIngredientName(prepared)} preparation added poison.");
            }

            return new DisgustingEvaluation(reasons.Count > 0, reasons);
        }

        private static string GetIngredientName(PreparedIngredientState prepared)
        {
            return prepared.Ingredient != null ? prepared.Ingredient.DisplayName : "Unknown ingredient";
        }
    }
}
