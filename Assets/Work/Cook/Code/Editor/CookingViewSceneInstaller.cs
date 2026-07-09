using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Editor
{
    /// <summary>
    /// CookTestScene에 도마 중심 조리 뷰를 생성하고 CookingGamePanel에 연결
    /// </summary>
    public static class CookingViewSceneInstaller
    {
        private const string MENU_PATH = "Tools/Dungeon Dinner/Install Cooking View In CookTestScene";
        private const string SCENE_PATH = "Assets/Work/Cook/Scene/CookTestScene.unity";
        private const string DEFAULT_FONT_PATH = "Assets/Font/MangoDdobak-B(otf) SDF.asset";
        private const string CARD_PREFAB_PATH = "Assets/Work/Cook/Prefabs/UI/CookingPreparationOptionCard.prefab";
        private const string VIEW_ROOT_NAME = "CookingViewRoot";

        [MenuItem(MENU_PATH)]
        public static void InstallInCookTestScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            CookingGamePanel panel = FindFirstSceneObject<CookingGamePanel>();
            if (panel == null)
            {
                Debug.LogError("CookTestScene does not contain a CookingGamePanel.");
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DEFAULT_FONT_PATH);
            CookingPreparationOptionCardView cardPrefab = AssetDatabase.LoadAssetAtPath<CookingPreparationOptionCardView>(CARD_PREFAB_PATH);
            GameObject viewRoot = CreateOrUpdateCookingView(panel, font, cardPrefab);
            ConfigureCookingGamePanel(panel, viewRoot);
            DisableLegacyPreparationViews(viewRoot);

            EditorUtility.SetDirty(viewRoot);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Cooking view UI and CookTestScene binding installed.");
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ValidateInstallInCookTestScene()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode == false;
        }

        private static GameObject CreateOrUpdateCookingView(
            CookingGamePanel panel,
            TMP_FontAsset font,
            CookingPreparationOptionCardView cardPrefab)
        {
            Transform parent = FindViewParent(panel);
            GameObject root = FindChildByName(parent, VIEW_ROOT_NAME);
            if (root == null)
            {
                root = new GameObject(
                    VIEW_ROOT_NAME,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup),
                    typeof(CookingView));
                root.transform.SetParent(parent, false);
            }
            else
            {
                EnsureComponent<Image>(root);
                EnsureComponent<CanvasGroup>(root);
                EnsureComponent<CookingView>(root);
                ClearChildren(root.transform);
            }

            root.SetActive(false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, Vector2.zero, Vector2.zero);

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0.08f, 0.055f, 0.035f, 0.98f);
            rootImage.raycastTarget = true;

            CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            TextMeshProUGUI titleField = CreateText(
                "Title",
                root.transform,
                "조리대",
                font,
                42f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.86f, 0.5f, 1f));
            SetTop(titleField.rectTransform, new Vector2(560f, 64f), new Vector2(0f, -30f));

            TextMeshProUGUI progressField = CreateText(
                "Progress",
                root.transform,
                string.Empty,
                font,
                24f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.95f, 0.88f, 0.72f, 1f));
            SetTop(progressField.rectTransform, new Vector2(560f, 44f), new Vector2(0f, -86f));

            CookingWorkbenchView workbenchView = CreateWorkbench(root.transform, font);
            CookingActivePreparationSlotView slotView = CreateActiveSlot(root.transform, font);
            CookingOrderNoteView orderNoteView = CreateOrderNote(root.transform, font);
            CookingPreparationHandView handView = CreatePreparationHand(root.transform, font, cardPrefab);
            CookingViewTransition transition = CreateTransition(root.transform);

            ConfigureCookingView(
                root.GetComponent<CookingView>(),
                panel,
                ResolveFlowRunner(panel),
                ResolveKnowledgeStore(panel),
                workbenchView,
                handView,
                slotView,
                orderNoteView,
                transition,
                progressField,
                font);

            return root;
        }

        private static CookingWorkbenchView CreateWorkbench(Transform parent, TMP_FontAsset font)
        {
            GameObject board = CreateImage("Workbench", parent, new Color(0.34f, 0.19f, 0.09f, 1f));
            RectTransform boardRect = board.GetComponent<RectTransform>();
            SetCenter(boardRect, new Vector2(620f, 410f), new Vector2(0f, 18f));
            CookingWorkbenchView workbenchView = board.AddComponent<CookingWorkbenchView>();

            TextMeshProUGUI ingredientName = CreateText(
                "IngredientName",
                board.transform,
                string.Empty,
                font,
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.92f, 0.74f, 1f));
            SetTop(ingredientName.rectTransform, new Vector2(520f, 52f), new Vector2(0f, -26f));

            Button ingredientButton = CreateButton("IngredientButton", board.transform, new Color(0.84f, 0.68f, 0.42f, 1f));
            RectTransform ingredientRect = ingredientButton.GetComponent<RectTransform>();
            SetCenter(ingredientRect, new Vector2(220f, 220f), new Vector2(0f, -18f));
            Image ingredientImage = ingredientButton.GetComponent<Image>();
            ingredientImage.preserveAspect = true;

            TextMeshProUGUI instruction = CreateText(
                "Instruction",
                board.transform,
                string.Empty,
                font,
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.97f, 0.88f, 0.68f, 1f));
            SetBottom(instruction.rectTransform, new Vector2(540f, 76f), new Vector2(0f, 22f));

            SerializedObject serializedWorkbench = new SerializedObject(workbenchView);
            SetObjectReference(serializedWorkbench, "boardImage", board.GetComponent<Image>());
            SetObjectReference(serializedWorkbench, "ingredientButton", ingredientButton);
            SetObjectReference(serializedWorkbench, "ingredientImage", ingredientImage);
            SetObjectReference(serializedWorkbench, "ingredientNameField", ingredientName);
            SetObjectReference(serializedWorkbench, "instructionField", instruction);
            serializedWorkbench.ApplyModifiedPropertiesWithoutUndo();
            return workbenchView;
        }

        private static CookingActivePreparationSlotView CreateActiveSlot(Transform parent, TMP_FontAsset font)
        {
            GameObject slot = CreateImage("ActivePreparationSlot", parent, new Color(0.16f, 0.1f, 0.07f, 0.96f));
            SetLeft(slot.GetComponent<RectTransform>(), new Vector2(300f, 260f), new Vector2(54f, -18f));
            CookingActivePreparationSlotView slotView = slot.AddComponent<CookingActivePreparationSlotView>();

            Image iconImage = CreateImage("Icon", slot.transform, new Color(0.32f, 0.22f, 0.14f, 1f)).GetComponent<Image>();
            SetTop(iconImage.rectTransform, new Vector2(92f, 92f), new Vector2(0f, -28f));
            iconImage.enabled = false;
            iconImage.preserveAspect = true;

            TextMeshProUGUI title = CreateText(
                "Title",
                slot.transform,
                "작업 슬롯",
                font,
                25f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.85f, 0.5f, 1f));
            SetTop(title.rectTransform, new Vector2(250f, 44f), new Vector2(0f, -130f));

            TextMeshProUGUI description = CreateText(
                "Description",
                slot.transform,
                string.Empty,
                font,
                18f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.9f, 0.82f, 0.68f, 1f));
            SetBottom(description.rectTransform, new Vector2(250f, 72f), new Vector2(0f, 26f));

            SerializedObject serializedSlot = new SerializedObject(slotView);
            SetObjectReference(serializedSlot, "iconImage", iconImage);
            SetObjectReference(serializedSlot, "titleField", title);
            SetObjectReference(serializedSlot, "descriptionField", description);
            serializedSlot.ApplyModifiedPropertiesWithoutUndo();
            return slotView;
        }

        private static CookingOrderNoteView CreateOrderNote(Transform parent, TMP_FontAsset font)
        {
            GameObject note = CreateImage("OrderNote", parent, new Color(0.18f, 0.13f, 0.09f, 0.96f));
            SetRight(note.GetComponent<RectTransform>(), new Vector2(320f, 430f), new Vector2(-54f, 18f));
            CookingOrderNoteView noteView = note.AddComponent<CookingOrderNoteView>();

            TextMeshProUGUI title = CreateText(
                "Title",
                note.transform,
                "주문 명세서",
                font,
                27f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.86f, 0.52f, 1f));
            SetTop(title.rectTransform, new Vector2(270f, 48f), new Vector2(0f, -24f));

            TextMeshProUGUI body = CreateText(
                "Body",
                note.transform,
                string.Empty,
                font,
                19f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                new Color(0.92f, 0.86f, 0.74f, 1f));
            SetStretch(body.rectTransform, new Vector2(24f, 26f), new Vector2(-24f, -88f));

            SerializedObject serializedNote = new SerializedObject(noteView);
            SetObjectReference(serializedNote, "titleField", title);
            SetObjectReference(serializedNote, "bodyField", body);
            serializedNote.ApplyModifiedPropertiesWithoutUndo();
            return noteView;
        }

        private static CookingPreparationHandView CreatePreparationHand(
            Transform parent,
            TMP_FontAsset font,
            CookingPreparationOptionCardView cardPrefab)
        {
            GameObject hand = CreateImage("PreparationHand", parent, new Color(0.12f, 0.08f, 0.055f, 0.97f));
            SetBottom(hand.GetComponent<RectTransform>(), new Vector2(1120f, 220f), new Vector2(0f, 34f));
            CanvasGroup cardGroup = hand.AddComponent<CanvasGroup>();
            CookingPreparationHandView handView = hand.AddComponent<CookingPreparationHandView>();

            GameObject cardRootObject = new GameObject("CardRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardRootObject.transform.SetParent(hand.transform, false);
            RectTransform cardRoot = cardRootObject.GetComponent<RectTransform>();
            SetStretch(cardRoot, new Vector2(24f, 18f), new Vector2(-24f, -18f));

            HorizontalLayoutGroup layoutGroup = cardRootObject.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 18f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            SerializedObject serializedHand = new SerializedObject(handView);
            SetObjectReference(serializedHand, "cardRoot", cardRoot);
            SetObjectReference(serializedHand, "preparationOptionCardPrefab", cardPrefab);
            SetObjectReference(serializedHand, "cardGroup", cardGroup);
            SetObjectReference(serializedHand, "fontAsset", font);
            serializedHand.ApplyModifiedPropertiesWithoutUndo();
            return handView;
        }

        private static CookingViewTransition CreateTransition(Transform parent)
        {
            GameObject fade = CreateImage("CookingViewTransition", parent, Color.black);
            SetStretch(fade.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            CanvasGroup fadeGroup = fade.AddComponent<CanvasGroup>();
            CookingViewTransition transition = fade.AddComponent<CookingViewTransition>();
            Image fadeImage = fade.GetComponent<Image>();
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
            fade.SetActive(false);

            SerializedObject serializedTransition = new SerializedObject(transition);
            SetObjectReference(serializedTransition, "fadeGroup", fadeGroup);
            SetObjectReference(serializedTransition, "fadeImage", fadeImage);
            SetFloat(serializedTransition, "enterFadeDuration", 0.25f);
            SetColor(serializedTransition, "fadeColor", Color.black);
            serializedTransition.ApplyModifiedPropertiesWithoutUndo();
            fade.transform.SetAsLastSibling();
            return transition;
        }

        private static void ConfigureCookingView(
            CookingView view,
            CookingGamePanel panel,
            CookingFlowRunner runner,
            CookingKnowledgeStore knowledgeStore,
            CookingWorkbenchView workbenchView,
            CookingPreparationHandView handView,
            CookingActivePreparationSlotView slotView,
            CookingOrderNoteView orderNoteView,
            CookingViewTransition transition,
            TextMeshProUGUI progressField,
            TMP_FontAsset font)
        {
            SerializedObject serializedView = new SerializedObject(view);
            SetObjectReference(serializedView, "gamePanel", panel);
            SetObjectReference(serializedView, "flowRunner", runner);
            SetObjectReference(serializedView, "knowledgeStore", knowledgeStore);
            SetObjectReference(serializedView, "workbenchView", workbenchView);
            SetObjectReference(serializedView, "handView", handView);
            SetObjectReference(serializedView, "activeSlotView", slotView);
            SetObjectReference(serializedView, "orderNoteView", orderNoteView);
            SetObjectReference(serializedView, "transition", transition);
            SetObjectReference(serializedView, "progressField", progressField);
            SetObjectReference(serializedView, "fontAsset", font);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCookingGamePanel(CookingGamePanel panel, GameObject preparationView)
        {
            SerializedObject serializedPanel = new SerializedObject(panel);
            SetObjectReference(serializedPanel, "preparationView", preparationView);
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DisableLegacyPreparationViews(GameObject cookingViewRoot)
        {
            CookingPreparationView[] legacyViews = UnityEngine.Object.FindObjectsByType<CookingPreparationView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < legacyViews.Length; i++)
            {
                if (legacyViews[i] == null || legacyViews[i].gameObject == cookingViewRoot)
                    continue;

                legacyViews[i].gameObject.SetActive(false);
                EditorUtility.SetDirty(legacyViews[i].gameObject);
            }
        }

        private static Transform FindViewParent(CookingGamePanel panel)
        {
            Canvas canvas = panel.GetComponentInParent<Canvas>(true);
            if (canvas == null)
                canvas = FindFirstSceneObject<Canvas>();

            return canvas != null ? canvas.rootCanvas.transform : panel.transform;
        }

        private static CookingFlowRunner ResolveFlowRunner(CookingGamePanel panel)
        {
            if (panel.FlowRunner != null)
                return panel.FlowRunner;

            CookingFlowRunner runner = panel.GetComponentInChildren<CookingFlowRunner>(true);
            if (runner != null)
                return runner;

            return FindFirstSceneObject<CookingFlowRunner>();
        }

        private static CookingKnowledgeStore ResolveKnowledgeStore(CookingGamePanel panel)
        {
            CookingKnowledgeStore store = panel.GetComponentInChildren<CookingKnowledgeStore>(true);
            if (store != null)
                return store;

            store = panel.GetComponent<CookingKnowledgeStore>();
            if (store != null)
                return store;

            return FindFirstSceneObject<CookingKnowledgeStore>();
        }

        private static T FindFirstSceneObject<T>()
            where T : UnityEngine.Object
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return objects.Length > 0 ? objects[0] : null;
        }

        private static T EnsureComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            if (component != null)
                return component;

            return target.AddComponent<T>();
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

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
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

        private static Button CreateButton(string name, Transform parent, Color color)
        {
            GameObject gameObject = CreateImage(name, parent, color);
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            return button;
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
            textField.text = text ?? string.Empty;
            textField.font = font;
            textField.fontSize = fontSize;
            textField.fontStyle = fontStyle;
            textField.alignment = alignment;
            textField.color = color;
            textField.raycastTarget = false;
            return textField;
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.colorValue = value;
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

        private static void SetLeft(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void SetRight(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
