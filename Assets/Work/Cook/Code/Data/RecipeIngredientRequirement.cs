using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Runtime;

namespace Work.Cook.Code.Data
{
    [Serializable]
    public sealed class RecipeIngredientRequirement
    {
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private IngredientCategorySO ingredientCategory;
        [SerializeField] private List<FoodTagSO> requiredTags = new List<FoodTagSO>();
        [SerializeField] private List<IngredientSO> alternatives = new List<IngredientSO>();
        [SerializeField] private List<RecipeIngredientAlternative> alternativeOptions = new List<RecipeIngredientAlternative>();
        [SerializeField, HideInInspector] private PreparationMethodSO requiredPreparationMethod;
        [SerializeField] private List<PreparationMethodSO> requiredPreparationMethods = new List<PreparationMethodSO>(2);
        [SerializeField, Min(0)] private int minCount = 1;
        [SerializeField, Min(0)] private int maxCount = 1;
        [SerializeField] private bool recipeDefining = true;
        [SerializeField] private bool requireManualPreparation;
        [SerializeField] private bool usePreparationResultNameModifier = true;

        public IngredientSO Ingredient => ingredient;
        public IngredientCategorySO IngredientCategory => ingredientCategory;
        public IReadOnlyList<FoodTagSO> RequiredTags => requiredTags;
        public IReadOnlyList<IngredientSO> Alternatives => alternatives;
        public IReadOnlyList<RecipeIngredientAlternative> AlternativeOptions => alternativeOptions;
        public IReadOnlyList<PreparationMethodSO> RequiredPreparationMethods => requiredPreparationMethods;
        public PreparationMethodSO RequiredPreparationMethod => GetFirstRequiredPreparationMethod();
        public int MinCount => Mathf.Max(0, minCount);
        public int MaxCount => Mathf.Max(0, maxCount);
        public bool HasMaxCount => MaxCount > 0;
        public bool RecipeDefining => recipeDefining;
        public bool RequireManualPreparation => requireManualPreparation;
        public bool UsePreparationResultNameModifier => usePreparationResultNameModifier;
        public bool RequiresChoice => ingredient == null
                                      && (ingredientCategory != null
                                          || requiredTags.Count > 0
                                          || alternatives.Count > 0
                                          || alternativeOptions.Count > 0);

        public bool IsMatchedBy(IngredientSO candidate)
        {
            if (candidate == null)
                return false;

            if (MatchesIngredientIdentity(candidate) == false)
                return false;

            return MatchesRequiredTags(candidate);
        }

        public bool IsPreparedMatch(PreparedIngredientState prepared)
        {
            if (prepared == null || IsMatchedBy(prepared.Ingredient) == false)
                return false;

            return IsPreparationMethodAllowed(prepared.Method);
        }

        public bool HasRequiredPreparationMethods => GetRequiredPreparationMethodCount() > 0;

        public bool IsPreparationMethodAllowed(PreparationMethodSO method)
        {
            int count = GetRequiredPreparationMethodCount();
            if (count == 0)
                return true;

            for (int i = 0; i < requiredPreparationMethods.Count; i++)
            {
                PreparationMethodSO requiredMethod = requiredPreparationMethods[i];
                if (requiredMethod != null && requiredMethod == method)
                    return true;
            }

            return requiredPreparationMethods.Count == 0
                   && requiredPreparationMethod != null
                   && requiredPreparationMethod == method;
        }

        private int GetRequiredPreparationMethodCount()
        {
            int count = 0;
            for (int i = 0; i < requiredPreparationMethods.Count; i++)
            {
                if (requiredPreparationMethods[i] != null)
                    count++;
            }

            if (count == 0 && requiredPreparationMethod != null)
                count = 1;

            return count;
        }

        private PreparationMethodSO GetFirstRequiredPreparationMethod()
        {
            for (int i = 0; i < requiredPreparationMethods.Count; i++)
            {
                if (requiredPreparationMethods[i] != null)
                    return requiredPreparationMethods[i];
            }

            return requiredPreparationMethod;
        }

        public bool IsCountSatisfied(int count)
        {
            if (count < MinCount)
                return false;

            return HasMaxCount == false || count <= MaxCount;
        }

        public bool CanAcceptMore(int count)
        {
            return HasMaxCount == false || count < MaxCount;
        }

        private bool MatchesIngredientIdentity(IngredientSO candidate)
        {
            if (candidate == ingredient)
                return true;

            if (ingredientCategory != null && candidate.Category == ingredientCategory)
                return true;

            for (int i = 0; i < alternatives.Count; i++)
            {
                if (candidate == alternatives[i])
                    return true;
            }

            for (int i = 0; i < alternativeOptions.Count; i++)
            {
                RecipeIngredientAlternative alternative = alternativeOptions[i];
                if (alternative != null && alternative.IsMatchedBy(candidate))
                    return true;
            }

            return ingredient == null
                   && ingredientCategory == null
                   && alternatives.Count == 0
                   && alternativeOptions.Count == 0;
        }

        private bool MatchesRequiredTags(IngredientSO candidate)
        {
            if (requiredTags.Count == 0)
                return true;

            for (int tagIndex = 0; tagIndex < requiredTags.Count; tagIndex++)
            {
                FoodTagSO requiredTag = requiredTags[tagIndex];
                if (requiredTag == null)
                    continue;

                bool matched = false;
                for (int candidateTagIndex = 0; candidateTagIndex < candidate.BaseTags.Count; candidateTagIndex++)
                {
                    if (candidate.BaseTags[candidateTagIndex] == requiredTag)
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched == false)
                    return false;
            }

            return true;
        }

        public string GetAlternativeResultNameModifier(IngredientSO candidate)
        {
            if (candidate == null || candidate == ingredient)
                return string.Empty;

            for (int i = 0; i < alternativeOptions.Count; i++)
            {
                RecipeIngredientAlternative alternative = alternativeOptions[i];
                if (alternative != null && alternative.IsMatchedBy(candidate))
                    return alternative.ResultNameModifier;
            }

            return string.Empty;
        }
    }

    [Serializable]
    public sealed class RecipeIngredientAlternative
    {
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private string resultNameModifier;

        public IngredientSO Ingredient => ingredient;
        public string ResultNameModifier => resultNameModifier;

        public bool IsMatchedBy(IngredientSO candidate)
        {
            return ingredient != null && candidate == ingredient;
        }
    }
}
