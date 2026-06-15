using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Runtime;

namespace Work.Cook.Code.Data
{
    [CreateAssetMenu(fileName = "Recipe", menuName = "Dungeon Dinner/Cooking/Recipe")]
    public sealed class RecipeSO : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
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
            if (discovered)
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

            bool[] usedIngredients = new bool[ingredients.Count];
            int[] counts = new int[requiredIngredients.Count];
            return TryMatchIngredientsRecursive(ingredients, usedIngredients, counts, 0);
        }

        public bool MatchesPreparedIngredients(IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            if (preparedIngredients == null)
                return false;

            bool[] usedIngredients = new bool[preparedIngredients.Count];
            int[] counts = new int[requiredIngredients.Count];
            return TryMatchPreparedRecursive(preparedIngredients, usedIngredients, counts, 0);
        }

        public int CalculateMatchScore(IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            if (MatchesPreparedIngredients(preparedIngredients) == false)
                return -1;

            int score = priority * 1000;
            for (int requirementIndex = 0; requirementIndex < requiredIngredients.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[requirementIndex];
                if (requirement == null)
                    continue;

                if (requirement.Ingredient != null)
                    score += 100;

                if (requirement.IngredientCategory != null)
                    score += 40;

                score += requirement.RequiredTags.Count * 20;

                if (requirement.RequiredPreparationMethod != null)
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
                    if (requirement != null && requirement.RequiredPreparationMethod != null)
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

            return false;
        }

        private bool TryMatchPreparedRecursive(
            IReadOnlyList<PreparedIngredientState> preparedIngredients,
            bool[] usedIngredients,
            int[] counts,
            int preparedIndex)
        {
            if (preparedIndex >= preparedIngredients.Count)
                return AreRequirementCountsSatisfied(counts);

            PreparedIngredientState prepared = preparedIngredients[preparedIndex];
            if (prepared == null)
                return TryMatchPreparedRecursive(preparedIngredients, usedIngredients, counts, preparedIndex + 1);

            for (int requirementIndex = 0; requirementIndex < requiredIngredients.Count; requirementIndex++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[requirementIndex];
                if (requirement == null
                    || requirement.CanAcceptMore(counts[requirementIndex]) == false
                    || requirement.IsPreparedMatch(prepared) == false)
                {
                    continue;
                }

                usedIngredients[preparedIndex] = true;
                counts[requirementIndex]++;

                if (TryMatchPreparedRecursive(preparedIngredients, usedIngredients, counts, preparedIndex + 1))
                    return true;

                counts[requirementIndex]--;
                usedIngredients[preparedIndex] = false;
            }

            return false;
        }

        private bool AreRequirementCountsSatisfied(IReadOnlyList<int> counts)
        {
            if (counts == null || counts.Count != requiredIngredients.Count)
                return false;

            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = requiredIngredients[i];
                if (requirement == null)
                    continue;

                if (requirement.IsCountSatisfied(counts[i]) == false)
                    return false;
            }

            return true;
        }
    }
}
