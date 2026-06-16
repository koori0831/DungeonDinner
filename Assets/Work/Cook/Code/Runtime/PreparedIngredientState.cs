using System.Collections.Generic;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class PreparedIngredientState
    {
        public IngredientSO Ingredient { get; }
        public IngredientPreparationOption PreparationOption { get; }
        public PreparationMethodSO Method => PreparationOption?.Method;
        public IReadOnlyList<FoodTagSO> AddTags => PreparationOption?.AddTags ?? EmptyTags;
        public IReadOnlyList<FoodTagSO> RemoveTags => PreparationOption?.RemoveTags ?? EmptyTags;
        public bool CausesDisgusting => PreparationOption != null && PreparationOption.CausesDisgusting;
        public bool AddsPoison => PreparationOption != null && PreparationOption.AddsPoison;
        public int QualityDelta => PreparationOption?.QualityDelta ?? 0;
        public string ResultNameModifier => PreparationOption?.ResultNameModifier ?? string.Empty;

        private static readonly IReadOnlyList<FoodTagSO> EmptyTags = new List<FoodTagSO>();

        public PreparedIngredientState(IngredientSO ingredient, IngredientPreparationOption preparationOption)
        {
            Ingredient = ingredient;
            PreparationOption = preparationOption;
        }
    }
}
