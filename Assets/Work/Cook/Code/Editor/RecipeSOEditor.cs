using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime;

namespace Work.Cook.Code.Editor
{
    [CustomEditor(typeof(RecipeSO))]
    public sealed class RecipeSOEditor : UnityEditor.Editor
    {
        private readonly List<TestPreparedIngredient> _testPreparedIngredients = new List<TestPreparedIngredient>();

        private SerializedProperty _recipeId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _revealNameByDefault;
        private SerializedProperty _hiddenDisplayName;
        private SerializedProperty _undiscoveredDescription;
        private SerializedProperty _hintDescription;
        private SerializedProperty _discoveredDescription;
        private SerializedProperty _category;
        private SerializedProperty _priority;
        private SerializedProperty _baseTags;
        private SerializedProperty _requiredIngredients;

        private bool _basicFoldout = true;
        private bool _tagsFoldout = true;
        private bool _requirementsFoldout = true;
        private bool _testFoldout = true;
        private bool _summaryFoldout = true;

        private void OnEnable()
        {
            _recipeId = serializedObject.FindProperty("recipeId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _revealNameByDefault = serializedObject.FindProperty("revealNameByDefault");
            _hiddenDisplayName = serializedObject.FindProperty("hiddenDisplayName");
            _undiscoveredDescription = serializedObject.FindProperty("undiscoveredDescription");
            _hintDescription = serializedObject.FindProperty("hintDescription");
            _discoveredDescription = serializedObject.FindProperty("discoveredDescription");
            _category = serializedObject.FindProperty("category");
            _priority = serializedObject.FindProperty("priority");
            _baseTags = serializedObject.FindProperty("baseTags");
            _requiredIngredients = serializedObject.FindProperty("requiredIngredients");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RecipeSO recipe = (RecipeSO)target;
            EditorGUILayout.HelpBox(
                "레시피는 선택 모드에서는 최종 음식으로 고정되고, 직접 재료 선택 모드에서는 손질 완료 결과로 매칭됩니다. " +
                "필요 재료 슬롯에는 특정 재료, 재료 카테고리, 태그, 손질법, 개수 조건을 함께 지정할 수 있습니다.",
                MessageType.Info);

            DrawValidationMessages(recipe);
            DrawBasicSection();
            DrawTagsSection();
            DrawRequirementsSection();

            serializedObject.ApplyModifiedProperties();

            DrawMatchTestSection(recipe);
            DrawSummarySection(recipe);
        }

        private void DrawBasicSection()
        {
            _basicFoldout = DrawSectionHeader("기본 정보", _basicFoldout);
            if (_basicFoldout == false)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(_recipeId, new GUIContent("레시피 ID"));
                EditorGUILayout.PropertyField(_displayName, new GUIContent("표시 이름"));
                EditorGUILayout.PropertyField(_category, new GUIContent("음식 카테고리"));
                EditorGUILayout.PropertyField(_priority, new GUIContent("매칭 우선순위"));
                EditorGUILayout.PropertyField(_description, new GUIContent("설명"));
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("도감 공개 정보", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_revealNameByDefault, new GUIContent("발견 전 이름 표시"));
                EditorGUILayout.PropertyField(_hiddenDisplayName, new GUIContent("숨김 이름"));
                EditorGUILayout.PropertyField(_undiscoveredDescription, new GUIContent("미발견 설명"));
                EditorGUILayout.PropertyField(_hintDescription, new GUIContent("시도 후 힌트 설명"));
                EditorGUILayout.PropertyField(_discoveredDescription, new GUIContent("발견 후 설명"));
            }
        }

        private void DrawTagsSection()
        {
            _tagsFoldout = DrawSectionHeader("기본 태그", _tagsFoldout);
            if (_tagsFoldout == false)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox("완성 음식에 기본으로 붙는 태그입니다.", MessageType.None);
                DrawObjectReferenceArray(_baseTags, typeof(FoodTagSO), "태그", "+ 태그 추가", "기본 태그가 없습니다.");
            }
        }

        private void DrawRequirementsSection()
        {
            _requirementsFoldout = DrawSectionHeader("필요 재료 슬롯", _requirementsFoldout);
            if (_requirementsFoldout == false)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox(
                    "각 슬롯은 특정 재료, 재료 카테고리, 필수 태그, 필수 손질법, 최소/최대 개수를 가질 수 있습니다. " +
                    "레시피 선택 모드에서 필수 손질법이 있고 자동 적용이 켜져 있으면 손질 UI를 건너뛰고 해당 손질이 기록됩니다.",
                    MessageType.Info);

                if (_requiredIngredients.arraySize == 0)
                    EditorGUILayout.HelpBox("필요 재료 슬롯이 없습니다.", MessageType.Warning);

                for (int i = 0; i < _requiredIngredients.arraySize; i++)
                    DrawRequirementElement(i);

                if (GUILayout.Button("+ 필요 재료 슬롯 추가"))
                    AddRequiredIngredientElement();
            }
        }

        private void DrawRequirementElement(int index)
        {
            SerializedProperty element = _requiredIngredients.GetArrayElementAtIndex(index);
            SerializedProperty ingredient = element.FindPropertyRelative("ingredient");
            SerializedProperty ingredientCategory = element.FindPropertyRelative("ingredientCategory");
            SerializedProperty requiredTags = element.FindPropertyRelative("requiredTags");
            SerializedProperty alternatives = element.FindPropertyRelative("alternatives");
            SerializedProperty alternativeOptions = element.FindPropertyRelative("alternativeOptions");
            SerializedProperty requiredPreparationMethod = element.FindPropertyRelative("requiredPreparationMethod");
            SerializedProperty requiredPreparationMethods = element.FindPropertyRelative("requiredPreparationMethods");
            SerializedProperty minCount = element.FindPropertyRelative("minCount");
            SerializedProperty maxCount = element.FindPropertyRelative("maxCount");
            SerializedProperty recipeDefining = element.FindPropertyRelative("recipeDefining");
            SerializedProperty requireManualPreparation = element.FindPropertyRelative("requireManualPreparation");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"슬롯 {index + 1}: {BuildRequirementEditorTitle(element)}", EditorStyles.boldLabel);
                    if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                    {
                        _requiredIngredients.DeleteArrayElementAtIndex(index);
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.PropertyField(ingredient, new GUIContent("기준 재료"));
                EditorGUILayout.PropertyField(ingredientCategory, new GUIContent("재료군"));
                DrawObjectReferenceArray(requiredTags, typeof(FoodTagSO), "필수 태그", "+ 필수 태그 추가", "필수 태그가 없습니다.");
                DrawObjectReferenceArray(alternatives, typeof(IngredientSO), "단순 대체", "+ 단순 대체 추가", "단순 대체 재료가 없습니다.");
                DrawAlternativeOptionArray(alternativeOptions);
                DrawObjectReferenceArray(requiredPreparationMethods, typeof(PreparationMethodSO), "필수 손질법", "+ 필수 손질법 추가", "필수 손질법 없음", 2);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("손질/개수 조건", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(minCount, new GUIContent("최소 개수"));
                EditorGUILayout.PropertyField(maxCount, new GUIContent("최대 개수 (0 = 제한 없음)"));

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("레시피 선택 모드 처리", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(recipeDefining, new GUIContent("요리 결정 조건"));
                EditorGUILayout.PropertyField(requireManualPreparation, new GUIContent("직접 손질 필요"));
            }
        }

        private void DrawMatchTestSection(RecipeSO recipe)
        {
            _testFoldout = DrawSectionHeader("매칭 테스트", _testFoldout);
            if (_testFoldout == false)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox(
                    "직접 재료 선택 모드에서 손질이 끝났을 때 이 레시피와 매칭되는지 테스트합니다. " +
                    "손질법을 비워두면 해당 재료는 손질 없음으로 테스트됩니다.",
                    MessageType.None);

                for (int i = 0; i < _testPreparedIngredients.Count; i++)
                {
                    TestPreparedIngredient item = _testPreparedIngredients[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        item.Ingredient = (IngredientSO)EditorGUILayout.ObjectField(item.Ingredient, typeof(IngredientSO), false);
                        item.Method = (PreparationMethodSO)EditorGUILayout.ObjectField(item.Method, typeof(PreparationMethodSO), false);
                        if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                        {
                            _testPreparedIngredients.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ 테스트 재료 추가"))
                        _testPreparedIngredients.Add(new TestPreparedIngredient());

                    if (GUILayout.Button("필요 슬롯 기준 채우기"))
                        FillTestIngredientsFromRecipe(recipe);
                }

                List<PreparedIngredientState> prepared = BuildTestPreparedIngredients();
                bool ingredientOnlyMatch = recipe.MatchesIngredients(BuildTestIngredients());
                bool preparedMatch = recipe.MatchesPreparedIngredients(prepared);
                int score = recipe.CalculateMatchScore(prepared);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField($"재료만 매칭: {(ingredientOnlyMatch ? "성공" : "실패")}");
                EditorGUILayout.LabelField($"손질 포함 매칭: {(preparedMatch ? "성공" : "실패")}");
                EditorGUILayout.LabelField($"매칭 점수: {score}");
            }
        }

        private void DrawSummarySection(RecipeSO recipe)
        {
            _summaryFoldout = DrawSectionHeader("요약", _summaryFoldout);
            if (_summaryFoldout == false)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                string summary = BuildRecipeSummary(recipe);
                EditorGUILayout.TextArea(summary, GUILayout.MinHeight(160f));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("요약 복사"))
                        EditorGUIUtility.systemCopyBuffer = summary;

                    if (GUILayout.Button("콘솔 출력"))
                        Debug.Log(summary, recipe);
                }
            }
        }

        private static void DrawObjectReferenceArray(
            SerializedProperty property,
            Type objectType,
            string rowLabel,
            string addButtonLabel,
            string emptyMessage,
            int maxCount = int.MaxValue)
        {
            if (property == null || property.isArray == false)
                return;

            if (property.arraySize == 0)
                EditorGUILayout.HelpBox(emptyMessage, MessageType.None);

            EditorGUI.indentLevel++;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{rowLabel} {i + 1}", GUILayout.Width(92f));
                    element.objectReferenceValue = EditorGUILayout.ObjectField(element.objectReferenceValue, objectType, false);
                    if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                    {
                        DeleteArrayElement(property, i);
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUI.indentLevel--;

            if (property.arraySize < maxCount && GUILayout.Button(addButtonLabel))
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

            EditorGUILayout.LabelField("이름 수식어가 있는 대체 재료", EditorStyles.boldLabel);

            if (property.arraySize == 0)
                EditorGUILayout.HelpBox("대체 재료가 없습니다.", MessageType.None);

            EditorGUI.indentLevel++;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                SerializedProperty ingredient = element.FindPropertyRelative("ingredient");
                SerializedProperty resultNameModifier = element.FindPropertyRelative("resultNameModifier");

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"대체 {i + 1}", EditorStyles.boldLabel);
                        if (GUILayout.Button("삭제", GUILayout.Width(52f)))
                        {
                            property.DeleteArrayElementAtIndex(i);
                            GUIUtility.ExitGUI();
                        }
                    }

                    EditorGUILayout.PropertyField(ingredient, new GUIContent("재료"));
                    EditorGUILayout.PropertyField(resultNameModifier, new GUIContent("결과 이름 수식어"));
                }
            }
            EditorGUI.indentLevel--;

            if (GUILayout.Button("+ 수식어 대체 재료 추가"))
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
            element.FindPropertyRelative("ingredientCategory").objectReferenceValue = null;
            element.FindPropertyRelative("requiredPreparationMethod").objectReferenceValue = null;
            ClearArray(element.FindPropertyRelative("requiredPreparationMethods"));
            element.FindPropertyRelative("minCount").intValue = 1;
            element.FindPropertyRelative("maxCount").intValue = 1;
            element.FindPropertyRelative("recipeDefining").boolValue = true;
            element.FindPropertyRelative("requireManualPreparation").boolValue = false;

            ClearArray(element.FindPropertyRelative("requiredTags"));
            ClearArray(element.FindPropertyRelative("alternatives"));
            ClearArray(element.FindPropertyRelative("alternativeOptions"));
        }

        private List<IngredientSO> BuildTestIngredients()
        {
            List<IngredientSO> ingredients = new List<IngredientSO>();
            for (int i = 0; i < _testPreparedIngredients.Count; i++)
            {
                if (_testPreparedIngredients[i].Ingredient != null)
                    ingredients.Add(_testPreparedIngredients[i].Ingredient);
            }

            return ingredients;
        }

        private List<PreparedIngredientState> BuildTestPreparedIngredients()
        {
            List<PreparedIngredientState> prepared = new List<PreparedIngredientState>();
            for (int i = 0; i < _testPreparedIngredients.Count; i++)
            {
                TestPreparedIngredient test = _testPreparedIngredients[i];
                if (test.Ingredient == null)
                    continue;

                IngredientPreparationOption option = test.Ingredient.FindPreparationOption(test.Method);
                prepared.Add(new PreparedIngredientState(test.Ingredient, option));
            }

            return prepared;
        }

        private void FillTestIngredientsFromRecipe(RecipeSO recipe)
        {
            _testPreparedIngredients.Clear();
            if (recipe == null)
                return;

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null || requirement.Ingredient == null)
                    continue;

                int count = Mathf.Max(1, requirement.MinCount);
                for (int countIndex = 0; countIndex < count; countIndex++)
                {
                    _testPreparedIngredients.Add(new TestPreparedIngredient
                    {
                        Ingredient = requirement.Ingredient,
                        Method = requirement.RequiredPreparationMethod
                    });
                }
            }
        }

        private static void DeleteArrayElement(SerializedProperty property, int index)
        {
            int previousSize = property.arraySize;
            property.DeleteArrayElementAtIndex(index);
            if (property.arraySize == previousSize)
                property.DeleteArrayElementAtIndex(index);
        }

        private static void ClearArray(SerializedProperty property)
        {
            if (property != null && property.isArray)
                property.ClearArray();
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
                warnings.Add("음식 카테고리가 지정되지 않았습니다.");

            if (recipe.RequiredIngredients.Count == 0)
                warnings.Add("필요 재료 슬롯이 없습니다.");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                if (requirement == null)
                {
                    warnings.Add($"슬롯 {i + 1}이 비어 있습니다.");
                    continue;
                }

                bool hasAnyIdentity =
                    requirement.Ingredient != null
                    || requirement.IngredientCategory != null
                    || requirement.RequiredTags.Count > 0
                    || requirement.Alternatives.Count > 0
                    || requirement.AlternativeOptions.Count > 0;

                if (hasAnyIdentity == false)
                    warnings.Add($"슬롯 {i + 1}에는 재료/카테고리/태그/대체 재료 중 하나가 필요합니다.");

                if (requirement.MinCount == 0)
                    warnings.Add($"슬롯 {i + 1}의 최소 개수가 0입니다. 선택 슬롯 의도가 아니라면 1 이상을 권장합니다.");

                if (requirement.HasMaxCount && requirement.MaxCount < requirement.MinCount)
                    warnings.Add($"슬롯 {i + 1}의 최대 개수가 최소 개수보다 작습니다.");

                if (requirement.RequiredPreparationMethod != null && requirement.Ingredient != null)
                {
                    IngredientPreparationOption option =
                        requirement.Ingredient.FindPreparationOption(requirement.RequiredPreparationMethod);
                    if (option == null)
                        warnings.Add($"슬롯 {i + 1}의 기준 재료에 필수 손질법이 등록되어 있지 않습니다.");
                }
            }

            return warnings;
        }

        private static string BuildRecipeSummary(RecipeSO recipe)
        {
            if (recipe == null)
                return "레시피가 없습니다.";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"레시피: {recipe.DisplayName} ({recipe.RecipeId})");
            builder.AppendLine($"음식 카테고리: {(recipe.Category != null ? recipe.Category.DisplayName : "없음")}");
            builder.AppendLine($"우선순위: {recipe.Priority}");
            builder.AppendLine($"기본 태그: {BuildTagText(recipe.BaseTags)}");
            builder.AppendLine();
            builder.AppendLine("필요 재료 슬롯:");

            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                RecipeIngredientRequirement requirement = recipe.RequiredIngredients[i];
                builder.AppendLine($"- {BuildRequirementSummary(requirement)}");
            }

            return builder.ToString();
        }

        private static string BuildRequirementEditorTitle(SerializedProperty element)
        {
            IngredientSO ingredient = element.FindPropertyRelative("ingredient").objectReferenceValue as IngredientSO;
            IngredientCategorySO category =
                element.FindPropertyRelative("ingredientCategory").objectReferenceValue as IngredientCategorySO;
            PreparationMethodSO method =
                element.FindPropertyRelative("requiredPreparationMethod").objectReferenceValue as PreparationMethodSO;

            string target = ingredient != null
                ? ingredient.DisplayName
                : category != null ? $"{category.DisplayName} 카테고리" : "조건 미지정";
            string prep = method != null ? $" + {method.DisplayName}" : string.Empty;
            return $"{target}{prep}";
        }

        private static string BuildRequirementSummary(RecipeIngredientRequirement requirement)
        {
            if (requirement == null)
                return "비어 있음";

            List<string> parts = new List<string>();
            if (requirement.Ingredient != null)
                parts.Add($"재료 {requirement.Ingredient.DisplayName}");
            if (requirement.IngredientCategory != null)
                parts.Add($"재료군 {requirement.IngredientCategory.DisplayName}");
            if (requirement.RequiredTags.Count > 0)
                parts.Add($"태그 {BuildTagText(requirement.RequiredTags)}");
            if (requirement.RequiredPreparationMethod != null)
                parts.Add($"손질 {requirement.RequiredPreparationMethod.DisplayName}");

            string count = requirement.HasMaxCount
                ? $"{requirement.MinCount}-{requirement.MaxCount}개"
                : $"{requirement.MinCount}개 이상";
            parts.Add(count);

            if (requirement.RequireManualPreparation)
                parts.Add("직접 손질");

            return parts.Count > 0 ? string.Join(" / ", parts) : "조건 없음";
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

        [Serializable]
        private sealed class TestPreparedIngredient
        {
            public IngredientSO Ingredient;
            public PreparationMethodSO Method;
        }
    }
}
