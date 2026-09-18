using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Work.Adventure.Code;
using Work.Adventure.Code.AdventureEvents;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Players.Code.Inventory;
using Work.UtillUI.Code;
using Object = UnityEngine.Object;

/// <summary>Idempotent authoring migration for the shipped scene and its reusable prefabs.</summary>
public static partial class PlaytestFeedbackInstaller
{
    private const string CookingRoot = "Assets/Work/Cook/Prefabs/UI/CookingPresentationRoot.prefab";
    private const string OverlayRoot = "Assets/Work/Cook/Prefabs/UI/CookingMiniGameOverlayRoot.prefab";
    private const string AdventureRoot = "Assets/Work/Adventure/Prefabs/AdventureCanvas.prefab";
    private static CookingUiPresentationSettingsSO Theme => AssetDatabase.LoadAssetAtPath<CookingUiPresentationSettingsSO>(
        "Assets/Work/Cook/SO/CookingUiPresentationSettings.asset");

    [MenuItem("Tools/Dungeon Dinner/Apply Playtest Feedback")]
    public static void Apply()
    {
        ConfigureTheme();
        var harvest = CreateFirstHarvest();
        RegisterIncompleteDish();
        ConfigureDiscovery();
        foreach (string path in new[] { AdventureRoot, OverlayRoot, CookingRoot, "Assets/Work/Cook/Prefabs/UI/CookingResultPresentationRoot.prefab", "Assets/Work/Dispatch/Prefabs/DispatchUIRoot.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureRoot(root, harvest);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p == "Assets/MainScene.unity" || p.EndsWith("AdventureTestScene.unity")
                || p.EndsWith("DungeonDinnerScene.unity") || p.EndsWith("CookTestScene.unity") || p.EndsWith("TitleScene.unity")))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) ConfigureRoot(root, harvest);
            RemoveUnusedAdventureOverrides(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Playtest feedback assets and scenes updated.");
    }

    public static void ApplyToScene(UnityEngine.SceneManagement.Scene scene)
    {
        ConfigureTheme();
        var harvest = CreateFirstHarvest();
        RegisterIncompleteDish();
        ConfigureDiscovery();
        foreach (var root in scene.GetRootGameObjects()) ConfigureRoot(root, harvest);
    }

    private static void RemoveUnusedAdventureOverrides(UnityEngine.SceneManagement.Scene scene)
    {
        var instances = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject)
            .Where(PrefabUtility.IsAnyPrefabInstanceRoot)
            .Where(go => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == AdventureRoot)
            .ToArray();
        if (instances.Length > 0) PrefabUtility.RemoveUnusedOverrides(instances, InteractionMode.AutomatedAction);
    }

    private static AdventureEventSO CreateFirstHarvest()
    {
        const string path = "Assets/Work/Adventure/SO/Dialog/FirstHarvest.asset";
        var harvest = AssetDatabase.LoadAssetAtPath<AdventureEventSO>(path);
        if (harvest == null)
        {
            harvest = ScriptableObject.CreateInstance<AdventureEventSO>();
            AssetDatabase.CreateAsset(harvest, path);
        }
        harvest.dialogDatas.Clear();
        harvest.options.Clear();
        harvest.dialogDatas.Add(new AdventrueDialogData("이끼동굴 입구에 작은 채집터가 보인다. 바위 틈의 소금 결정과 버섯, 슬라임이 남긴 흔적이 손에 닿는 곳에 있다."));
        harvest.dialogDatas.Add(new AdventrueDialogData("낡은 배낭 옆에는 잘 싸 둔 옥수수와 치즈도 있다. 이 정도면 첫 손님을 위한 실험을 시작할 수 있겠다."));
        var option = new Options("먹을 수 있는 것들을 조심스레 챙긴다");
        option.ResultdialogDatas.Add(new AdventrueDialogData("부서지기 쉬운 것부터 따로 담았다. 오늘의 첫 수확이다. 안쪽을 더 살피거나 가게로 돌아갈 수 있다."));
        foreach (string name in new[] { "RockSalt", "SlimeMucus", "SlimeNucleus", "MushroomCap", "FlatMushroom", "CornCheese" })
        {
            var ingredient = AssetDatabase.LoadAssetAtPath<IngredientItemDataSO>("Assets/Work/Items/SO/Ingredients/" + name + "IngredientItem.asset");
            if (ingredient == null) throw new InvalidOperationException("Missing first harvest ingredient: " + name);
            option.rewardMethod.Add(new IngredientReward(ingredient, 1));
        }
        harvest.options.Add(option);
        EditorUtility.SetDirty(harvest);
        return harvest;
    }

    private static void RegisterIncompleteDish()
    {
        const string imagePath = "Assets/Work/Adventure/Graphics/Item/IncompleteDish.png";
        var importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        const string path = "Assets/Resources/IncompleteDish.asset";
        var definition = AssetDatabase.LoadAssetAtPath<IncompleteDishDefinitionSO>(path);
        if (definition == null) { definition = ScriptableObject.CreateInstance<IncompleteDishDefinitionSO>(); AssetDatabase.CreateAsset(definition, path); }
        Set(definition, "icon", AssetDatabase.LoadAssetAtPath<Sprite>(imagePath));
        EditorUtility.SetDirty(definition);
    }

    private static void ConfigureRoot(GameObject root, AdventureEventSO harvest)
    {
        foreach (var guide in root.GetComponentsInChildren<Work.Cook.Code.Info.InfoDictionaryPanel>(true))
        {
            Transform node = guide.transform;
            while (node.parent != null && node.parent.GetComponent<Canvas>() == null) node = node.parent;
            var container = node as RectTransform;
            if (container == null || container.parent == null || container.parent.GetComponent<Canvas>() == null) continue;
            container.anchorMin = Vector2.zero; container.anchorMax = Vector2.one;
            container.offsetMin = container.offsetMax = Vector2.zero;
        }
        foreach (var knowledge in root.GetComponentsInChildren<CookingKnowledgeStore>(true)) ClearKnowledgeSeeds(knowledge);
        foreach (var inventory in root.GetComponentsInChildren<PlayerInventoryModule>(true))
        {
            var so = new SerializedObject(inventory);
            so.FindProperty("slots").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var preparation in root.GetComponentsInChildren<PreparationManager>(true)) Set(preparation, "startWithAdventure", true);
        foreach (var business in root.GetComponentsInChildren<CookingBusinessFlowController>(true)) Set(business, "startFirstCustomerOnStart", false);
        foreach (var adventure in root.GetComponentsInChildren<AdventureManager>(true)) Set(adventure, "firstHarvestEvent", harvest);
        foreach (var events in root.GetComponentsInChildren<EventSystem>(true))
            if (events.GetComponent<GameUiInputDriver>() == null) events.gameObject.AddComponent<GameUiInputDriver>();

        var maps = root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && c.GetType().Name == "AdventureMapUI").ToArray();
        foreach (var map in maps)
        {
            var mapRoot = new SerializedObject(map).FindProperty("root")?.objectReferenceValue as RectTransform;
            if (mapRoot != null && mapRoot.gameObject != root) Object.DestroyImmediate(mapRoot.gameObject);
            if (map != null) Object.DestroyImmediate(map);
        }
        foreach (var continuation in root.GetComponentsInChildren<Work.Adventure.Code.UI.GoAndStopSelectUI>(true))
        {
            Set(continuation, "root", continuation.GetComponent<Image>());
            var buttons = continuation.GetComponentsInChildren<Button>(true);
            Set(continuation, "goButton", buttons.First(b => b.name == "ContinueButton"));
            Set(continuation, "stopButton", buttons.First(b => b.name == "StopButton"));
        }
        foreach (var host in root.GetComponentsInChildren<CookingMiniGameOverlayHost>(true)) ConfigureOverlay(host);
        ConfigureSystemPanels(root);
        foreach (var dispatch in root.GetComponentsInChildren<Work.Dispatch.Code.UI.DispatchScreenPresenter>(true)) Set(dispatch, "presentationTheme", Theme);
        foreach (var bag in root.GetComponentsInChildren<CookingIngredientSelectionView>(true)) ConfigureBag(bag);
        foreach (var result in root.GetComponentsInChildren<CookingResultView>(true)) ConfigureResult(result);
        foreach (var button in root.GetComponentsInChildren<Button>(true))
            if (button.image != null && button.image.sprite == Theme.SecondaryButtonSprite)
                foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true)) label.color = Theme.ParchmentColor;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
            if (image.sprite != null && AssetDatabase.GetAssetPath(image.sprite).EndsWith("CookingUiEmblems.png"))
            { image.sprite = Theme.RewardIcon; image.preserveAspect = true; }
    }

    private static void ConfigureOverlay(CookingMiniGameOverlayHost host)
    {
        var so = new SerializedObject(host);
        var frame = so.FindProperty("targetFrame").objectReferenceValue as RectTransform;
        var mask = so.FindProperty("maskImage").objectReferenceValue as Image;
        if (frame == null || mask == null) throw new InvalidOperationException("Missing overlay work area");
        var interaction = Child(frame, "InteractionLayer"); Stretch(interaction);
        var surfaces = Child(mask.transform, "SurfaceEffects"); Stretch(surfaces);
        so.FindProperty("controllerContainer").objectReferenceValue = interaction;
        so.ApplyModifiedPropertiesWithoutUndo();
        var controllers = host.GetComponentsInChildren<CookingOverlayMiniGameController>(true);
        foreach (var controller in controllers)
        {
            var rect = (RectTransform)controller.transform;
            rect.SetParent(interaction, false); Stretch(rect);
            var effects = new List<GameObject>();
            var tools = new List<Image>();
            // Previously migrated effects retain their references on a second run.
            var serialized = new SerializedObject(controller);
            var oldEffects = serialized.FindProperty("surfaceObjects");
            for (int i = 0; i < oldEffects.arraySize; i++)
                if (oldEffects.GetArrayElementAtIndex(i).objectReferenceValue is GameObject value) effects.Add(value);
            foreach (var image in controller.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == rect) { image.color = Color.clear; image.raycastTarget = true; continue; }
                image.raycastTarget = false;
                string name = image.name;
                bool surface = name == "HeatTint" || name == "CookTint" || name == "MixtureTint" || name.StartsWith("FrostCell") || name.StartsWith("Particle") || name.StartsWith("Bubble");
                if (surface)
                {
                    image.rectTransform.SetParent(surfaces, false);
                    effects.Add(image.gameObject);
                }
                else image.maskable = false;
                if (new[] { "KnifeGuide", "PestleGuide", "BrushGuide", "PitcherGuide", "ColdHandGuide", "ActionGuide" }.Contains(name)) tools.Add(image);
            }
            SetArray(controller, "surfaceObjects", effects.Distinct().Cast<Object>().ToArray());
            SetArray(controller, "dragVisuals", tools.Cast<Object>().ToArray());
            if (controller is CookingStewingMiniGameView)
            {
                var waste = controller.GetComponentsInChildren<Image>(true).First(i => i.name == "WasteZone");
                ConfigureDestination(waste, true);
                waste.rectTransform.anchorMin = waste.rectTransform.anchorMax = new Vector2(0.81f, 0.81f);
                waste.rectTransform.anchoredPosition = Vector2.zero;
                waste.rectTransform.sizeDelta = new Vector2(120, 100);
            }
            if (controller is CookingDilutingMiniGameView)
            {
                var zone = Child(rect, "PourZone");
                zone.anchorMin = new Vector2(0.26f, 0.45f); zone.anchorMax = new Vector2(0.74f, 0.94f);
                zone.offsetMin = zone.offsetMax = Vector2.zero;
                zone.SetAsFirstSibling();
                var image = Ensure<Image>(zone.gameObject);
                ConfigureDestination(image, false);
                Set(controller, "pourZone", zone);
            }
        }
        var shield = host.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "InputShield");
        // Block preparation inputs while leaving the original workbench and cutting board visible.
        if (shield != null) { shield.color = Color.clear; shield.raycastTarget = true; }
        var result = so.FindProperty("resultCanvasGroup").objectReferenceValue as CanvasGroup;
        foreach (var text in host.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (result != null && text.transform.IsChildOf(result.transform)) continue;
            if (text.name == "ProgressLabel" || text.name == "TimerValue") continue;
            if (text.name == "Instruction" && text.transform.parent != null && text.transform.parent.name == "ActionHUD") continue;
            Object.DestroyImmediate(text.gameObject);
        }
        // Both prefab variants share the instruction panel, numeric HUD and graphic cues.
        var hud = so.FindProperty("hudRoot").objectReferenceValue as RectTransform;
        if (hud != null) { hud.sizeDelta = new Vector2(64, 56); var background = hud.GetComponent<Image>(); if (background != null) { background.color = Color.clear; background.raycastTarget = false; } }
        var cancel = so.FindProperty("cancelButton").objectReferenceValue as Button;
        if (cancel != null)
        {
            cancel.image.sprite = Sprite("Navigation/ui_nav_back.png");
            cancel.image.color = Color.white; cancel.image.preserveAspect = true;
            Fixed((RectTransform)cancel.transform, hud, Vector2.zero, new Vector2(48, 48));
        }
        var actionHud = Child(host.transform, "ActionHUD");
        actionHud.anchorMin = actionHud.anchorMax = new Vector2(0.5f, 0.5f);
        actionHud.sizeDelta = new Vector2(440, 210);
        Panel(actionHud, Theme.PanelSprite);
        var instruction = Label(actionHud, "Instruction", string.Empty, Vector2.zero, new Vector2(384, 84), 24);
        instruction.alignment = TextAlignmentOptions.TopLeft;
        instruction.textWrappingMode = TextWrappingModes.Normal;
        instruction.enableAutoSizing = false;
        instruction.maskable = false;
        instruction.transform.SetAsLastSibling();
        Set(host, "instructionField", instruction);
        var progress = Child(actionHud, "ProgressGauge");
        var progressBg = Ensure<Image>(progress.gameObject);
        progressBg.color = new Color(0.37f, 0.24f, 0.14f); progressBg.raycastTarget = false;
        var fill = Image(Child(progress, "Fill"), new Color(0.68f, 0.5f, 0.22f)); Stretch(fill.rectTransform);
        var band = Image(Child(progress, "TargetBand"), new Color(0.57f, 0.72f, 0.4f)); Stretch(band.rectTransform);
        var marker = Image(Child(progress, "TargetMarker"), new Color(0.29f, 0.16f, 0.08f)); marker.rectTransform.sizeDelta = new Vector2(5, 32);
        var label = Child(actionHud, "ProgressLabel").GetComponent<TextMeshProUGUI>() ?? Child(actionHud, "ProgressLabel").gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Theme.FontAsset; label.fontSize = 24; label.color = Theme.PrimaryTextColor; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; label.text = "0%";
        var timer = Child(actionHud, "TimerGauge");
        var timerFill = Image(Child(timer, "Fill"), new Color(0.55f, 0.37f, 0.18f)); Stretch(timerFill.rectTransform);
        var guide = Child(interaction, "GraphicDemonstration"); Stretch(guide);
        var graphic = Ensure<CookingGestureGraphic>(guide.gameObject); graphic.raycastTarget = false;
        Set(host, "gestureGraphic", graphic); Set(host, "actionHudRoot", actionHud); Set(host, "progressGaugeRoot", progress);
        Set(host, "progressFill", fill); Set(host, "targetBand", band); Set(host, "targetMarker", marker); Set(host, "progressField", label);
        Set(host, "timerGaugeRoot", timer); Set(host, "timerFill", timerFill);
        foreach (var t in host.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("FocusDim") || t.name == "MistakeToast" || t.name == "LocalActionHUD").ToArray())
            if (t != null) Object.DestroyImmediate(t.gameObject);
        if (result != null)
        {
            var resultRect = (RectTransform)result.transform;
            resultRect.sizeDelta = new Vector2(420, 188);
            Panel(resultRect, Theme.PanelSprite);
            var grade = Label(resultRect, "Grade", "", new Vector2(0, 57), new Vector2(380, 42), 32);
            var score = Label(resultRect, "Score", "", new Vector2(0, 8), new Vector2(380, 36), 24);
            var reason = Label(resultRect, "Reason", "", new Vector2(0, -50), new Vector2(380, 64), 19);
            Set(host, "resultField", grade); Set(host, "resultScoreField", score); Set(host, "resultReasonField", reason);
        }
    }

    private static void ConfigureDestination(Image image, bool waste)
    {
        image.sprite = null; image.raycastTarget = false; image.maskable = false;
        image.color = waste ? new Color(0.8f, 0.2f, 0.16f, 0.22f) : new Color(0.55f, 0.71f, 0.4f, 0.2f);
        var outline = Ensure<Outline>(image.gameObject);
        outline.effectColor = waste ? new Color(0.65f, 0.24f, 0.18f, 0.9f) : new Color(0.36f, 0.48f, 0.23f, 0.9f);
        outline.effectDistance = new Vector2(2, -2);
        if (waste)
        {
            var icon = Child(image.transform, "DestinationDrawing"); Stretch(icon);
            var graphic = Ensure<CookingGestureGraphic>(icon.gameObject);
            Set(graphic, "destinationOnly", true); graphic.raycastTarget = false;
        }
    }

    internal static T Ensure<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    internal static RectTransform Child(Transform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;
        var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); result.SetParent(parent, false); return result;
    }
    internal static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
    internal static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Work/Cook/Graphics/UIAsset/" + path);
    internal static Image Image(RectTransform rect, Color color) { var image = Ensure<Image>(rect.gameObject); image.color = color; image.raycastTarget = false; return image; }
    internal static void Panel(RectTransform rect, Sprite sprite)
    {
        var image = Image(rect, Color.white);
        image.sprite = sprite == Theme.CardSprite ? Theme.ReceiptSprite : sprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        if (sprite == Theme.PanelSprite)
        {
            var paper = Child(rect, "PaperBackground"); Stretch(paper);
            paper.offsetMin = new Vector2(14, 14); paper.offsetMax = new Vector2(-14, -14);
            var sheet = Image(paper, Color.white); sheet.sprite = Theme.ReceiptSprite; sheet.type = UnityEngine.UI.Image.Type.Sliced;
            paper.SetAsFirstSibling();
        }
    }
    private static void ConfigureTheme()
    {
        var so = new SerializedObject(Theme);
        so.FindProperty("parchmentColor").colorValue = new Color(.97f, .94f, .86f, 1);
        so.FindProperty("panelColor").colorValue = new Color(.29f, .19f, .13f, 1);
        so.FindProperty("primaryTextColor").colorValue = new Color(.26f, .16f, .09f, 1);
        so.FindProperty("secondaryTextColor").colorValue = new Color(.44f, .32f, .21f, 1);
        so.FindProperty("positiveColor").colorValue = new Color(.33f, .47f, .22f, 1);
        var qualities = so.FindProperty("qualityVisuals");
        for (int i = 0; i < qualities.arraySize; i++)
            qualities.GetArrayElementAtIndex(i).FindPropertyRelative("color").colorValue = i == 0
                ? new Color(.50f, .31f, .08f, 1) : new Color(.37f, .27f, .17f, 1);
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach (var sprite in new[] { Theme.PanelSprite, Theme.ReceiptSprite, Theme.PrimaryButtonSprite, Theme.SecondaryButtonSprite })
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
            if (importer != null && importer.spriteBorder == Vector4.zero)
            { importer.spriteBorder = new Vector4(32, 32, 32, 32); importer.SaveAndReimport(); }
        }
        var path = "Assets/Work/Cook/Prefabs/UI/CookingIngredientButton.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var button = root.GetComponentInChildren<Button>(true);
        button.image.sprite = Theme.ReceiptSprite; button.image.type = UnityEngine.UI.Image.Type.Sliced;
        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) text.color = Theme.PrimaryTextColor;
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }
    internal static void Set(Object target, string field, Object value) { var so = new SerializedObject(target); var p = so.FindProperty(field); if (p == null) throw new Exception(target.name + ": " + field); p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    internal static void Set(Object target, string field, bool value) { var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    internal static void SetArray(Object target, string field, Object[] values) { var so = new SerializedObject(target); var p = so.FindProperty(field); p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
}
