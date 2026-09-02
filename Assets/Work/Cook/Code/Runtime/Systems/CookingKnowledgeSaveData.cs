using System;
using System.Collections.Generic;
using Work.Cook.Code.Runtime.Core;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Runtime.Systems
{
    [Serializable]
    internal sealed class CookingKnowledgeSaveData
    {
        public int schemaVersion;
        public List<KnownRecipeRecord> recipeRecords = new List<KnownRecipeRecord>();

        // Versionless/V1 fields are intentionally retained for one-way migration.
        public List<string> discoveredRecipeIds = new List<string>();
        public List<string> knownPreparationEffectKeys = new List<string>();
        public List<string> attemptedRecipeIds = new List<string>();
        public List<string> triedIngredientIds = new List<string>();
        public List<string> triedPreparationKeys = new List<string>();
        public List<KnownRecipeTagSaveData> knownRecipeTags = new List<KnownRecipeTagSaveData>();
        public List<KnownRecipeVariantSaveData> knownRecipeVariants = new List<KnownRecipeVariantSaveData>();
    }

    [Serializable]
    public sealed class KnownRecipeRecord
    {
        public string recipeId;
        public int completionCount;
        public DishCraftGrade bestCraftGrade;
        public List<string> knownTagIds = new List<string>();
        public List<KnownRecipeVariantRecord> variants = new List<KnownRecipeVariantRecord>();
        public List<RecipeGuestSummaryRecord> guestSummaries = new List<RecipeGuestSummaryRecord>();
    }

    [Serializable]
    public sealed class KnownRecipeVariantRecord
    {
        public string variantId;
        public List<VariantComponentRecord> identityComponents = new List<VariantComponentRecord>();
        public List<VariantComponentRecord> replayComponents = new List<VariantComponentRecord>();
        public int completionCount;
        public DishCraftGrade bestCraftGrade;
        public List<string> knownTagIds = new List<string>();
        public int discoveryOrder;
        public bool hasBizarreObservation;
        public bool hasDangerousObservation;
        public string legacyVariantKey;
    }

    [Serializable]
    public sealed class VariantComponentRecord
    {
        public string requirementId;
        public string ingredientId;
        public string preparationOptionId;
        public string variantEffectId;
        public VariantComponentKind kind;
    }

    [Serializable]
    public sealed class RecipeGuestSummaryRecord
    {
        public string npcId;
        public int serveCount;
        public NpcConversationResult bestResult;
        public NpcConversationResult lastResult;
    }

    [Serializable]
    internal sealed class KnownRecipeTagSaveData
    {
        public string recipeId;
        public List<string> tagIds = new List<string>();
    }

    [Serializable]
    internal sealed class KnownRecipeVariantSaveData
    {
        public string recipeId;
        public List<string> variantKeys = new List<string>();
    }
}
