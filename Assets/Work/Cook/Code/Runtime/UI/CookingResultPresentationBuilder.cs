using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime.UI
{
    public static class CookingResultPresentationBuilder
    {
        private const string UnknownGuestText = "손님";
        private const string FreeCookingText = "자유 조리";

        public static CookingResultPresentationModel BuildResult(
            DishResult result,
            CookingGameSnapshot snapshot,
            NpcDishMatchReport report,
            CookingDataCatalogSO catalog,
            CookingUiPresentationSettingsSO settings,
            Func<string, string> npcNameResolver,
            int previewReward,
            bool canHandToNpc)
        {
            if (result == null)
                return null;

            bool hasNpcReport = report != null;
            NpcConversationResult reaction = report?.Evaluation?.Result ?? NpcConversationResult.Wrong;
            CookingReactionVisual reactionVisual = settings != null
                ? settings.GetReactionVisual(reaction)
                : null;

            string npcId = report?.Order?.NpcId;
            string npcName = ResolveNpcName(npcId, npcNameResolver);
            int matchScore = report?.MatchScore ?? 0;
            int maxMatchScore = report?.MaxMatchScore ?? 0;
            int matchPercent = maxMatchScore > 0
                ? Mathf.RoundToInt((float)matchScore / maxMatchScore * 100f)
                : 0;

            List<string> reasons = BuildPlayerFacingReasons(result.Reasons);
            string evaluationReason = BuildPlayerFacingEvaluationReason(report);
            if (string.IsNullOrWhiteSpace(evaluationReason) == false && ContainsIgnoreCase(reasons, evaluationReason) == false)
                reasons.Add(evaluationReason);

            return new CookingResultPresentationModel(
                result.DisplayName,
                ResolveRecipeName(result, catalog),
                result.Category != null ? result.Category.DisplayName : "분류 없음",
                result.CraftGrade,
                BuildCraftGradeName(result.CraftGrade),
                result.QualityScore,
                hasNpcReport ? BuildRevealedRepresentativeTags(result.Tags, report) : new List<string>(),
                npcName,
                reaction,
                hasNpcReport
                    ? reactionVisual?.DisplayName ?? BuildReactionName(reaction)
                    : "판정 대기",
                hasNpcReport
                    ? reactionVisual?.Summary ?? string.Empty
                    : "현재 손님의 주문 정보를 확인할 수 없습니다.",
                matchScore,
                maxMatchScore,
                matchPercent,
                Mathf.Max(0, previewReward),
                hasNpcReport,
                canHandToNpc,
                BuildTagComparisons(report, catalog),
                BuildPreparedIngredients(result.PreparedIngredients),
                reasons,
                result);
        }

        public static CookingOrderPresentationModel BuildOrder(
            CookingGameSnapshot snapshot,
            NpcOrderContext order,
            CookingDataCatalogSO catalog,
            Func<string, string> npcNameResolver)
        {
            bool hasOrder = HasOrderInformation(order);
            string recipeName = snapshot?.SelectedRecipe != null
                ? snapshot.SelectedRecipe.DisplayName
                : FreeCookingText;

            if (string.IsNullOrWhiteSpace(recipeName))
                recipeName = FreeCookingText;

            return new CookingOrderPresentationModel(
                hasOrder,
                hasOrder ? ResolveNpcName(order?.NpcId, npcNameResolver) : string.Empty,
                recipeName,
                new List<CookingTagChipModel>(),
                snapshot?.PreparedIngredientCount ?? 0,
                snapshot?.SelectedIngredientCount ?? 0,
                hasOrder ? string.Empty : "아직 확인된 주문 단서가 없습니다.");
        }

        public static string ResolveTagId(string tagId, CookingDataCatalogSO catalog)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                return string.Empty;

            if (catalog?.Tags != null)
            {
                for (int i = 0; i < catalog.Tags.Count; i++)
                {
                    FoodTagSO tag = catalog.Tags[i];
                    if (tag != null && string.Equals(tag.TagId, tagId, StringComparison.OrdinalIgnoreCase))
                        return tag.DisplayName;
                }
            }

            return "알 수 없는 특성";
        }

        public static string ResolveRecipeId(string recipeId, CookingDataCatalogSO catalog, string fallback = "알 수 없는 요리")
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return fallback;

            RecipeSO recipe = catalog?.FindRecipeById(recipeId);
            return recipe != null ? recipe.DisplayName : fallback;
        }

        public static string HumanizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.Trim().Replace('_', ' ').Replace('-', ' ');
            System.Text.StringBuilder builder = new System.Text.StringBuilder(trimmed.Length + 8);
            for (int i = 0; i < trimmed.Length; i++)
            {
                char current = trimmed[i];
                if (i > 0 && char.IsUpper(current) && char.IsLower(trimmed[i - 1]))
                    builder.Append(' ');

                builder.Append(current);
            }

            return builder.ToString().Trim();
        }

        private static List<CookingPreparedIngredientPresentationModel> BuildPreparedIngredients(
            IReadOnlyList<PreparedIngredientState> preparedIngredients)
        {
            List<CookingPreparedIngredientPresentationModel> models = new List<CookingPreparedIngredientPresentationModel>();
            if (preparedIngredients == null)
                return models;

            for (int i = 0; i < preparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = preparedIngredients[i];
                if (prepared == null)
                    continue;

                CookingMiniGameGrade? grade = prepared.HasMiniGameResult
                    ? prepared.MiniGameResult.Grade
                    : (CookingMiniGameGrade?)null;

                models.Add(new CookingPreparedIngredientPresentationModel(
                    prepared.Ingredient != null ? prepared.Ingredient.DisplayName : "알 수 없는 재료",
                    prepared.Method != null ? prepared.Method.DisplayName : "손질 없음",
                    grade.HasValue ? BuildMiniGameGradeName(grade.Value) : "직접 손질",
                    grade,
                    prepared.QualityDelta,
                    prepared.MiniGameFeedbackText,
                    BuildPreparedEffects(prepared),
                    prepared));
            }

            return models;
        }

        private static List<string> BuildPreparedEffects(PreparedIngredientState prepared)
        {
            List<string> labels = new List<string>();
            if (prepared.QualityDelta != 0)
                labels.Add($"품질 {prepared.QualityDelta:+#;-#;0}");

            AddTagEffects(labels, "+", prepared.AddedTags);
            AddTagEffects(labels, "−", prepared.RemoveTags);

            if (string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                labels.Add($"이름: {prepared.ResultNameModifier.Trim()}");
            if (prepared.CausesDisgusting)
                labels.Add("혐오 위험");
            if (prepared.AddsPoison)
                labels.Add("독성");
            if (labels.Count == 0)
                labels.Add("변화 없음");

            return labels;
        }

        private static void AddTagEffects(List<string> target, string prefix, IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null)
                return;

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    target.Add($"{prefix}{tags[i].DisplayName}");
            }
        }

        private static List<CookingTagChipModel> BuildTagComparisons(
            NpcDishMatchReport report,
            CookingDataCatalogSO catalog)
        {
            List<CookingTagChipModel> models = new List<CookingTagChipModel>();
            if (report?.Order == null)
                return models;

            AddComparisonGroup(
                models,
                report.Order.RequiredTags,
                report.MatchedRequiredTags,
                report.MissingRequiredTags,
                CookingTagPresentationKind.Required,
                catalog);
            AddComparisonGroup(
                models,
                report.Order.PreferredTags,
                report.MatchedPreferredTags,
                report.MissingPreferredTags,
                CookingTagPresentationKind.Preferred,
                catalog);
            AddTriggeredGroup(
                models,
                report.Order.AvoidTags,
                report.MatchedAvoidTags,
                CookingTagPresentationKind.Avoid,
                catalog);
            AddTriggeredGroup(
                models,
                report.Order.DisgustingTags,
                report.MatchedDisgustingTags,
                CookingTagPresentationKind.Danger,
                catalog);
            return models;
        }

        private static List<CookingTagChipModel> BuildOrderTags(NpcOrderContext order, CookingDataCatalogSO catalog)
        {
            List<CookingTagChipModel> models = new List<CookingTagChipModel>();
            if (order == null)
                return models;

            AddNeutralGroup(models, order.RequiredTags, CookingTagPresentationKind.Required, catalog);
            AddNeutralGroup(models, order.PreferredTags, CookingTagPresentationKind.Preferred, catalog);
            AddNeutralGroup(models, order.AvoidTags, CookingTagPresentationKind.Avoid, catalog);
            AddNeutralGroup(models, order.DisgustingTags, CookingTagPresentationKind.Danger, catalog);
            return models;
        }

        private static void AddComparisonGroup(
            ICollection<CookingTagChipModel> target,
            IReadOnlyList<string> all,
            IReadOnlyList<string> matched,
            IReadOnlyList<string> missing,
            CookingTagPresentationKind kind,
            CookingDataCatalogSO catalog)
        {
            if (all == null)
                return;

            for (int i = 0; i < all.Count; i++)
            {
                string id = all[i];
                CookingTagPresentationStatus status = ContainsIgnoreCase(matched, id)
                    ? CookingTagPresentationStatus.Matched
                    : ContainsIgnoreCase(missing, id)
                        ? CookingTagPresentationStatus.Missing
                        : CookingTagPresentationStatus.Neutral;
                AddChip(target, id, kind, status, catalog);
            }
        }

        private static void AddTriggeredGroup(
            ICollection<CookingTagChipModel> target,
            IReadOnlyList<string> all,
            IReadOnlyList<string> triggered,
            CookingTagPresentationKind kind,
            CookingDataCatalogSO catalog)
        {
            if (all == null)
                return;

            for (int i = 0; i < all.Count; i++)
            {
                string id = all[i];
                CookingTagPresentationStatus status = ContainsIgnoreCase(triggered, id)
                    ? CookingTagPresentationStatus.Triggered
                    : CookingTagPresentationStatus.Neutral;
                AddChip(target, id, kind, status, catalog);
            }
        }

        private static void AddNeutralGroup(
            ICollection<CookingTagChipModel> target,
            IReadOnlyList<string> values,
            CookingTagPresentationKind kind,
            CookingDataCatalogSO catalog)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
                AddChip(target, values[i], kind, CookingTagPresentationStatus.Neutral, catalog);
        }

        private static void AddChip(
            ICollection<CookingTagChipModel> target,
            string id,
            CookingTagPresentationKind kind,
            CookingTagPresentationStatus status,
            CookingDataCatalogSO catalog)
        {
            string displayName = ResolveTagId(id, catalog);
            if (string.IsNullOrWhiteSpace(displayName))
                return;

            target.Add(new CookingTagChipModel(displayName, kind, status));
        }

        private static List<string> BuildRepresentativeTags(IReadOnlyList<FoodTagSO> tags)
        {
            List<string> names = new List<string>();
            if (tags == null)
                return names;

            for (int i = 0; i < tags.Count && names.Count < 4; i++)
            {
                if (tags[i] != null && string.IsNullOrWhiteSpace(tags[i].DisplayName) == false)
                    names.Add(tags[i].DisplayName);
            }

            return names;
        }

        private static string ResolveRecipeName(DishResult result, CookingDataCatalogSO catalog)
        {
            if (result == null || result.FormationStatus == DishFormationStatus.Unformed)
                return "요리 미성립";

            if (result?.BaseRecipe != null)
                return result.IsVariant ? $"변형 · {result.BaseRecipe.DisplayName}" : result.BaseRecipe.DisplayName;

            if (result != null && result.IsRecipeMatched == false)
                return FreeCookingText;

            return ResolveRecipeId(result?.RecipeId, catalog);
        }

        private static string ResolveNpcName(string npcId, Func<string, string> resolver)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return UnknownGuestText;

            string resolved = resolver?.Invoke(npcId);
            if (string.IsNullOrWhiteSpace(resolved) == false
                && string.Equals(resolved, npcId, StringComparison.OrdinalIgnoreCase) == false)
            {
                return resolved.Trim();
            }

            return UnknownGuestText;
        }

        private static bool HasOrderInformation(NpcOrderContext order)
        {
            if (order == null)
                return false;

            return string.IsNullOrWhiteSpace(order.NpcId) == false
                   || string.IsNullOrWhiteSpace(order.CorrectRecipeId) == false
                   || order.RequiredTags.Count > 0
                   || order.PreferredTags.Count > 0
                   || order.AvoidTags.Count > 0
                   || order.DisgustingTags.Count > 0;
        }

        private static string BuildCraftGradeName(DishCraftGrade grade)
        {
            switch (grade)
            {
                case DishCraftGrade.Bad:
                    return "미흡";
                case DishCraftGrade.Good:
                    return "좋음";
                case DishCraftGrade.Perfect:
                    return "완벽";
                default:
                    return "보통";
            }
        }

        private static List<string> BuildRevealedRepresentativeTags(
            IReadOnlyList<FoodTagSO> tags,
            NpcDishMatchReport report)
        {
            List<string> names = new List<string>();
            if (tags == null || report == null)
                return names;

            HashSet<string> revealedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddIds(revealedIds, report.MatchedRequiredTags);
            AddIds(revealedIds, report.MatchedPreferredTags);
            AddIds(revealedIds, report.MatchedAvoidTags);
            AddIds(revealedIds, report.MatchedDisgustingTags);

            for (int i = 0; i < tags.Count && names.Count < 4; i++)
            {
                FoodTagSO tag = tags[i];
                if (tag != null && revealedIds.Contains(tag.TagId))
                    names.Add(tag.DisplayName);
            }

            return names;
        }

        private static void AddIds(ISet<string> target, IReadOnlyList<string> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i]) == false)
                    target.Add(source[i]);
            }
        }

        private static string BuildReactionName(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return "황홀함";
                case NpcConversationResult.Correct:
                    return "만족";
                case NpcConversationResult.Similar:
                    return "흥미";
                case NpcConversationResult.Disgusting:
                    return "거부감";
                default:
                    return "아쉬움";
            }
        }

        private static string BuildMiniGameGradeName(CookingMiniGameGrade grade)
        {
            switch (grade)
            {
                case CookingMiniGameGrade.Perfect:
                    return "완벽";
                case CookingMiniGameGrade.Good:
                    return "좋음";
                case CookingMiniGameGrade.Normal:
                    return "보통";
                default:
                    return "아쉬움";
            }
        }

        private static List<string> BuildPlayerFacingReasons(IReadOnlyList<string> values)
        {
            List<string> reasons = new List<string>();
            if (values == null)
                return reasons;

            for (int i = 0; i < values.Count; i++)
            {
                string reason = BuildPlayerFacingDishReason(values[i]);
                if (string.IsNullOrWhiteSpace(reason) == false && ContainsIgnoreCase(reasons, reason) == false)
                    reasons.Add(reason);
            }

            return reasons;
        }

        private static string BuildPlayerFacingDishReason(string rawReason)
        {
            if (string.IsNullOrWhiteSpace(rawReason))
                return string.Empty;

            string reason = rawReason.Trim();
            if (string.Equals(reason, "Cooking session is missing.", StringComparison.OrdinalIgnoreCase))
                return "조리 정보를 불러오지 못했습니다.";
            if (string.Equals(reason, "Selected ingredients do not form a known recipe.", StringComparison.OrdinalIgnoreCase))
                return "선택한 재료로 알려진 요리를 완성하지 못했습니다.";
            if (string.Equals(reason, "Prepared ingredients did not match any authored recipe.", StringComparison.OrdinalIgnoreCase))
                return "선택한 재료와 손질로 성립하는 요리를 완성하지 못했습니다.";
            if (string.Equals(reason, "No ingredients were selected.", StringComparison.OrdinalIgnoreCase))
                return "선택한 재료가 없습니다.";

            const string disgustingSuffix = " preparation causes disgusting result.";
            if (reason.EndsWith(disgustingSuffix, StringComparison.OrdinalIgnoreCase))
            {
                string ingredientName = ResolveReasonIngredientName(reason, disgustingSuffix);
                return $"{ingredientName} 손질 결과가 먹기 어려운 상태가 되었습니다.";
            }

            const string poisonSuffix = " preparation added poison.";
            if (reason.EndsWith(poisonSuffix, StringComparison.OrdinalIgnoreCase))
            {
                string ingredientName = ResolveReasonIngredientName(reason, poisonSuffix);
                return $"{ingredientName} 손질 과정에서 독성이 생겼습니다.";
            }

            return "조리 과정에서 주의할 결과가 확인되었습니다.";
        }

        private static string ResolveReasonIngredientName(string reason, string suffix)
        {
            string name = reason.Substring(0, reason.Length - suffix.Length).Trim();
            if (string.IsNullOrWhiteSpace(name)
                || string.Equals(name, "Unknown ingredient", StringComparison.OrdinalIgnoreCase))
            {
                return "재료";
            }

            return name;
        }

        private static string BuildPlayerFacingEvaluationReason(NpcDishMatchReport report)
        {
            if (report?.Evaluation == null)
                return string.Empty;

            if (report.Dish?.IsFormed == false)
                return "음식으로 성립하지 않은 결과입니다.";
            if (report.Dish?.IsDangerous == true)
                return "손님에게 위험한 결과입니다.";
            if (report.MatchedDisgustingTags.Count > 0)
                return "손님이 꺼리는 치명적인 특성이 포함되었습니다.";

            switch (report.Evaluation.Result)
            {
                case NpcConversationResult.Perfect:
                    return "주문한 조리법과 요청 조건을 모두 만족했습니다.";
                case NpcConversationResult.Correct:
                    return "손님이 요청한 주요 조건을 만족했습니다.";
                case NpcConversationResult.Similar:
                    return report.RecipeMatches && report.MatchedAvoidTags.Count > 0
                        ? "조리법은 맞지만 손님이 피하고 싶은 특성이 포함되었습니다."
                        : "손님의 요청 조건을 일부 만족했습니다.";
                case NpcConversationResult.Disgusting:
                    return "손님이 먹기 어려운 특성이 포함되었습니다.";
                default:
                    return "손님의 주요 요청 조건을 충분히 만족하지 못했습니다.";
            }
        }

        private static bool ContainsIgnoreCase(IReadOnlyList<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value))
                return false;

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
