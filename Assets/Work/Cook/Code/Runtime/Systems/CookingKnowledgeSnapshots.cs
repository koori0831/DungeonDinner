using System;
using System.Collections.Generic;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Runtime.Systems
{
    public sealed class CookingRecipeKnowledgeSnapshot
    {
        public RecipeSO Recipe { get; }
        public bool IsDiscovered { get; }
        public int CompletionCount { get; }
        public DishCraftGrade BestCraftGrade { get; }
        public IReadOnlyList<FoodTagSO> KnownTags { get; }
        public IReadOnlyList<CookingRecipeVariantKnowledgeSnapshot> Variants { get; }
        public IReadOnlyList<RecipeGuestSummarySnapshot> GuestSummaries { get; }

        public CookingRecipeKnowledgeSnapshot(
            RecipeSO recipe,
            bool isDiscovered,
            int completionCount,
            DishCraftGrade bestCraftGrade,
            IReadOnlyList<FoodTagSO> knownTags,
            IReadOnlyList<CookingRecipeVariantKnowledgeSnapshot> variants,
            IReadOnlyList<RecipeGuestSummarySnapshot> guestSummaries)
        {
            Recipe = recipe;
            IsDiscovered = isDiscovered;
            CompletionCount = Math.Max(0, completionCount);
            BestCraftGrade = bestCraftGrade;
            KnownTags = knownTags ?? Array.Empty<FoodTagSO>();
            Variants = variants ?? Array.Empty<CookingRecipeVariantKnowledgeSnapshot>();
            GuestSummaries = guestSummaries ?? Array.Empty<RecipeGuestSummarySnapshot>();
        }
    }

    public sealed class CookingRecipeVariantKnowledgeSnapshot
    {
        public string VariantId { get; }
        public IReadOnlyList<VariantComponentRecord> IdentityComponents { get; }
        public IReadOnlyList<VariantComponentRecord> ReplayComponents { get; }
        public int CompletionCount { get; }
        public DishCraftGrade BestCraftGrade { get; }
        public IReadOnlyList<FoodTagSO> KnownTags { get; }
        public int DiscoveryOrder { get; }
        public bool HasBizarreObservation { get; }
        public bool HasDangerousObservation { get; }
        public string LegacyVariantKey { get; }
        public bool CanReplay => ReplayComponents.Count > 0;

        public CookingRecipeVariantKnowledgeSnapshot(
            string variantId,
            IReadOnlyList<VariantComponentRecord> identityComponents,
            IReadOnlyList<VariantComponentRecord> replayComponents,
            int completionCount,
            DishCraftGrade bestCraftGrade,
            IReadOnlyList<FoodTagSO> knownTags,
            int discoveryOrder,
            bool hasBizarreObservation,
            bool hasDangerousObservation,
            string legacyVariantKey)
        {
            VariantId = variantId ?? string.Empty;
            IdentityComponents = identityComponents ?? Array.Empty<VariantComponentRecord>();
            ReplayComponents = replayComponents ?? Array.Empty<VariantComponentRecord>();
            CompletionCount = Math.Max(0, completionCount);
            BestCraftGrade = bestCraftGrade;
            KnownTags = knownTags ?? Array.Empty<FoodTagSO>();
            DiscoveryOrder = discoveryOrder;
            HasBizarreObservation = hasBizarreObservation;
            HasDangerousObservation = hasDangerousObservation;
            LegacyVariantKey = legacyVariantKey ?? string.Empty;
        }
    }

    public sealed class RecipeGuestSummarySnapshot
    {
        public string NpcId { get; }
        public int ServeCount { get; }
        public NpcConversationResult BestResult { get; }
        public NpcConversationResult LastResult { get; }

        public RecipeGuestSummarySnapshot(
            string npcId,
            int serveCount,
            NpcConversationResult bestResult,
            NpcConversationResult lastResult)
        {
            NpcId = npcId ?? string.Empty;
            ServeCount = Math.Max(0, serveCount);
            BestResult = bestResult;
            LastResult = lastResult;
        }
    }
}
