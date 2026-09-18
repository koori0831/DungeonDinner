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
        private void SaveSelectedAsset()
        {
            if (_selectedAsset == null)
                return;

            Undo.RecordObject(_selectedAsset, $"{GetModeKoreanName(currentMode)} SO 수정");
            SerializedObject serialized = new SerializedObject(_selectedAsset);

            switch (currentMode)
            {
                case DataMode.Recipe:
                    SaveRecipe(serialized);
                    break;
                case DataMode.Category:
                    SaveCategory(serialized);
                    break;
                case DataMode.IngredientCategory:
                    SaveIngredientCategory(serialized);
                    break;
                case DataMode.Tag:
                    SaveTag(serialized);
                    break;
                case DataMode.PreparationMethod:
                    SaveMethod(serialized);
                    break;
                case DataMode.Ingredient:
                    SaveIngredient(serialized);
                    break;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selectedAsset);
            RenameSelectedAssetToMatchData();
            AssetDatabase.SaveAssets();

            BuildDraftFromSelection();
            _hasUnsavedChanges = false;
            RefreshAssets();
            UpdateDetailPanel();
            Debug.Log($"{GetModeKoreanName(currentMode)} SO 저장 완료: {GetAssetPath(_selectedAsset)}", _selectedAsset);
        }

        private void RenameSelectedAssetToMatchData()
        {
            if (_selectedAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(_selectedAsset);
            if (string.IsNullOrWhiteSpace(path))
                return;

            string desiredName = GetDesiredAssetName(_selectedAsset);
            if (string.IsNullOrWhiteSpace(desiredName))
                return;

            string currentName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(currentName, desiredName, StringComparison.Ordinal))
                return;

            string folder = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(folder))
                return;

            folder = folder.Replace('\\', '/');
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{desiredName}.asset");
            string uniqueName = Path.GetFileNameWithoutExtension(uniquePath);
            string error = AssetDatabase.RenameAsset(path, uniqueName);

            if (string.IsNullOrEmpty(error) == false)
                Debug.LogWarning($"SO 에셋 이름 변경 실패: {error}", _selectedAsset);
        }

        private void SaveRecipe(SerializedObject serialized)
        {
            SetString(serialized, "recipeId", _recipeDraft.RecipeId);
            SetString(serialized, "displayName", _recipeDraft.DisplayName);
            SetObject(serialized, "iconSprite", _recipeDraft.IconSprite);
            SetString(serialized, "description", _recipeDraft.Description);
            SetObject(serialized, "category", _recipeDraft.Category);
            SetInt(serialized, "priority", _recipeDraft.Priority);
            SetObjectArray(serialized, "baseTags", _recipeDraft.BaseTags);
            SetRequiredIngredients(serialized, _recipeDraft.RequiredIngredients);
            SetPerfectRules(serialized, Array.Empty<PerfectRuleDraft>());
        }

        private void SaveCategory(SerializedObject serialized)
        {
            SetString(serialized, "categoryId", _categoryDraft.CategoryId);
            SetString(serialized, "displayName", _categoryDraft.DisplayName);
            SetObject(serialized, "icon", _categoryDraft.Icon);
            SetString(serialized, "description", _categoryDraft.Description);
        }

        private void SaveIngredientCategory(SerializedObject serialized)
        {
            SetString(serialized, "categoryId", _ingredientCategoryDraft.CategoryId);
            SetString(serialized, "displayName", _ingredientCategoryDraft.DisplayName);
            SetObject(serialized, "icon", _ingredientCategoryDraft.Icon);
            SetString(serialized, "description", _ingredientCategoryDraft.Description);
        }

        private void SaveTag(SerializedObject serialized)
        {
            SetString(serialized, "tagId", _tagDraft.TagId);
            SetString(serialized, "displayName", _tagDraft.DisplayName);
            SetString(serialized, "description", _tagDraft.Description);
        }

        private void SaveMethod(SerializedObject serialized)
        {
            SetString(serialized, "methodId", _methodDraft.MethodId);
            SetString(serialized, "displayName", _methodDraft.DisplayName);
            SetObject(serialized, "iconSprite", _methodDraft.IconSprite);
            SetString(serialized, "description", _methodDraft.Description);
        }

        private void SaveIngredient(SerializedObject serialized)
        {
            SetString(serialized, "ingredientId", _ingredientDraft.IngredientId);
            SetString(serialized, "displayName", _ingredientDraft.DisplayName);
            SetObject(serialized, "iconSprite", _ingredientDraft.IconSprite);
            SetString(serialized, "description", _ingredientDraft.Description);
            SetObject(serialized, "category", _ingredientDraft.Category);
            SetObject(serialized, "modelPrefab", _ingredientDraft.ModelPrefab);
            SetObjectArray(serialized, "baseTags", _ingredientDraft.BaseTags);
            SetPreparationOptions(serialized, _ingredientDraft.PreparationOptions);
        }

        private void RevertSelectedAsset()
        {
            if (_selectedAsset == null)
                return;

            BuildDraftFromSelection();
            _hasUnsavedChanges = false;
            UpdateDetailPanel();
            _formContainer.MarkDirtyRepaint();
        }

        private void PingSelectedAsset()
        {
            if (_selectedAsset == null)
                return;

            Selection.activeObject = _selectedAsset;
            EditorGUIUtility.PingObject(_selectedAsset);
        }

        private void RegisterSelectedAssetToCatalog()
        {
            if (_selectedAsset == null || catalog == null || IsSelectedAssetInCatalog() == true)
                return;

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty(GetCatalogPropertyName(currentMode));
            if (list == null || list.isArray == false)
                return;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = _selectedAsset;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            UpdateCatalogSummary();
            UpdateDetailPanel();
        }

        private void CreateNewAsset()
        {
            if (TryHandleUnsavedChanges() == false)
                return;

            string folder = ResolveCreateFolder(currentMode, assetFolder);
            EnsureFolder(folder);

            UnityEngine.Object asset = CreateInstance(GetAssetType(currentMode));
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{GetDefaultFileName(currentMode)}.asset");
            AssetDatabase.CreateAsset(asset, path);

            SerializedObject serialized = new SerializedObject(asset);
            string id = Path.GetFileNameWithoutExtension(path);
            SetInitialValues(serialized, currentMode, id);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _selectedAsset = asset;
            if (catalog != null)
                RegisterSelectedAssetToCatalog();

            RefreshAssets(false);
            SelectAsset(asset);
            RestoreListSelection();
        }

        private bool TryHandleUnsavedChanges()
        {
            if (_hasUnsavedChanges == false)
                return true;

            int result = EditorUtility.DisplayDialogComplex(
                "저장되지 않은 수정사항",
                "현재 SO에 저장되지 않은 수정사항이 있습니다. 어떻게 할까요?",
                "저장",
                "버리기",
                "취소");

            if (result == 0)
            {
                SaveSelectedAsset();
                return true;
            }

            return result == 1;
        }

        private void RestoreListSelection()
        {
            if (_assetListView == null)
                return;

            _isRestoringSelection = true;
            int index = _selectedAsset != null ? _visibleAssets.IndexOf(_selectedAsset) : -1;
            if (index >= 0)
                _assetListView.SetSelection(index);
            else
                _assetListView.ClearSelection();

            _isRestoringSelection = false;
        }

        private void MarkDraftDirty()
        {
            _hasUnsavedChanges = true;
            UpdateDetailPanel();
        }
    }
}
