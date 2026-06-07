using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [Serializable]
    public sealed class RecipeIngredientRequirement
    {
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private List<IngredientSO> alternatives = new List<IngredientSO>();
        [SerializeField] private List<RecipeIngredientAlternative> alternativeOptions = new List<RecipeIngredientAlternative>();

        public IngredientSO Ingredient => ingredient;
        public IReadOnlyList<IngredientSO> Alternatives => alternatives;
        public IReadOnlyList<RecipeIngredientAlternative> AlternativeOptions => alternativeOptions;

        public bool IsMatchedBy(IngredientSO candidate)
        {
            if (candidate == null)
                return false;

            if (candidate == ingredient)
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

            return false;
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
