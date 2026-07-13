using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;
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
        private const string MINI_GAME_ROOT_NAME = "CookingMiniGameRoot";

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
            GameObject miniGameRoot = CreateOrUpdateMiniGameView(panel, font);
            ConfigureCookingGamePanel(panel, viewRoot, miniGameRoot);
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

        private static GameObject CreateChoppingMiniGamePanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreateMiniGamePanelFrame(
                "ChoppingMiniGamePanel",
                parent,
                font,
                "다지기 미니게임",
                out TextMeshProUGUI title,
                out TextMeshProUGUI instruction,
                out TextMeshProUGUI progress,
                out Button cancelButton);
            CookingChoppingMiniGameView view = panel.AddComponent<CookingChoppingMiniGameView>();

            GameObject targetArea = CreateImage("TargetArea", panel.transform, new Color(0.36f, 0.2f, 0.09f, 1f));
            SetCenter(targetArea.GetComponent<RectTransform>(), new Vector2(680f, 420f), new Vector2(0f, -8f));
            Vector2[] positions =
            {
                new Vector2(-210f, 90f),
                new Vector2(0f, 120f),
                new Vector2(210f, 80f),
                new Vector2(-120f, -100f),
                new Vector2(120f, -110f)
            };
            Button[] targets = new Button[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Button target = CreateButton($"Target{i + 1}", targetArea.transform, new Color(0.45f, 0.3f, 0.16f, 1f));
                SetCenter(target.GetComponent<RectTransform>(), new Vector2(86f, 86f), positions[i]);
                targets[i] = target;
            }

            SerializedObject serializedView = new SerializedObject(view);
            SetObjectReferenceArray(serializedView, "targetButtons", targets);
            SetObjectReference(serializedView, "titleField", title);
            SetObjectReference(serializedView, "instructionField", instruction);
            SetObjectReference(serializedView, "progressField", progress);
            SetObjectReference(serializedView, "cancelButton", cancelButton);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateCleansingMiniGamePanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreateMiniGamePanelFrame(
                "CleansingMiniGamePanel",
                parent,
                font,
                "씻기 미니게임",
                out TextMeshProUGUI title,
                out TextMeshProUGUI instruction,
                out TextMeshProUGUI progress,
                out Button cancelButton);
            CookingCleansingMiniGameView view = panel.AddComponent<CookingCleansingMiniGameView>();

            GameObject interactionObject = CreateImage("CleansingInteractionArea", panel.transform, new Color(0.18f, 0.34f, 0.42f, 1f));
            RectTransform interactionArea = interactionObject.GetComponent<RectTransform>();
            SetCenter(interactionArea, new Vector2(680f, 420f), new Vector2(0f, -8f));
            Image ingredientImage = CreateImage("Ingredient", interactionObject.transform, new Color(0.72f, 0.78f, 0.62f, 1f)).GetComponent<Image>();
            SetCenter(ingredientImage.rectTransform, new Vector2(500f, 300f), Vector2.zero);
            ingredientImage.raycastTarget = false;
            ingredientImage.preserveAspect = true;

            Vector2[] stainPositions =
            {
                new Vector2(-170f, 80f),
                new Vector2(40f, 105f),
                new Vector2(180f, -20f),
                new Vector2(-65f, -105f)
            };
            Image[] stains = new Image[stainPositions.Length];
            for (int i = 0; i < stains.Length; i++)
            {
                Image stain = CreateImage("Stain" + (i + 1), interactionObject.transform, new Color(0.24f, 0.14f, 0.08f, 1f)).GetComponent<Image>();
                SetCenter(stain.rectTransform, new Vector2(105f, 82f), stainPositions[i]);
                stain.raycastTarget = false;
                stains[i] = stain;
            }

            Image brush = CreateImage("Brush", interactionObject.transform, new Color(0.75f, 0.92f, 1f, 0.9f)).GetComponent<Image>();
            SetCenter(brush.rectTransform, new Vector2(70f, 70f), Vector2.zero);
            brush.raycastTarget = false;
            brush.gameObject.SetActive(false);

            SerializedObject serializedView = new SerializedObject(view);
            SetObjectReference(serializedView, "interactionArea", interactionArea);
            SetObjectReference(serializedView, "ingredientImage", ingredientImage);
            SetObjectReference(serializedView, "brushImage", brush);
            SetObjectReferenceArray(serializedView, "stainImages", stains);
            SetObjectReference(serializedView, "titleField", title);
            SetObjectReference(serializedView, "instructionField", instruction);
            SetObjectReference(serializedView, "progressField", progress);
            SetObjectReference(serializedView, "cancelButton", cancelButton);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateGaugeMiniGamePanel(
            Transform parent,
            TMP_FontAsset font,
            string panelName,
            CookingMiniGameType miniGameType,
            string actionName,
            string instructionText,
            float duration,
            float targetMin,
            float targetMax)
        {
            GameObject panel = CreateMiniGamePanelFrame(
                panelName,
                parent,
                font,
                actionName + " 미니게임",
                out TextMeshProUGUI title,
                out TextMeshProUGUI instruction,
                out TextMeshProUGUI progress,
                out Button cancelButton);
            CookingGaugeMiniGameView view = panel.AddComponent<CookingGaugeMiniGameView>();

            Slider slider = CreateProgressSlider("CookingGauge", panel.transform, new Vector2(600f, 72f), new Vector2(0f, -10f));
            GameObject targetObject = CreateImage("TargetZone", slider.transform, new Color(0.3f, 0.8f, 0.32f, 0.52f));
            RectTransform targetZone = targetObject.GetComponent<RectTransform>();
            targetObject.GetComponent<Image>().raycastTarget = false;

            Button stopButton = CreateLabeledButton(
                "StopButton",
                panel.transform,
                "멈추기",
                font,
                new Color(0.72f, 0.4f, 0.14f, 1f),
                new Vector2(220f, 80f),
                new Vector2(0f, -155f));

            SerializedObject serializedView = new SerializedObject(view);
            SetInteger(serializedView, "miniGameType", (int)miniGameType);
            SetObjectReference(serializedView, "progressSlider", slider);
            SetObjectReference(serializedView, "targetZone", targetZone);
            SetObjectReference(serializedView, "titleField", title);
            SetObjectReference(serializedView, "instructionField", instruction);
            SetObjectReference(serializedView, "progressField", progress);
            SetObjectReference(serializedView, "stopButton", stopButton);
            SetObjectReference(serializedView, "cancelButton", cancelButton);
            SetString(serializedView, "actionName", actionName);
            SetString(serializedView, "instructionText", instructionText);
            SetFloat(serializedView, "duration", duration);
            SetFloat(serializedView, "targetMin", targetMin);
            SetFloat(serializedView, "targetMax", targetMax);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateGrindingMiniGamePanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreateMiniGamePanelFrame(
                "GrindingMiniGamePanel",
                parent,
                font,
                "빻기 미니게임",
                out TextMeshProUGUI title,
                out TextMeshProUGUI instruction,
                out TextMeshProUGUI progress,
                out Button cancelButton);
            CookingGrindingMiniGameView view = panel.AddComponent<CookingGrindingMiniGameView>();
            Slider slider = CreateProgressSlider("ParticleGauge", panel.transform, new Vector2(560f, 62f), new Vector2(0f, 55f));
            Button strikeButton = CreateLabeledButton(
                "StrikeButton",
                panel.transform,
                "막자 내려치기",
                font,
                new Color(0.56f, 0.34f, 0.18f, 1f),
                new Vector2(280f, 110f),
                new Vector2(0f, -95f));

            SerializedObject serializedView = new SerializedObject(view);
            SetObjectReference(serializedView, "strikeButton", strikeButton);
            SetObjectReference(serializedView, "cancelButton", cancelButton);
            SetObjectReference(serializedView, "particleSlider", slider);
            SetObjectReference(serializedView, "titleField", title);
            SetObjectReference(serializedView, "instructionField", instruction);
            SetObjectReference(serializedView, "progressField", progress);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateStewingMiniGamePanel(Transform parent, TMP_FontAsset font)
        {
            GameObject panel = CreateMiniGamePanelFrame(
                "StewingMiniGamePanel",
                parent,
                font,
                "끓이기 미니게임",
                out TextMeshProUGUI title,
                out TextMeshProUGUI instruction,
                out TextMeshProUGUI progress,
                out Button cancelButton);
            CookingStewingMiniGameView view = panel.AddComponent<CookingStewingMiniGameView>();

            Button heatButton = CreateLabeledButton(
                "HeatButton",
                panel.transform,
                "불 조절",
                font,
                new Color(0.75f, 0.3f, 0.12f, 1f),
                new Vector2(180f, 100f),
                new Vector2(-210f, -40f));
            Button stirButton = CreateLabeledButton(
                "StirButton",
                panel.transform,
                "젓기",
                font,
                new Color(0.34f, 0.48f, 0.72f, 1f),
                new Vector2(180f, 100f),
                new Vector2(0f, -40f));
            Button skimButton = CreateLabeledButton(
                "SkimButton",
                panel.transform,
                "거품 걷기",
                font,
                new Color(0.35f, 0.65f, 0.42f, 1f),
                new Vector2(180f, 100f),
                new Vector2(210f, -40f));

            SerializedObject serializedView = new SerializedObject(view);
            SetObjectReference(serializedView, "heatButton", heatButton);
            SetObjectReference(serializedView, "stirButton", stirButton);
            SetObjectReference(serializedView, "skimButton", skimButton);
            SetObjectReference(serializedView, "cancelButton", cancelButton);
            SetObjectReference(serializedView, "titleField", title);
            SetObjectReference(serializedView, "instructionField", instruction);
            SetObjectReference(serializedView, "progressField", progress);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateMiniGamePanelFrame(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string titleText,
            out TextMeshProUGUI title,
            out TextMeshProUGUI instruction,
            out TextMeshProUGUI progress,
            out Button cancelButton)
        {
            GameObject panel = CreateImage(name, parent, new Color(0.12f, 0.075f, 0.045f, 0.98f));
            SetCenter(panel.GetComponent<RectTransform>(), new Vector2(820f, 680f), Vector2.zero);
            title = CreateText(
                "Title",
                panel.transform,
                titleText,
                font,
                36f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.86f, 0.5f, 1f));
            SetTop(title.rectTransform, new Vector2(640f, 58f), new Vector2(0f, -36f));
            instruction = CreateText(
                "Instruction",
                panel.transform,
                string.Empty,
                font,
                23f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.94f, 0.86f, 0.72f, 1f));
            SetTop(instruction.rectTransform, new Vector2(680f, 76f), new Vector2(0f, -94f));
            progress = CreateText(
                "Progress",
                panel.transform,
                string.Empty,
                font,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(0.9f, 0.95f, 0.78f, 1f));
            SetBottom(progress.rectTransform, new Vector2(680f, 52f), new Vector2(0f, 28f));
            cancelButton = CreateLabeledButton(
                "CancelButton",
                panel.transform,
                "취소",
                font,
                new Color(0.48f, 0.2f, 0.16f, 1f),
                new Vector2(120f, 52f),
                new Vector2(332f, 290f));
            return panel;
        }

        private static GameObject CreateOrUpdateMiniGameView(CookingGamePanel panel, TMP_FontAsset font)
        {
            Transform parent = FindViewParent(panel);
            GameObject root = FindChildByName(parent, MINI_GAME_ROOT_NAME);
            if (root == null)
            {
                root = new GameObject(
                    MINI_GAME_ROOT_NAME,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup),
                    typeof(CookingMiniGameRouterView));
                root.transform.SetParent(parent, false);
            }
            else
            {
                EnsureComponent<Image>(root);
                EnsureComponent<CanvasGroup>(root);
                EnsureComponent<CookingMiniGameRouterView>(root);
                ClearChildren(root.transform);
            }

            root.SetActive(false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, Vector2.zero, Vector2.zero);

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.58f);
            rootImage.raycastTarget = true;

            CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            GameObject slicingPanel = CreateImage("SlicingMiniGamePanel", root.transform, new Color(0.12f, 0.075f, 0.045f, 0.98f));
            SetCenter(slicingPanel.GetComponent<RectTransform>(), new Vector2(820f, 680f), Vector2.zero);
            CookingSlicingMiniGameView slicingView = slicingPanel.AddComponent<CookingSlicingMiniGameView>();

            TextMeshProUGUI title = CreateText(
                "Title",
                slicingPanel.transform,
                "썰기 미니게임",
                font,
                36f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(1f, 0.86f, 0.5f, 1f));
            SetTop(title.rectTransform, new Vector2(640f, 58f), new Vector2(0f, -36f));

            TextMeshProUGUI instruction = CreateText(
                "Instruction",
                slicingPanel.transform,
                string.Empty,
                font,
                23f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color(0.94f, 0.86f, 0.72f, 1f));
            SetTop(instruction.rectTransform, new Vector2(680f, 76f), new Vector2(0f, -94f));

            TextMeshProUGUI progress = CreateText(
                "Progress",
                slicingPanel.transform,
                string.Empty,
                font,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(0.9f, 0.95f, 0.78f, 1f));
            SetBottom(progress.rectTransform, new Vector2(680f, 52f), new Vector2(0f, 28f));

            Button cancelButton = CreateButton("CancelButton", slicingPanel.transform, new Color(0.48f, 0.2f, 0.16f, 1f));
            SetTop(cancelButton.GetComponent<RectTransform>(), new Vector2(120f, 52f), new Vector2(332f, -24f));
            TextMeshProUGUI cancelLabel = CreateText(
                "Label",
                cancelButton.transform,
                "취소",
                font,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white);
            SetStretch(cancelLabel.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -6f));

            GameObject interactionObject = CreateImage(
                "SlicingInteractionArea",
                slicingPanel.transform,
                new Color(0.36f, 0.2f, 0.09f, 1f));
            RectTransform interactionArea = interactionObject.GetComponent<RectTransform>();
            SetCenter(interactionArea, new Vector2(680f, 440f), new Vector2(0f, -4f));

            Image ingredientImage = CreateImage(
                "Ingredient",
                interactionObject.transform,
                new Color(0.84f, 0.68f, 0.42f, 1f)).GetComponent<Image>();
            SetCenter(ingredientImage.rectTransform, new Vector2(500f, 300f), Vector2.zero);
            ingredientImage.preserveAspect = true;
            ingredientImage.raycastTarget = false;

            Image[] cutLines = new Image[3];
            float[] linePositions = { -150f, 0f, 150f };
            for (int i = 0; i < cutLines.Length; i++)
            {
                Image cutLine = CreateImage(
                    $"CutLine{i + 1}",
                    interactionObject.transform,
                    new Color(1f, 0.88f, 0.45f, 0.9f)).GetComponent<Image>();
                SetCenter(cutLine.rectTransform, new Vector2(12f, 280f), new Vector2(linePositions[i], 0f));
                cutLine.raycastTarget = false;
                cutLines[i] = cutLine;
            }

            Image knifeImage = CreateImage(
                "Knife",
                interactionObject.transform,
                new Color(0.88f, 0.9f, 0.94f, 1f)).GetComponent<Image>();
            SetCenter(knifeImage.rectTransform, new Vector2(30f, 96f), Vector2.zero);
            knifeImage.raycastTarget = false;
            knifeImage.gameObject.SetActive(false);

            ConfigureSlicingMiniGameView(
                slicingView,
                interactionArea,
                ingredientImage,
                knifeImage,
                cutLines,
                title,
                instruction,
                progress,
                cancelButton);
            slicingPanel.SetActive(false);

            GameObject choppingPanel = CreateChoppingMiniGamePanel(root.transform, font);
            GameObject cleansingPanel = CreateCleansingMiniGamePanel(root.transform, font);
            GameObject roastingPanel = CreateGaugeMiniGamePanel(
                root.transform,
                font,
                "RoastingMiniGamePanel",
                CookingMiniGameType.Roasting,
                "굽기",
                "재료가 노릇해지는 목표 구간에서 꺼내세요.",
                6f,
                0.58f,
                0.76f);
            GameObject burningPanel = CreateGaugeMiniGamePanel(
                root.transform,
                font,
                "BurningMiniGamePanel",
                CookingMiniGameType.Burning,
                "태우기",
                "재가 되기 직전의 위험 구간에서 꺼내세요.",
                5f,
                0.78f,
                0.92f);
            GameObject boilingPanel = CreateGaugeMiniGamePanel(
                root.transform,
                font,
                "BoilingMiniGamePanel",
                CookingMiniGameType.Boiling,
                "삶기",
                "재료가 부드럽게 익은 구간에서 건져내세요.",
                6f,
                0.52f,
                0.7f);
            GameObject freezingPanel = CreateGaugeMiniGamePanel(
                root.transform,
                font,
                "FreezingMiniGamePanel",
                CookingMiniGameType.Freezing,
                "얼리기",
                "얼음 결정이 적절히 퍼졌을 때 냉기를 멈추세요.",
                5.5f,
                0.6f,
                0.78f);
            GameObject dilutingPanel = CreateGaugeMiniGamePanel(
                root.transform,
                font,
                "DilutingMiniGamePanel",
                CookingMiniGameType.Diluting,
                "묽게 만들기",
                "목표 농도 구간에서 액체 붓기를 멈추세요.",
                5f,
                0.48f,
                0.66f);
            GameObject stewingPanel = CreateStewingMiniGamePanel(root.transform, font);
            GameObject grindingPanel = CreateGrindingMiniGamePanel(root.transform, font);

            ConfigureMiniGameRouterView(
                root.GetComponent<CookingMiniGameRouterView>(),
                new UnityEngine.Object[]
                {
                    slicingPanel,
                    choppingPanel,
                    cleansingPanel,
                    roastingPanel,
                    burningPanel,
                    boilingPanel,
                    stewingPanel,
                    freezingPanel,
                    grindingPanel,
                    dilutingPanel
                });
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

        private static void ConfigureSlicingMiniGameView(
            CookingSlicingMiniGameView view,
            RectTransform interactionArea,
            Image ingredientImage,
            Image knifeImage,
            Image[] cutLines,
            TextMeshProUGUI title,
            TextMeshProUGUI instruction,
            TextMeshProUGUI progress,
            Button cancelButton)
        {
            SerializedObject serializedView = new SerializedObject(view);
            SetObjectReference(serializedView, "interactionArea", interactionArea);
            SetObjectReference(serializedView, "ingredientImage", ingredientImage);
            SetObjectReference(serializedView, "knifeImage", knifeImage);
            SetObjectReferenceArray(serializedView, "cutLineImages", cutLines);
            SetObjectReference(serializedView, "titleField", title);
            SetObjectReference(serializedView, "instructionField", instruction);
            SetObjectReference(serializedView, "progressField", progress);
            SetObjectReference(serializedView, "cancelButton", cancelButton);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMiniGameRouterView(
            CookingMiniGameRouterView view,
            UnityEngine.Object[] miniGamePanels)
        {
            SerializedObject serializedView = new SerializedObject(view);
            SetObjectReferenceArray(serializedView, "miniGameViewObjects", miniGamePanels);
            SetBool(serializedView, "autoCollectChildViews", true);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCookingGamePanel(
            CookingGamePanel panel,
            GameObject preparationView,
            GameObject miniGameView)
        {
            SerializedObject serializedPanel = new SerializedObject(panel);
            SetObjectReference(serializedPanel, "preparationView", preparationView);
            SetObjectReference(serializedPanel, "miniGameView", miniGameView);
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

        private static Button CreateLabeledButton(
            string name,
            Transform parent,
            string label,
            TMP_FontAsset font,
            Color color,
            Vector2 size,
            Vector2 position)
        {
            Button button = CreateButton(name, parent, color);
            SetCenter(button.GetComponent<RectTransform>(), size, position);
            TextMeshProUGUI labelField = CreateText(
                "Label",
                button.transform,
                label,
                font,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white);
            SetStretch(labelField.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -6f));
            return button;
        }

        private static Slider CreateProgressSlider(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 position)
        {
            GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            SetCenter(sliderObject.GetComponent<RectTransform>(), size, position);

            Image background = CreateImage("Background", sliderObject.transform, new Color(0.2f, 0.14f, 0.1f, 1f)).GetComponent<Image>();
            SetStretch(background.rectTransform, Vector2.zero, Vector2.zero);
            background.raycastTarget = false;

            GameObject fillAreaObject = new GameObject("FillArea", typeof(RectTransform));
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillArea = fillAreaObject.GetComponent<RectTransform>();
            SetStretch(fillArea, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            Image fill = CreateImage("Fill", fillArea, new Color(0.9f, 0.56f, 0.16f, 1f)).GetComponent<Image>();
            SetStretch(fill.rectTransform, Vector2.zero, Vector2.zero);
            fill.raycastTarget = false;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.interactable = false;
            return slider;
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

        private static void SetObjectReferenceArray(
            SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object[] values)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.objectReferenceValue = values[i];
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        private static void SetInteger(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value ?? string.Empty;
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
