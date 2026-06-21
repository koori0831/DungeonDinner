using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Info;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingRecipeEntryData : InfoDictionaryEntryData
    {
        public RecipeSO Recipe { get; }
        public bool IsDirectIngredientSelection { get; }
        public bool IsDiscovered { get; }
        public bool HasAttempted { get; }
        public IReadOnlyList<FoodTagSO> KnownEffectiveTags { get; }

        public CookingRecipeEntryData(
            RecipeSO recipe,
            Sprite icon,
            bool isDiscovered,
            bool hasAttempted,
            IReadOnlyList<FoodTagSO> knownEffectiveTags)
            : base(
                recipe != null ? recipe.GetKnowledgeDisplayName(isDiscovered) : string.Empty,
                icon,
                recipe != null ? recipe.GetKnowledgeDescription(isDiscovered, hasAttempted) : string.Empty)
        {
            Recipe = recipe;
            IsDiscovered = isDiscovered;
            HasAttempted = hasAttempted;
            KnownEffectiveTags = knownEffectiveTags ?? new List<FoodTagSO>();
        }

        public CookingRecipeEntryData(
            string displayName,
            Sprite icon,
            string description,
            bool isDirectIngredientSelection)
            : base(displayName, icon, description)
        {
            IsDirectIngredientSelection = isDirectIngredientSelection;
            KnownEffectiveTags = new List<FoodTagSO>();
        }
    }
}
