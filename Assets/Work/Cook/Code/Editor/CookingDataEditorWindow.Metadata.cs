using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Editor
{
    public sealed partial class CookingDataEditorWindow
    {
        private static bool MatchesSearch(UnityEngine.Object asset, string keyword)
        {
            if (asset == null)
                return false;

            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            string normalized = keyword.Trim();
            return Contains(GetAssetId(asset), normalized)
                   || Contains(GetDisplayName(asset), normalized)
                   || Contains(GetListMeta(asset), normalized)
                   || Contains(GetAssetPath(asset), normalized);
        }

        private static bool Contains(string source, string value)
        {
            return string.IsNullOrEmpty(source) == false
                   && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CompareAssets(UnityEngine.Object left, UnityEngine.Object right)
        {
            return string.Compare(GetDisplayName(left), GetDisplayName(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetDisplayName(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case RecipeSO recipe:
                    return string.IsNullOrWhiteSpace(recipe.DisplayName) ? "(이름 없음)" : recipe.DisplayName;
                case FoodCategorySO category:
                    return string.IsNullOrWhiteSpace(category.DisplayName) ? "(이름 없음)" : category.DisplayName;
                case IngredientCategorySO ingredientCategory:
                    return string.IsNullOrWhiteSpace(ingredientCategory.DisplayName) ? "(이름 없음)" : ingredientCategory.DisplayName;
                case FoodTagSO tag:
                    return string.IsNullOrWhiteSpace(tag.DisplayName) ? "(이름 없음)" : tag.DisplayName;
                case PreparationMethodSO method:
                    return string.IsNullOrWhiteSpace(method.DisplayName) ? "(이름 없음)" : method.DisplayName;
                case IngredientSO ingredient:
                    return string.IsNullOrWhiteSpace(ingredient.DisplayName) ? "(이름 없음)" : ingredient.DisplayName;
                default:
                    return asset != null ? asset.name : "(없음)";
            }
        }

        private static string GetAssetId(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case RecipeSO recipe:
                    return recipe.RecipeId;
                case FoodCategorySO category:
                    return category.CategoryId;
                case IngredientCategorySO ingredientCategory:
                    return ingredientCategory.CategoryId;
                case FoodTagSO tag:
                    return tag.TagId;
                case PreparationMethodSO method:
                    return method.MethodId;
                case IngredientSO ingredient:
                    return ingredient.IngredientId;
                default:
                    return string.Empty;
            }
        }

        private static string GetListMeta(UnityEngine.Object asset)
        {
            switch (asset)
            {
                case RecipeSO recipe:
                    return $"{recipe.RecipeId}  |  {(recipe.Category != null ? recipe.Category.DisplayName : "카테고리 없음")}";
                case FoodCategorySO category:
                    return $"{category.CategoryId}  |  음식 분류";
                case IngredientCategorySO ingredientCategory:
                    return $"{ingredientCategory.CategoryId}  |  재료군";
                case FoodTagSO tag:
                    return $"{tag.TagId}  |  맛/속성 태그";
                case PreparationMethodSO method:
                    return $"{method.MethodId}  |  재료 손질 선택지";
                case IngredientSO ingredient:
                    return $"{ingredient.IngredientId}  |  태그 {ingredient.BaseTags.Count} / 손질법 {ingredient.PreparationOptions.Count}";
                default:
                    return GetAssetPath(asset);
            }
        }

        private static string GetAssetPath(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(path) ? "저장 경로 없음" : path;
        }

        private static string GetDesiredAssetName(UnityEngine.Object asset)
        {
            string sourceName = GetAssetId(asset);
            if (string.IsNullOrWhiteSpace(sourceName))
                sourceName = GetDisplayName(asset);

            if (string.IsNullOrWhiteSpace(sourceName)
                || string.Equals(sourceName, "(이름 없음)", StringComparison.Ordinal))
            {
                sourceName = asset != null ? asset.name : string.Empty;
            }

            return SanitizeAssetName(sourceName);
        }

        private static string SanitizeAssetName(string value)
        {
            string safeName = string.IsNullOrWhiteSpace(value) ? "CookingData" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(invalid, '_');

            return string.IsNullOrWhiteSpace(safeName) ? "CookingData" : safeName;
        }

        private static string GetSearchFilter(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return "t:RecipeSO";
                case DataMode.Category:
                    return "t:FoodCategorySO";
                case DataMode.IngredientCategory:
                    return "t:IngredientCategorySO";
                case DataMode.Tag:
                    return "t:FoodTagSO";
                case DataMode.PreparationMethod:
                    return "t:PreparationMethodSO";
                case DataMode.Ingredient:
                    return "t:IngredientSO";
                default:
                    return "t:ScriptableObject";
            }
        }

        private static Type GetAssetType(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return typeof(RecipeSO);
                case DataMode.Category:
                    return typeof(FoodCategorySO);
                case DataMode.IngredientCategory:
                    return typeof(IngredientCategorySO);
                case DataMode.Tag:
                    return typeof(FoodTagSO);
                case DataMode.PreparationMethod:
                    return typeof(PreparationMethodSO);
                case DataMode.Ingredient:
                    return typeof(IngredientSO);
                default:
                    return typeof(ScriptableObject);
            }
        }

        private static string GetCatalogPropertyName(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return "recipes";
                case DataMode.Category:
                    return "categories";
                case DataMode.IngredientCategory:
                    return "ingredientCategories";
                case DataMode.Tag:
                    return "tags";
                case DataMode.PreparationMethod:
                    return "preparationMethods";
                case DataMode.Ingredient:
                    return "ingredients";
                default:
                    return string.Empty;
            }
        }

        private static string GetDefaultFileName(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return "NewRecipe";
                case DataMode.Category:
                    return "NewCategory";
                case DataMode.IngredientCategory:
                    return "NewIngredientCategory";
                case DataMode.Tag:
                    return "NewTag";
                case DataMode.PreparationMethod:
                    return "NewPreparationMethod";
                case DataMode.Ingredient:
                    return "NewIngredient";
                default:
                    return "NewCookingData";
            }
        }

        private static string GetModeKoreanName(DataMode mode)
        {
            if (mode == DataMode.IngredientCategory)
                return "재료군";

            switch (mode)
            {
                case DataMode.Recipe:
                    return "레시피";
                case DataMode.Category:
                    return "카테고리";
                case DataMode.Tag:
                    return "태그";
                case DataMode.PreparationMethod:
                    return "손질법";
                case DataMode.Ingredient:
                    return "재료";
                default:
                    return "데이터";
            }
        }

        private static string GetHelpText(DataMode mode)
        {
            if (mode == DataMode.IngredientCategory)
                return "재료군은 고기, 채소, 향신료처럼 레시피 슬롯에서 대체 가능한 큰 재료 묶음입니다.";

            switch (mode)
            {
                case DataMode.Recipe:
                    return "레시피는 완성 음식의 기준입니다. 필요 재료 슬롯에서 재료, 재료군, 태그, 필수 손질법, 개수 조건을 연결합니다.";
                case DataMode.Category:
                    return "카테고리는 음식의 큰 분류입니다. NPC 판정에서 FoodType/Category 기준으로 사용하기 좋습니다.";
                case DataMode.Tag:
                    return "태그는 맛, 온도, 식감, 독 같은 속성입니다. 재료 손질법은 요리에 태그를 추가하거나 제거할 수 있습니다.";
                case DataMode.PreparationMethod:
                    return "손질법은 재료가 제공하는 선택지의 기본 이름입니다. 실제 효과는 재료 탭에서 손질법별로 따로 설정합니다.";
                case DataMode.Ingredient:
                    return "재료는 기본 태그와 손질법별 효과를 가집니다. 각 손질 선택지에서 추가 태그, 제거 태그, 독/괴식 여부, 결과 이름 수식어를 설정하세요.";
                default:
                    return string.Empty;
            }
        }
    }
}