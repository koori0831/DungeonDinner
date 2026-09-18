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
        private void AddMissingPerfectRulesFromRequirements()
        {
            for (int i = 0; i < _recipeDraft.RequiredIngredients.Count; i++)
            {
                IngredientSO ingredient = _recipeDraft.RequiredIngredients[i].Ingredient;
                if (ingredient == null || HasPerfectRule(ingredient))
                    continue;

                _recipeDraft.PerfectRules.Add(new PerfectRuleDraft { Ingredient = ingredient });
            }
        }

        private bool HasPerfectRule(IngredientSO ingredient)
        {
            for (int i = 0; i < _recipeDraft.PerfectRules.Count; i++)
            {
                if (_recipeDraft.PerfectRules[i].Ingredient == ingredient)
                    return true;
            }

            return false;
        }

        private bool IsSelectedAssetInCatalog()
        {
            if (_selectedAsset == null || catalog == null)
                return false;

            IReadOnlyList<UnityEngine.Object> values = GetCatalogValues(currentMode);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == _selectedAsset)
                    return true;
            }

            return false;
        }

        private IReadOnlyList<UnityEngine.Object> GetCatalogValues(DataMode mode)
        {
            List<UnityEngine.Object> values = new List<UnityEngine.Object>();
            if (catalog == null)
                return values;

            switch (mode)
            {
                case DataMode.Recipe:
                    AddObjects(values, catalog.Recipes);
                    break;
                case DataMode.Category:
                    AddObjects(values, catalog.Categories);
                    break;
                case DataMode.IngredientCategory:
                    AddObjects(values, catalog.IngredientCategories);
                    break;
                case DataMode.Tag:
                    AddObjects(values, catalog.Tags);
                    break;
                case DataMode.PreparationMethod:
                    AddObjects(values, catalog.PreparationMethods);
                    break;
                case DataMode.Ingredient:
                    AddObjects(values, catalog.Ingredients);
                    break;
            }

            return values;
        }

        private static void AddObjects<T>(List<UnityEngine.Object> target, IReadOnlyList<T> source)
            where T : UnityEngine.Object
        {
            for (int i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        private List<string> BuildRecipeWarnings()
        {
            List<string> warnings = new List<string>();
            if (_recipeDraft == null)
                return warnings;

            if (string.IsNullOrWhiteSpace(_recipeDraft.RecipeId))
                warnings.Add("레시피 ID가 비어 있습니다.");

            if (_recipeDraft.Category == null)
                warnings.Add("카테고리가 지정되지 않았습니다.");

            if (_recipeDraft.RequiredIngredients.Count == 0)
                warnings.Add("필요 재료가 없으면 직접 재료 선택으로 이 레시피를 매칭할 수 없습니다.");

            HashSet<IngredientSO> requiredIngredients = new HashSet<IngredientSO>();
            for (int i = 0; i < _recipeDraft.RequiredIngredients.Count; i++)
            {
                IngredientRequirementDraft requirement = _recipeDraft.RequiredIngredients[i];
                IngredientSO ingredient = requirement.Ingredient;
                bool hasAnyCondition = ingredient != null
                                       || requirement.IngredientCategory != null
                                       || requirement.RequiredTags.Count > 0
                                       || requirement.SimpleAlternatives.Count > 0
                                       || requirement.Alternatives.Count > 0;

                if (hasAnyCondition == false)
                {
                    warnings.Add($"필요 재료 {i + 1}번에 재료/재료군/태그/대체재료 조건이 없습니다.");
                    continue;
                }

                if (requirement.MaxCount > 0 && requirement.MaxCount < requirement.MinCount)
                    warnings.Add($"필요 재료 {i + 1}번의 최대 개수가 최소 개수보다 작습니다.");

                if (ingredient != null && requiredIngredients.Add(ingredient) == false)
                    warnings.Add($"중복된 필요 재료가 있습니다: {ingredient.DisplayName}");
            }

            return warnings;
        }

        private List<string> BuildCategoryWarnings()
        {
            List<string> warnings = new List<string>();
            if (_categoryDraft != null && string.IsNullOrWhiteSpace(_categoryDraft.CategoryId))
                warnings.Add("카테고리 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildIngredientCategoryWarnings()
        {
            List<string> warnings = new List<string>();
            if (_ingredientCategoryDraft != null && string.IsNullOrWhiteSpace(_ingredientCategoryDraft.CategoryId))
                warnings.Add("재료군 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildTagWarnings()
        {
            List<string> warnings = new List<string>();
            if (_tagDraft != null && string.IsNullOrWhiteSpace(_tagDraft.TagId))
                warnings.Add("태그 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildMethodWarnings()
        {
            List<string> warnings = new List<string>();
            if (_methodDraft != null && string.IsNullOrWhiteSpace(_methodDraft.MethodId))
                warnings.Add("손질법 ID가 비어 있습니다.");

            return warnings;
        }

        private List<string> BuildIngredientWarnings()
        {
            List<string> warnings = new List<string>();
            if (_ingredientDraft == null)
                return warnings;

            if (string.IsNullOrWhiteSpace(_ingredientDraft.IngredientId))
                warnings.Add("재료 ID가 비어 있습니다.");

            if (_ingredientDraft.PreparationOptions.Count != 3)
                warnings.Add($"현재 손질 선택지가 {_ingredientDraft.PreparationOptions.Count}개입니다. 플레이어에게 3가지를 보여주려면 3개를 등록하세요.");

            HashSet<PreparationMethodSO> methods = new HashSet<PreparationMethodSO>();
            for (int i = 0; i < _ingredientDraft.PreparationOptions.Count; i++)
            {
                PreparationOptionDraft option = _ingredientDraft.PreparationOptions[i];
                if (option.Method == null)
                {
                    warnings.Add($"손질 선택지 {i + 1}번의 손질법이 비어 있습니다.");
                    continue;
                }

                if (methods.Add(option.Method) == false)
                    warnings.Add($"중복된 손질법이 있습니다: {option.Method.DisplayName}");
            }

            return warnings;
        }
    }
}