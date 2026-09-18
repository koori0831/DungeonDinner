using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Work.Items.Code.Editor
{
    [CustomEditor(typeof(ItemCatalogSO))]
    public sealed class ItemCatalogSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ItemCatalogSO catalog = (ItemCatalogSO)target;
            List<string> messages = catalog.BuildValidationMessages();
            for (int i = 0; i < messages.Count; i++)
            {
                EditorGUILayout.HelpBox(messages[i], MessageType.Error);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("프로젝트의 모든 아이템으로 갱신"))
            {
                PopulateFromProject(catalog);
            }
        }

        private static void PopulateFromProject(ItemCatalogSO catalog)
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDataSO");
            List<ItemDataSO> foundItems = new List<ItemDataSO>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ItemDataSO item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
                if (item != null)
                {
                    foundItems.Add(item);
                }
            }

            foundItems.Sort((left, right) =>
                string.Compare(left.ItemId, right.ItemId, System.StringComparison.OrdinalIgnoreCase));

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty itemsProperty = serializedCatalog.FindProperty("items");
            itemsProperty.arraySize = foundItems.Count;

            for (int i = 0; i < foundItems.Count; i++)
            {
                itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = foundItems[i];
            }

            serializedCatalog.ApplyModifiedProperties();
            catalog.RebuildIndex();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
