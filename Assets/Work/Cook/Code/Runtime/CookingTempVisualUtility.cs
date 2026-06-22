using UnityEngine;
using Work.Cook.Code.Data;
using Work.Items.Code;

namespace Work.Cook.Code.Runtime
{
    /// <summary>
    /// 요리/재료 Temp 시각 요소 해석 유틸리티
    /// </summary>
    public static class CookingTempVisualUtility
    {
        /// <summary>
        /// 재료 표시용 Temp 아이콘 반환
        /// </summary>
        /// <param name="ingredient">표시할 재료</param>
        /// <returns>표시용 아이콘</returns>
        public static Sprite ResolveIngredientIcon(IngredientSO ingredient)
        {
            string key = ingredient != null ? ingredient.IngredientId : "missing_ingredient";
            string label = ingredient != null ? ingredient.DisplayName : "Missing Ingredient";
            return ItemIconUtility.GetOrCreateTempIcon($"ingredient_{key}", label, 0.42f, 0.62f, 0.32f);
        }

        /// <summary>
        /// 요리 결과 표시용 Temp 아이콘 반환
        /// </summary>
        /// <param name="result">표시할 요리 결과</param>
        /// <returns>표시용 아이콘</returns>
        public static Sprite ResolveDishIcon(DishResult result)
        {
            if (result == null)
            {
                return ItemIconUtility.GetOrCreateTempIcon("dish_missing", "Missing Dish", 0.62f, 0.42f, 0.28f);
            }

            string key = string.IsNullOrWhiteSpace(result.RecipeId) == false ? result.RecipeId : result.DisplayName;
            if (result.IsDisgusting == true)
            {
                key = $"disgusting_{key}";
            }

            return ItemIconUtility.GetOrCreateTempIcon($"dish_{key}", result.DisplayName, 0.78f, 0.54f, 0.32f);
        }

        /// <summary>
        /// 재료 3D 모델 프리팹 반환
        /// </summary>
        /// <param name="ingredient">표시할 재료</param>
        /// <returns>재료 모델 프리팹</returns>
        public static GameObject ResolveIngredientModelPrefab(IngredientSO ingredient)
        {
            return ingredient != null ? ingredient.ModelPrefab : null;
        }
    }
}
