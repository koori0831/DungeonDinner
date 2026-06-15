using System;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [Serializable]
    public sealed class RecipePreparationRule
    {
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private PreparationMethodSO perfectMethod;

        public IngredientSO Ingredient => ingredient;
        public PreparationMethodSO PerfectMethod => perfectMethod;

        public bool IsSatisfiedBy(IngredientSO candidateIngredient, PreparationMethodSO candidateMethod)
        {
            return ingredient != null
                   && perfectMethod != null
                   && candidateIngredient == ingredient
                   && candidateMethod == perfectMethod;
        }
    }
}
