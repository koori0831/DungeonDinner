using System;
using System.Collections.Generic;
using System.Text;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public enum VariantComponentKind
    {
        Canonical = 0,
        Alternative = 1,
        Optional = 2
    }

    public sealed class CookingVariantComponent
    {
        public string RequirementId { get; }
        public int RequirementIndex { get; }
        public int PreparedIngredientIndex { get; }
        public IngredientSO Ingredient { get; }
        public IngredientPreparationOption PreparationOption { get; }
        public string VariantEffectId { get; }
        public VariantComponentKind Kind { get; }

        public CookingVariantComponent(
            string requirementId,
            int requirementIndex,
            int preparedIngredientIndex,
            IngredientSO ingredient,
            IngredientPreparationOption preparationOption,
            string variantEffectId,
            VariantComponentKind kind)
        {
            RequirementId = requirementId ?? string.Empty;
            RequirementIndex = requirementIndex;
            PreparedIngredientIndex = preparedIngredientIndex;
            Ingredient = ingredient;
            PreparationOption = preparationOption;
            VariantEffectId = variantEffectId ?? string.Empty;
            Kind = kind;
        }
    }

    public sealed class CookingVariantIdentity
    {
        private static readonly IReadOnlyList<CookingVariantComponent> EMPTY_COMPONENTS =
            new List<CookingVariantComponent>();

        public static CookingVariantIdentity Base { get; } =
            new CookingVariantIdentity(string.Empty, EMPTY_COMPONENTS, EMPTY_COMPONENTS, false);

        public static CookingVariantIdentity LegacyVariant { get; } =
            new CookingVariantIdentity("legacy-variant", EMPTY_COMPONENTS, EMPTY_COMPONENTS, true);

        public string VariantId { get; }
        public IReadOnlyList<CookingVariantComponent> IdentityComponents { get; }
        public IReadOnlyList<CookingVariantComponent> ReplayComponents { get; }
        public bool IsVariant { get; }

        public CookingVariantIdentity(
            string variantId,
            IReadOnlyList<CookingVariantComponent> identityComponents,
            IReadOnlyList<CookingVariantComponent> replayComponents,
            bool isVariant)
        {
            VariantId = variantId ?? string.Empty;
            IdentityComponents = identityComponents ?? EMPTY_COMPONENTS;
            ReplayComponents = replayComponents ?? EMPTY_COMPONENTS;
            IsVariant = isVariant;
        }
    }

    public static class CookingVariantIdentityBuilder
    {
        public static CookingVariantIdentity Build(
            RecipeSO recipe,
            IReadOnlyList<RecipeIngredientMatchBinding> bindings)
        {
            if (recipe == null || bindings == null)
                return CookingVariantIdentity.Base;

            List<CookingVariantComponent> identityComponents = new List<CookingVariantComponent>();
            List<CookingVariantComponent> replayComponents = new List<CookingVariantComponent>();
            List<string> identityKeys = new List<string>();

            for (int i = 0; i < bindings.Count; i++)
            {
                RecipeIngredientMatchBinding binding = bindings[i];
                PreparedIngredientState prepared = binding?.PreparedIngredient;
                if (prepared?.Ingredient == null)
                    continue;

                VariantComponentKind kind = ConvertKind(binding.Kind);
                string effectId = ResolveVariantEffectId(prepared.MiniGameFeedbackRule);
                CookingVariantComponent replayComponent = new CookingVariantComponent(
                    binding.RequirementId,
                    binding.RequirementIndex,
                    binding.PreparedIngredientIndex,
                    prepared.Ingredient,
                    prepared.PreparationOption,
                    effectId,
                    kind);
                replayComponents.Add(replayComponent);

                bool definesVariant = binding.Kind == RecipeIngredientMatchKind.Alternative
                                      || binding.Kind == RecipeIngredientMatchKind.Optional
                                      || prepared.PreparationOption?.HasIdentityEffect == true
                                      || prepared.MiniGameFeedbackRule?.HasIdentityEffect == true;
                if (definesVariant == false)
                    continue;

                // Replay keeps the exact preparation used, while identity keeps
                // only authored changes that alter what the variant is. A
                // quality-only preparation therefore updates the same variant.
                CookingVariantComponent identityComponent = new CookingVariantComponent(
                    binding.RequirementId,
                    binding.RequirementIndex,
                    binding.PreparedIngredientIndex,
                    prepared.Ingredient,
                    prepared.PreparationOption?.HasIdentityEffect == true
                        ? prepared.PreparationOption
                        : null,
                    effectId,
                    kind);
                identityComponents.Add(identityComponent);
                identityKeys.Add(BuildComponentKey(identityComponent));
            }

            SortComponents(replayComponents);
            SortComponents(identityComponents);
            identityKeys.Sort(StringComparer.OrdinalIgnoreCase);

            if (identityKeys.Count == 0)
                return new CookingVariantIdentity(string.Empty, identityComponents, replayComponents, false);

            StringBuilder source = new StringBuilder(NormalizeId(recipe.RecipeId));
            for (int i = 0; i < identityKeys.Count; i++)
                source.Append('|').Append(identityKeys[i]);

            return new CookingVariantIdentity(
                "variant_" + ComputeStableHash(source.ToString()),
                identityComponents,
                replayComponents,
                true);
        }

        private static string BuildComponentKey(CookingVariantComponent component)
        {
            string ingredientId = component.Ingredient != null
                ? NormalizeId(component.Ingredient.IngredientId)
                : "none";
            string optionId = component.PreparationOption != null
                ? NormalizeId(component.PreparationOption.PreparationOptionId)
                : "none";
            return NormalizeId(component.RequirementId)
                   + ":" + (int)component.Kind
                   + ":" + ingredientId
                   + ":" + optionId
                   + ":" + NormalizeId(component.VariantEffectId);
        }

        private static string ResolveVariantEffectId(CookingMiniGameFeedbackRule rule)
        {
            return rule != null && rule.HasIdentityEffect ? rule.VariantEffectId : string.Empty;
        }

        private static VariantComponentKind ConvertKind(RecipeIngredientMatchKind kind)
        {
            switch (kind)
            {
                case RecipeIngredientMatchKind.Alternative:
                    return VariantComponentKind.Alternative;
                case RecipeIngredientMatchKind.Optional:
                    return VariantComponentKind.Optional;
                default:
                    return VariantComponentKind.Canonical;
            }
        }

        private static void SortComponents(List<CookingVariantComponent> components)
        {
            components.Sort((left, right) =>
            {
                int requirement = left.RequirementIndex.CompareTo(right.RequirementIndex);
                if (requirement != 0)
                    return requirement;

                int ingredient = string.Compare(
                    left.Ingredient?.IngredientId,
                    right.Ingredient?.IngredientId,
                    StringComparison.OrdinalIgnoreCase);
                if (ingredient != 0)
                    return ingredient;

                int option = string.Compare(
                    left.PreparationOption?.PreparationOptionId,
                    right.PreparationOption?.PreparationOptionId,
                    StringComparison.OrdinalIgnoreCase);
                if (option != 0)
                    return option;

                return left.PreparedIngredientIndex.CompareTo(right.PreparedIngredientIndex);
            });
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "none" : value.Trim().ToLowerInvariant();
        }

        private static string ComputeStableHash(string value)
        {
            unchecked
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;
                ulong hash = offset;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }

                return hash.ToString("x16");
            }
        }
    }
}
