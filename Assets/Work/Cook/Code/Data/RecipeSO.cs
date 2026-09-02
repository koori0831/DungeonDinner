using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "Recipe", menuName = "Dungeon Dinner/Cooking/Recipe")]
    public sealed class RecipeSO : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite iconSprite;
        [SerializeField, TextArea] private string description;
        [SerializeField] private bool revealNameByDefault = true;
        [SerializeField] private string hiddenDisplayName = "???";
        [SerializeField, TextArea] private string undiscoveredDescription;
        [SerializeField, TextArea] private string hintDescription;
        [SerializeField, TextArea] private string discoveredDescription;
        [SerializeField] private FoodCategorySO category;
        [SerializeField] private int priority;
        [SerializeField] private List<FoodTagSO> baseTags = new List<FoodTagSO>();
        [SerializeField] private List<RecipeIngredientRequirement> requiredIngredients = new List<RecipeIngredientRequirement>();
        [SerializeField] private List<RecipePreparationRule> perfectPreparationRules = new List<RecipePreparationRule>();

        public string RecipeId => recipeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? recipeId : displayName;
        public Sprite IconSprite => iconSprite;
        public string Description => description;
        public bool RevealNameByDefault => revealNameByDefault;
        public string HiddenDisplayName => string.IsNullOrWhiteSpace(hiddenDisplayName) ? "???" : hiddenDisplayName;
        public string UndiscoveredDescription => undiscoveredDescription;
        public string HintDescription => hintDescription;
        public string DiscoveredDescription => discoveredDescription;
        public FoodCategorySO Category => category;
        public int Priority => priority;
        public IReadOnlyList<FoodTagSO> BaseTags => baseTags;
        public IReadOnlyList<RecipeIngredientRequirement> RequiredIngredients => requiredIngredients;
        public IReadOnlyList<RecipePreparationRule> PerfectPreparationRules => perfectPreparationRules;

        public string GetKnowledgeDisplayName(bool discovered)
        {
            return discovered || revealNameByDefault ? DisplayName : HiddenDisplayName;
        }

        public string GetKnowledgeDescription(bool discovered, bool hasAttempted)
        {
            if (discovered == true)
            {
                if (string.IsNullOrWhiteSpace(discoveredDescription) == false)
                    return discoveredDescription;

                return Description;
            }

            if (hasAttempted && string.IsNullOrWhiteSpace(hintDescription) == false)
                return hintDescription;

            if (string.IsNullOrWhiteSpace(undiscoveredDescription) == false)
                return undiscoveredDescription;

            return string.IsNullOrWhiteSpace(Description) ? "아직 정확한 조리법을 알 수 없습니다." : Description;
        }

        public bool MatchesIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null)
                return false;

            if (HasRecipeDefiningRequirement() == false)
                return false;

            bool[] usedIngredients = new bool[ingredients.Count];
            int[] counts = new int[requiredIngredients.Count];
            return TryMatchIngredientsRecursive(ingredients, usedIngredients, counts, 0);
        }

        public bool MatchesPreparedIngredients(IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            return MatchPreparedIngredients(preparedIngredients).Status == RecipeMatchStatus.Matched;
        }

        public RecipePreparedMatchResult MatchPreparedIngredients(
            IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            if (preparedIngredients == null)
            {
                return new RecipePreparedMatchResult(
                    RecipeMatchStatus.NoMatch,
                    null,
                    "Prepared ingredients are missing.");
            }

            if (HasRecipeDefiningRequirement() == false)
            {
                return new RecipePreparedMatchResult(
                    RecipeMatchStatus.NoMatch,
                    null,
                    "Recipe has no defining ingredient slot.");
            }

            int[] counts = new int[requiredIngredients.Count];
            int[] assignments = new int[preparedIngredients.Count];
            for (int i = 0; i < assignments.Length; i++)
                assignments[i] = -1;

            HashSet<string> semanticBindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<RecipeIngredientMatchBinding> firstBindings = null;
            FindPreparedBindingsRecursive(
                preparedIngredients,
                counts,
                assignments,
                0,
                semanticBindings,
                ref firstBindings);

            if (semanticBindings.Count == 0)
            {
                return new RecipePreparedMatchResult(
                    RecipeMatchStatus.NoMatch,
                    null,
                    "Prepared ingredients do not satisfy this recipe.");
            }

            if (semanticBindings.Count > 1)
            {
                return new RecipePreparedMatchResult(
                    RecipeMatchStatus.Ambiguous,
                    firstBindings,
                    "Prepared ingredients can bind to different recipe slots.");
            }

            return new RecipePreparedMatchResult(
                RecipeMatchStatus.Matched,
                firstBindings,
                "Prepared ingredients matched one stable slot binding.");
        }

        public int CalculateMatchScore(IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            if (MatchesPreparedIngredients(preparedIngredients) == false)
                return -1;

            return CalculateMatchSpecificityScore();
        }

        public int CalculateMatchSpecificityScore()
        {
            int score = priority * 1000;
            for (int requirementIndex = 0; requirementIndex < requiredIngredients.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[requirementIndex];
                if (IsRecipeDefiningRequirement(requirement) == false)
                    continue;

                if (requirement.Ingredient != null)
                    score += 100;

                if (requirement.IngredientCategory != null)
                    score += 40;

                score += requirement.RequiredTags.Count * 20;

                if (requirement.HasRequiredPreparationMethods)
                    score += 80;

                score += requirement.MinCount * 5;
            }

            return score;
        }

        public bool IsPerfectPreparation(IngredientSO ingredient, PreparationMethodSO method)
        {
            if (ingredient == null || method == null)
                return false;

            for (int i = 0; i < perfectPreparationRules.Count; i++)
            {
                RecipePreparationRule rule = perfectPreparationRules[i];
                if (rule != null
                    && rule.PerfectMethod == method
                    && IsRequirementIngredientMatchedBy(rule.Ingredient, ingredient))
                    return true;
            }

            return false;
        }

        public bool IsRequirementIngredientMatchedBy(IngredientSO requirementIngredient, IngredientSO candidate)
        {
            if (requirementIngredient == null || candidate == null)
                return false;

            if (candidate == requirementIngredient)
                return true;

            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[i];
                if (requirement != null
                    && requirement.Ingredient == requirementIngredient
                    && requirement.IsMatchedBy(candidate))
                    return true;
            }

            return false;
        }

        public bool HasPerfectPreparationRules => perfectPreparationRules.Count > 0;
        public bool HasRequiredPreparationMethods
        {
            get
            {
                for (int i = 0; i < requiredIngredients.Count; i++)
                {
                    RecipeIngredientRequirement requirement = requiredIngredients[i];
                    if (IsRecipeDefiningRequirement(requirement) && requirement.HasRequiredPreparationMethods)
                        return true;
                }

                return false;
            }
        }

        private bool TryMatchIngredientsRecursive(
            IReadOnlyList<IngredientSO> ingredients,
            bool[] usedIngredients,
            int[] counts,
            int ingredientIndex)
        {
            if (ingredientIndex >= ingredients.Count)
                return AreRequirementCountsSatisfied(counts);

            IngredientSO ingredient = ingredients[ingredientIndex];
            if (ingredient == null)
                return TryMatchIngredientsRecursive(ingredients, usedIngredients, counts, ingredientIndex + 1);

            for (int requirementIndex = 0; requirementIndex < requiredIngredients.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[requirementIndex];
                if (requirement == null
                    || requirement.CanAcceptMore(counts[requirementIndex]) == false
                    || requirement.IsMatchedBy(ingredient) == false)
                {
                    continue;
                }

                usedIngredients[ingredientIndex] = true;
                counts[requirementIndex]++;

                if (TryMatchIngredientsRecursive(ingredients, usedIngredients, counts, ingredientIndex + 1))
                    return true;

                counts[requirementIndex]--;
                usedIngredients[ingredientIndex] = false;
            }

            // Every supplied ingredient must belong to an authored recipe slot.
            // Non recipe-defining requirements are optional, but they are still the
            // explicit allow-list for garnishes and other narrow variations.
            return false;
        }

        private void FindPreparedBindingsRecursive(
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            int[] counts,
            int[] assignments,
            int preparedIndex)
        {
            List<RecipeIngredientMatchBinding> ignored = null;
            HashSet<string> signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            FindPreparedBindingsRecursive(
                preparedIngredients,
                counts,
                assignments,
                preparedIndex,
                signatures,
                ref ignored);
        }

        private void FindPreparedBindingsRecursive(
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            int[] counts,
            int[] assignments,
            int preparedIndex,
            ISet<string> semanticBindings,
            ref List<RecipeIngredientMatchBinding> firstBindings)
        {
            if (semanticBindings.Count > 1)
                return;

            if (preparedIndex >= preparedIngredients.Count)
            {
                if (AreRequirementCountsSatisfied(counts) == false)
                    return;

                List<RecipeIngredientMatchBinding> bindings = BuildBindings(preparedIngredients, assignments);
                string signature = BuildBindingSignature(bindings);
                if (semanticBindings.Add(signature) && firstBindings == null)
                    firstBindings = bindings;
                return;
            }

            PreparedIngredientState prepared = preparedIngredients[preparedIndex];
            if (prepared == null)
            {
                FindPreparedBindingsRecursive(
                    preparedIngredients,
                    counts,
                    assignments,
                    preparedIndex + 1,
                    semanticBindings,
                    ref firstBindings);
                return;
            }

            for (int requirementIndex = 0; requirementIndex < requiredIngredients.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[requirementIndex];
                if (requirement == null
                    || requirement.CanAcceptMore(counts[requirementIndex]) == false
                    || requirement.IsPreparedMatch(prepared) == false)
                {
                    continue;
                }

                counts[requirementIndex]++;
                assignments[preparedIndex] = requirementIndex;

                FindPreparedBindingsRecursive(
                    preparedIngredients,
                    counts,
                    assignments,
                    preparedIndex + 1,
                    semanticBindings,
                    ref firstBindings);

                counts[requirementIndex]--;
                assignments[preparedIndex] = -1;
            }
        }

        private List<RecipeIngredientMatchBinding> BuildBindings(
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            IReadOnlyList<int> assignments)
        {
            List<RecipeIngredientMatchBinding> bindings = new List<RecipeIngredientMatchBinding>();
            for (int preparedIndex = 0; preparedIndex < assignments.Count; preparedIndex++)
            {
                int requirementIndex = assignments[preparedIndex];
                if (requirementIndex < 0 || requirementIndex >= requiredIngredients.Count)
                    continue;

                RecipeIngredientRequirement requirement = requiredIngredients[requirementIndex];
                PreparedIngredientState prepared = preparedIngredients[preparedIndex];
                bindings.Add(new RecipeIngredientMatchBinding(
                    GetStableRequirementId(requirement, requirementIndex),
                    requirementIndex,
                    preparedIndex,
                    requirement,
                    prepared,
                    GetMatchKind(requirement, prepared?.Ingredient)));
            }

            return bindings;
        }

        private static string BuildBindingSignature(IReadOnlyList<RecipeIngredientMatchBinding> bindings)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < bindings.Count; i++)
            {
                RecipeIngredientMatchBinding binding = bindings[i];
                PreparedIngredientState prepared = binding.PreparedIngredient;
                string ingredientId = prepared?.Ingredient != null
                    ? prepared.Ingredient.IngredientId
                    : "none";
                string optionId = prepared?.PreparationOption != null
                    ? prepared.PreparationOption.PreparationOptionId
                    : "none";
                string effectId = prepared?.MiniGameFeedbackRule != null
                    ? prepared.MiniGameFeedbackRule.VariantEffectId
                    : "none";
                parts.Add(binding.RequirementId + ":" + ingredientId + ":" + optionId + ":" + effectId);
            }

            parts.Sort(StringComparer.OrdinalIgnoreCase);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < parts.Count; i++)
                builder.Append(parts[i]).Append('|');
            return builder.ToString();
        }

        private static RecipeIngredientMatchKind GetMatchKind(
            RecipeIngredientRequirement requirement,
            IngredientSO ingredient)
        {
            if (requirement == null || requirement.RecipeDefining == false)
                return RecipeIngredientMatchKind.Optional;
            if (requirement.Ingredient != null && requirement.Ingredient != ingredient)
                return RecipeIngredientMatchKind.Alternative;
            return RecipeIngredientMatchKind.Canonical;
        }

        private static string GetStableRequirementId(
            RecipeIngredientRequirement requirement,
            int requirementIndex)
        {
            if (requirement != null && string.IsNullOrWhiteSpace(requirement.RequirementId) == false)
                return requirement.RequirementId;

            // Runtime never writes IDs. This fallback only keeps legacy data usable
            // until the editor migration tool persists authored IDs.
            return "slot_legacy_" + requirementIndex;
        }

        private bool AreRequirementCountsSatisfied(IReadOnlyList<int> counts)
        {
            if (counts == null || counts.Count != requiredIngredients.Count)
                return false;

            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[i];
                if (IsRecipeDefiningRequirement(requirement) == false)
                    continue;

                if (requirement.IsCountSatisfied(counts[i]) == false)
                    return false;
            }

            return true;
        }

        private bool HasRecipeDefiningRequirement()
        {
            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                if (IsRecipeDefiningRequirement(requiredIngredients[i]))
                    return true;
            }

            return false;
        }

        private static bool IsRecipeDefiningRequirement(RecipeIngredientRequirement requirement)
        {
            return requirement != null && requirement.RecipeDefining;
        }
    }
}
