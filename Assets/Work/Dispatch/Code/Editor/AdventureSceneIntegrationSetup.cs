using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.Adventure.Code;
using Work.Cook.Code.Info;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Dispatch.Code.Runtime;
using Work.Dispatch.Code.UI;
using Work.NPC.Code.Runtime;
using Work.TimeSystem;

namespace Work.Dispatch.Code.Editor
{
    /// <summary>
    /// AdventureTestScene을 기반으로 만든 DungeonDinnerScene에 CookTestScene의 최신 조리 구성을 동기화합니다.
    /// 원본 테스트 씬은 수정하지 않으며 반복 실행해도 프리팹이나 씬 오브젝트를 중복 생성하지 않습니다.
    /// </summary>
    public static class AdventureSceneIntegrationSetup
    {
        private const string AdventureScenePath = "Assets/Work/Adventure/Scene/AdventureTestScene.unity";
        private const string CookReferenceScenePath = "Assets/Work/Cook/Scene/CookTestScene.unity";
        private const string IntegrationSceneFolder = "Assets/Work/Integration/Scene";
        public const string IntegrationScenePath = IntegrationSceneFolder + "/DungeonDinnerScene.unity";
        private const string TitleScenePath = "Assets/Work/Title/Scene/TitleScene.unity";

        public const string CookingPresentationPrefabPath =
            "Assets/Work/Cook/Prefabs/UI/CookingPresentationRoot.prefab";
        private const string LegacyMiniGamePrefabPath =
            "Assets/Work/Cook/Prefabs/UI/CookingMiniGameOverlayRoot.prefab";
        private const string LegacyResultPrefabPath =
            "Assets/Work/Cook/Prefabs/UI/CookingResultPresentationRoot.prefab";

        private const string CookingPresentationRootName = "CookingPresentationRoot";
        private const string CookingViewRootName = "CookingViewRoot";
        private const string MiniGameOverlayName = "CookingMiniGameOverlayRoot";
        private const string ResultViewName = "CookingResultPresentationRoot";
        private const string KnowledgeUpdateViewName = "CookingKnowledgeUpdateView";
        private const string RewardToastViewName = "CookingRewardToastRoot";

        private static readonly string[] CookingPanelSettings =
        {
            "initialScreen",
            "applyInitialScreenOnAwake",
            "resetFlowWhenOpeningRecipeSelection",
            "resetFlowAfterHandingDish",
            "autoOpenInventoryWhenNpcReady",
            "keepNpcConversationVisibleBeforePreparation",
            "keepNpcConversationVisibleDuringCooking",
            "keepRecipeSelectionVisibleBeforePreparation",
            "keepRecipeSelectionVisibleDuringInventory",
            "allowLayeredPrimaryViews",
            "allowRecipeConfirmation",
            "temporaryUiFontAsset",
            "autoCreateRewardSystems"
        };

        private static readonly string[] KnowledgeStoreSettings =
        {
            "catalog",
            "initialDiscoveredRecipes",
            "initialKnownRecipeTags",
            "initialKnownPreparationEffects",
            "loadFromPlayerPrefsOnAwake",
            "saveToPlayerPrefs",
            "playerPrefsKey"
        };

        private static readonly string[] RewardCalculatorSettings =
        {
            "disgustingReward",
            "wrongReward",
            "similarReward",
            "correctReward",
            "perfectReward",
            "perfectDishBonus",
            "goodDishBonus",
            "normalDishBonus",
            "qualityScoreBonusPerPoint",
            "qualityScorePenaltyPerPoint"
        };

        private static readonly string[] RecipeSelectionSettings =
        {
            "fallbackCatalog",
            "uncategorizedCategoryDisplayName",
            "defaultCategoryIcon",
            "recipeDisplayViewType",
            "showAllRecipeNamesInEncyclopedia",
            "showAllRecipesUntilKnowledgeStoreExists",
            "showBaseTagsAsKnownForTesting",
            "discoveredRecipes",
            "knownRecipeTags",
            "encyclopediaOnlyMode",
            "includeDirectIngredientSelection",
            "directSelectionDisplayName",
            "directSelectionDescription",
            "directSelectionIcon",
            "refreshOnEnable"
        };

        private static readonly string[] IngredientSelectionSettings =
        {
            "searchIngredientSourceInParents",
            "searchIngredientSourceInChildren",
            "availableIngredientButtonPrefab",
            "selectedIngredientButtonPrefab",
            "minSelectedIngredients",
            "maxSelectedIngredients",
            "showIngredientQuantities",
            "hideUnavailableIngredients",
            "fontAsset",
            "availableTitleText",
            "selectedTitleText",
            "emptyAvailableText",
            "emptySearchResultText",
            "emptySelectedText",
            "emptyIngredientDetailText"
        };

        [MenuItem("Tools/Dungeon Dinner/Dispatch/Audit Adventure Scene Integration")]
        public static void Audit()
        {
            AuditScene(AdventureScenePath, "Adventure scene integration audit", false, false);
        }

        [MenuItem("Tools/Dungeon Dinner/Integration/Audit DungeonDinner Scene")]
        public static void AuditRuntimeIntegrationScene()
        {
            AuditScene(IntegrationScenePath, "DungeonDinner runtime integration audit", true, false);
        }

        [MenuItem("Tools/Dungeon Dinner/Integration/Create DungeonDinner Scene From Adventure")]
        public static void CreateRuntimeIntegrationSceneFromAdventure()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath) != null)
            {
                throw new InvalidOperationException(
                    $"통합 씬이 이미 존재합니다. 기존 수작업을 보호하기 위해 덮어쓰지 않습니다: {IntegrationScenePath}");
            }

            EnsureFolder(IntegrationSceneFolder);
            if (AssetDatabase.CopyAsset(AdventureScenePath, IntegrationScenePath) == false)
                throw new InvalidOperationException($"통합 씬을 복제하지 못했습니다: {IntegrationScenePath}");

            AssetDatabase.ImportAsset(IntegrationScenePath, ImportAssetOptions.ForceSynchronousImport);
            RegisterIntegrationSceneInBuildSettings();
            ConfigureTitleStartScene();
            AssetDatabase.SaveAssets();

            Apply();
            Debug.Log($"DungeonDinner runtime integration scene created: {IntegrationScenePath}");
        }

        [MenuItem("Tools/Dungeon Dinner/Integration/Sync DungeonDinner Scene")]
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"통합 씬을 찾을 수 없습니다. 먼저 생성 메뉴를 실행하세요: {IntegrationScenePath}");
            }

            Scene targetScene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (FindSceneObject(targetScene, "DispatchUIRoot") == null)
            {
                throw new InvalidOperationException(
                    "DungeonDinnerScene에 AdventureTestScene의 DispatchUIRoot가 없습니다. 원본 씬을 수정하지 않기 위해 자동 생성하지 않습니다.");
            }

            CookingGamePanel cookingPanel = GetSingleSceneComponent<CookingGamePanel>(targetScene, "CookingGamePanel");
            CookingRecipeIngredientChoiceSource ingredientChoiceSource =
                cookingPanel.GetComponent<CookingRecipeIngredientChoiceSource>();
            if (ingredientChoiceSource == null)
                ingredientChoiceSource = cookingPanel.gameObject.AddComponent<CookingRecipeIngredientChoiceSource>();
            SetReference(cookingPanel, "recipeIngredientChoiceSource", ingredientChoiceSource);

            RemoveLegacyCookingPresentation(targetScene);
            RemoveSavedDictionaryClones(targetScene);

            GameObject presentationRoot = EnsureCookingPresentationRoot(targetScene);
            CopyCookReferenceConfiguration(targetScene, presentationRoot, cookingPanel);
            BindCookingPresentation(targetScene, presentationRoot, cookingPanel);

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            AssetDatabase.SaveAssets();

            Debug.Log("DungeonDinner scene cooking presentation synchronized from CookTestScene.");
            AuditScene(IntegrationScenePath, "DungeonDinner runtime integration audit", true, true);
        }

        private static GameObject EnsureCookingPresentationRoot(Scene targetScene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CookingPresentationPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"최신 조리 프레젠테이션 프리팹을 찾을 수 없습니다: {CookingPresentationPrefabPath}");

            List<GameObject> instances = FindPrefabInstanceRoots(targetScene, CookingPresentationPrefabPath);
            GameObject instance = instances.Count > 0 ? instances[0] : null;
            for (int i = 1; i < instances.Count; i++)
                UnityEngine.Object.DestroyImmediate(instances[i]);

            if (instance == null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, targetScene) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("CookingPresentationRoot 프리팹을 DungeonDinnerScene에 생성하지 못했습니다.");
            }

            GameObject cookCanvas = FindSceneObject(targetScene, "CookUICanvas");
            if (cookCanvas == null)
                throw new InvalidOperationException("DungeonDinnerScene에서 CookUICanvas를 찾을 수 없습니다.");

            instance.name = CookingPresentationRootName;
            if (instance.transform.parent != cookCanvas.transform)
                instance.transform.SetParent(cookCanvas.transform, false);
            return instance;
        }

        private static void CopyCookReferenceConfiguration(
            Scene targetScene,
            GameObject targetPresentationRoot,
            CookingGamePanel targetPanel)
        {
            Scene sourceScene = EditorSceneManager.OpenScene(CookReferenceScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject sourcePresentationRoot = FindPrefabInstanceRoots(
                    sourceScene,
                    CookingPresentationPrefabPath).Find(root => root != null);
                if (sourcePresentationRoot == null)
                    throw new InvalidOperationException("CookTestScene에 최신 CookingPresentationRoot 프리팹 인스턴스가 없습니다.");

                ApplyPersistentCookPrefabOverrides(sourcePresentationRoot, targetPresentationRoot);

                CookingGamePanel sourcePanel = GetSingleSceneComponent<CookingGamePanel>(
                    sourceScene,
                    "CookTestScene CookingGamePanel");
                CopySerializedProperties(sourcePanel, targetPanel, CookingPanelSettings);
                CopySerializedProperties(sourcePanel.FlowRunner, targetPanel.FlowRunner, "catalog");
                CopyComponentSettings<CookingKnowledgeStore>(
                    sourcePanel.gameObject,
                    targetPanel.gameObject,
                    KnowledgeStoreSettings);
                CopyComponentSettings<CookingRewardWallet>(
                    sourcePanel.gameObject,
                    targetPanel.gameObject,
                    "startingBalance",
                    "loadFromPlayerPrefsOnAwake",
                    "saveToPlayerPrefs",
                    "playerPrefsKey");
                CopyComponentSettings<CookingRewardCalculator>(
                    sourcePanel.gameObject,
                    targetPanel.gameObject,
                    RewardCalculatorSettings);
                CopyComponentSettings<CookingRecipeIngredientChoiceSource>(
                    sourcePanel.gameObject,
                    targetPanel.gameObject,
                    "sourceName");

                CookingRecipeSelectionView sourceRecipeSelection =
                    sourcePanel.RecipeSelectionView != null
                        ? sourcePanel.RecipeSelectionView.GetComponent<CookingRecipeSelectionView>()
                        : null;
                CookingRecipeSelectionView targetRecipeSelection =
                    targetPanel.RecipeSelectionView != null
                        ? targetPanel.RecipeSelectionView.GetComponent<CookingRecipeSelectionView>()
                        : null;
                CopySerializedProperties(sourceRecipeSelection, targetRecipeSelection, RecipeSelectionSettings);

                CookingIngredientSelectionView sourceIngredientSelection =
                    sourcePanel.InventoryView != null
                        ? sourcePanel.InventoryView.GetComponent<CookingIngredientSelectionView>()
                        : null;
                CookingIngredientSelectionView targetIngredientSelection =
                    targetPanel.InventoryView != null
                        ? targetPanel.InventoryView.GetComponent<CookingIngredientSelectionView>()
                        : null;
                CopySerializedProperties(
                    sourceIngredientSelection,
                    targetIngredientSelection,
                    IngredientSelectionSettings);
            }
            finally
            {
                EditorSceneManager.CloseScene(sourceScene, true);
                if (targetScene.IsValid())
                    SceneManager.SetActiveScene(targetScene);
            }
        }

        private static void ApplyPersistentCookPrefabOverrides(GameObject sourceRoot, GameObject targetRoot)
        {
            PropertyModification[] sourceModifications = PrefabUtility.GetPropertyModifications(sourceRoot);
            if (sourceModifications == null)
                return;

            List<PropertyModification> filtered = new List<PropertyModification>();
            for (int i = 0; i < sourceModifications.Length; i++)
            {
                PropertyModification modification = sourceModifications[i];
                if (modification == null)
                    continue;

                UnityEngine.Object referencedObject = modification.objectReference;
                if (referencedObject != null && EditorUtility.IsPersistent(referencedObject) == false)
                    continue;

                filtered.Add(modification);
            }

            PrefabUtility.SetPropertyModifications(targetRoot, filtered.ToArray());
        }

        private static void BindCookingPresentation(
            Scene scene,
            GameObject presentationRoot,
            CookingGamePanel cookingPanel)
        {
            CookingFlowRunner flowRunner = cookingPanel.FlowRunner;
            CookingKnowledgeStore knowledgeStore = cookingPanel.GetComponent<CookingKnowledgeStore>();
            CookingRewardWallet rewardWallet = cookingPanel.GetComponent<CookingRewardWallet>();
            NpcConversationRunner npcRunner = GetSingleSceneComponent<NpcConversationRunner>(scene, "NpcConversationRunner");
            NpcEncounterDirector encounterDirector = GetSingleSceneComponent<NpcEncounterDirector>(scene, "NpcEncounterDirector");

            GameObject dispatchRoot = FindSceneObject(scene, "DispatchUIRoot");
            GameTimeService gameTimeService = dispatchRoot != null ? dispatchRoot.GetComponent<GameTimeService>() : null;
            if (gameTimeService == null)
                throw new InvalidOperationException("DispatchUIRoot에서 GameTimeService를 찾을 수 없습니다.");

            GameObject preparationView = FindDescendant(presentationRoot, CookingViewRootName);
            GameObject miniGameView = FindDescendant(presentationRoot, MiniGameOverlayName);
            GameObject resultView = FindDescendant(presentationRoot, ResultViewName);
            GameObject knowledgeUpdateView = FindDescendant(presentationRoot, KnowledgeUpdateViewName);
            GameObject rewardView = FindDescendant(presentationRoot, RewardToastViewName);

            RequireObject(preparationView, CookingViewRootName);
            RequireObject(miniGameView, MiniGameOverlayName);
            RequireObject(resultView, ResultViewName);
            RequireObject(knowledgeUpdateView, KnowledgeUpdateViewName);
            RequireObject(rewardView, RewardToastViewName);

            SetReference(cookingPanel, "flowRunner", flowRunner);
            SetReference(cookingPanel, "npcRunner", npcRunner);
            SetReference(cookingPanel, "knowledgeStore", knowledgeStore);
            SetReference(cookingPanel, "rewardWallet", rewardWallet);
            SetReference(cookingPanel, "preparationView", preparationView);
            SetReference(cookingPanel, "miniGameView", miniGameView);
            SetReference(cookingPanel, "resultView", resultView);
            SetReference(cookingPanel, "knowledgeUpdateView", knowledgeUpdateView);
            SetReference(cookingPanel, "rewardView", rewardView);

            CookingView cookingView = preparationView.GetComponent<CookingView>();
            CookingResultView cookingResultView = resultView.GetComponent<CookingResultView>();
            CookingKnowledgeUpdateView cookingKnowledgeView =
                knowledgeUpdateView.GetComponent<CookingKnowledgeUpdateView>();
            CookingRewardToastView rewardToastView = rewardView.GetComponent<CookingRewardToastView>();
            CookingBusinessFlowController businessFlow =
                presentationRoot.GetComponentInChildren<CookingBusinessFlowController>(true);
            NpcOrderSlipPanel orderSlipPanel = presentationRoot.GetComponentInChildren<NpcOrderSlipPanel>(true);

            SetReference(cookingView, "gamePanel", cookingPanel);
            SetReference(cookingView, "flowRunner", flowRunner);
            SetReference(cookingView, "knowledgeStore", knowledgeStore);
            SetReference(cookingResultView, "gamePanel", cookingPanel);
            SetReference(cookingResultView, "flowRunner", flowRunner);
            SetReference(cookingKnowledgeView, "gamePanel", cookingPanel);
            SetReference(cookingKnowledgeView, "knowledgeStore", knowledgeStore);
            SetReference(rewardToastView, "gamePanel", cookingPanel);
            SetReference(rewardToastView, "rewardWallet", rewardWallet);

            SetReference(businessFlow, "gamePanel", cookingPanel);
            SetReference(businessFlow, "encounterDirector", encounterDirector);
            SetReference(businessFlow, "npcRunner", npcRunner);
            SetReference(businessFlow, "gameTimeService", gameTimeService);
            SetReference(cookingPanel, "businessFlowController", businessFlow);
            SetReference(npcRunner, "orderSlipPanel", orderSlipPanel);
        }

        private static void RemoveLegacyCookingPresentation(Scene scene)
        {
            HashSet<GameObject> removalRoots = new HashSet<GameObject>();
            List<GameObject> allObjects = GetAllSceneObjects(scene);
            for (int i = 0; i < allObjects.Count; i++)
            {
                GameObject candidate = allObjects[i];
                if (candidate == null || IsInsideLatestPresentation(candidate))
                    continue;

                string prefabPath = NormalizePath(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate));
                if (string.Equals(prefabPath, LegacyMiniGamePrefabPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefabPath, LegacyResultPrefabPath, StringComparison.OrdinalIgnoreCase))
                {
                    GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(candidate);
                    removalRoots.Add(instanceRoot != null ? instanceRoot : candidate);
                    continue;
                }

                string objectName = candidate.name;
                if (string.Equals(objectName, "TemporaryPreparationView", StringComparison.Ordinal)
                    || string.Equals(objectName, "TemporaryResultView", StringComparison.Ordinal)
                    || string.Equals(objectName, "CookingRewardOverlayRoot", StringComparison.Ordinal))
                {
                    removalRoots.Add(candidate);
                }
            }

            CookingPreparationView[] legacyViews = GetSceneComponents<CookingPreparationView>(scene);
            for (int i = 0; i < legacyViews.Length; i++)
            {
                if (legacyViews[i] != null && IsInsideLatestPresentation(legacyViews[i].gameObject) == false)
                    removalRoots.Add(legacyViews[i].gameObject);
            }

            DestroyTopLevelCandidates(removalRoots);
        }

        private static void RemoveSavedDictionaryClones(Scene scene)
        {
            HashSet<GameObject> removalRoots = new HashSet<GameObject>();
            InfoDictionaryPanel[] panels = GetSceneComponents<InfoDictionaryPanel>(scene);
            for (int panelIndex = 0; panelIndex < panels.Length; panelIndex++)
            {
                Transform[] children = panels[panelIndex].GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0; childIndex < children.Length; childIndex++)
                {
                    Transform child = children[childIndex];
                    if (child != null && child != panels[panelIndex].transform
                        && child.name.EndsWith("(Clone)", StringComparison.Ordinal))
                    {
                        removalRoots.Add(child.gameObject);
                    }
                }
            }

            DestroyTopLevelCandidates(removalRoots);
        }

        private static void DestroyTopLevelCandidates(HashSet<GameObject> candidates)
        {
            List<GameObject> ordered = new List<GameObject>(candidates);
            ordered.Sort((left, right) => GetHierarchyDepth(left).CompareTo(GetHierarchyDepth(right)));
            HashSet<GameObject> destroyedRoots = new HashSet<GameObject>();
            for (int i = 0; i < ordered.Count; i++)
            {
                GameObject candidate = ordered[i];
                if (candidate == null || HasAncestorInSet(candidate.transform, destroyedRoots))
                    continue;

                destroyedRoots.Add(candidate);
                UnityEngine.Object.DestroyImmediate(candidate);
            }
        }

        private static bool AuditScene(
            string scenePath,
            string reportTitle,
            bool requireLatestCookingPresentation,
            bool throwOnFailure)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
                throw new InvalidOperationException($"감사할 씬을 찾을 수 없습니다: {scenePath}");

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            StringBuilder report = new StringBuilder();
            report.AppendLine(reportTitle);
            report.AppendLine($"Scene: {scenePath}");
            bool valid = true;

            List<string> missingScriptPaths = FindMissingScriptPaths(scene);
            report.AppendLine($"Missing scripts: {missingScriptPaths.Count}");
            valid &= missingScriptPaths.Count == 0;
            for (int i = 0; i < missingScriptPaths.Count; i++)
                report.AppendLine("- " + missingScriptPaths[i]);

            CookingGamePanel[] cookingPanels = GetSceneComponents<CookingGamePanel>(scene);
            CookingGamePanel cookingPanel = cookingPanels.Length == 1 ? cookingPanels[0] : null;
            valid &= AppendCount(report, nameof(CookingGamePanel), cookingPanels.Length, 1);
            valid &= AppendReferenceStatus(report, cookingPanel, "flowRunner");
            valid &= AppendReferenceStatus(report, cookingPanel, "npcRunner");
            valid &= AppendReferenceStatus(report, cookingPanel, "knowledgeStore");
            valid &= AppendReferenceStatus(report, cookingPanel, "recipeIngredientChoiceSource");
            valid &= AppendReferenceStatus(report, cookingPanel, "rewardWallet");
            valid &= AppendReferenceStatus(report, cookingPanel, "rewardCalculator");
            valid &= AppendReferenceStatus(report, cookingPanel, "businessFlowController");
            valid &= AppendReferenceStatus(report, cookingPanel, "preparationView");
            valid &= AppendReferenceStatus(report, cookingPanel, "miniGameView");
            valid &= AppendReferenceStatus(report, cookingPanel, "resultView");
            valid &= AppendReferenceStatus(report, cookingPanel, "knowledgeUpdateView");
            valid &= AppendReferenceStatus(report, cookingPanel, "rewardView");

            CookingBusinessFlowController[] businessFlows =
                GetSceneComponents<CookingBusinessFlowController>(scene);
            CookingBusinessFlowController businessFlow = businessFlows.Length == 1 ? businessFlows[0] : null;
            valid &= AppendCount(report, nameof(CookingBusinessFlowController), businessFlows.Length, 1);
            valid &= AppendReferenceStatus(report, businessFlow, "gamePanel");
            valid &= AppendReferenceStatus(report, businessFlow, "encounterDirector");
            valid &= AppendReferenceStatus(report, businessFlow, "npcRunner");
            valid &= AppendReferenceStatus(report, businessFlow, "gameTimeService");

            GameObject dispatchRoot = FindSceneObject(scene, "DispatchUIRoot");
            valid &= AppendCount(report, "DispatchUIRoot", dispatchRoot != null ? 1 : 0, 1);
            DispatchManager dispatchManager = dispatchRoot != null ? dispatchRoot.GetComponent<DispatchManager>() : null;
            valid &= AppendReferenceStatus(report, dispatchManager, "catalog");
            valid &= AppendReferenceStatus(report, dispatchManager, "gameTimeService");
            valid &= AppendReferenceStatus(report, dispatchManager, "playerInventory");
            valid &= AppendReferenceStatus(
                report,
                dispatchRoot != null ? dispatchRoot.GetComponent<DispatchNpcQuery>() : null,
                "encounterDirector");
            valid &= AppendReferenceStatus(
                report,
                dispatchRoot != null ? dispatchRoot.GetComponent<DispatchScreenPresenter>() : null,
                "dispatchManager");

            valid &= AppendCount(report, nameof(AdventureManager), GetSceneComponents<AdventureManager>(scene).Length, 1);
            valid &= AppendCount(report, nameof(PreparationManager), GetSceneComponents<PreparationManager>(scene).Length, 1);
            valid &= AppendCount(report, nameof(GameTimeService), GetSceneComponents<GameTimeService>(scene).Length, 1);

            if (requireLatestCookingPresentation)
                valid &= AuditLatestCookingPresentation(report, scene, cookingPanel, businessFlow);

            if (valid)
                Debug.Log(report.ToString());
            else
                Debug.LogError(report.ToString());

            if (valid == false && throwOnFailure)
                throw new InvalidOperationException($"씬 통합 감사에 실패했습니다: {scenePath}\n{report}");

            return valid;
        }

        private static bool AuditLatestCookingPresentation(
            StringBuilder report,
            Scene scene,
            CookingGamePanel cookingPanel,
            CookingBusinessFlowController businessFlow)
        {
            bool valid = true;
            List<GameObject> presentationRoots = FindPrefabInstanceRoots(scene, CookingPresentationPrefabPath);
            valid &= AppendCount(report, CookingPresentationRootName, presentationRoots.Count, 1);
            GameObject presentationRoot = presentationRoots.Count == 1 ? presentationRoots[0] : null;

            valid &= AppendCount(
                report,
                nameof(CookingPreparationView) + " legacy views",
                GetSceneComponents<CookingPreparationView>(scene).Length,
                0);
            valid &= AppendCount(
                report,
                "legacy split cooking prefabs",
                FindPrefabInstanceRoots(scene, LegacyMiniGamePrefabPath).Count
                + FindPrefabInstanceRoots(scene, LegacyResultPrefabPath).Count,
                0);
            valid &= AppendCount(
                report,
                "saved dictionary clones",
                CountSavedDictionaryClones(scene),
                0);

            valid &= AppendReferenceInsideRootStatus(report, cookingPanel, "preparationView", presentationRoot);
            valid &= AppendReferenceInsideRootStatus(report, cookingPanel, "miniGameView", presentationRoot);
            valid &= AppendReferenceInsideRootStatus(report, cookingPanel, "resultView", presentationRoot);
            valid &= AppendReferenceInsideRootStatus(report, cookingPanel, "knowledgeUpdateView", presentationRoot);
            valid &= AppendReferenceInsideRootStatus(report, cookingPanel, "rewardView", presentationRoot);
            valid &= AppendReferenceInsideRootStatus(report, cookingPanel, "businessFlowController", presentationRoot);

            NpcConversationRunner[] npcRunners = GetSceneComponents<NpcConversationRunner>(scene);
            NpcConversationRunner npcRunner = npcRunners.Length == 1 ? npcRunners[0] : null;
            valid &= AppendCount(report, nameof(NpcConversationRunner), npcRunners.Length, 1);
            valid &= AppendReferenceInsideRootStatus(report, npcRunner, "orderSlipPanel", presentationRoot);

            GameObject dispatchRoot = FindSceneObject(scene, "DispatchUIRoot");
            GameTimeService gameTimeService = dispatchRoot != null ? dispatchRoot.GetComponent<GameTimeService>() : null;
            valid &= AppendExpectedReferenceStatus(report, businessFlow, "gameTimeService", gameTimeService);

            GameObject preparationView = GetReference(cookingPanel, "preparationView") as GameObject;
            bool usesLatestCookingView = preparationView != null && preparationView.GetComponent<CookingView>() != null;
            report.AppendLine($"Preparation uses CookingView: {usesLatestCookingView}");
            valid &= usesLatestCookingView;
            return valid;
        }

        private static int CountSavedDictionaryClones(Scene scene)
        {
            int count = 0;
            InfoDictionaryPanel[] panels = GetSceneComponents<InfoDictionaryPanel>(scene);
            for (int panelIndex = 0; panelIndex < panels.Length; panelIndex++)
            {
                Transform[] children = panels[panelIndex].GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0; childIndex < children.Length; childIndex++)
                {
                    if (children[childIndex] != null
                        && children[childIndex] != panels[panelIndex].transform
                        && children[childIndex].name.EndsWith("(Clone)", StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private static void RegisterIntegrationSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(scenes[i].path, IntegrationScenePath, StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                scenes[i] = new EditorBuildSettingsScene(IntegrationScenePath, true);
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            int titleIndex = scenes.FindIndex(scene =>
                string.Equals(scene.path, TitleScenePath, StringComparison.OrdinalIgnoreCase));
            int insertionIndex = titleIndex >= 0 ? titleIndex + 1 : scenes.Count;
            scenes.Insert(insertionIndex, new EditorBuildSettingsScene(IntegrationScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureTitleStartScene()
        {
            Scene titleScene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            TitleUIManager titleManager = UnityEngine.Object.FindFirstObjectByType<TitleUIManager>(
                FindObjectsInactive.Include);
            if (titleManager == null)
                throw new InvalidOperationException("TitleScene에서 TitleUIManager를 찾을 수 없습니다.");

            SerializedObject serializedTitle = new SerializedObject(titleManager);
            SerializedProperty startSceneName = serializedTitle.FindProperty("startSceneName");
            if (startSceneName == null)
                throw new InvalidOperationException("TitleUIManager.startSceneName 필드를 찾을 수 없습니다.");

            startSceneName.stringValue = System.IO.Path.GetFileNameWithoutExtension(IntegrationScenePath);
            serializedTitle.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(titleManager);
            EditorSceneManager.MarkSceneDirty(titleScene);
            EditorSceneManager.SaveScene(titleScene);
        }

        private static void CopyComponentSettings<T>(
            GameObject sourceOwner,
            GameObject targetOwner,
            params string[] propertyNames) where T : Component
        {
            T source = sourceOwner != null ? sourceOwner.GetComponent<T>() : null;
            T target = targetOwner != null ? targetOwner.GetComponent<T>() : null;
            CopySerializedProperties(source, target, propertyNames);
        }

        private static void CopySerializedProperties(
            UnityEngine.Object source,
            UnityEngine.Object target,
            params string[] propertyNames)
        {
            if (source == null || target == null)
                throw new InvalidOperationException("CookTestScene 조리 설정을 복사할 대상 컴포넌트가 없습니다.");

            SerializedObject sourceSerialized = new SerializedObject(source);
            SerializedObject targetSerialized = new SerializedObject(target);
            for (int i = 0; i < propertyNames.Length; i++)
            {
                SerializedProperty sourceProperty = sourceSerialized.FindProperty(propertyNames[i]);
                SerializedProperty targetProperty = targetSerialized.FindProperty(propertyNames[i]);
                if (sourceProperty == null || targetProperty == null)
                {
                    throw new InvalidOperationException(
                        $"조리 설정 필드를 찾을 수 없습니다: {source.GetType().Name}.{propertyNames[i]}");
                }

                targetSerialized.CopyFromSerializedProperty(sourceProperty);
            }

            targetSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static T GetSingleSceneComponent<T>(Scene scene, string label) where T : Component
        {
            T[] components = GetSceneComponents<T>(scene);
            if (components.Length != 1)
                throw new InvalidOperationException($"{label} 개수가 1이 아닙니다: {components.Length}");
            return components[0];
        }

        private static T[] GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            if (scene.IsValid() == false)
                return results.ToArray();

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return results.ToArray();
        }

        private static List<GameObject> GetAllSceneObjects(Scene scene)
        {
            List<GameObject> results = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                    results.Add(transforms[j].gameObject);
            }
            return results;
        }

        private static List<GameObject> FindPrefabInstanceRoots(Scene scene, string prefabPath)
        {
            List<GameObject> results = new List<GameObject>();
            string normalizedPath = NormalizePath(prefabPath);
            List<GameObject> objects = GetAllSceneObjects(scene);
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null || PrefabUtility.IsAnyPrefabInstanceRoot(candidate) == false)
                    continue;

                string candidatePath = NormalizePath(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate));
                if (string.Equals(candidatePath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                    results.Add(candidate);
            }
            return results;
        }

        private static bool IsInsideLatestPresentation(GameObject candidate)
        {
            Transform current = candidate != null ? candidate.transform : null;
            while (current != null)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(current.gameObject))
                {
                    string path = NormalizePath(
                        PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject));
                    if (string.Equals(path, CookingPresentationPrefabPath, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                current = current.parent;
            }
            return false;
        }

        private static GameObject FindDescendant(GameObject root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, objectName, StringComparison.Ordinal))
                    return transforms[i].gameObject;
            }
            return null;
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

        private static int GetHierarchyDepth(GameObject target)
        {
            int depth = 0;
            Transform current = target != null ? target.transform.parent : null;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }
            return depth;
        }

        private static bool HasAncestorInSet(Transform target, HashSet<GameObject> candidates)
        {
            Transform current = target != null ? target.parent : null;
            while (current != null)
            {
                if (candidates.Contains(current.gameObject))
                    return true;
                current = current.parent;
            }
            return false;
        }

        private static bool IsDescendantOrSelf(GameObject candidate, GameObject root)
        {
            if (candidate == null || root == null)
                return false;
            Transform current = candidate.transform;
            while (current != null)
            {
                if (current.gameObject == root)
                    return true;
                current = current.parent;
            }
            return false;
        }

        private static bool AppendCount(
            StringBuilder report,
            string label,
            int actual,
            int expected)
        {
            bool valid = actual == expected;
            report.AppendLine($"{label}: {actual} (expected {expected})");
            return valid;
        }

        private static bool AppendReferenceStatus(
            StringBuilder report,
            UnityEngine.Object target,
            string propertyName)
        {
            UnityEngine.Object value = GetReference(target, propertyName);
            bool valid = value != null;
            string targetName = target != null ? target.GetType().Name : "missing target";
            report.AppendLine($"{targetName}.{propertyName}: {(valid ? value.name : "MISSING")}");
            return valid;
        }

        private static bool AppendReferenceInsideRootStatus(
            StringBuilder report,
            UnityEngine.Object target,
            string propertyName,
            GameObject expectedRoot)
        {
            UnityEngine.Object value = GetReference(target, propertyName);
            GameObject valueObject = value as GameObject;
            if (valueObject == null && value is Component component)
                valueObject = component.gameObject;

            bool valid = valueObject != null && IsDescendantOrSelf(valueObject, expectedRoot);
            string targetName = target != null ? target.GetType().Name : "missing target";
            report.AppendLine(
                $"{targetName}.{propertyName} inside {CookingPresentationRootName}: {valid} "
                + $"({(valueObject != null ? valueObject.name : "MISSING")})");
            return valid;
        }

        private static bool AppendExpectedReferenceStatus(
            StringBuilder report,
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object expected)
        {
            UnityEngine.Object value = GetReference(target, propertyName);
            bool valid = value != null && value == expected;
            string targetName = target != null ? target.GetType().Name : "missing target";
            report.AppendLine(
                $"{targetName}.{propertyName} expected reference: {valid} "
                + $"({(value != null ? value.name : "MISSING")})");
            return valid;
        }

        private static UnityEngine.Object GetReference(UnityEngine.Object target, string propertyName)
        {
            if (target == null)
                return null;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue : null;
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            if (target == null)
                throw new InvalidOperationException($"{propertyName} 참조를 연결할 대상이 없습니다.");
            if (value == null)
                throw new InvalidOperationException($"{target.GetType().Name}.{propertyName}에 연결할 오브젝트가 없습니다.");

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Serialized property not found: {target.GetType().Name}.{propertyName}");

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void RequireObject(GameObject value, string objectName)
        {
            if (value == null)
                throw new InvalidOperationException($"CookingPresentationRoot에서 {objectName} 오브젝트를 찾을 수 없습니다.");
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace('\\', '/');
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
