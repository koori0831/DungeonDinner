using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;

namespace Work.Cook.Code.Editor
{
    public sealed class CookingCatalogValidatorWindow : EditorWindow
    {
        private CookingDataCatalogSO _catalog;
        private CookingDataValidationReport _report;
        private Vector2 _scroll;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo;

        [MenuItem("Tools/Dungeon Dinner/Cooking Catalog Validator")]
        private static void Open()
        {
            GetWindow<CookingCatalogValidatorWindow>("Cooking Validator").Show();
        }

        private void OnEnable()
        {
            if (_catalog == null)
                _catalog = CookingEditorCatalogUtility.FindFirstCatalog();
            RunValidation();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Cooking Catalog Validator", EditorStyles.boldLabel);
            _catalog = (CookingDataCatalogSO)EditorGUILayout.ObjectField("Catalog", _catalog, typeof(CookingDataCatalogSO), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 검사"))
                    RunValidation();
                if (GUILayout.Button("누락 ID 일괄 생성") && _catalog != null)
                {
                    CookingDataIdGenerator.GenerateMissingIds(_catalog);
                    RunValidation();
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _showErrors = GUILayout.Toggle(_showErrors, "오류");
                _showWarnings = GUILayout.Toggle(_showWarnings, "경고");
                _showInfo = GUILayout.Toggle(_showInfo, "정보");
            }

            if (_report == null)
                return;
            EditorGUILayout.HelpBox(
                $"오류 {_report.ErrorCount} · 경고 {_report.WarningCount}",
                _report.HasErrors ? MessageType.Error : _report.WarningCount > 0 ? MessageType.Warning : MessageType.Info);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _report.Issues.Count; i++)
            {
                CookingDataValidationIssue issue = _report.Issues[i];
                if (ShouldShow(issue.Severity) == false)
                    continue;
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField($"[{issue.Severity}] {issue.Code}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);
                    if (issue.Asset != null && GUILayout.Button($"선택: {issue.Asset.name}"))
                    {
                        Selection.activeObject = issue.Asset;
                        EditorGUIUtility.PingObject(issue.Asset);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunValidation()
        {
            _report = _catalog != null
                ? new CookingDataValidationService().ValidateCatalog(_catalog)
                : null;
            Repaint();
        }

        private bool ShouldShow(CookingDataValidationSeverity severity)
        {
            return severity == CookingDataValidationSeverity.Error ? _showErrors
                : severity == CookingDataValidationSeverity.Warning ? _showWarnings
                : _showInfo;
        }
    }

    internal static class CookingDataIdGenerator
    {
        public static int GenerateMissingIds(CookingDataCatalogSO catalog)
        {
            if (catalog == null)
                return 0;
            int changed = 0;
            for (int i = 0; i < catalog.Recipes.Count; i++)
                changed += GenerateMissingIdsForRecipe(catalog.Recipes[i]);
            for (int i = 0; i < catalog.Ingredients.Count; i++)
                changed += GenerateMissingIdsForIngredient(catalog.Ingredients[i]);
            if (changed > 0)
                AssetDatabase.SaveAssets();
            return changed;
        }

        public static int GenerateMissingIdsForRecipe(RecipeSO recipe)
        {
            if (recipe == null)
                return 0;
            int changed = 0;
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null)
                    continue;
                if (string.IsNullOrWhiteSpace(requirement.RequirementId) == false)
                {
                    used.Add(requirement.RequirementId);
                    continue;
                }
                if (changed == 0)
                    Undo.RecordObject(recipe, "Generate cooking slot IDs");
                string id = GenerateUnique("slot_", used);
                requirement.EditorSetRequirementId(id);
                used.Add(id);
                changed++;
            }
            if (changed > 0)
                EditorUtility.SetDirty(recipe);
            return changed;
        }

        private static int GenerateMissingIdsForIngredient(IngredientSO ingredient)
        {
            if (ingredient == null)
                return 0;
            int changed = 0;
            HashSet<string> usedOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ingredient.PreparationOptions.Count; i++)
            {
                IngredientPreparationOption option = ingredient.PreparationOptions[i];
                if (option == null)
                    continue;
                if (string.IsNullOrWhiteSpace(option.PreparationOptionId))
                {
                    if (changed == 0)
                        Undo.RecordObject(ingredient, "Generate cooking option/effect IDs");
                    string baseId = SanitizeId(option.Method != null ? option.Method.MethodId : "option");
                    string id = baseId;
                    while (usedOptions.Contains(id))
                        id = baseId + "_" + Guid.NewGuid().ToString("N").Substring(0, 4);
                    option.EditorSetPreparationOptionId(id);
                    changed++;
                }
                usedOptions.Add(option.PreparationOptionId);

                for (int ruleIndex = 0; ruleIndex < option.MiniGameFeedbackRules.Count; ruleIndex++)
                {
                    CookingMiniGameFeedbackRule rule = option.MiniGameFeedbackRules[ruleIndex];
                    if (rule == null || rule.HasIdentityEffect == false || string.IsNullOrWhiteSpace(rule.VariantEffectId) == false)
                        continue;
                    if (changed == 0)
                        Undo.RecordObject(ingredient, "Generate cooking option/effect IDs");
                    rule.EditorSetVariantEffectId("effect_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                    changed++;
                }
            }
            if (changed > 0)
                EditorUtility.SetDirty(ingredient);
            return changed;
        }

        private static string GenerateUnique(string prefix, ISet<string> used)
        {
            string id;
            do
                id = prefix + Guid.NewGuid().ToString("N").Substring(0, 8);
            while (used.Contains(id));
            return id;
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "option";
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                builder.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-' ? c : '_');
            }
            return builder.ToString();
        }
    }

    internal static class CookingEditorCatalogUtility
    {
        public static CookingDataCatalogSO FindFirstCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:CookingDataCatalogSO");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        public static CookingDataCatalogSO FindCatalogContaining(RecipeSO recipe)
        {
            string[] guids = AssetDatabase.FindAssets("t:CookingDataCatalogSO");
            for (int i = 0; i < guids.Length; i++)
            {
                CookingDataCatalogSO catalog = AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (catalog != null && Contains(catalog.Recipes, recipe))
                    return catalog;
            }
            return null;
        }

        private static bool Contains(IReadOnlyList<RecipeSO> recipes, RecipeSO recipe)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                if (recipes[i] == recipe)
                    return true;
            }
            return false;
        }
    }

    public sealed class CookingCatalogBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            string[] guids = AssetDatabase.FindAssets("t:CookingDataCatalogSO");
            List<string> errors = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                CookingDataCatalogSO catalog = AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                CookingDataValidationReport validation = new CookingDataValidationService().ValidateCatalog(catalog);
                for (int issueIndex = 0; issueIndex < validation.Issues.Count; issueIndex++)
                {
                    CookingDataValidationIssue issue = validation.Issues[issueIndex];
                    if (issue.Severity == CookingDataValidationSeverity.Error)
                        errors.Add(issue.ToString());
                }
            }
            if (errors.Count > 0)
                throw new BuildFailedException("Cooking catalog validation failed:\n" + string.Join("\n", errors));
        }
    }
}
