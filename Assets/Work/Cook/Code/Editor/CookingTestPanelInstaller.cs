using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime;

namespace Work.Cook.Code.Editor
{
    public static class CookingTestPanelInstaller
    {
        private const string MenuPath = "Tools/Dungeon Dinner/Create Cooking Test UI In Scene";
        private const string ObjectName = "Cooking Test UI";
        private const string DefaultFontPath = "Assets/Font/MangoDdobak-B(otf) SDF.asset";

        [MenuItem(MenuPath)]
        public static void CreateInScene()
        {
            CookingDataCatalogSO catalog = FindFirstAsset<CookingDataCatalogSO>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);

            GameObject root = new GameObject(ObjectName);
            Undo.RegisterCreatedObjectUndo(root, "Create Cooking Test UI");

            CookingFlowRunner runner = Undo.AddComponent<CookingFlowRunner>(root);
            CookingTestPanel panel = Undo.AddComponent<CookingTestPanel>(root);

            AssignRunner(runner, catalog);
            AssignPanel(panel, runner, catalog, font);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.scene);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            if (catalog == null)
            {
                Debug.LogWarning("Cooking Test UI was created, but no CookingDataCatalogSO asset was found. Assign a catalog before pressing Play.", root);
                return;
            }

            Debug.Log($"Cooking Test UI was created with catalog '{catalog.name}'. Press Play to try the cooking flow.", root);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateInScene()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode == false;
        }

        private static void AssignRunner(CookingFlowRunner runner, CookingDataCatalogSO catalog)
        {
            Undo.RecordObject(runner, "Configure Cooking Flow Runner");

            SerializedObject serializedRunner = new SerializedObject(runner);
            serializedRunner.FindProperty("catalog").objectReferenceValue = catalog;
            serializedRunner.ApplyModifiedProperties();

            EditorUtility.SetDirty(runner);
        }

        private static void AssignPanel(
            CookingTestPanel panel,
            CookingFlowRunner runner,
            CookingDataCatalogSO catalog,
            TMP_FontAsset font)
        {
            Undo.RecordObject(panel, "Configure Cooking Test Panel");

            SerializedObject serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("runner").objectReferenceValue = runner;
            serializedPanel.FindProperty("catalog").objectReferenceValue = catalog;
            serializedPanel.FindProperty("fontAsset").objectReferenceValue = font;
            serializedPanel.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel);
        }

        private static T FindFirstAsset<T>()
            where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
