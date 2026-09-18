using System;
using System.Collections.Generic;
using System.Text;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingRecipeKnowledgePresentationModel
    {
        public string RecipeDescription { get; }
        public string CompletionSummary { get; }
        public string KnownTags { get; }
        public string GuestSummaries { get; }
        public IReadOnlyList<CookingRecipeVariantPresentationModel> Variants { get; }

        public CookingRecipeKnowledgePresentationModel(
            string recipeDescription,
            string completionSummary,
            string knownTags,
            string guestSummaries,
            IReadOnlyList<CookingRecipeVariantPresentationModel> variants)
        {
            RecipeDescription = recipeDescription ?? string.Empty;
            CompletionSummary = completionSummary ?? string.Empty;
            KnownTags = knownTags ?? string.Empty;
            GuestSummaries = guestSummaries ?? string.Empty;
            Variants = variants ?? Array.Empty<CookingRecipeVariantPresentationModel>();
        }
    }

    public sealed class CookingRecipeVariantPresentationModel
    {
        public string VariantId { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public string Details { get; }
        public bool CanReplay { get; }

        public CookingRecipeVariantPresentationModel(
            string variantId,
            string displayName,
            string summary,
            string details,
            bool canReplay)
        {
            VariantId = variantId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Summary = summary ?? string.Empty;
            Details = details ?? string.Empty;
            CanReplay = canReplay;
        }
    }

    public sealed class CookingRecipeKnowledgePresentationBuilder
    {
        private readonly CookingDataCatalogSO _catalog;

        public CookingRecipeKnowledgePresentationBuilder(CookingDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public CookingRecipeKnowledgePresentationModel Build(CookingRecipeKnowledgeSnapshot snapshot)
        {
            if (snapshot == null)
                return new CookingRecipeKnowledgePresentationModel(string.Empty, string.Empty, string.Empty, string.Empty, null);
            List<CookingRecipeVariantPresentationModel> variants = new List<CookingRecipeVariantPresentationModel>();
            for (int i = 0; i < snapshot.Variants.Count; i++)
                variants.Add(BuildVariant(snapshot.Recipe, snapshot.Variants[i]));
            return new CookingRecipeKnowledgePresentationModel(
                snapshot.Recipe != null ? snapshot.Recipe.Description : string.Empty,
                $"전체 완성 {snapshot.CompletionCount}회 · 최고 완성도 {BuildCraftGrade(snapshot.BestCraftGrade)}",
                BuildKnownTags(snapshot.KnownTags),
                BuildGuests(snapshot.GuestSummaries),
                variants);
        }

        private CookingRecipeVariantPresentationModel BuildVariant(
            RecipeSO recipe,
            CookingRecipeVariantKnowledgeSnapshot variant)
        {
            List<string> modifiers = new List<string>();
            List<string> changes = new List<string>();
            for (int i = 0; i < variant.IdentityComponents.Count; i++)
            {
                VariantComponentRecord component = variant.IdentityComponents[i];
                IngredientSO ingredient = FindIngredient(component.ingredientId);
                RecipeIngredientRequirement requirement = FindRequirement(recipe, component.requirementId);
                IngredientPreparationOption option = ingredient?.FindPreparationOption(component.preparationOptionId);

                if (component.kind == VariantComponentKind.Alternative
                    || component.kind == VariantComponentKind.Optional)
                {
                    string modifier = requirement?.GetAlternativeResultNameModifier(ingredient);
                    AddUnique(modifiers, string.IsNullOrWhiteSpace(modifier) ? ingredient?.DisplayName : modifier);
                    changes.Add(component.kind == VariantComponentKind.Optional
                        ? $"추가 재료: {ingredient?.DisplayName ?? component.ingredientId}"
                        : $"대체 재료: {ingredient?.DisplayName ?? component.ingredientId}");
                }

                if (option != null)
                {
                    AddUnique(modifiers,
                        string.IsNullOrWhiteSpace(option.ResultNameModifier)
                            ? option.DisplayName
                            : option.ResultNameModifier);
                    changes.Add($"손질: {ingredient.DisplayName} → {option.DisplayName}");
                }

                IngredientPreparationOption feedbackOption = option
                    ?? FindReplayPreparationOption(variant, component);
                CookingMiniGameFeedbackRule feedback = FindFeedbackRule(feedbackOption, component.variantEffectId);
                if (feedback != null && string.IsNullOrWhiteSpace(feedback.ResultNameModifier) == false)
                    AddUnique(modifiers, feedback.ResultNameModifier);
            }

            string baseName = recipe != null ? recipe.DisplayName : "요리";
            string displayName = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " + baseName : "변형 " + baseName;
            List<string> badges = new List<string>
            {
                $"{variant.CompletionCount}회",
                BuildCraftGrade(variant.BestCraftGrade)
            };
            if (variant.HasDangerousObservation)
                badges.Add("위험 관찰");
            if (variant.HasBizarreObservation)
                badges.Add("기괴 관찰");
            if (string.IsNullOrWhiteSpace(variant.LegacyVariantKey) == false)
                badges.Add("이전 버전 기록");

            StringBuilder details = new StringBuilder();
            if (changes.Count > 0)
            {
                for (int i = 0; i < changes.Count; i++)
                    details.Append("- ").AppendLine(changes[i]);
            }
            else
                details.AppendLine("이전 버전에서 발견한 변형입니다. 세부 조합을 복원할 수 없습니다.");
            details.AppendLine(BuildKnownTags(variant.KnownTags));

            return new CookingRecipeVariantPresentationModel(
                variant.VariantId,
                displayName.Trim(),
                string.Join(" · ", badges),
                details.ToString().TrimEnd(),
                variant.CanReplay);
        }

        private static string BuildKnownTags(IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return "확인된 태그: 아직 없음";
            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    names.Add(tags[i].DisplayName);
            }
            return names.Count > 0 ? "확인된 태그: " + string.Join(", ", names) : "확인된 태그: 아직 없음";
        }

        private static string BuildGuests(IReadOnlyList<RecipeGuestSummarySnapshot> guests)
        {
            if (guests == null || guests.Count == 0)
                return "제공 손님: 아직 기록 없음";
            StringBuilder builder = new StringBuilder("제공 손님");
            for (int i = 0; i < guests.Count; i++)
            {
                RecipeGuestSummarySnapshot guest = guests[i];
                builder.AppendLine().Append("- ").Append(guest.NpcId)
                    .Append(": ").Append(guest.ServeCount).Append("회")
                    .Append(" · 최고 ").Append(BuildReaction(guest.BestResult))
                    .Append(" · 최근 ").Append(BuildReaction(guest.LastResult));
            }
            return builder.ToString();
        }

        private IngredientSO FindIngredient(string ingredientId)
        {
            if (_catalog == null)
                return null;
            for (int i = 0; i < _catalog.Ingredients.Count; i++)
            {
                IngredientSO ingredient = _catalog.Ingredients[i];
                if (ingredient != null
                    && string.Equals(ingredient.IngredientId, ingredientId, StringComparison.OrdinalIgnoreCase))
                    return ingredient;
            }
            return null;
        }

        private static RecipeIngredientRequirement FindRequirement(RecipeSO recipe, string requirementId)
        {
            if (recipe == null)
                return null;
            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement != null
                    && string.Equals(requirement.RequirementId, requirementId, StringComparison.OrdinalIgnoreCase))
                    return requirement;
            }
            return null;
        }

        private static CookingMiniGameFeedbackRule FindFeedbackRule(
            IngredientPreparationOption option,
            string variantEffectId)
        {
            if (option == null || string.IsNullOrWhiteSpace(variantEffectId))
                return null;
            for (int i = 0; i < option.MiniGameFeedbackRules.Count; i++)
            {
                CookingMiniGameFeedbackRule rule = option.MiniGameFeedbackRules[i];
                if (rule != null
                    && string.Equals(rule.VariantEffectId, variantEffectId, StringComparison.OrdinalIgnoreCase))
                    return rule;
            }
            return null;
        }

        private IngredientPreparationOption FindReplayPreparationOption(
            CookingRecipeVariantKnowledgeSnapshot variant,
            VariantComponentRecord identityComponent)
        {
            if (variant == null || identityComponent == null)
                return null;
            for (int i = 0; i < variant.ReplayComponents.Count; i++)
            {
                VariantComponentRecord replay = variant.ReplayComponents[i];
                if (replay == null
                    || string.Equals(replay.requirementId, identityComponent.requirementId, StringComparison.OrdinalIgnoreCase) == false
                    || string.Equals(replay.ingredientId, identityComponent.ingredientId, StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                IngredientSO ingredient = FindIngredient(replay.ingredientId);
                IngredientPreparationOption option = ingredient?.FindPreparationOption(replay.preparationOptionId);
                if (FindFeedbackRule(option, identityComponent.variantEffectId) != null)
                    return option;
            }
            return null;
        }

        private static void AddUnique(ICollection<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value) || values.Contains(value))
                return;
            values.Add(value.Trim());
        }

        private static string BuildCraftGrade(DishCraftGrade grade)
        {
            switch (grade)
            {
                case DishCraftGrade.Perfect: return "완벽";
                case DishCraftGrade.Good: return "훌륭";
                case DishCraftGrade.Normal: return "보통";
                default: return "미흡";
            }
        }

        private static string BuildReaction(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect: return "최고";
                case NpcConversationResult.Correct: return "만족";
                case NpcConversationResult.Similar: return "비슷함";
                case NpcConversationResult.Disgusting: return "혐오";
                default: return "불일치";
            }
        }
    }
}
