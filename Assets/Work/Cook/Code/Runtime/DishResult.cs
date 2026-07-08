using System.Collections.Generic;
using System.Text;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class DishResult
    {
        public string DisplayName { get; }
        public RecipeSO BaseRecipe { get; }
        public FoodCategorySO Category { get; }
        public IReadOnlyList<FoodTagSO> Tags { get; }
        public DishQuality Quality { get; }
        public int QualityScore { get; }
        public bool IsDisgusting { get; }
        public bool IsRecipeMatched { get; }
        public IReadOnlyList<PreparedIngredientState> PreparedIngredients { get; }
        public IReadOnlyList<string> Reasons { get; }

        public string RecipeId => BaseRecipe != null ? BaseRecipe.RecipeId : string.Empty;
        public string CategoryId => Category != null ? Category.CategoryId : string.Empty;

        public DishResult(
            string displayName,
            RecipeSO baseRecipe,
            FoodCategorySO category,
            IReadOnlyList<FoodTagSO> tags,
            DishQuality quality,
            int qualityScore,
            bool isDisgusting,
            bool isRecipeMatched,
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            IReadOnlyList<string> reasons)
        {
            DisplayName = displayName;
            BaseRecipe = baseRecipe;
            Category = category;
            Tags = tags ?? new List<FoodTagSO>();
            Quality = quality;
            QualityScore = qualityScore;
            IsDisgusting = isDisgusting;
            IsRecipeMatched = isRecipeMatched;
            PreparedIngredients = preparedIngredients ?? new List<PreparedIngredientState>();
            Reasons = reasons ?? new List<string>();
        }

        public string BuildTagText(char separator = '|')
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Tags.Count; i++)
            {
                FoodTagSO tag = Tags[i];
                if (tag == null || string.IsNullOrWhiteSpace(tag.TagId))
                    continue;

                if (builder.Length > 0)
                    builder.Append(separator);

                builder.Append(tag.TagId);
            }

            return builder.ToString();
        }

        public string BuildDebugSummary()
        {
            return $"DishResult name={DisplayName}, recipe={RecipeId}, category={CategoryId}, " +
                   $"quality={Quality}, qualityScore={QualityScore}, disgusting={IsDisgusting}, tags={BuildTagText()}";
        }
    }
}
