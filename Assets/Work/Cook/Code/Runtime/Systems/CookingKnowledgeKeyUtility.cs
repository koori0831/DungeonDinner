using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Systems
{
    /// <summary>
    /// 요리 지식 저장에 사용하는 ID와 표시 문구 생성 유틸리티
    /// </summary>
    internal static class CookingKnowledgeKeyUtility
    {
        public static string BuildPreparationKey(IngredientSO ingredient, IngredientPreparationOption option)
        {
            if (ingredient == null || option == null)
                return string.Empty;

            string ingredientId = GetIngredientId(ingredient);
            string methodId = option.Method != null ? option.Method.MethodId : option.DisplayName;
            methodId = NormalizeId(methodId);

            if (string.IsNullOrWhiteSpace(ingredientId) || string.IsNullOrWhiteSpace(methodId))
                return string.Empty;

            return $"{ingredientId}:{methodId}";
        }

        public static string BuildPreparationUpdateBody(IngredientSO ingredient, IngredientPreparationOption option)
        {
            string ingredientName = ingredient != null ? ingredient.DisplayName : "알 수 없는 재료";
            string methodName = "손질 없음";

            if (option != null)
            {
                methodName = string.IsNullOrWhiteSpace(option.DisplayName) == false
                    ? option.DisplayName
                    : option.Method != null ? option.Method.DisplayName : "알 수 없는 손질";
            }

            return $"{ingredientName} - {methodName}";
        }

        public static string GetRecipeId(RecipeSO recipe)
        {
            if (recipe == null)
                return string.Empty;

            string recipeId = NormalizeId(recipe.RecipeId);
            return string.IsNullOrWhiteSpace(recipeId) ? NormalizeId(recipe.DisplayName) : recipeId;
        }

        public static string GetIngredientId(IngredientSO ingredient)
        {
            if (ingredient == null)
                return string.Empty;

            string ingredientId = NormalizeId(ingredient.IngredientId);
            return string.IsNullOrWhiteSpace(ingredientId) ? NormalizeId(ingredient.DisplayName) : ingredientId;
        }

        public static string GetTagId(FoodTagSO tag)
        {
            if (tag == null)
                return string.Empty;

            string tagId = NormalizeId(tag.TagId);
            return string.IsNullOrWhiteSpace(tagId) ? NormalizeId(tag.DisplayName) : tagId;
        }

        public static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
