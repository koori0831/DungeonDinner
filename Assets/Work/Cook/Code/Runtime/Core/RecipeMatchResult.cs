using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public enum RecipeMatchStatus
    {
        NoMatch = 0,
        Matched = 1,
        Ambiguous = 2
    }

    public enum RecipeIngredientMatchKind
    {
        Canonical = 0,
        Alternative = 1,
        Optional = 2
    }

    public sealed class RecipeIngredientMatchBinding
    {
        public string RequirementId { get; }
        public int RequirementIndex { get; }
        public int PreparedIngredientIndex { get; }
        public RecipeIngredientRequirement Requirement { get; }
        public PreparedIngredientState PreparedIngredient { get; }
        public RecipeIngredientMatchKind Kind { get; }

        public RecipeIngredientMatchBinding(
            string requirementId,
            int requirementIndex,
            int preparedIngredientIndex,
            RecipeIngredientRequirement requirement,
            PreparedIngredientState preparedIngredient,
            RecipeIngredientMatchKind kind)
        {
            RequirementId = requirementId ?? string.Empty;
            RequirementIndex = requirementIndex;
            PreparedIngredientIndex = preparedIngredientIndex;
            Requirement = requirement;
            PreparedIngredient = preparedIngredient;
            Kind = kind;
        }
    }

    public sealed class RecipePreparedMatchResult
    {
        private static readonly IReadOnlyList<RecipeIngredientMatchBinding> EMPTY_BINDINGS =
            new List<RecipeIngredientMatchBinding>();

        public RecipeMatchStatus Status { get; }
        public IReadOnlyList<RecipeIngredientMatchBinding> Bindings { get; }
        public string Reason { get; }

        public RecipePreparedMatchResult(
            RecipeMatchStatus status,
            IReadOnlyList<RecipeIngredientMatchBinding> bindings,
            string reason)
        {
            Status = status;
            Bindings = bindings ?? EMPTY_BINDINGS;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class RecipeMatchResult
    {
        private static readonly IReadOnlyList<RecipeIngredientMatchBinding> EMPTY_BINDINGS =
            new List<RecipeIngredientMatchBinding>();

        public RecipeMatchStatus Status { get; }
        public RecipeSO Recipe { get; }
        public RecipeSO TargetRecipe { get; }
        public IReadOnlyList<RecipeIngredientMatchBinding> Bindings { get; }
        public CookingVariantIdentity VariantIdentity { get; }
        public bool IsMatched => Status == RecipeMatchStatus.Matched && Recipe != null;
        public bool IsTargetRecipeMatched => IsMatched && Recipe == TargetRecipe;
        public bool IsVariant => IsMatched && VariantIdentity != null && VariantIdentity.IsVariant;
        public string Reason { get; }

        public RecipeMatchResult(
            RecipeSO recipe,
            string reason,
            RecipeSO targetRecipe = null,
            bool isVariant = false)
            : this(
                recipe != null ? RecipeMatchStatus.Matched : RecipeMatchStatus.NoMatch,
                recipe,
                targetRecipe,
                EMPTY_BINDINGS,
                isVariant ? CookingVariantIdentity.LegacyVariant : CookingVariantIdentity.Base,
                reason)
        {
        }

        public RecipeMatchResult(
            RecipeMatchStatus status,
            RecipeSO recipe,
            RecipeSO targetRecipe,
            IReadOnlyList<RecipeIngredientMatchBinding> bindings,
            CookingVariantIdentity variantIdentity,
            string reason)
        {
            Status = status;
            Recipe = status == RecipeMatchStatus.Matched ? recipe : null;
            TargetRecipe = targetRecipe;
            Bindings = bindings ?? EMPTY_BINDINGS;
            VariantIdentity = variantIdentity ?? CookingVariantIdentity.Base;
            Reason = reason ?? string.Empty;
        }
    }
}
