using System.Collections.Generic;
using System.Text;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class DishNameBuilder : IDishNameBuilder
    {
        public string BuildName(
            RecipeSO recipe,
            DishQuality quality,
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            bool isDisgusting)
        {
            if (isDisgusting)
                return "괴식";

            string baseName = recipe != null ? recipe.DisplayName : "알 수 없는 음식";
            string modifierText = BuildModifierText(recipe, preparedIngredients);
            string modifiedName = string.IsNullOrWhiteSpace(modifierText)
                ? baseName
                : $"{modifierText} {baseName}";

            return quality == DishQuality.Perfect ? $"완벽한 {modifiedName}" : modifiedName;
        }

        private static string BuildModifierText(
            RecipeSO recipe,
            IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            if (preparedIngredients == null)
                return string.Empty;

            HashSet<string> seen = new HashSet<string>();
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < preparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = preparedIngredients[i];
                if (prepared == null)
                    continue;

                AppendModifier(builder, seen, FindAlternativeModifier(recipe, prepared.Ingredient));
                if (CanUsePreparationModifier(recipe, prepared.Ingredient))
                    AppendModifier(builder, seen, prepared.ResultNameModifier);
            }

            return builder.ToString();
        }

        private static bool CanUsePreparationModifier(RecipeSO recipe, IngredientSO ingredient)
        {
            if (recipe == null || ingredient == null)
                return true;

            bool matchedAnyRequirement = false;
            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null || requirement.IsMatchedBy(ingredient) == false)
                    continue;

                matchedAnyRequirement = true;
                if (requirement.UsePreparationResultNameModifier)
                    return true;
            }

            return matchedAnyRequirement == false;
        }

        private static string FindAlternativeModifier(RecipeSO recipe, IngredientSO ingredient)
        {
            if (recipe == null || ingredient == null)
                return string.Empty;

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null)
                    continue;

                string modifier = requirement.GetAlternativeResultNameModifier(ingredient);
                if (string.IsNullOrWhiteSpace(modifier) == false)
                    return modifier;
            }

            return string.Empty;
        }

        private static void AppendModifier(StringBuilder builder, ISet<string> seen, string modifier)
        {
            if (string.IsNullOrWhiteSpace(modifier) || seen.Add(modifier) == false)
                return;

            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(modifier);
        }
    }
}
