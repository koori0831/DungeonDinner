using System;
using System.Collections.Generic;

namespace Work.Cook.Code.Runtime.Systems
{
    [Serializable]
    internal sealed class CookingKnowledgeSaveData
    {
        public List<string> discoveredRecipeIds = new List<string>();
        public List<string> knownPreparationEffectKeys = new List<string>();
        public List<string> attemptedRecipeIds = new List<string>();
        public List<string> triedIngredientIds = new List<string>();
        public List<string> triedPreparationKeys = new List<string>();
        public List<KnownRecipeTagSaveData> knownRecipeTags = new List<KnownRecipeTagSaveData>();
    }

    [Serializable]
    internal sealed class KnownRecipeTagSaveData
    {
        public string recipeId;
        public List<string> tagIds = new List<string>();
    }
}
