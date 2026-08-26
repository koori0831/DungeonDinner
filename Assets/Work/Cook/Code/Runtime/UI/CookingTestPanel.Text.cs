using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.NPC.Code.Data;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed partial class CookingTestPanel
    {
        private string BuildCatalogStatusText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("카탈로그 상태");
            builder.AppendLine($"레시피 {runner.Recipes.Count}개 / 재료 {runner.Ingredients.Count}개");

            if (catalog == null)
                builder.AppendLine("경고: CookingDataCatalogSO가 연결되지 않았습니다.");

            if (runner.Recipes.Count == 0 || runner.Ingredients.Count == 0)
                builder.AppendLine("경고: 레시피 또는 재료가 비어 있습니다.");

            return builder.ToString();
        }

        private string BuildRecipeInfo(RecipeSO recipe)
        {
            if (recipe == null)
                return "선택된 레시피가 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"{recipe.DisplayName}  ({recipe.RecipeId})");
            builder.AppendLine($"카테고리: {(recipe.Category != null ? recipe.Category.DisplayName : "없음")}");
            builder.AppendLine($"태그: {BuildTagDisplayText(recipe.BaseTags)}");
            builder.AppendLine("필요 재료:");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null || requirement.Ingredient == null)
                    continue;

                builder.Append($"- {requirement.Ingredient.DisplayName}");
                string alternativeText = BuildAlternativeText(requirement);
                if (string.IsNullOrWhiteSpace(alternativeText) == false)
                    builder.Append($" / 대체: {alternativeText}");

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildAlternativeText(RecipeIngredientRequirement requirement)
        {
            if (requirement == null)
                return string.Empty;

            List<string> names = new List<string>();
            for (int i = 0; i < requirement.AlternativeOptions.Count; i++)
            {
                RecipeIngredientAlternative alternative = requirement.AlternativeOptions[i];
                if (alternative == null || alternative.Ingredient == null)
                    continue;

                string label = alternative.Ingredient.DisplayName;
                if (string.IsNullOrWhiteSpace(alternative.ResultNameModifier) == false)
                    label += $" -> {alternative.ResultNameModifier}";

                names.Add(label);
            }

            for (int i = 0; i < requirement.Alternatives.Count; i++)
            {
                IngredientSO ingredient = requirement.Alternatives[i];
                if (ingredient != null)
                    names.Add(ingredient.DisplayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : string.Empty;
        }

        private string BuildRecipeWarnings(RecipeSO recipe)
        {
            return string.Empty;
        }

        private string BuildDirectSelectionText(RecipeSO previewRecipe)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("선택한 재료");

            if (_directSelection.Count == 0)
            {
                builder.AppendLine("- 없음");
            }
            else
            {
                for (int i = 0; i < _directSelection.Count; i++)
                    builder.AppendLine($"- {_directSelection[i].DisplayName} ({_directSelection[i].IngredientId})");
            }

            builder.Append("예상 매칭: ");
            builder.AppendLine(previewRecipe != null
                ? $"{previewRecipe.DisplayName} ({previewRecipe.RecipeId})"
                : "알려진 레시피 없음 - 완성 시 괴식 판정 가능");

            return builder.ToString();
        }

        private string BuildSessionStatusText(RecipeSO recipe, IngredientSO currentIngredient)
        {
            CookingSession session = runner.Controller.CurrentSession;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"현재 재료: {currentIngredient.DisplayName} ({currentIngredient.IngredientId})");
            builder.AppendLine($"요리 방식: {(session != null && session.Mode == CookingMode.Recipe ? "레시피 선택" : "재료 직접 선택")}");
            builder.AppendLine($"예상 레시피: {(recipe != null ? $"{recipe.DisplayName} ({recipe.RecipeId})" : "없음")}");

            return builder.ToString();
        }

        private string BuildPreparationButtonLabel(
            RecipeSO recipe,
            IngredientSO ingredient,
            IngredientPreparationOption option)
        {
            if (option == null)
                return "손질 없음";

            string prefix = option.CausesDisgusting || option.AddsPoison ? "[위험]" : "[선택]";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"{prefix} {option.DisplayName}");

            if (option.Method != null)
                builder.AppendLine($"손질법 ID: {option.Method.MethodId}");

            builder.Append(BuildPreparationEffectText(option));
            return builder.ToString();
        }

        private string BuildPreparationProgressText(RecipeSO recipe)
        {
            CookingSession session = runner.Controller.CurrentSession;
            if (session == null)
                return "진행 중인 요리가 없습니다.";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < session.SelectedIngredients.Count; i++)
            {
                IngredientSO ingredient = session.SelectedIngredients[i];
                PreparedIngredientState prepared = session.GetPreparedIngredient(ingredient);

                builder.Append($"- {ingredient.DisplayName}: ");
                if (prepared == null)
                {
                    builder.AppendLine("대기 중");
                    continue;
                }

                string methodName = prepared.Method != null ? prepared.Method.DisplayName : "손질 없음";
                string methodId = prepared.Method != null ? prepared.Method.MethodId : "none";
                builder.AppendLine($"{methodName} ({methodId})");

                string effects = BuildPreparedEffectText(prepared);
                if (string.IsNullOrWhiteSpace(effects) == false)
                    builder.AppendLine($"  {effects}");
            }

            return builder.ToString();
        }

        private string BuildPreparedWarnings(RecipeSO recipe)
        {
            CookingSession session = runner.Controller.CurrentSession;
            if (session == null)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < session.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = session.PreparedIngredients[i];
                if (prepared == null)
                    continue;

                if (prepared.CausesDisgusting)
                    builder.AppendLine($"- {prepared.Ingredient.DisplayName}: 이 손질은 괴식을 만듭니다.");

                if (prepared.AddsPoison)
                    builder.AppendLine($"- {prepared.Ingredient.DisplayName}: 이 손질은 독을 추가합니다.");

                if (string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                    builder.AppendLine($"- {prepared.Ingredient.DisplayName}: 이름 수식어 \"{prepared.ResultNameModifier}\"가 붙습니다.");

            }

            return builder.ToString();
        }

        private string BuildResultText(DishResult result)
        {
            if (result == null)
                return "요리 결과가 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(result.DisplayName);
            builder.AppendLine($"품질: {BuildQualityText(result.Quality)}");
            builder.AppendLine($"괴식: {(result.IsDisgusting ? "예" : "아니오")}");
            builder.AppendLine($"레시피 매칭: {(result.IsRecipeMatched ? "성공" : "실패")}");
            builder.AppendLine($"레시피: {(result.BaseRecipe != null ? $"{result.BaseRecipe.DisplayName} ({result.RecipeId})" : "없음")}");
            builder.AppendLine($"카테고리: {(result.Category != null ? $"{result.Category.DisplayName} ({result.CategoryId})" : "없음")}");
            builder.AppendLine($"태그 ID: {result.BuildTagText()}");
            return builder.ToString();
        }

        private string BuildResultPreparationText(DishResult result)
        {
            if (result == null || result.PreparedIngredients.Count == 0)
                return "손질 이력이 없습니다.";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.PreparedIngredients.Count; i++)
            {
                PreparedIngredientState prepared = result.PreparedIngredients[i];
                if (prepared == null)
                    continue;

                builder.AppendLine($"- {prepared.Ingredient.DisplayName}");
                builder.AppendLine($"  손질: {(prepared.Method != null ? $"{prepared.Method.DisplayName} ({prepared.Method.MethodId})" : "없음")}");

                string effects = BuildPreparedEffectText(prepared);
                if (string.IsNullOrWhiteSpace(effects) == false)
                    builder.AppendLine($"  효과: {effects}");
            }

            return builder.ToString();
        }

        private static string BuildReasonText(DishResult result)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Reasons.Count; i++)
                builder.AppendLine($"- {result.Reasons[i]}");

            return builder.ToString();
        }

        private string BuildNpcMatchText(DishResult result)
        {
            if (result == null)
                return "요리 결과가 없어 NPC 요청과 비교할 수 없습니다.";

            if (npcRunner == null)
                npcRunner = FindFirstObjectByType<NpcConversationRunner>();

            if (npcRunner == null)
                return "현재 씬에서 NpcConversationRunner를 찾지 못했습니다.\nNPC 대화 UI와 연결하면 현재 캐릭터의 요청 일치도를 표시할 수 있습니다.";

            if (CookingNpcDishAdapter.TryBuildMatchReport(npcRunner, result, out NpcDishMatchReport report) == false)
                return "현재 NPC 주문이 아직 준비되지 않았습니다.\nNPC 대화에서 요리 단계까지 진행한 뒤 다시 확인하세요.";

            int percent = Mathf.RoundToInt(report.MatchRatio * 100f);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"현재 NPC: {ValueOrNone(report.Order.NpcId)}");
            builder.AppendLine($"판정 예상: {BuildNpcResultText(report.Evaluation.Result)}");
            builder.AppendLine($"요청 일치도: {report.MatchScore}/{report.MaxMatchScore} ({percent}%)");
            builder.AppendLine($"레시피: {BuildMatchStateText(report.RecipeMatches)}  목표 {ValueOrNone(report.Order.CorrectRecipeId)} / 제출 {ValueOrNone(report.Dish.RecipeId)}");
            builder.AppendLine($"분류: {BuildMatchStateText(report.FoodTypeMatches)}  목표 {BuildStringListText(report.Order.AllowedFoodTypes)} / 제출 {ValueOrNone(report.Dish.FoodType)}");
            builder.AppendLine($"필수 태그: 맞음 {BuildStringListText(report.MatchedRequiredTags)} / 부족 {BuildStringListText(report.MissingRequiredTags)}");
            builder.AppendLine($"선호 태그: 맞음 {BuildStringListText(report.MatchedPreferredTags)} / 남음 {BuildStringListText(report.MissingPreferredTags)}");

            if (report.MatchedAvoidTags.Count > 0)
                builder.AppendLine($"회피 태그 감지: {BuildStringListText(report.MatchedAvoidTags)}");

            if (report.Dish.IsDisgusting || report.MatchedDisgustingTags.Count > 0)
            {
                string tags = report.MatchedDisgustingTags.Count > 0
                    ? BuildStringListText(report.MatchedDisgustingTags)
                    : "요리 결과가 괴식으로 표시됨";
                builder.AppendLine($"괴식 위험: {tags}");
            }

            builder.AppendLine($"판정 사유: {report.Evaluation.Reason}");
            return builder.ToString();
        }

        private static string BuildNpcResultText(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return "완전 일치";
                case NpcConversationResult.Correct:
                    return "요청 충족";
                case NpcConversationResult.Similar:
                    return "일부 일치";
                case NpcConversationResult.Disgusting:
                case NpcConversationResult.Wrong:
                default:
                    return "불일치";
            }
        }

        private static string BuildMatchStateText(bool isMatched)
        {
            return isMatched ? "일치" : "불일치";
        }

        private static string BuildStringListText(IReadOnlyList<string> values)
        {
            return values != null && values.Count > 0 ? string.Join("|", values) : "없음";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "없음" : value;
        }

        private static string BuildPreparationEffectText(IngredientPreparationOption option)
        {
            if (option == null)
                return "효과: 없음";

            List<string> facts = new List<string>();
            if (option.QualityDelta != 0)
                facts.Add($"품질 {option.QualityDelta:+#;-#;0}");
            if (option.AddTags.Count > 0)
                facts.Add($"추가 태그 {BuildTagDisplayText(option.AddTags)}");
            if (option.RemoveTags.Count > 0)
                facts.Add($"제거 태그 {BuildTagDisplayText(option.RemoveTags)}");
            if (string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
                facts.Add($"이름 \"{option.ResultNameModifier}\"");
            if (option.CausesDisgusting)
                facts.Add("괴식");
            if (option.AddsPoison)
                facts.Add("독");

            return facts.Count > 0 ? $"효과: {string.Join(" / ", facts)}" : "효과: 없음";
        }

        private static string BuildPreparedEffectText(PreparedIngredientState prepared)
        {
            if (prepared == null)
                return string.Empty;

            List<string> facts = new List<string>();
            if (prepared.QualityDelta != 0)
                facts.Add($"품질 {prepared.QualityDelta:+#;-#;0}");
            if (prepared.AddedTags.Count > 0)
                facts.Add($"추가 {BuildTagDisplayText(prepared.AddedTags)}");
            if (prepared.RemoveTags.Count > 0)
                facts.Add($"제거 {BuildTagDisplayText(prepared.RemoveTags)}");
            if (string.IsNullOrWhiteSpace(prepared.ResultNameModifier) == false)
                facts.Add($"이름 \"{prepared.ResultNameModifier}\"");
            if (prepared.CausesDisgusting)
                facts.Add("괴식");
            if (prepared.AddsPoison)
                facts.Add("독");

            return string.Join(" / ", facts);
        }

        private static string BuildTagDisplayText(IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return "없음";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < tags.Count; i++)
            {
                FoodTagSO tag = tags[i];
                if (tag == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(tag.DisplayName);
                if (string.IsNullOrWhiteSpace(tag.TagId) == false)
                    builder.Append($"({tag.TagId})");
            }

            return builder.Length > 0 ? builder.ToString() : "없음";
        }

        private static string BuildQualityText(DishQuality quality)
        {
            switch (quality)
            {
                case DishQuality.Perfect:
                    return "완벽";
                case DishQuality.Altered:
                    return "변형";
                case DishQuality.Disgusting:
                    return "괴식";
                case DishQuality.Normal:
                default:
                    return "일반";
            }
        }
    }
}