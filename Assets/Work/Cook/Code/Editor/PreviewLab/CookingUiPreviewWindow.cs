using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Editor.PreviewLab
{
    public sealed class CookingUiPreviewWindow : EditorWindow
    {
        internal const string CookTestScenePath = "Assets/Work/Cook/Scene/CookTestScene.unity";
        internal const string PreviewRootPath = "Assets/Work/Cook/Code/Editor/PreviewLab";
        internal const string DefaultScenarioPath =
            "Assets/Work/Cook/Code/Editor/PreviewLab/Scenarios/DefaultCookingUiPreviewScenario.asset";

        private const string PresentationPrefabPath =
            "Assets/Work/Cook/Prefabs/UI/CookingPresentationRoot.prefab";
        private const string PresentationSettingsPath =
            "Assets/Work/Cook/SO/CookingUiPresentationSettings.asset";
        private const string MiniGameSettingsPath =
            "Assets/Work/Cook/SO/CookingMiniGameOverlaySettings.asset";
        private const string DataCatalogPath =
            "Assets/Work/Cook/SO/CookingDataCatalog.asset";

        [SerializeField] private CookingUiPreviewScenario scenario;
        [SerializeField] private CookingGamePanel targetPanel;
        [SerializeField] private bool autoReapplyAfterAssetChange = true;
        [SerializeField] private Vector2 scrollPosition;

        private UnityEditor.Editor _scenarioEditor;
        private string _lastCapturePath = string.Empty;

        [MenuItem("Tools/Dungeon Dinner/Cooking UI Preview Lab")]
        [MenuItem("Window/Dungeon Dinner/Cooking UI Preview Lab")]
        public static void Open()
        {
            CookingUiPreviewWindow window = GetWindow<CookingUiPreviewWindow>("Cooking UI Preview");
            window.minSize = new Vector2(520f, 680f);
            window.Show();
        }

        internal static void RepaintOpenWindows()
        {
            CookingUiPreviewWindow[] windows = Resources.FindObjectsOfTypeAll<CookingUiPreviewWindow>();
            for (int i = 0; i < windows.Length; i++)
                windows[i]?.Repaint();
        }

        private void OnEnable()
        {
            if (scenario == null)
                scenario = AssetDatabase.LoadAssetAtPath<CookingUiPreviewScenario>(DefaultScenarioPath);
            ResolvePanel();
        }

        private void OnDisable()
        {
            DestroyScenarioEditor();
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && targetPanel == null)
                ResolvePanel();
            Repaint();
        }

        private void OnProjectChange()
        {
            if (autoReapplyAfterAssetChange == false || Application.isPlaying == false || scenario == null)
                return;

            EditorApplication.delayCall += () =>
            {
                if (this == null || Application.isPlaying == false)
                    return;

                ResolvePanel();
                if (targetPanel == null)
                    return;

                targetPanel.ReinitializeCookingViews();
                CookingUiPreviewDriver.Apply(targetPanel, scenario, logResult: false);
                Repaint();
            };
        }

        private void OnGUI()
        {
            DrawHeader();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawScenarioSection();
            DrawSceneSection();
            DrawPlayModeControls();
            DrawSourceAssetSection();
            DrawStatusSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cooking UI Preview Lab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "이 창의 코드와 시나리오는 Editor 전용입니다. CookTestScene에는 Preview 컴포넌트를 추가하지 않고, " +
                "Play Mode에서 기존 CookingGamePanel 공개 API를 외부에서 구동합니다.",
                MessageType.Info);
        }

        private void DrawScenarioSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("프리뷰 시나리오", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            CookingUiPreviewScenario nextScenario = (CookingUiPreviewScenario)EditorGUILayout.ObjectField(
                "Scenario",
                scenario,
                typeof(CookingUiPreviewScenario),
                false);
            if (EditorGUI.EndChangeCheck())
                SetScenario(nextScenario);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("새 시나리오 생성"))
                CreateScenarioAsset();
            using (new EditorGUI.DisabledScope(scenario == null))
            {
                if (GUILayout.Button("선택/Ping"))
                {
                    Selection.activeObject = scenario;
                    EditorGUIUtility.PingObject(scenario);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (scenario != null)
            {
                EnsureScenarioEditor();
                EditorGUILayout.Space(4f);
                _scenarioEditor?.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "재료, 손질 옵션, 강제 등급을 저장할 시나리오를 생성하거나 선택하세요.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("대상 씬", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("현재 씬", SceneManager.GetActiveScene().path);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("CookTestScene 열기"))
                    OpenCookTestScene();
                using (new EditorGUI.DisabledScope(scenario == null))
                {
                    if (GUILayout.Button("열기 + Play + 적용"))
                        CookingUiPreviewPlayModeCoordinator.StartCookTestPreview(scenario);
                }
                EditorGUILayout.EndHorizontal();
            }

            autoReapplyAfterAssetChange = EditorGUILayout.ToggleLeft(
                "프리팹/SO 변경 후 현재 시나리오 자동 재적용",
                autoReapplyAfterAssetChange);
            EditorGUILayout.EndVertical();
        }

        private void DrawPlayModeControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Play Mode 프리뷰", EditorStyles.boldLabel);

            if (Application.isPlaying == false)
            {
                EditorGUILayout.HelpBox("Play Mode에서 실제 UGUI, DOTween, 미니게임 입력을 확인할 수 있습니다.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            targetPanel = (CookingGamePanel)EditorGUILayout.ObjectField(
                "Target Panel",
                targetPanel,
                typeof(CookingGamePanel),
                true);

            if (targetPanel == null && GUILayout.Button("CookingGamePanel 다시 찾기"))
                ResolvePanel();

            using (new EditorGUI.DisabledScope(targetPanel == null || scenario == null))
            {
                if (GUILayout.Button("Scenario Target 적용", GUILayout.Height(28f)))
                    ApplyScenario(null);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("재료 선택"))
                    ApplyScenario(CookingUiPreviewScreen.IngredientSelection);
                if (GUILayout.Button("손질"))
                    ApplyScenario(CookingUiPreviewScreen.Preparation);
                if (GUILayout.Button("미니게임"))
                    ApplyScenario(CookingUiPreviewScreen.MiniGame);
                if (GUILayout.Button("결과"))
                    ApplyScenario(CookingUiPreviewScreen.Result);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("미니게임 강제 판정"))
                    CookingUiPreviewDriver.ForceActiveMiniGameResult(targetPanel, scenario);
                if (GUILayout.Button("View 재초기화 + 재적용"))
                {
                    targetPanel.ReinitializeCookingViews();
                    ApplyScenario(null);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("현재 Game View 캡처"))
                    CaptureGameView();
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_lastCapturePath)))
                {
                    if (GUILayout.Button("마지막 캡처 위치 열기"))
                        EditorUtility.RevealInFinder(_lastCapturePath);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSourceAssetSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("공통 원본 에셋", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "여기서 여는 프리팹과 SO가 CookTestScene에서도 사용하는 원본입니다. Play Mode 인스턴스를 직접 수정하지 마세요.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            DrawOpenAssetButton("UI 프리팹", PresentationPrefabPath);
            DrawOpenAssetButton("UI 테마", PresentationSettingsPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawOpenAssetButton("미니게임 설정", MiniGameSettingsPath);
            DrawOpenAssetButton("요리 카탈로그", DataCatalogPath);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(CookingUiPreviewDriver.LastMessage)
                    ? "아직 적용된 프리뷰가 없습니다."
                    : CookingUiPreviewDriver.LastMessage,
                MessageType.None);

            if (targetPanel != null)
                EditorGUILayout.TextArea(targetPanel.CurrentSnapshot.BuildDebugSummary(), GUILayout.MinHeight(70f));
            EditorGUILayout.EndVertical();
        }

        private void SetScenario(CookingUiPreviewScenario value)
        {
            if (scenario == value)
                return;

            DestroyScenarioEditor();
            scenario = value;
            Repaint();
        }

        private void EnsureScenarioEditor()
        {
            if (_scenarioEditor != null && _scenarioEditor.target == scenario)
                return;

            DestroyScenarioEditor();
            if (scenario != null)
                _scenarioEditor = UnityEditor.Editor.CreateEditor(scenario);
        }

        private void DestroyScenarioEditor()
        {
            if (_scenarioEditor == null)
                return;

            DestroyImmediate(_scenarioEditor);
            _scenarioEditor = null;
        }

        private void CreateScenarioAsset()
        {
            string defaultFolder = $"{PreviewRootPath}/Scenarios";
            string absoluteFolder = Path.GetFullPath(defaultFolder);
            if (Directory.Exists(absoluteFolder) == false)
            {
                Directory.CreateDirectory(absoluteFolder);
                AssetDatabase.Refresh();
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Cooking UI Preview Scenario 생성",
                "CookingUiPreviewScenario",
                "asset",
                "Editor 전용 프리뷰 시나리오를 저장할 위치를 선택하세요.",
                defaultFolder);
            if (string.IsNullOrWhiteSpace(path))
                return;

            CookingUiPreviewScenario asset = CreateInstance<CookingUiPreviewScenario>();
            AssetDatabase.CreateAsset(asset, path);
            InitializeScenarioDefaults(asset);
            AssetDatabase.SaveAssets();
            SetScenario(asset);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void InitializeScenarioDefaults(CookingUiPreviewScenario asset)
        {
            CookingDataCatalogSO catalog = AssetDatabase.LoadAssetAtPath<CookingDataCatalogSO>(DataCatalogPath);
            if (asset == null || catalog == null)
                return;

            IngredientSO ingredient = FindUsefulDefaultIngredient(catalog);
            SerializedObject serializedScenario = new SerializedObject(asset);
            serializedScenario.FindProperty("catalogOverride").objectReferenceValue = catalog;

            if (ingredient != null)
            {
                SerializedProperty ingredients = serializedScenario.FindProperty("ingredients");
                ingredients.arraySize = 1;
                SerializedProperty entry = ingredients.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("ingredient").objectReferenceValue = ingredient;
                entry.FindPropertyRelative("quantity").intValue = 1;
                entry.FindPropertyRelative("preparationOptionIndex").intValue =
                    Mathf.Max(0, FindMiniGameOptionIndex(ingredient));
            }

            serializedScenario.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static IngredientSO FindUsefulDefaultIngredient(CookingDataCatalogSO catalog)
        {
            IngredientSO firstValid = null;
            IngredientSO firstWithPreparation = null;
            for (int ingredientIndex = 0; ingredientIndex < catalog.Ingredients.Count; ingredientIndex++)
            {
                IngredientSO ingredient = catalog.Ingredients[ingredientIndex];
                if (ingredient == null)
                    continue;

                firstValid ??= ingredient;
                if (ingredient.PreparationOptions == null || ingredient.PreparationOptions.Count == 0)
                    continue;

                firstWithPreparation ??= ingredient;
                if (FindMiniGameOptionIndex(ingredient) >= 0)
                    return ingredient;
            }

            return firstWithPreparation != null ? firstWithPreparation : firstValid;
        }

        private static int FindMiniGameOptionIndex(IngredientSO ingredient)
        {
            if (ingredient?.PreparationOptions == null)
                return -1;

            for (int optionIndex = 0; optionIndex < ingredient.PreparationOptions.Count; optionIndex++)
            {
                IngredientPreparationOption option = ingredient.PreparationOptions[optionIndex];
                if (option != null && option.MiniGameType != CookingMiniGameType.None)
                    return optionIndex;
            }

            return -1;
        }

        private static void OpenCookTestScene()
        {
            if (Application.isPlaying)
                return;
            if (SceneManager.GetActiveScene().path == CookTestScenePath)
                return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;

            EditorSceneManager.OpenScene(CookTestScenePath, OpenSceneMode.Single);
        }

        private void ResolvePanel()
        {
            targetPanel = CookingUiPreviewDriver.FindPanel();
        }

        private void ApplyScenario(CookingUiPreviewScreen? screen)
        {
            ResolvePanel();
            CookingUiPreviewDriver.Apply(targetPanel, scenario, screen);
            Repaint();
        }

        private void CaptureGameView()
        {
            if (Application.isPlaying == false)
                return;

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string directory = Path.Combine(projectRoot, "Captures", "CookingUiPreview");
            Directory.CreateDirectory(directory);

            string scenarioName = scenario != null ? SanitizeFileName(scenario.name) : "Scenario";
            string screenName = targetPanel != null ? targetPanel.CurrentScreen.ToString() : "Unknown";
            string fileName = $"{scenarioName}_{screenName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            _lastCapturePath = Path.Combine(directory, fileName);
            ScreenCapture.CaptureScreenshot(_lastCapturePath);
            Debug.Log($"[Cooking UI Preview] Game View 캡처 예약: {_lastCapturePath}", targetPanel);
        }

        private static string SanitizeFileName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Scenario" : value;
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                result = result.Replace(invalid[i], '_');
            return result;
        }

        private static void DrawOpenAssetButton(string label, string path)
        {
            if (GUILayout.Button(label) == false)
                return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                Debug.LogWarning($"[Cooking UI Preview] 원본 에셋을 찾지 못했습니다: {path}");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }
    }
}
