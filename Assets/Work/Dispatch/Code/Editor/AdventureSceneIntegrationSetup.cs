using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Dispatch.Code.Runtime;
using Work.Dispatch.Code.UI;
using Work.NPC.Code.Runtime;
using Work.Players.Code.Inventory;
using Work.TimeSystem;

namespace Work.Dispatch.Code.Editor
{
    /// <summary>
    /// AdventureTestScene의 파견/시간 및 조리 UI 필수 참조를 실제 씬 오브젝트로 연결합니다.
    /// 반복 실행해도 같은 오브젝트를 중복 생성하지 않습니다.
    /// </summary>
    public static class AdventureSceneIntegrationSetup
    {
        private const string AdventureScenePath = "Assets/Work/Adventure/Scene/AdventureTestScene.unity";
        private const string CookReferenceScenePath = "Assets/Work/Cook/Scene/CookTestScene.unity";
        private const string MiniGameOverlayName = "CookingMiniGameOverlayRoot";
        private const string ResultViewName = "CookingResultPresentationRoot";
        private const string MiniGamePrefabFolder = "Assets/Work/Cook/Prefabs/UI";
        private const string MiniGamePrefabPath = MiniGamePrefabFolder + "/CookingMiniGameOverlayRoot.prefab";
        private const string ResultViewPrefabPath = MiniGamePrefabFolder + "/CookingResultPresentationRoot.prefab";

        [MenuItem("Tools/Dungeon Dinner/Dispatch/Audit Adventure Scene Integration")]
        public static void Audit()
        {
            Scene scene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
            StringBuilder report = new StringBuilder();
            report.AppendLine("Adventure scene integration audit");

            List<string> missingScriptPaths = FindMissingScriptPaths(scene);
            report.AppendLine($"Missing scripts: {missingScriptPaths.Count}");
            for (int i = 0; i < missingScriptPaths.Count; i++)
                report.AppendLine("- " + missingScriptPaths[i]);

            CookingGamePanel cookingPanel = UnityEngine.Object.FindFirstObjectByType<CookingGamePanel>(
                FindObjectsInactive.Include);
            AppendReferenceStatus(report, cookingPanel, "recipeIngredientChoiceSource");
            AppendReferenceStatus(report, cookingPanel, "miniGameView");
            AppendReferenceStatus(report, cookingPanel, "resultView");

            CookingBusinessFlowController businessFlow = UnityEngine.Object.FindFirstObjectByType<CookingBusinessFlowController>(
                FindObjectsInactive.Include);
            AppendReferenceStatus(report, businessFlow, "gamePanel");
            AppendReferenceStatus(report, businessFlow, "encounterDirector");
            AppendReferenceStatus(report, businessFlow, "npcRunner");
            AppendReferenceStatus(report, businessFlow, "gameTimeService");

            GameObject dispatchRoot = GameObject.Find("DispatchUIRoot");
            AppendReferenceStatus(report, dispatchRoot != null ? dispatchRoot.GetComponent<DispatchManager>() : null, "catalog");
            AppendReferenceStatus(report, dispatchRoot != null ? dispatchRoot.GetComponent<DispatchManager>() : null, "gameTimeService");
            AppendReferenceStatus(report, dispatchRoot != null ? dispatchRoot.GetComponent<DispatchManager>() : null, "playerInventory");
            AppendReferenceStatus(report, dispatchRoot != null ? dispatchRoot.GetComponent<DispatchNpcQuery>() : null, "encounterDirector");
            AppendReferenceStatus(report, dispatchRoot != null ? dispatchRoot.GetComponent<DispatchScreenPresenter>() : null, "dispatchManager");

            Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Dungeon Dinner/Dispatch/Apply Adventure Scene Integration")]
        public static void Apply()
        {
            DispatchProjectSetup.Run();

            Scene targetScene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
            CookingGamePanel cookingPanel = UnityEngine.Object.FindFirstObjectByType<CookingGamePanel>(
                FindObjectsInactive.Include);
            if (cookingPanel == null)
                throw new InvalidOperationException("Adventure scene에서 CookingGamePanel을 찾을 수 없습니다.");

            CookingRecipeIngredientChoiceSource ingredientChoiceSource =
                cookingPanel.GetComponent<CookingRecipeIngredientChoiceSource>();
            if (ingredientChoiceSource == null)
                ingredientChoiceSource = cookingPanel.gameObject.AddComponent<CookingRecipeIngredientChoiceSource>();
            SetReference(cookingPanel, "recipeIngredientChoiceSource", ingredientChoiceSource);

            GameObject miniGameOverlay = FindSceneObject(targetScene, MiniGameOverlayName);
            if (miniGameOverlay == null)
            {
                GameObject overlayPrefab = CreateOrUpdateMiniGameOverlayPrefab();
                miniGameOverlay = (GameObject)PrefabUtility.InstantiatePrefab(overlayPrefab, targetScene);
                miniGameOverlay.name = MiniGameOverlayName;
            }

            Transform overlayParent = ResolveTargetOverlayParent(targetScene);
            if (overlayParent != null && miniGameOverlay.transform.parent != overlayParent)
                miniGameOverlay.transform.SetParent(overlayParent, false);

            NormalizeFullScreenRect(miniGameOverlay.transform as RectTransform);
            miniGameOverlay.SetActive(false);
            SetReference(cookingPanel, "miniGameView", miniGameOverlay);

            GameObject resultView = FindSceneObject(targetScene, ResultViewName);
            if (resultView == null)
            {
                GameObject resultViewPrefab = CreateOrUpdateReferencePrefab(
                    ResultViewName,
                    ResultViewPrefabPath);
                resultView = (GameObject)PrefabUtility.InstantiatePrefab(resultViewPrefab, targetScene);
                resultView.name = ResultViewName;
            }

            if (overlayParent != null && resultView.transform.parent != overlayParent)
                resultView.transform.SetParent(overlayParent, false);
            NormalizeFullScreenRect(resultView.transform as RectTransform);
            resultView.SetActive(false);
            SetReference(cookingPanel, "resultView", resultView);

            CookingBusinessFlowController businessFlow = UnityEngine.Object.FindFirstObjectByType<CookingBusinessFlowController>(
                FindObjectsInactive.Include);
            NpcEncounterDirector encounterDirector = UnityEngine.Object.FindFirstObjectByType<NpcEncounterDirector>(
                FindObjectsInactive.Include);
            NpcConversationRunner npcRunner = UnityEngine.Object.FindFirstObjectByType<NpcConversationRunner>(
                FindObjectsInactive.Include);
            SetReference(businessFlow, "gamePanel", cookingPanel);
            SetReference(businessFlow, "encounterDirector", encounterDirector);
            SetReference(businessFlow, "npcRunner", npcRunner);

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            AssetDatabase.SaveAssets();
            Debug.Log("Adventure scene integration applied.");
            Audit();
        }

        private static GameObject CreateOrUpdateMiniGameOverlayPrefab()
        {
            return CreateOrUpdateReferencePrefab(MiniGameOverlayName, MiniGamePrefabPath);
        }

        private static GameObject CreateOrUpdateReferencePrefab(string sourceObjectName, string prefabPath)
        {
            EnsureFolder(MiniGamePrefabFolder);
            Scene targetScene = SceneManager.GetActiveScene();
            Scene sourceScene = EditorSceneManager.OpenScene(CookReferenceScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject sourceOverlay = FindSceneObject(sourceScene, sourceObjectName);
                if (sourceOverlay == null)
                    throw new InvalidOperationException($"CookTestScene에서 {sourceObjectName} 오브젝트를 찾을 수 없습니다.");

                GameObject temporaryClone = UnityEngine.Object.Instantiate(sourceOverlay);
                temporaryClone.name = sourceObjectName;
                temporaryClone.transform.SetParent(null, false);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporaryClone, prefabPath);
                UnityEngine.Object.DestroyImmediate(temporaryClone);
                if (prefab == null)
                    throw new InvalidOperationException("조리 미니게임 오버레이 프리팹을 만들지 못했습니다.");

                return prefab;
            }
            finally
            {
                EditorSceneManager.CloseScene(sourceScene, true);
                if (targetScene.IsValid())
                    SceneManager.SetActiveScene(targetScene);
            }
        }

        private static Transform ResolveTargetOverlayParent(Scene scene)
        {
            GameObject rewardOverlay = FindSceneObject(scene, "CookingRewardOverlayRoot");
            if (rewardOverlay != null)
                return rewardOverlay.transform;

            GameObject cookCanvas = FindSceneObject(scene, "CookUICanvas");
            return cookCanvas != null ? cookCanvas.transform : null;
        }

        private static void NormalizeFullScreenRect(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static List<string> FindMissingScriptPaths(Scene scene)
        {
            List<string> paths = new List<string>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    GameObject target = transforms[j].gameObject;
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);
                    for (int k = 0; k < missingCount; k++)
                        paths.Add(GetHierarchyPath(target));
                }
            }
            return paths;
        }

        private static string GetHierarchyPath(GameObject target)
        {
            string path = target.name;
            Transform parent = target.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            if (scene.IsValid() == false || string.IsNullOrWhiteSpace(objectName))
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (string.Equals(transforms[j].name, objectName, StringComparison.Ordinal))
                        return transforms[j].gameObject;
                }
            }
            return null;
        }

        private static void AppendReferenceStatus(StringBuilder report, UnityEngine.Object target, string propertyName)
        {
            if (target == null)
            {
                report.AppendLine($"{propertyName}: target missing");
                return;
            }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            string status = property != null && property.objectReferenceValue != null
                ? property.objectReferenceValue.name
                : "MISSING";
            report.AppendLine($"{target.GetType().Name}.{propertyName}: {status}");
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Serialized property not found: {target.GetType().Name}.{propertyName}");

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) == false && AssetDatabase.IsValidFolder(parent) == false)
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
