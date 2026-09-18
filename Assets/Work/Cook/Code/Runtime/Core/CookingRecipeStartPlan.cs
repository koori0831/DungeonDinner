using System;
using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public enum CookingPreparationRecommendationKind
    {
        None = 0,
        ReplayedVariant = 1,
        KnownPerfect = 2,
        SoleRecipeAllowed = 3
    }

    public sealed class RecipeRequirementPlan
    {
        public RecipeIngredientRequirement Requirement { get; }
        public IReadOnlyList<IngredientSO> Candidates { get; }
        public int RequiredCount => Requirement != null && Requirement.RecipeDefining ? Requirement.MinCount : 0;

        public RecipeRequirementPlan(
            RecipeIngredientRequirement requirement,
            IReadOnlyList<IngredientSO> candidates)
        {
            Requirement = requirement;
            Candidates = candidates ?? Array.Empty<IngredientSO>();
        }
    }

    public sealed class PlannedIngredientOccurrence
    {
        public string RequirementId { get; }
        public IngredientSO Ingredient { get; }
        public int OccurrenceIndex { get; }

        public PlannedIngredientOccurrence(string requirementId, IngredientSO ingredient, int occurrenceIndex)
        {
            RequirementId = requirementId ?? string.Empty;
            Ingredient = ingredient;
            OccurrenceIndex = Math.Max(0, occurrenceIndex);
        }
    }

    public sealed class PlannedPreparation
    {
        public string RequirementId { get; }
        public IngredientSO Ingredient { get; }
        public int OccurrenceIndex { get; }
        public IngredientPreparationOption PreparationOption { get; }
        public CookingPreparationRecommendationKind Kind { get; }

        public PlannedPreparation(
            string requirementId,
            IngredientSO ingredient,
            int occurrenceIndex,
            IngredientPreparationOption preparationOption,
            CookingPreparationRecommendationKind kind)
        {
            RequirementId = requirementId ?? string.Empty;
            Ingredient = ingredient;
            OccurrenceIndex = occurrenceIndex;
            PreparationOption = preparationOption;
            Kind = kind;
        }
    }

    public sealed class IngredientShortage
    {
        public string RequirementId { get; }
        public IngredientSO Ingredient { get; }
        public int RequiredQuantity { get; }
        public int OwnedQuantity { get; }
        public int MissingQuantity => Math.Max(0, RequiredQuantity - OwnedQuantity);

        public IngredientShortage(
            string requirementId,
            IngredientSO ingredient,
            int requiredQuantity,
            int ownedQuantity)
        {
            RequirementId = requirementId ?? string.Empty;
            Ingredient = ingredient;
            RequiredQuantity = Math.Max(0, requiredQuantity);
            OwnedQuantity = Math.Max(0, ownedQuantity);
        }
    }

    public sealed class CookingIngredientOccurrence
    {
        public int SelectedIndex { get; }
        public int IngredientOccurrenceIndex { get; }
        public IngredientSO Ingredient { get; }

        public CookingIngredientOccurrence(int selectedIndex, int ingredientOccurrenceIndex, IngredientSO ingredient)
        {
            SelectedIndex = selectedIndex;
            IngredientOccurrenceIndex = ingredientOccurrenceIndex;
            Ingredient = ingredient;
        }
    }

    public sealed class CookingRecipeStartPlan
    {
        public RecipeSO Recipe { get; }
        public string SourceVariantId { get; }
        public IReadOnlyList<RecipeRequirementPlan> Requirements { get; }
        public IReadOnlyList<IngredientSO> Candidates { get; }
        public IReadOnlyList<PlannedIngredientOccurrence> PresetIngredients { get; }
        public IReadOnlyList<PlannedPreparation> PreparationRecommendations { get; }
        public IReadOnlyList<IngredientShortage> Shortages { get; }
        public bool IsLegacyVariantReplayUnavailable { get; }

        public CookingRecipeStartPlan(
            RecipeSO recipe,
            string sourceVariantId,
            IReadOnlyList<RecipeRequirementPlan> requirements,
            IReadOnlyList<IngredientSO> candidates,
            IReadOnlyList<PlannedIngredientOccurrence> presetIngredients,
            IReadOnlyList<PlannedPreparation> preparationRecommendations,
            IReadOnlyList<IngredientShortage> shortages,
            bool isLegacyVariantReplayUnavailable)
        {
            Recipe = recipe;
            SourceVariantId = sourceVariantId ?? string.Empty;
            Requirements = requirements ?? Array.Empty<RecipeRequirementPlan>();
            Candidates = candidates ?? Array.Empty<IngredientSO>();
            PresetIngredients = presetIngredients ?? Array.Empty<PlannedIngredientOccurrence>();
            PreparationRecommendations = preparationRecommendations ?? Array.Empty<PlannedPreparation>();
            Shortages = shortages ?? Array.Empty<IngredientShortage>();
            IsLegacyVariantReplayUnavailable = isLegacyVariantReplayUnavailable;
        }

        public int GetRequiredQuantity(IngredientSO ingredient)
        {
            if (ingredient == null)
                return 0;

            if (string.IsNullOrWhiteSpace(SourceVariantId) == false)
            {
                int replayRequired = 0;
                for (int i = 0; i < PresetIngredients.Count; i++)
                {
                    if (PresetIngredients[i]?.Ingredient == ingredient)
                        replayRequired++;
                }
                for (int i = 0; i < Shortages.Count; i++)
                {
                    IngredientShortage shortage = Shortages[i];
                    if (shortage?.Ingredient == ingredient)
                        replayRequired += shortage.MissingQuantity;
                }
                return replayRequired;
            }

            int required = 0;
            for (int i = 0; i < Requirements.Count; i++)
            {
                RecipeRequirementPlan plan = Requirements[i];
                if (plan?.Requirement == null || plan.Requirement.RecipeDefining == false)
                    continue;
                if (plan.Requirement.IsMatchedBy(ingredient))
                    required += plan.Requirement.MinCount;
            }
            return required;
        }

        public bool IsSelectionValid(
            IReadOnlyList<IngredientSO> selectedIngredients,
            Func<IngredientSO, int> quantityProvider,
            out string reason)
        {
            if (Recipe == null || selectedIngredients == null || Recipe.MatchesIngredients(selectedIngredients) == false)
            {
                reason = "레시피 슬롯을 충족하도록 재료를 선택해야 합니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SourceVariantId) == false
                && SatisfiesReplayedRequirementCounts(selectedIngredients, out reason) == false)
            {
                return false;
            }

            Dictionary<IngredientSO, int> selectedCounts = new Dictionary<IngredientSO, int>();
            for (int i = 0; i < selectedIngredients.Count; i++)
            {
                IngredientSO ingredient = selectedIngredients[i];
                if (ingredient == null)
                    continue;
                selectedCounts.TryGetValue(ingredient, out int count);
                selectedCounts[ingredient] = count + 1;
            }

            if (quantityProvider != null)
            {
                foreach (KeyValuePair<IngredientSO, int> pair in selectedCounts)
                {
                    int owned = Math.Max(0, quantityProvider(pair.Key));
                    if (owned < pair.Value)
                    {
                        reason = $"{pair.Key.DisplayName}: 필요 {pair.Value}, 보유 {owned}";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        private bool SatisfiesReplayedRequirementCounts(
            IReadOnlyList<IngredientSO> selectedIngredients,
            out string reason)
        {
            for (int requirementIndex = 0; requirementIndex < Requirements.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = Requirements[requirementIndex]?.Requirement;
                if (requirement == null)
                    continue;
                int replayCount = GetReplayedRequirementCount(requirement.RequirementId);
                if (replayCount <= 0)
                    continue;

                int selectedCount = 0;
                for (int ingredientIndex = 0; ingredientIndex < selectedIngredients.Count; ingredientIndex++)
                {
                    if (requirement.IsMatchedBy(selectedIngredients[ingredientIndex]))
                        selectedCount++;
                }
                if (selectedCount >= replayCount)
                    continue;

                reason = $"변형 조합의 {BuildRequirementLabel(requirement)} 슬롯이 {replayCount - selectedCount}개 부족합니다.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private int GetReplayedRequirementCount(string requirementId)
        {
            int count = 0;
            for (int i = 0; i < PresetIngredients.Count; i++)
            {
                if (string.Equals(PresetIngredients[i]?.RequirementId, requirementId, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            for (int i = 0; i < Shortages.Count; i++)
            {
                IngredientShortage shortage = Shortages[i];
                if (string.Equals(shortage?.RequirementId, requirementId, StringComparison.OrdinalIgnoreCase))
                    count += shortage.MissingQuantity;
            }
            return count;
        }

        private static string BuildRequirementLabel(RecipeIngredientRequirement requirement)
        {
            if (requirement?.Ingredient != null)
                return requirement.Ingredient.DisplayName;
            if (requirement?.IngredientCategory != null)
                return requirement.IngredientCategory.DisplayName;
            return requirement?.RequirementId ?? "재료";
        }

        public PlannedPreparation GetPreparationRecommendation(
            IReadOnlyList<IngredientSO> selectedIngredients,
            int selectedIndex)
        {
            if (selectedIngredients == null || selectedIndex < 0 || selectedIndex >= selectedIngredients.Count)
                return null;
            IngredientSO ingredient = selectedIngredients[selectedIndex];
            string requirementId = ResolveRequirementId(selectedIngredients, selectedIndex);
            int occurrence = 0;
            for (int i = 0; i < selectedIndex; i++)
            {
                if (selectedIngredients[i] == ingredient
                    && string.Equals(ResolveRequirementId(selectedIngredients, i), requirementId, StringComparison.OrdinalIgnoreCase))
                {
                    occurrence++;
                }
            }

            PlannedPreparation fallback = null;
            for (int i = 0; i < PreparationRecommendations.Count; i++)
            {
                PlannedPreparation recommendation = PreparationRecommendations[i];
                if (recommendation?.Ingredient != ingredient
                    || string.Equals(recommendation.RequirementId, requirementId, StringComparison.OrdinalIgnoreCase) == false)
                    continue;
                if (recommendation.OccurrenceIndex == occurrence)
                    return recommendation;
                if (recommendation.OccurrenceIndex < 0)
                    fallback = recommendation;
            }
            return fallback;
        }

        public bool IsPreparationAllowed(
            IReadOnlyList<IngredientSO> selectedIngredients,
            int selectedIndex,
            IngredientPreparationOption option)
        {
            RecipeIngredientRequirement requirement = ResolveRequirement(selectedIngredients, selectedIndex);
            return requirement == null || requirement.IsPreparationMethodAllowed(option?.Method);
        }

        private string ResolveRequirementId(IReadOnlyList<IngredientSO> ingredients, int selectedIndex)
        {
            RecipeIngredientRequirement requirement = ResolveRequirement(ingredients, selectedIndex);
            return requirement != null ? requirement.RequirementId : string.Empty;
        }

        private RecipeIngredientRequirement ResolveRequirement(
            IReadOnlyList<IngredientSO> ingredients,
            int selectedIndex)
        {
            if (ingredients == null || selectedIndex < 0 || selectedIndex >= ingredients.Count)
                return null;
            int[] counts = new int[Requirements.Count];
            for (int ingredientIndex = 0; ingredientIndex <= selectedIndex; ingredientIndex++)
            {
                IngredientSO ingredient = ingredients[ingredientIndex];
                for (int requirementIndex = 0; requirementIndex < Requirements.Count; requirementIndex++)
                {
                    RecipeIngredientRequirement requirement = Requirements[requirementIndex]?.Requirement;
                    if (requirement == null
                        || requirement.CanAcceptMore(counts[requirementIndex]) == false
                        || requirement.IsMatchedBy(ingredient) == false)
                    {
                        continue;
                    }
                    counts[requirementIndex]++;
                    if (ingredientIndex == selectedIndex)
                        return requirement;
                    break;
                }
            }
            return null;
        }
    }
}
