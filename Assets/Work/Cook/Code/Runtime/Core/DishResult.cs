using System.Collections.Generic;
using System.Text;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class DishResult
    {
        public string DisplayName { get; }
        public RecipeSO BaseRecipe { get; }
        public FoodCategorySO Category { get; }
        public IReadOnlyList<FoodTagSO> Tags { get; }
        public DishFormationStatus FormationStatus { get; }
        public DishVariantStatus VariantStatus { get; }
        public DishOddity Oddity { get; }
        public DishSafety Safety { get; }
        public DishCraftGrade CraftGrade { get; }
        public int QualityScore { get; }
        public string CookingSessionId { get; }
        public RecipeSO TargetRecipe { get; }
        public bool IsTargetRecipeMatched { get; }
        public CookingVariantIdentity VariantIdentity { get; }
        public string VariantId => VariantIdentity?.VariantId ?? string.Empty;
        public string VariantKey => VariantId;
        public IReadOnlyList<PreparedIngredientState> PreparedIngredients { get; }
        public IReadOnlyList<string> Reasons { get; }

        public string RecipeId => BaseRecipe != null ? BaseRecipe.RecipeId : string.Empty;
        public string CategoryId => Category != null ? Category.CategoryId : string.Empty;
        public bool IsRecipeMatched => FormationStatus == DishFormationStatus.Formed && BaseRecipe != null;
        public bool IsVariant => IsRecipeMatched && VariantStatus == DishVariantStatus.Variant;
        public bool IsBizarre => Oddity == DishOddity.Bizarre;
        public bool IsDangerous => Safety == DishSafety.Dangerous;

        public DishResult(
            string displayName,
            RecipeSO baseRecipe,
            FoodCategorySO category,
            IReadOnlyList<FoodTagSO> tags,
            DishFormationStatus formationStatus,
            DishVariantStatus variantStatus,
            DishOddity oddity,
            DishSafety safety,
            DishCraftGrade craftGrade,
            int qualityScore,
            string cookingSessionId,
            RecipeSO targetRecipe,
            bool isTargetRecipeMatched,
            CookingVariantIdentity variantIdentity,
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            IReadOnlyList<string> reasons)
        {
            DisplayName = displayName;
            BaseRecipe = baseRecipe;
            Category = category;
            Tags = tags ?? new List<FoodTagSO>();
            FormationStatus = formationStatus;
            VariantStatus = variantStatus;
            Oddity = oddity;
            Safety = safety;
            CraftGrade = craftGrade;
            QualityScore = qualityScore;
            CookingSessionId = cookingSessionId ?? string.Empty;
            TargetRecipe = targetRecipe;
            IsTargetRecipeMatched = isTargetRecipeMatched;
            VariantIdentity = variantIdentity ?? CookingVariantIdentity.Base;
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
                   $"formation={FormationStatus}, variant={VariantStatus}, oddity={Oddity}, safety={Safety}, " +
                   $"craft={CraftGrade}, qualityScore={QualityScore}, targetMatched={IsTargetRecipeMatched}, tags={BuildTagText()}";
        }

    }
}
