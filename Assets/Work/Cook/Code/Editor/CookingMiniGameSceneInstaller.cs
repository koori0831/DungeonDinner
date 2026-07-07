using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime;

namespace Work.Cook.Code.Editor
{
    /// <summary>
    /// CookTestScene에 기본 타이밍 미니게임 프리팹과 씬 연결을 생성
    /// </summary>
    public static class CookingMiniGameSceneInstaller
    {
        private const string MENU_PATH = "Tools/Dungeon Dinner/Install Cooking Mini Game UI In CookTestScene";
        private const string PREFAB_FOLDER_PATH = "Assets/Work/Cook/Prefabs";
        private const string PREFAB_PATH = PREFAB_FOLDER_PATH + "/CookingTimingMiniGameView.prefab";
        private const string SCENE_PATH = "Assets/Work/Cook/Scene/CookTestScene.unity";
        private const string DEFAULT_FONT_PATH = "Assets/Font/MangoDdobak-B(otf) SDF.asset";
        private const string OVERLAY_ROOT_NAME = "CookingRewardOverlayRoot";
        private const string VIEW_NAME = "CookingTimingMiniGameView";

        [MenuItem(MENU_PATH)]
        public static void InstallInCookTestScene()
        {
            EnsurePrefabFolder();
            GameObject prefab = CreateOrUpdatePrefab();
            InstallSceneInstance(prefab);
            ConfigureSlicingMethods();
            ConfigureDefaultFeedbackRules();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Cooking mini game UI prefab and CookTestScene binding installed.");
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ValidateInstallInCookTestScene()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode == false;
        }

        private static void EnsurePrefabFolder()
        {
            if (AssetDatabase.IsValidFolder(PREFAB_FOLDER_PATH) == true)
                return;

            if (AssetDatabase.IsValidFolder("Assets/Work/Cook") == false)
            {
                Debug.LogError("Cannot create cooking mini game prefab folder because Assets/Work/Cook is missing.");
                return;
            }

            AssetDatabase.CreateFolder("Assets/Work/Cook", "Prefabs");
        }

        private static GameObject CreateOrUpdatePrefab()
        {
            GameObject root = CreatePrefabRoot();
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);
            return savedPrefab;
        }

        private static GameObject CreatePrefabRoot()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DEFAULT_FONT_PATH);

            GameObject root = new GameObject(
                VIEW_NAME,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(CookingTimingMiniGameView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, Vector2.zero, Vector2.zero);

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.55f);
            rootImage.raycastTarget = true;

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            GameObject panel = CreateImage("Panel", rootRect, new Color(0.12f, 0.09f, 0.07f, 0.96f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            SetCenter(panelRect, new Vector2(760f, 420f), Vector2.zero);

            TextMeshProUGUI title = CreateText(
                "Title",
                panelRect,
                "손질 미니게임",
                font,
                34f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.88f, 0.55f, 1f));
            SetTop(title.rectTransform, new Vector2(680f, 52f), new Vector2(0f, -34f));

            TextMeshProUGUI description = CreateText(
                "Description",
                panelRect,
                "목표 구간에 맞춰 칼질하세요.",
                font,
                22f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.95f, 0.9f, 0.82f, 1f));
            SetTop(description.rectTransform, new Vector2(680f, 68f), new Vector2(0f, -92f));

            GameObject gauge = CreateImage("GaugeRoot", panelRect, new Color(0.22f, 0.18f, 0.14f, 1f));
            RectTransform gaugeRect = gauge.GetComponent<RectTransform>();
            SetCenter(gaugeRect, new Vector2(560f, 42f), new Vector2(0f, 18f));

            GameObject target = CreateImage("TargetZone", gaugeRect, new Color(0.33f, 0.72f, 0.42f, 0.78f));
            RectTransform targetRect = target.GetComponent<RectTransform>();
            SetCenter(targetRect, new Vector2(160f, 42f), Vector2.zero);

            GameObject cursor = CreateImage("Cursor", gaugeRect, new Color(1f, 0.86f, 0.32f, 1f));
            RectTransform cursorRect = cursor.GetComponent<RectTransform>();
            SetCenter(cursorRect, new Vector2(14f, 68f), Vector2.zero);

            TextMeshProUGUI grade = CreateText(
                "Grade",
                panelRect,
                "타이밍을 맞추세요",
                font,
                26f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.95f, 0.8f, 1f));
            SetCenter(grade.rectTransform, new Vector2(600f, 42f), new Vector2(0f, -62f));

            TextMeshProUGUI feedback = CreateText(
                "Feedback",
                panelRect,
                string.Empty,
                font,
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.86f, 0.82f, 0.74f, 1f));
            SetCenter(feedback.rectTransform, new Vector2(650f, 48f), new Vector2(0f, -112f));

            Button button = CreateButton(panelRect, font, "칼질하기");
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            SetBottom(buttonRect, new Vector2(240f, 58f), new Vector2(0f, 34f));
            TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);

            ConfigureMiniGameView(
                root.GetComponent<CookingTimingMiniGameView>(),
                canvasGroup,
                title,
                description,
                grade,
                feedback,
                gaugeRect,
                targetRect,
                cursorRect,
                button,
                buttonLabel,
                font);

            return root;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return gameObject;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            TMP_FontAsset font,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI textField = gameObject.GetComponent<TextMeshProUGUI>();
            textField.text = text;
            textField.font = font;
            textField.fontSize = fontSize;
            textField.fontStyle = fontStyle;
            textField.alignment = alignment;
            textField.color = color;
            textField.raycastTarget = false;
            return textField;
        }

        private static Button CreateButton(Transform parent, TMP_FontAsset font, string label)
        {
            GameObject gameObject = CreateImage("ActionButton", parent, new Color(0.72f, 0.42f, 0.18f, 1f));
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();

            TextMeshProUGUI labelField = CreateText(
                "Label",
                gameObject.transform,
                label,
                font,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white);
            SetStretch(labelField.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void ConfigureMiniGameView(
            CookingTimingMiniGameView view,
            CanvasGroup canvasGroup,
            TextMeshProUGUI title,
            TextMeshProUGUI description,
            TextMeshProUGUI grade,
            TextMeshProUGUI feedback,
            RectTransform gaugeRoot,
            RectTransform targetZone,
            RectTransform cursor,
            Button actionButton,
            TextMeshProUGUI actionButtonLabel,
            TMP_FontAsset font)
        {
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("supportedMiniGameType").enumValueIndex = (int)CookingMiniGameType.Slicing;
            serializedView.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedView.FindProperty("titleField").objectReferenceValue = title;
            serializedView.FindProperty("descriptionField").objectReferenceValue = description;
            serializedView.FindProperty("gradeField").objectReferenceValue = grade;
            serializedView.FindProperty("feedbackField").objectReferenceValue = feedback;
            serializedView.FindProperty("gaugeRoot").objectReferenceValue = gaugeRoot;
            serializedView.FindProperty("targetZone").objectReferenceValue = targetZone;
            serializedView.FindProperty("cursor").objectReferenceValue = cursor;
            serializedView.FindProperty("actionButton").objectReferenceValue = actionButton;
            serializedView.FindProperty("actionButtonLabel").objectReferenceValue = actionButtonLabel;
            serializedView.FindProperty("fontAsset").objectReferenceValue = font;
            serializedView.FindProperty("randomizeTargetCenter").boolValue = true;
            serializedView.FindProperty("timeLimit").floatValue = 5f;
            serializedView.FindProperty("cursorSpeed").floatValue = 1.2f;
            serializedView.FindProperty("perfectWindow").floatValue = 0.045f;
            serializedView.FindProperty("goodWindow").floatValue = 0.11f;
            serializedView.FindProperty("normalWindow").floatValue = 0.22f;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InstallSceneInstance(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Cooking mini game prefab could not be created.");
                return;
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            CookingGamePanel panel = FindFirstSceneObject<CookingGamePanel>();
            if (panel == null)
            {
                Debug.LogError("CookTestScene does not contain a CookingGamePanel.");
                return;
            }

            Transform overlayRoot = FindOrCreateOverlayRoot(panel);
            GameObject viewObject = FindChildByName(overlayRoot, VIEW_NAME);
            if (viewObject == null)
            {
                viewObject = PrefabUtility.InstantiatePrefab(prefab, overlayRoot) as GameObject;
                if (viewObject != null)
                    viewObject.name = VIEW_NAME;
            }

            if (viewObject == null)
            {
                Debug.LogError("Cooking mini game scene instance could not be created.");
                return;
            }

            RectTransform rectTransform = viewObject.transform as RectTransform;
            if (rectTransform != null)
                SetStretch(rectTransform, Vector2.zero, Vector2.zero);

            viewObject.SetActive(false);
            ConfigureCookingGamePanel(panel, viewObject);

            EditorUtility.SetDirty(viewObject);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureCookingGamePanel(CookingGamePanel panel, GameObject miniGameView)
        {
            SerializedObject serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("miniGameView").objectReferenceValue = miniGameView;
            serializedPanel.FindProperty("useMiniGames").boolValue = true;
            serializedPanel.FindProperty("continueWithoutMiniGameView").boolValue = true;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindOrCreateOverlayRoot(CookingGamePanel panel)
        {
            Canvas canvas = panel.GetComponentInParent<Canvas>(true);
            if (canvas == null)
                canvas = FindFirstSceneObject<Canvas>();

            Transform canvasTransform = canvas != null ? canvas.rootCanvas.transform : panel.transform;
            Transform overlayRoot = canvasTransform.Find(OVERLAY_ROOT_NAME);
            if (overlayRoot != null)
                return overlayRoot;

            GameObject rootObject = new GameObject(OVERLAY_ROOT_NAME, typeof(RectTransform));
            rootObject.transform.SetParent(canvasTransform, false);
            RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
            SetStretch(rectTransform, Vector2.zero, Vector2.zero);
            EditorUtility.SetDirty(rootObject);
            return rootObject.transform;
        }

        private static void ConfigureSlicingMethods()
        {
            string[] guids = AssetDatabase.FindAssets("t:PreparationMethodSO", new[] { "Assets/Work/Cook" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                PreparationMethodSO method = AssetDatabase.LoadAssetAtPath<PreparationMethodSO>(path);
                if (method == null)
                    continue;

                CookingMiniGameType miniGameType = ResolveMiniGameType(method.MethodId);
                SerializedObject serializedMethod = new SerializedObject(method);
                serializedMethod.FindProperty("miniGameType").enumValueIndex = (int)miniGameType;
                serializedMethod.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(method);
            }
        }

        private static CookingMiniGameType ResolveMiniGameType(string methodId)
        {
            string normalized = methodId != null ? methodId.ToLowerInvariant() : string.Empty;
            if (normalized.Contains("slicing") == true || normalized.Contains("chopping") == true)
                return CookingMiniGameType.Slicing;

            return CookingMiniGameType.None;
        }

        private static void ConfigureDefaultFeedbackRules()
        {
            FoodTagSO softTag = FindFoodTag("soft");
            FoodTagSO bleakTag = FindFoodTag("bleak");
            string[] guids = AssetDatabase.FindAssets("t:IngredientSO", new[] { "Assets/Work/Cook" });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                IngredientSO ingredient = AssetDatabase.LoadAssetAtPath<IngredientSO>(path);
                if (ingredient == null)
                    continue;

                SerializedObject serializedIngredient = new SerializedObject(ingredient);
                SerializedProperty options = serializedIngredient.FindProperty("preparationOptions");
                bool changed = false;

                for (int optionIndex = 0; optionIndex < options.arraySize; optionIndex++)
                {
                    SerializedProperty option = options.GetArrayElementAtIndex(optionIndex);
                    SerializedProperty methodProperty = option.FindPropertyRelative("method");
                    PreparationMethodSO method = methodProperty.objectReferenceValue as PreparationMethodSO;
                    if (method == null || method.MiniGameType != CookingMiniGameType.Slicing)
                        continue;

                    SerializedProperty rules = option.FindPropertyRelative("miniGameFeedbackRules");
                    if (rules == null || rules.arraySize > 0)
                        continue;

                    CreateDefaultFeedbackRules(rules, softTag, bleakTag);
                    changed = true;
                }

                if (changed == true)
                {
                    serializedIngredient.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(ingredient);
                }
            }
        }

        private static void CreateDefaultFeedbackRules(SerializedProperty rules, FoodTagSO softTag, FoodTagSO bleakTag)
        {
            rules.arraySize = 4;
            ConfigureFeedbackRule(
                rules.GetArrayElementAtIndex(0),
                CookingMiniGameGrade.Bad,
                -1,
                bleakTag,
                null,
                "거칠게",
                "칼질이 거칠어 식감이 투박해졌습니다.");
            ConfigureFeedbackRule(
                rules.GetArrayElementAtIndex(1),
                CookingMiniGameGrade.Normal,
                0,
                null,
                null,
                string.Empty,
                "무난하게 손질했습니다.");
            ConfigureFeedbackRule(
                rules.GetArrayElementAtIndex(2),
                CookingMiniGameGrade.Good,
                1,
                null,
                null,
                "깔끔하게",
                "칼질이 일정해 완성도가 올랐습니다.");
            ConfigureFeedbackRule(
                rules.GetArrayElementAtIndex(3),
                CookingMiniGameGrade.Perfect,
                2,
                softTag,
                null,
                "정교하게",
                "재료 결을 완벽하게 살려 부드러운 식감이 더해졌습니다.");
        }

        private static void ConfigureFeedbackRule(
            SerializedProperty rule,
            CookingMiniGameGrade grade,
            int qualityDelta,
            FoodTagSO addTag,
            FoodTagSO removeTag,
            string resultNameModifier,
            string feedbackText)
        {
            rule.FindPropertyRelative("grade").enumValueIndex = (int)grade;
            rule.FindPropertyRelative("qualityDelta").intValue = qualityDelta;
            SetSingleTag(rule.FindPropertyRelative("addTags"), addTag);
            SetSingleTag(rule.FindPropertyRelative("removeTags"), removeTag);
            rule.FindPropertyRelative("resultNameModifier").stringValue = resultNameModifier ?? string.Empty;
            rule.FindPropertyRelative("feedbackText").stringValue = feedbackText ?? string.Empty;
        }

        private static void SetSingleTag(SerializedProperty tags, FoodTagSO tag)
        {
            if (tags == null)
                return;

            if (tag == null)
            {
                tags.arraySize = 0;
                return;
            }

            tags.arraySize = 1;
            tags.GetArrayElementAtIndex(0).objectReferenceValue = tag;
        }

        private static FoodTagSO FindFoodTag(string tagId)
        {
            string[] guids = AssetDatabase.FindAssets("t:FoodTagSO", new[] { "Assets/Work/Cook" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                FoodTagSO tag = AssetDatabase.LoadAssetAtPath<FoodTagSO>(path);
                if (tag != null && string.Equals(tag.TagId, tagId, System.StringComparison.OrdinalIgnoreCase) == true)
                    return tag;
            }

            return null;
        }

        private static T FindFirstSceneObject<T>()
            where T : Object
        {
            T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return objects.Length > 0 ? objects[0] : null;
        }

        private static GameObject FindChildByName(Transform root, string name)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != null && child.name == name)
                    return child.gameObject;
            }

            return null;
        }

        private static void SetStretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetCenter(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void SetTop(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void SetBottom(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
