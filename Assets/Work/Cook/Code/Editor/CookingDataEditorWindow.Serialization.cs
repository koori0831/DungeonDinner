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
        private static void SetInitialValues(SerializedObject serialized, DataMode mode, string id)
        {
            if (mode == DataMode.IngredientCategory)
            {
                SetString(serialized, "categoryId", id);
                SetString(serialized, "displayName", "새 재료군");
                return;
            }

            switch (mode)
            {
                case DataMode.Recipe:
                    SetString(serialized, "recipeId", id);
                    SetString(serialized, "displayName", "새 레시피");
                    break;
                case DataMode.Category:
                    SetString(serialized, "categoryId", id);
                    SetString(serialized, "displayName", "새 카테고리");
                    break;
                case DataMode.Tag:
                    SetString(serialized, "tagId", id);
                    SetString(serialized, "displayName", "새 태그");
                    break;
                case DataMode.PreparationMethod:
                    SetString(serialized, "methodId", id);
                    SetString(serialized, "displayName", "새 손질법");
                    break;
                case DataMode.Ingredient:
                    SetString(serialized, "ingredientId", id);
                    SetString(serialized, "displayName", "새 재료");
                    break;
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value ?? string.Empty;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetObjectArray<T>(SerializedObject serialized, string propertyName, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            SetRelativeObjectArray(property, values);
        }

        private static void SetRequiredIngredients(
            SerializedObject serialized,
            IReadOnlyList<IngredientRequirementDraft> requirements)
        {
            SerializedProperty property = serialized.FindProperty("requiredIngredients");
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (requirements == null)
                return;

            for (int i = 0; i < requirements.Count; i++)
            {
                IngredientRequirementDraft requirement = requirements[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("ingredient").objectReferenceValue = requirement.Ingredient;
                element.FindPropertyRelative("ingredientCategory").objectReferenceValue = requirement.IngredientCategory;
                element.FindPropertyRelative("requiredPreparationMethod").objectReferenceValue = null;
                SetRelativeObjectArray(element.FindPropertyRelative("requiredPreparationMethods"), requirement.RequiredPreparationMethods);
                element.FindPropertyRelative("minCount").intValue = requirement.MinCount;
                element.FindPropertyRelative("maxCount").intValue = requirement.MaxCount;
                element.FindPropertyRelative("recipeDefining").boolValue = requirement.RecipeDefining;
                element.FindPropertyRelative("requireManualPreparation").boolValue = requirement.RequireManualPreparation;
                SerializedProperty usePreparationModifier = element.FindPropertyRelative("usePreparationResultNameModifier");
                if (usePreparationModifier != null)
                    usePreparationModifier.boolValue = requirement.UsePreparationResultNameModifier;
                SetRelativeObjectArray(element.FindPropertyRelative("requiredTags"), requirement.RequiredTags);
                SetRelativeObjectArray(element.FindPropertyRelative("alternatives"), requirement.SimpleAlternatives);
                SetAlternativeOptions(element.FindPropertyRelative("alternativeOptions"), requirement.Alternatives);
            }
        }

        private static void SetAlternativeOptions(
            SerializedProperty property,
            IReadOnlyList<IngredientAlternativeDraft> alternatives)
        {
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (alternatives == null)
                return;

            for (int i = 0; i < alternatives.Count; i++)
            {
                IngredientAlternativeDraft alternative = alternatives[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("ingredient").objectReferenceValue = alternative.Ingredient;
                element.FindPropertyRelative("resultNameModifier").stringValue = alternative.ResultNameModifier ?? string.Empty;
            }
        }

        private static void SetPerfectRules(SerializedObject serialized, IReadOnlyList<PerfectRuleDraft> rules)
        {
            SerializedProperty property = serialized.FindProperty("perfectPreparationRules");
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (rules == null)
                return;

            for (int i = 0; i < rules.Count; i++)
            {
                PerfectRuleDraft rule = rules[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("ingredient").objectReferenceValue = rule.Ingredient;
                element.FindPropertyRelative("perfectMethod").objectReferenceValue = rule.PerfectMethod;
            }
        }

        private static void SetPreparationOptions(SerializedObject serialized, IReadOnlyList<PreparationOptionDraft> options)
        {
            SerializedProperty property = serialized.FindProperty("preparationOptions");
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (options == null)
                return;

            for (int i = 0; i < options.Count; i++)
            {
                PreparationOptionDraft option = options[i];
                property.InsertArrayElementAtIndex(property.arraySize);
                SerializedProperty element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.FindPropertyRelative("method").objectReferenceValue = option.Method;
                element.FindPropertyRelative("displayNameOverride").stringValue = option.DisplayNameOverride ?? string.Empty;
                element.FindPropertyRelative("description").stringValue = option.Description ?? string.Empty;
                SetRelativeObjectArray(element.FindPropertyRelative("addTags"), option.AddTags);
                SetRelativeObjectArray(element.FindPropertyRelative("removeTags"), option.RemoveTags);
                element.FindPropertyRelative("qualityDelta").intValue = option.QualityDelta;
                element.FindPropertyRelative("causesDisgusting").boolValue = option.CausesDisgusting;
                element.FindPropertyRelative("addsPoison").boolValue = option.AddsPoison;
                element.FindPropertyRelative("resultNameModifier").stringValue = option.ResultNameModifier ?? string.Empty;
            }
        }

        private static void SetRelativeObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            if (property == null || property.isArray == false)
                return;

            property.ClearArray();
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = values[i];
            }
        }

        private static List<T> ReadObjectArray<T>(SerializedObject serialized, string propertyName)
            where T : UnityEngine.Object
        {
            return ReadObjectArray<T>(serialized.FindProperty(propertyName));
        }

        private static List<T> ReadObjectArray<T>(SerializedProperty property)
            where T : UnityEngine.Object
        {
            List<T> values = new List<T>();
            if (property == null || property.isArray == false)
                return values;

            for (int i = 0; i < property.arraySize; i++)
                values.Add(property.GetArrayElementAtIndex(i).objectReferenceValue as T);

            return values;
        }

        private static string ReadString(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.stringValue : string.Empty;
        }

        private static int ReadInt(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.intValue : 0;
        }

        private static T ReadObject<T>(SerializedObject serialized, string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static string ReadRelativeString(SerializedProperty property, string propertyName)
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null ? relative.stringValue : string.Empty;
        }

        private static T ReadRelativeObject<T>(SerializedProperty property, string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null ? relative.objectReferenceValue as T : null;
        }

        private static bool ReadRelativeBool(SerializedProperty property, string propertyName)
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null && relative.boolValue;
        }

        private static int ReadRelativeInt(SerializedProperty property, string propertyName)
        {
            SerializedProperty relative = property.FindPropertyRelative(propertyName);
            return relative != null ? relative.intValue : 0;
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = NormalizeFolder(folder);
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string[] parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new InvalidOperationException("에셋 생성 위치는 Assets 폴더 안이어야 합니다.");

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (AssetDatabase.IsValidFolder(next) == false)
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return DefaultAssetFolder;

            return folder.Replace('\\', '/').TrimEnd('/');
        }

        private static string ResolveCreateFolder(DataMode mode, string folder)
        {
            string normalized = NormalizeFolder(folder);
            if (string.Equals(normalized, DefaultAssetFolder, StringComparison.OrdinalIgnoreCase) == true)
                return GetDefaultAssetFolder(mode);

            return normalized;
        }

        private static string GetDefaultAssetFolder(DataMode mode)
        {
            switch (mode)
            {
                case DataMode.Recipe:
                    return RecipeAssetFolder;
                case DataMode.Category:
                    return CategoryAssetFolder;
                case DataMode.IngredientCategory:
                    return IngredientCategoryAssetFolder;
                case DataMode.Tag:
                    return TagAssetFolder;
                case DataMode.PreparationMethod:
                    return MethodAssetFolder;
                case DataMode.Ingredient:
                    return IngredientAssetFolder;
                default:
                    return DefaultAssetFolder;
            }
        }
    }
}