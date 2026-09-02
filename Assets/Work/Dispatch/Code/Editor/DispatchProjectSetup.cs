using System;
using System.Collections.Generic;
using System.IO;
using Assets.Work.Adventure.Code;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Work.Adventure.Code;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Systems;
using Work.Dispatch.Code.Data;
using Work.Dispatch.Code.Runtime;
using Work.Dispatch.Code.UI;
using Work.Items.Code;
using Work.NPC.Code.Runtime;
using Work.Players.Code.Inventory;
using Work.TimeSystem;

namespace Work.Dispatch.Code.Editor
{
    public static class DispatchProjectSetup
    {
        private const string ItemCatalogPath = "Assets/Work/Items/SO/ItemCatalog.asset";
        private const string DispatchDataFolder = "Assets/Work/Dispatch/Data";
        private const string DispatchRegionFolder = DispatchDataFolder + "/Regions";
        private const string DispatchCatalogPath = DispatchDataFolder + "/DispatchCatalog.asset";
        private const string PanelSettingsPath = "Assets/Work/Dispatch/UI/DispatchPanelSettings.asset";
        private const string PrefabFolder = "Assets/Work/Dispatch/Prefabs";
        private const string PrefabPath = PrefabFolder + "/DispatchUIRoot.prefab";
        private const string AdventureScenePath = "Assets/Work/Adventure/Scene/AdventureTestScene.unity";

        [MenuItem("Tools/Dungeon Dinner/Dispatch/Setup Dispatch System")]
        public static void Run()
        {
            EnsureFolder(DispatchDataFolder);
            EnsureFolder(DispatchRegionFolder);
            EnsureFolder(PrefabFolder);

            ItemCatalogSO itemCatalog = CreateOrUpdateItemCatalog();
            MapInfoSO mossCave = ConfigureMap("Assets/Work/Adventure/SO/Map/MossCave.asset", "MossCave");
            MapInfoSO volcano = ConfigureMap("Assets/Work/Adventure/SO/Map/VolcanicZone.asset", "Volcano");

            DispatchRegionSO mossDispatch = CreateOrUpdateRegion(
                DispatchRegionFolder + "/MossCaveDispatch.asset",
                mossCave,
                2,
                itemCatalog,
                new[]
                {
                    new MaterialSpec("Cooking_mushroom_cap", 10, 2, 1, 60, 100),
                    new MaterialSpec("Cooking_flat_mushroom", 10, 2, 1, 60, 100),
                    new MaterialSpec("Cooking_rock_salt", 8, 1, 1, 50, 90)
                },
                new[]
                {
                    new RareSpec("Cooking_slime_nucleus", 2, 1, 1),
                    new RareSpec("Cooking_slime_mucus", 3, 1, 2)
                });

            DispatchRegionSO volcanoDispatch = CreateOrUpdateRegion(
                DispatchRegionFolder + "/VolcanoDispatch.asset",
                volcano,
                3,
                itemCatalog,
                new[]
                {
                    new MaterialSpec("Cooking_rock_salt", 10, 1, 1, 60, 100),
                    new MaterialSpec("Cooking_corn_cheese", 8, 2, 1, 50, 90),
                    new MaterialSpec("Cooking_slime_mucus", 8, 2, 1, 50, 90)
                },
                new[]
                {
                    new RareSpec("Cooking_slime_nucleus", 1, 1, 1)
                });

            DispatchCatalogSO dispatchCatalog = CreateOrUpdateDispatchCatalog(
                itemCatalog,
                new[] { mossDispatch, volcanoDispatch });
            PanelSettings panelSettings = CreateOrUpdatePanelSettings();
            GameObject prefab = CreateOrUpdatePrefab(dispatchCatalog, panelSettings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            InstallIntoAdventureScene(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("Dispatch system setup completed.");
        }

        private static ItemCatalogSO CreateOrUpdateItemCatalog()
        {
            ItemCatalogSO catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(ItemCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalogSO>();
                AssetDatabase.CreateAsset(catalog, ItemCatalogPath);
            }

            string[] guids = AssetDatabase.FindAssets("t:ItemDataSO");
            List<ItemDataSO> items = new List<ItemDataSO>();
            for (int i = 0; i < guids.Length; i++)
            {
                ItemDataSO item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (item != null)
                    items.Add(item);
            }
            items.Sort((left, right) => string.Compare(left.ItemId, right.ItemId, StringComparison.OrdinalIgnoreCase));

            SerializedObject serialized = new SerializedObject(catalog);
            SetObjectArray(serialized.FindProperty("items"), items);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            catalog.RebuildIndex();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static MapInfoSO ConfigureMap(string path, string regionId)
        {
            MapInfoSO map = AssetDatabase.LoadAssetAtPath<MapInfoSO>(path);
            if (map == null)
                throw new InvalidOperationException($"MapInfoSO not found: {path}");

            SerializedObject serialized = new SerializedObject(map);
            SerializedProperty regionIdProperty = serialized.FindProperty("<RegionId>k__BackingField");
            if (regionIdProperty == null)
                throw new InvalidOperationException("MapInfoSO RegionId property was not found.");
            regionIdProperty.stringValue = regionId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(map);
            return map;
        }

        private static DispatchRegionSO CreateOrUpdateRegion(
            string path,
            MapInfoSO map,
            int baseTravelTime,
            ItemCatalogSO itemCatalog,
            IReadOnlyList<MaterialSpec> materials,
            IReadOnlyList<RareSpec> rareRewards)
        {
            DispatchRegionSO region = AssetDatabase.LoadAssetAtPath<DispatchRegionSO>(path);
            if (region == null)
            {
                region = ScriptableObject.CreateInstance<DispatchRegionSO>();
                AssetDatabase.CreateAsset(region, path);
            }

            SerializedObject serialized = new SerializedObject(region);
            serialized.FindProperty("region").objectReferenceValue = map;
            serialized.FindProperty("baseTravelTime").intValue = baseTravelTime;

            SerializedProperty materialArray = serialized.FindProperty("materials");
            materialArray.arraySize = materials.Count;
            for (int i = 0; i < materials.Count; i++)
            {
                MaterialSpec spec = materials[i];
                SerializedProperty element = materialArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = FindIngredientItem(itemCatalog, spec.ItemId);
                element.FindPropertyRelative("maxRequestAmount").intValue = spec.MaxRequest;
                element.FindPropertyRelative("amountPerBatch").intValue = spec.AmountPerBatch;
                element.FindPropertyRelative("timePerBatch").intValue = spec.TimePerBatch;
                element.FindPropertyRelative("minYieldPercent").intValue = spec.MinYield;
                element.FindPropertyRelative("maxYieldPercent").intValue = spec.MaxYield;
            }

            SerializedProperty rareTable = serialized.FindProperty("rareRewards");
            rareTable.FindPropertyRelative("chancePerGatherTime").floatValue = 2f;
            rareTable.FindPropertyRelative("maximumChance").floatValue = 20f;
            SerializedProperty rareArray = rareTable.FindPropertyRelative("rewards");
            rareArray.arraySize = rareRewards.Count;
            for (int i = 0; i < rareRewards.Count; i++)
            {
                RareSpec spec = rareRewards[i];
                SerializedProperty element = rareArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = FindIngredientItem(itemCatalog, spec.ItemId);
                element.FindPropertyRelative("weight").intValue = spec.Weight;
                element.FindPropertyRelative("minAmount").intValue = spec.MinAmount;
                element.FindPropertyRelative("maxAmount").intValue = spec.MaxAmount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(region);
            return region;
        }

        private static DispatchCatalogSO CreateOrUpdateDispatchCatalog(
            ItemCatalogSO itemCatalog,
            IReadOnlyList<DispatchRegionSO> regions)
        {
            DispatchCatalogSO catalog = AssetDatabase.LoadAssetAtPath<DispatchCatalogSO>(DispatchCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DispatchCatalogSO>();
                AssetDatabase.CreateAsset(catalog, DispatchCatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            serialized.FindProperty("itemCatalog").objectReferenceValue = itemCatalog;
            SetObjectArray(serialized.FindProperty("regions"), regions);
            serialized.FindProperty("maxMaterialTypes").intValue = 3;

            SerializedProperty npcRules = serialized.FindProperty("npcRules");
            npcRules.arraySize = 1;
            SerializedProperty odin = npcRules.GetArrayElementAtIndex(0);
            odin.FindPropertyRelative("npcId").stringValue = "Odin";
            odin.FindPropertyRelative("requiredAffinity").intValue = 5;
            odin.FindPropertyRelative("timeMultiplierPercent").intValue = 100;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static PanelSettings CreateOrUpdatePanelSettings()
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }

            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.targetTexture = null;
            ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(
                "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
            if (theme != null)
                settings.themeStyleSheet = theme;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static GameObject CreateOrUpdatePrefab(DispatchCatalogSO catalog, PanelSettings panelSettings)
        {
            VisualTreeAsset screen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Work/Dispatch/UI/DispatchScreen.uxml");
            VisualTreeAsset npcRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Work/Dispatch/UI/DispatchNpcRow.uxml");
            VisualTreeAsset regionRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Work/Dispatch/UI/DispatchRegionRow.uxml");
            VisualTreeAsset materialRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Work/Dispatch/UI/DispatchMaterialRow.uxml");
            VisualTreeAsset reportRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Work/Dispatch/UI/DispatchReportRow.uxml");

            GameObject root = new GameObject("DispatchUIRoot");
            GameTimeService time = root.AddComponent<GameTimeService>();
            DispatchManager manager = root.AddComponent<DispatchManager>();
            DispatchNpcQuery query = root.AddComponent<DispatchNpcQuery>();
            UIDocument document = root.AddComponent<UIDocument>();
            DispatchScreenPresenter presenter = root.AddComponent<DispatchScreenPresenter>();

            document.panelSettings = panelSettings;
            document.visualTreeAsset = screen;
            document.sortingOrder = -10f;

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("catalog").objectReferenceValue = catalog;
            serializedManager.FindProperty("gameTimeService").objectReferenceValue = time;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("dispatchManager").objectReferenceValue = manager;
            serializedPresenter.FindProperty("npcQuery").objectReferenceValue = query;
            serializedPresenter.FindProperty("gameTimeService").objectReferenceValue = time;
            serializedPresenter.FindProperty("npcRowTemplate").objectReferenceValue = npcRow;
            serializedPresenter.FindProperty("regionRowTemplate").objectReferenceValue = regionRow;
            serializedPresenter.FindProperty("materialRowTemplate").objectReferenceValue = materialRow;
            serializedPresenter.FindProperty("reportRowTemplate").objectReferenceValue = reportRow;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void InstallIntoAdventureScene(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = GameObject.Find("DispatchUIRoot");
            if (sceneRoot == null)
                sceneRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);

            GameTimeService time = sceneRoot.GetComponent<GameTimeService>();
            DispatchManager manager = sceneRoot.GetComponent<DispatchManager>();
            DispatchNpcQuery query = sceneRoot.GetComponent<DispatchNpcQuery>();
            DispatchScreenPresenter presenter = sceneRoot.GetComponent<DispatchScreenPresenter>();
            NpcEncounterDirector npcDirector = UnityEngine.Object.FindFirstObjectByType<NpcEncounterDirector>();
            PlayerInventoryModule inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventoryModule>();

            SetReference(manager, "playerInventory", inventory);
            SetReference(query, "encounterDirector", npcDirector);
            SetReference(npcDirector, "gameTimeService", time);
            SetReference(npcDirector, "externalAvailabilityRuleSource", manager);

            CookingBusinessFlowController[] cookingControllers = UnityEngine.Object.FindObjectsByType<CookingBusinessFlowController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < cookingControllers.Length; i++)
                SetReference(cookingControllers[i], "gameTimeService", time);

            AdventureManager adventureManager = UnityEngine.Object.FindFirstObjectByType<AdventureManager>();
            SetReference(adventureManager, "gameTimeService", time);
            PreparationManager preparationManager = UnityEngine.Object.FindFirstObjectByType<PreparationManager>();
            SetReference(preparationManager, "dispatchScreen", presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static IngredientItemDataSO FindIngredientItem(ItemCatalogSO catalog, string itemId)
        {
            if (catalog.TryFindItem(itemId, out ItemDataSO item) && item is IngredientItemDataSO ingredient)
                return ingredient;

            throw new InvalidOperationException($"Ingredient item was not found in ItemCatalogSO: {itemId}");
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
                return;

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Serialized property not found: {target.GetType().Name}.{propertyName}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) == false && AssetDatabase.IsValidFolder(parent) == false)
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct MaterialSpec
        {
            public string ItemId { get; }
            public int MaxRequest { get; }
            public int AmountPerBatch { get; }
            public int TimePerBatch { get; }
            public int MinYield { get; }
            public int MaxYield { get; }

            public MaterialSpec(string itemId, int maxRequest, int amountPerBatch, int timePerBatch, int minYield, int maxYield)
            {
                ItemId = itemId;
                MaxRequest = maxRequest;
                AmountPerBatch = amountPerBatch;
                TimePerBatch = timePerBatch;
                MinYield = minYield;
                MaxYield = maxYield;
            }
        }

        private readonly struct RareSpec
        {
            public string ItemId { get; }
            public int Weight { get; }
            public int MinAmount { get; }
            public int MaxAmount { get; }

            public RareSpec(string itemId, int weight, int minAmount, int maxAmount)
            {
                ItemId = itemId;
                Weight = weight;
                MinAmount = minAmount;
                MaxAmount = maxAmount;
            }
        }
    }
}
