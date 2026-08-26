using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    internal static class CookingIngredientSelectionSourceResolver
    {
        public static ICookingIngredientSource Resolve(
            MonoBehaviour owner,
            MonoBehaviour configuredSource,
            ICookingIngredientSource runtimeSource,
            bool searchParents,
            bool searchChildren)
        {
            if (runtimeSource != null)
                return runtimeSource;

            ICookingIngredientSource source = configuredSource as ICookingIngredientSource;
            if (source != null)
                return source;

            if (owner == null)
                return null;

            if (searchParents == true)
            {
                source = FindIngredientSource(owner.GetComponentsInParent<MonoBehaviour>(true));
                if (source != null)
                    return source;
            }

            return searchChildren == true
                ? FindIngredientSource(owner.GetComponentsInChildren<MonoBehaviour>(true))
                : null;
        }

        private static ICookingIngredientSource FindIngredientSource(IReadOnlyList<MonoBehaviour> behaviours)
        {
            if (behaviours == null)
                return null;

            for (int i = 0; i < behaviours.Count; i++)
            {
                if (behaviours[i] is ICookingIngredientSource source)
                    return source;
            }

            return null;
        }
    }

    internal static class CookingIngredientSelectionRules
    {
        public static bool ContainsIngredient(IReadOnlyList<IngredientSO> ingredients, IngredientSO ingredient)
        {
            if (ingredients == null || ingredient == null)
                return false;

            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] == ingredient)
                    return true;
            }

            return false;
        }

        public static int CountIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null)
                return 0;

            int count = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] != null)
                    count++;
            }

            return count;
        }

        public static bool IsSelectionCountValid(
            IReadOnlyList<IngredientSO> selectedIngredients,
            int minSelectedIngredients,
            int maxSelectedIngredients)
        {
            int count = CountIngredients(selectedIngredients);
            return count >= minSelectedIngredients
                   && (maxSelectedIngredients <= 0 || count <= maxSelectedIngredients);
        }

        public static bool CanSelectMore(IReadOnlyList<IngredientSO> selectedIngredients, int maxSelectedIngredients)
        {
            return maxSelectedIngredients <= 0 || CountIngredients(selectedIngredients) < maxSelectedIngredients;
        }
    }

    internal static class CookingIngredientSearchMatcher
    {
        public static bool Matches(IngredientSO ingredient, string searchQuery)
        {
            if (ingredient == null)
                return false;

            string query = NormalizeSearch(searchQuery);
            if (string.IsNullOrWhiteSpace(query) == true)
                return true;

            if (ContainsSearchText(ingredient.DisplayName, query) == true
                || ContainsSearchText(ingredient.Description, query) == true
                || ContainsTagSearchText(ingredient.BaseTags, query) == true
                || ContainsPreparationSearchText(ingredient.PreparationOptions, query) == true)
            {
                return true;
            }

            return false;
        }

        private static bool ContainsTagSearchText(IReadOnlyList<FoodTagSO> tags, string query)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                FoodTagSO tag = tags[i];
                if (tag != null
                    && (ContainsSearchText(tag.DisplayName, query) == true
                        || ContainsSearchText(tag.Description, query) == true))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsPreparationSearchText(
            IReadOnlyList<IngredientPreparationOption> options,
            string query)
        {
            if (options == null)
                return false;

            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                PreparationMethodSO method = option?.Method;
                if (option != null
                    && (ContainsSearchText(option.DisplayName, query) == true
                        || ContainsSearchText(option.Description, query) == true
                        || ContainsSearchText(method != null ? method.DisplayName : string.Empty, query) == true
                        || ContainsSearchText(method != null ? method.Description : string.Empty, query) == true))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSearchText(string value, string query)
        {
            if (string.IsNullOrWhiteSpace(value) == true || string.IsNullOrWhiteSpace(query) == true)
                return false;

            return NormalizeSearch(value).Contains(query);
        }

        private static string NormalizeSearch(string value)
        {
            return string.IsNullOrWhiteSpace(value) == true ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }

    internal static class CookingIngredientSelectionTextFormatter
    {
        public static string BuildAvailableIngredientLabel(
            IngredientSO ingredient,
            int availableQuantity,
            bool showIngredientQuantities)
        {
            string displayName = ingredient != null ? ingredient.DisplayName : string.Empty;
            if (showIngredientQuantities == false)
                return displayName;

            return $"{displayName} x{availableQuantity}";
        }

        public static string BuildIngredientDetailText(
            IngredientSO ingredient,
            int availableQuantity,
            string emptyIngredientDetailText)
        {
            if (ingredient == null)
                return emptyIngredientDetailText;

            StringBuilder builder = new StringBuilder();
            builder.Append(ingredient.DisplayName);
            builder.Append($"  x{availableQuantity}");

            if (string.IsNullOrWhiteSpace(ingredient.Description) == false)
                builder.AppendLine().Append(ingredient.Description);

            builder.AppendLine();
            builder.Append("태그: ");
            builder.Append(BuildTagListText(ingredient.BaseTags));
            builder.AppendLine();
            builder.Append("손질법: ");
            builder.Append(BuildPreparationOptionListText(ingredient.PreparationOptions));
            return builder.ToString();
        }

        public static string BuildEmptyAvailableText(
            string searchQuery,
            string emptyAvailableText,
            string emptySearchResultText)
        {
            return string.IsNullOrWhiteSpace(searchQuery) == true ? emptyAvailableText : emptySearchResultText;
        }

        public static string BuildSelectedSummaryText(
            string selectedTitleText,
            int selectedCount,
            int maxSelectedIngredients)
        {
            if (maxSelectedIngredients > 0)
                return $"{selectedTitleText} {selectedCount} / {maxSelectedIngredients}";

            return $"{selectedTitleText} {selectedCount}";
        }

        public static string BuildSelectionRuleText(
            int selectedCount,
            int minSelectedIngredients,
            int maxSelectedIngredients)
        {
            if (maxSelectedIngredients > 0)
            {
                if (selectedCount < minSelectedIngredients)
                    return $"최소 {minSelectedIngredients}개, 최대 {maxSelectedIngredients}개 선택";

                if (selectedCount >= maxSelectedIngredients)
                    return $"최대 {maxSelectedIngredients}개까지 선택했습니다.";

                return $"최소 {minSelectedIngredients}개, 최대 {maxSelectedIngredients}개 선택";
            }

            if (selectedCount < minSelectedIngredients)
                return $"최소 {minSelectedIngredients}개 이상 선택";

            return string.Empty;
        }

        private static string BuildTagListText(IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return "없음";

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    names.Add(tags[i].DisplayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : "없음";
        }

        private static string BuildPreparationOptionListText(IReadOnlyList<IngredientPreparationOption> options)
        {
            if (options == null || options.Count == 0)
                return "없음";

            List<string> names = new List<string>();
            for (int i = 0; i < options.Count; i++)
            {
                IngredientPreparationOption option = options[i];
                if (option != null && string.IsNullOrWhiteSpace(option.DisplayName) == false)
                    names.Add(option.DisplayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : "없음";
        }
    }
}
