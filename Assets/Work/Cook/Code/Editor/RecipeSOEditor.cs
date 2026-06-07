using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Editor
{
    [CustomEditor(typeof(RecipeSO))]
    public sealed class RecipeSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _recipeId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _category;
        private SerializedProperty _baseTags;
        private SerializedProperty _requiredIngredients;
        private SerializedProperty _perfectPreparationRules;

        private bool _basicFoldout = true;
        private bool _tagsFoldout = true;
        private bool _ingredientsFoldout = true;
        private bool _perfectRulesFoldout = true;
        private bool _previewFoldout = true;

        private void OnEnable()
        {
            _recipeId = serializedObject.FindProperty("recipeId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _category = serializedObject.FindProperty("category");
            _baseTags = serializedObject.FindProperty("baseTags");
            _requiredIngredients = serializedObject.FindProperty("requiredIngredients");
            _perfectPreparationRules = serializedObject.FindProperty("perfectPreparationRules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "레시피는 정해진 음식의 기준 데이터입니다. 직접 재료 선택 결과가 필요 재료와 매칭되면 이 레시피 음식으로 판정되고, 손질 결과에 따라 완벽/맛 변화/괴식 판정이 붙습니다.",
                MessageType.Info);

            DrawValidationMessages((RecipeSO)target);
            DrawBasicSection();
            DrawTagsSection();
            DrawIngredientsSection();
            DrawPerfectRulesSection();

            serializedObject.ApplyModifiedProperties();
            DrawPreviewSection((RecipeSO)target);
        }

        private void DrawBasicSection()
        {
            _basicFoldout = DrawSectionHeader("기본 정보", _basicFoldout);
            if (_basicFoldout == false)
                return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(_recipeId, new GUIContent("레시피 ID"));
            EditorGUILayout.PropertyField(_displayName, new GUIContent("표시 이름"));
            EditorGUILayout.PropertyField(_category, new GUIContent("카테고리"));
            EditorGUILayout.PropertyField(_description, new GUIContent("설명"));
            EditorGUILayout.EndVertical();
        }

        private void DrawTagsSection()
        {
            _tagsFoldout = DrawSectionHeader("기본 태그", _tagsFoldout);
            if (_tagsFoldout == false)
                return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox(
                "레시피 자체에 항상 붙는 맛/속성 태그입니다. 손질 과정에서 재료 태그가 추가되거나 제거될 수 있습니다.",
                MessageType.None);
            DrawObjectReferenceArray(_baseTags, typeof(FoodTagSO), "태그", "+ 태그 추가", "기본 태그가 없습니다.");
            EditorGUILayout.EndVertical();
        }

        private void DrawIngredientsSection()
        {
            _ingredientsFoldout = DrawSectionHeader("필요 재료", _ingredientsFoldout);
            if (_ingredientsFoldout == false)
                return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox(
                "직접 재료 선택으로 요리할 때 이 목록을 모두 만족하면 해당 레시피로 매칭됩니다. 매칭 실패는 괴식 판정으로 넘길 수 있습니다.",
                MessageType.Info);
            DrawRequiredIngredientList();
            EditorGUILayout.EndVertical();
        }

        private void DrawPerfectRulesSection()
        {
            _perfectRulesFoldout = DrawSectionHeader("정석 손질 조건", _perfectRulesFoldout);
            if (_perfectRulesFoldout == false)
                return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox(
                "각 재료가 어떤 손질법을 선택해야 완벽한 음식으로 볼지 정합니다. 재료 손질 옵션에서 괴식/독/품질 하락이 발생하면 이 조건을 만족해도 결과가 달라질 수 있습니다.",
                MessageType.Info);
            DrawPerfectRuleList();
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSection(RecipeSO recipe)
        {
            _previewFoldout = DrawSectionHeader("요약 미리보기", _previewFoldout);
            if (_previewFoldout == false)
                return;

            EditorGUILayout.BeginVertical("box");
            string summary = BuildRecipeSummary(recipe);
            EditorGUILayout.TextArea(summary, GUILayout.MinHeight(116f));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("요약 복사"))
                EditorGUIUtility.systemCopyBuffer = summary;

            if (GUILayout.Button("콘솔에 출력"))
                Debug.Log(summary, recipe);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawRequiredIngredientList()
        {
            if (_requiredIngredients.arraySize == 0)
                EditorGUILayout.HelpBox("필요 재료가 없으면 직접 재료 선택으로 이 레시피를 찾을 수 없습니다.", MessageType.Warning);

            for (int i = 0; i < _requiredIngredients.arraySize; i++)
            {
                SerializedProperty element = _requiredIngredients.GetArrayElementAtIndex(i);
                SerializedProperty ingredient = element.FindPropertyRelative("ingredient");
                SerializedProperty alternativeOptions = element.FindPropertyRelative("alternativeOptions");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"필요 재료 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(48f)))
                {
                    _requiredIngredients.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(ingredient, new GUIContent("기준 재료"));
                DrawAlternativeOptionArray(alternativeOptions);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 필요 재료 추가"))
                AddRequiredIngredientElement();
        }

        private void DrawPerfectRuleList()
        {
            if (_perfectPreparationRules.arraySize == 0)
                EditorGUILayout.HelpBox("정석 손질 조건이 없으면 완벽한 음식 판정 기준으로 사용할 정보가 없습니다.", MessageType.None);

            for (int i = 0; i < _perfectPreparationRules.arraySize; i++)
            {
                SerializedProperty element = _perfectPreparationRules.GetArrayElementAtIndex(i);
                SerializedProperty ingredient = element.FindPropertyRelative("ingredient");
                SerializedProperty perfectMethod = element.FindPropertyRelative("perfectMethod");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"정석 조건 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(48f)))
                {
                    _perfectPreparationRules.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(ingredient, new GUIContent("재료"));
                EditorGUILayout.PropertyField(perfectMethod, new GUIContent("정석 손질법"));
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 정석 손질 조건 추가"))
                AddPerfectRuleElement();
        }

        private static void DrawObjectReferenceArray(
            SerializedProperty property,
            System.Type objectType,
            string rowLabel,
            string addButtonLabel,
            string emptyMessage)
        {
            if (property == null || property.isArray == false)
                return;

            if (property.arraySize == 0)
                EditorGUILayout.HelpBox(emptyMessage, MessageType.None);

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{rowLabel} {i + 1}", GUILayout.Width(76f));
                element.objectReferenceValue = EditorGUILayout.ObjectField(element.objectReferenceValue, objectType, false);
                if (GUILayout.Button("삭제", GUILayout.Width(48f)))
                {
                    DeleteArrayElement(property, i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button(addButtonLabel))
            {
                int newIndex = property.arraySize;
                property.InsertArrayElementAtIndex(newIndex);
                property.GetArrayElementAtIndex(newIndex).objectReferenceValue = null;
            }
        }

        private static void DrawAlternativeOptionArray(SerializedProperty property)
        {
            if (property == null || property.isArray == false)
                return;

            EditorGUILayout.LabelField("대체 재료", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("대체 재료를 사용했을 때 완성 음식 이름 앞에 붙일 수식어를 지정합니다.", MessageType.None);

            if (property.arraySize == 0)
                EditorGUILayout.HelpBox("대체 재료가 없습니다.", MessageType.None);

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                SerializedProperty ingredient = element.FindPropertyRelative("ingredient");
                SerializedProperty resultNameModifier = element.FindPropertyRelative("resultNameModifier");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"대체 재료 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(48f)))
                {
                    property.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(ingredient, new GUIContent("재료"));
                EditorGUILayout.PropertyField(resultNameModifier, new GUIContent("이름 수식어"));
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 대체 재료 추가"))
            {
                int newIndex = property.arraySize;
                property.InsertArrayElementAtIndex(newIndex);
                SerializedProperty element = property.GetArrayElementAtIndex(newIndex);
                element.FindPropertyRelative("ingredient").objectReferenceValue = null;
                element.FindPropertyRelative("resultNameModifier").stringValue = string.Empty;
            }
        }

        private void AddRequiredIngredientElement()
        {
            int newIndex = _requiredIngredients.arraySize;
            _requiredIngredients.InsertArrayElementAtIndex(newIndex);

            SerializedProperty element = _requiredIngredients.GetArrayElementAtIndex(newIndex);
            element.FindPropertyRelative("ingredient").objectReferenceValue = null;

            SerializedProperty alternatives = element.FindPropertyRelative("alternatives");
            if (alternatives != null && alternatives.isArray)
                alternatives.ClearArray();

            SerializedProperty alternativeOptions = element.FindPropertyRelative("alternativeOptions");
            if (alternativeOptions != null && alternativeOptions.isArray)
                alternativeOptions.ClearArray();
        }

        private void AddPerfectRuleElement()
        {
            int newIndex = _perfectPreparationRules.arraySize;
            _perfectPreparationRules.InsertArrayElementAtIndex(newIndex);

            SerializedProperty element = _perfectPreparationRules.GetArrayElementAtIndex(newIndex);
            element.FindPropertyRelative("ingredient").objectReferenceValue = null;
            element.FindPropertyRelative("perfectMethod").objectReferenceValue = null;
        }

        private static void DeleteArrayElement(SerializedProperty property, int index)
        {
            int previousSize = property.arraySize;
            property.DeleteArrayElementAtIndex(index);
            if (property.arraySize == previousSize)
                property.DeleteArrayElementAtIndex(index);
        }

        private static bool DrawSectionHeader(string title, bool foldout)
        {
            EditorGUILayout.Space(4f);
            return EditorGUILayout.Foldout(foldout, title, true);
        }

        private static void DrawValidationMessages(RecipeSO recipe)
        {
            List<string> warnings = BuildValidationWarnings(recipe);
            if (warnings.Count == 0)
                return;

            for (int i = 0; i < warnings.Count; i++)
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
        }

        private static List<string> BuildValidationWarnings(RecipeSO recipe)
        {
            List<string> warnings = new List<string>();
            if (recipe == null)
                return warnings;

            if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                warnings.Add("레시피 ID가 비어 있습니다.");

            if (recipe.Category == null)
                warnings.Add("카테고리가 지정되지 않았습니다.");

            if (recipe.RequiredIngredients.Count == 0)
                warnings.Add("필요 재료가 없습니다. 직접 재료 선택으로 이 레시피를 매칭할 수 없습니다.");

            HashSet<IngredientSO> requiredIngredients = new HashSet<IngredientSO>();
            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null || requirement.Ingredient == null)
                {
                    warnings.Add($"필요 재료 {i + 1}번 칸이 비어 있습니다.");
                    continue;
                }

                if (requiredIngredients.Add(requirement.Ingredient) == false)
                    warnings.Add($"중복된 필요 재료가 있습니다: {requirement.Ingredient.DisplayName}");
            }

            for (int i = 0; i < recipe.PerfectPreparationRules.Count; i++)
            {
                RecipePreparationRule rule = recipe.PerfectPreparationRules[i];
                if (rule == null)
                    continue;

                if (rule.Ingredient == null || rule.PerfectMethod == null)
                {
                    warnings.Add($"정석 손질 조건 {i + 1}번이 완성되지 않았습니다.");
                    continue;
                }

                if (requiredIngredients.Contains(rule.Ingredient) == false)
                    warnings.Add($"정석 손질 조건의 재료가 필요 재료 목록에 없습니다: {rule.Ingredient.DisplayName}");
            }

            return warnings;
        }

        private static string BuildRecipeSummary(RecipeSO recipe)
        {
            if (recipe == null)
                return "레시피가 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"레시피: {recipe.DisplayName} ({recipe.RecipeId})");
            builder.AppendLine($"카테고리: {(recipe.Category != null ? recipe.Category.DisplayName : "없음")}");
            builder.AppendLine($"태그: {BuildTagText(recipe.BaseTags)}");
            builder.AppendLine("필요 재료:");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null || requirement.Ingredient == null)
                {
                    builder.AppendLine("- 누락된 재료");
                    continue;
                }

                builder.AppendLine($"- {requirement.Ingredient.DisplayName}");
                for (int alternativeIndex = 0; alternativeIndex < requirement.AlternativeOptions.Count; alternativeIndex++)
                {
                    RecipeIngredientAlternative alternative = requirement.AlternativeOptions[alternativeIndex];
                    if (alternative == null || alternative.Ingredient == null)
                        continue;

                    string modifier = string.IsNullOrWhiteSpace(alternative.ResultNameModifier)
                        ? "수식어 없음"
                        : alternative.ResultNameModifier;
                    builder.AppendLine($"  대체: {alternative.Ingredient.DisplayName} / 이름 수식어: {modifier}");
                }
            }

            builder.AppendLine("정석 손질 조건:");
            if (recipe.PerfectPreparationRules.Count == 0)
            {
                builder.AppendLine("- 없음");
            }
            else
            {
                for (int i = 0; i < recipe.PerfectPreparationRules.Count; i++)
                {
                    RecipePreparationRule rule = recipe.PerfectPreparationRules[i];
                    string ingredient = rule != null && rule.Ingredient != null ? rule.Ingredient.DisplayName : "없음";
                    string method = rule != null && rule.PerfectMethod != null ? rule.PerfectMethod.DisplayName : "없음";
                    builder.AppendLine($"- {ingredient}: {method}");
                }
            }

            return builder.ToString();
        }

        private static string BuildTagText(IReadOnlyList<FoodTagSO> tags)
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
            }

            return builder.Length > 0 ? builder.ToString() : "없음";
        }
    }
}
