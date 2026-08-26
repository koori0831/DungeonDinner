using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Editor
{
    [CustomEditor(typeof(CookingGamePanel))]
    public sealed class CookingGamePanelEditor : UnityEditor.Editor
    {
        private const string OverlayRootName = "CookingRewardOverlayRoot";

        private SerializedProperty _flowRunner;
        private SerializedProperty _npcRunner;
        private SerializedProperty _knowledgeStore;
        private SerializedProperty _rewardWallet;
        private SerializedProperty _temporaryUiFontAsset;
        private SerializedProperty _npcConversationView;
        private SerializedProperty _recipeSelectionView;
        private SerializedProperty _inventoryView;
        private SerializedProperty _preparationView;
        private SerializedProperty _resultView;
        private SerializedProperty _rewardView;

        private void OnEnable()
        {
            _flowRunner = serializedObject.FindProperty("flowRunner");
            _npcRunner = serializedObject.FindProperty("npcRunner");
            _knowledgeStore = serializedObject.FindProperty("knowledgeStore");
            _rewardWallet = serializedObject.FindProperty("rewardWallet");
            _temporaryUiFontAsset = serializedObject.FindProperty("temporaryUiFontAsset");
            _npcConversationView = serializedObject.FindProperty("npcConversationView");
            _recipeSelectionView = serializedObject.FindProperty("recipeSelectionView");
            _inventoryView = serializedObject.FindProperty("inventoryView");
            _preparationView = serializedObject.FindProperty("preparationView");
            _resultView = serializedObject.FindProperty("resultView");
            _rewardView = serializedObject.FindProperty("rewardView");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            serializedObject.Update();

            CookingGamePanel panel = (CookingGamePanel)target;

            EditorGUILayout.Space(8f);
            DrawConnectionSummary(panel);
            DrawDiagnostics(panel);
            DrawPlayModeShortcuts(panel);
        }

        private void DrawConnectionSummary(CookingGamePanel panel)
        {
            CookingFlowRunner runner = ResolveFlowRunner(panel);
            CookingDataCatalogSO catalog = runner != null ? runner.Catalog : null;
            NpcConversationRunner npc = ResolveNpcRunner();
            CookingKnowledgeStore knowledgeStore = ResolveKnowledgeStore(panel);
            CookingRewardWallet rewardWallet = ResolveRewardWallet(panel);
            TMP_FontAsset font = GetReference<TMP_FontAsset>(_temporaryUiFontAsset);
            GameObject rewardView = GetReference<GameObject>(_rewardView);
            Canvas canvas = FindBestCanvas(panel, GetViewReferences());
            Transform overlayRoot = canvas != null ? FindOverlayRoot(canvas) : null;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("요리 UI 연결 진단", EditorStyles.boldLabel);

            DrawReadonlyObject("Flow Runner", runner);
            DrawReadonlyObject("Catalog", catalog);
            DrawReadonlyObject("NPC Runner", npc);
            DrawReadonlyObject("Knowledge Store", knowledgeStore);
            DrawReadonlyObject("Reward Wallet", rewardWallet);
            DrawReadonlyObject("TMP Font", font);
            DrawReadonlyObject("연결 Canvas", canvas);
            DrawReadonlyObject("보상 Overlay Root", overlayRoot != null ? overlayRoot.gameObject : null);

            if (catalog != null)
            {
                EditorGUILayout.LabelField(
                    "Catalog Count",
                    $"카테고리 {catalog.Categories.Count} / 재료 {catalog.Ingredients.Count} / 레시피 {catalog.Recipes.Count}");
            }

            if (knowledgeStore != null)
                EditorGUILayout.LabelField("Knowledge", knowledgeStore.BuildDebugSummary());

            if (rewardWallet != null)
                EditorGUILayout.LabelField("Reward", rewardWallet.BuildDebugSummary());

            EditorGUILayout.LabelField("Snapshot", panel.CurrentSnapshot.BuildDebugSummary());

            DrawViewLine("NPC 대화", GetReference<GameObject>(_npcConversationView));
            DrawViewLine(
                "레시피",
                GetReference<GameObject>(_recipeSelectionView),
                "ICookingRecipeSelectionView",
                HasContract<ICookingRecipeSelectionView>(GetReference<GameObject>(_recipeSelectionView)));
            DrawViewLine(
                "가방",
                GetReference<GameObject>(_inventoryView),
                "ICookingIngredientSelectionView",
                HasContract<ICookingIngredientSelectionView>(GetReference<GameObject>(_inventoryView)));
            EditorGUILayout.LabelField("가방 재료 공급원", DescribeIngredientSource(GetReference<GameObject>(_inventoryView)));
            DrawViewLine(
                "손질",
                GetReference<GameObject>(_preparationView),
                "ICookingPreparationView",
                HasContract<ICookingPreparationView>(GetReference<GameObject>(_preparationView)));
            DrawViewLine(
                "결과",
                GetReference<GameObject>(_resultView),
                "ICookingResultView",
                HasContract<ICookingResultView>(GetReference<GameObject>(_resultView)));
            DrawViewLine(
                "보상",
                rewardView,
                "ICookingRewardView",
                HasContract<ICookingRewardView>(rewardView));

            if (runner == null)
                EditorGUILayout.HelpBox("Flow Runner가 연결되지 않았습니다. 레시피 선택, 재료 선택, 손질 진행이 시작되지 않습니다.", MessageType.Warning);

            if (npc == null)
                EditorGUILayout.HelpBox("NPC Runner가 연결되지 않았습니다. 음식 건네주기 이후 NPC 반응으로 돌아갈 수 없습니다.", MessageType.Warning);

            if (canvas == null)
                EditorGUILayout.HelpBox("연결된 Canvas를 찾지 못했습니다. 자동 생성 UI의 위치가 예상과 다를 수 있습니다.", MessageType.Warning);

            if (rewardView != null && overlayRoot != null && rewardView.transform.parent != overlayRoot)
            {
                EditorGUILayout.HelpBox(
                    "Reward View가 보상 Overlay Root 아래에 있지 않습니다. 런타임에서는 자동으로 옮기지만, 씬에서 바로 확인하려면 아래 버튼으로 이동할 수 있습니다.",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics(CookingGamePanel panel)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("진단 도구", EditorStyles.boldLabel);

            if (GUILayout.Button("연결 상태 콘솔 출력"))
                LogConnectionReport(panel);

            if (GUILayout.Button("카탈로그 검증 콘솔 출력"))
                LogCatalogValidation(panel);

            if (GUILayout.Button("현재 스냅샷 콘솔 출력"))
                LogCurrentSnapshot(panel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("연결 View 재초기화"))
                ReinitializeCookingViews(panel);

            if (GUILayout.Button("현재 View 새로고침"))
                RefreshCookingViews(panel);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("보상 Overlay Root 선택/생성"))
                SelectOrCreateOverlayRoot(panel);

            if (GUILayout.Button("Reward View를 Overlay Root로 이동"))
                MoveRewardViewToOverlayRoot(panel);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("요리 지식 비우기"))
                ClearKnowledge(panel);

            if (GUILayout.Button("요리 지식 Seed 복원"))
                ResetKnowledgeToSeed(panel);

            if (GUILayout.Button("보상 재화 초기화"))
                ClearRewardWallet(panel);

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("정보 Clear"))
                ClearCookingDebugInfo(panel);

            EditorGUILayout.EndVertical();
        }

        private void DrawPlayModeShortcuts(CookingGamePanel panel)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Play Mode 화면 전환", EditorStyles.boldLabel);

            if (Application.isPlaying == false)
            {
                EditorGUILayout.HelpBox("플레이 중에만 화면 전환 버튼을 사용할 수 있습니다.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("현재 화면", panel.CurrentScreen.ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("NPC"))
                panel.ReturnToNpcConversation();
            if (GUILayout.Button("레시피"))
                panel.OpenRecipeSelection();
            if (GUILayout.Button("가방"))
                panel.OpenDirectIngredientSelection();
            if (GUILayout.Button("손질"))
                panel.OpenPreparation();
            if (GUILayout.Button("닫기"))
                panel.CloseCookingViews();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void LogConnectionReport(CookingGamePanel panel)
        {
            CookingFlowRunner runner = ResolveFlowRunner(panel);
            CookingDataCatalogSO catalog = runner != null ? runner.Catalog : null;
            NpcConversationRunner npc = ResolveNpcRunner();
            CookingKnowledgeStore knowledgeStore = ResolveKnowledgeStore(panel);
            CookingRewardWallet rewardWallet = ResolveRewardWallet(panel);
            GameObject[] views = GetViewReferences();
            GameObject rewardView = GetReference<GameObject>(_rewardView);
            Canvas canvas = FindBestCanvas(panel, views);
            Transform overlayRoot = canvas != null ? FindOverlayRoot(canvas) : null;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[CookingGamePanel 연결 진단]");
            builder.AppendLine($"Panel: {GetHierarchyPath(panel.transform)}");
            builder.AppendLine($"Flow Runner: {DescribeObject(runner)}");
            builder.AppendLine($"Catalog: {DescribeObject(catalog)}");
            if (catalog != null)
            {
                builder.AppendLine(
                    $"Catalog Count: categories={catalog.Categories.Count}, ingredients={catalog.Ingredients.Count}, recipes={catalog.Recipes.Count}");
            }

            builder.AppendLine($"NPC Runner: {DescribeObject(npc)}");
            builder.AppendLine($"Knowledge Store: {DescribeObject(knowledgeStore)}");
            if (knowledgeStore != null)
                builder.AppendLine($"Knowledge Summary: {knowledgeStore.BuildDebugSummary()}");

            builder.AppendLine($"Reward Wallet: {DescribeObject(rewardWallet)}");
            if (rewardWallet != null)
                builder.AppendLine($"Reward Summary: {rewardWallet.BuildDebugSummary()}");

            builder.AppendLine($"TMP Font: {DescribeObject(GetReference<TMP_FontAsset>(_temporaryUiFontAsset))}");
            builder.AppendLine($"Connected Canvas: {DescribeObject(canvas)}");
            builder.AppendLine($"Overlay Root: {(overlayRoot != null ? GetHierarchyPath(overlayRoot) : "없음")}");
            builder.AppendLine();
            builder.AppendLine("[Views]");

            AppendViewReport(builder, "NPC 대화", views[0]);
            AppendViewReport(builder, "레시피", views[1]);
            AppendViewReport(builder, "가방", views[2]);
            AppendViewReport(builder, "손질", views[3]);
            AppendViewReport(builder, "결과", views[4]);
            AppendViewReport(builder, "보상", rewardView);

            if (rewardView != null && overlayRoot != null && rewardView.transform.parent != overlayRoot)
                builder.AppendLine("Reward View parent mismatch: 보상 UI가 Overlay Root 아래에 있지 않습니다.");

            Debug.Log(builder.ToString(), panel);
        }

        private void LogCatalogValidation(CookingGamePanel panel)
        {
            CookingFlowRunner runner = ResolveFlowRunner(panel);
            CookingDataCatalogSO catalog = runner != null ? runner.Catalog : null;
            if (catalog == null)
            {
                Debug.LogWarning("카탈로그가 연결되지 않아 검증할 수 없습니다.", panel);
                return;
            }

            List<string> messages = catalog.BuildValidationMessages();
            if (messages.Count == 0)
            {
                Debug.Log(
                    $"Cooking catalog validation passed. categories={catalog.Categories.Count}, ingredients={catalog.Ingredients.Count}, recipes={catalog.Recipes.Count}",
                    catalog);
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Cooking catalog validation found {messages.Count} issue(s).");
            for (int i = 0; i < messages.Count; i++)
                builder.AppendLine($"- {messages[i]}");

            Debug.LogWarning(builder.ToString(), catalog);
        }

        private static void LogCurrentSnapshot(CookingGamePanel panel)
        {
            if (panel == null)
                return;

            CookingGameSnapshot snapshot = panel.CurrentSnapshot;
            Debug.Log($"[CookingGamePanel Snapshot]\n{snapshot.BuildDebugSummary()}", panel);
        }

        private static void ReinitializeCookingViews(CookingGamePanel panel)
        {
            if (panel == null)
                return;

            Undo.RecordObject(panel, "Reinitialize Cooking Views");
            panel.ReinitializeCookingViews();
            EditorUtility.SetDirty(panel);
            Debug.Log("CookingGamePanel에 연결된 View를 다시 초기화했습니다.", panel);
        }

        private static void RefreshCookingViews(CookingGamePanel panel)
        {
            if (panel == null)
                return;

            panel.RefreshCookingViews();
            Debug.Log("CookingGamePanel의 현재 화면 View를 새로고침했습니다.", panel);
        }

        private void ClearKnowledge(CookingGamePanel panel)
        {
            CookingKnowledgeStore knowledgeStore = ResolveKnowledgeStore(panel);
            if (knowledgeStore == null)
            {
                Debug.LogWarning("초기화할 CookingKnowledgeStore를 찾지 못했습니다.", panel);
                return;
            }

            Undo.RecordObject(knowledgeStore, "Clear Cooking Knowledge");
            knowledgeStore.ClearKnowledgeForDebug();
            EditorUtility.SetDirty(knowledgeStore);
            Debug.Log($"요리 지식을 비웠습니다. {knowledgeStore.BuildDebugSummary()}", knowledgeStore);
        }

        private void ResetKnowledgeToSeed(CookingGamePanel panel)
        {
            CookingKnowledgeStore knowledgeStore = ResolveKnowledgeStore(panel);
            if (knowledgeStore == null)
            {
                Debug.LogWarning("복원할 CookingKnowledgeStore를 찾지 못했습니다.", panel);
                return;
            }

            Undo.RecordObject(knowledgeStore, "Reset Cooking Knowledge To Seed Data");
            knowledgeStore.ResetToSeedDataForDebug();
            EditorUtility.SetDirty(knowledgeStore);
            Debug.Log($"요리 지식을 Seed 데이터로 복원했습니다. {knowledgeStore.BuildDebugSummary()}", knowledgeStore);
        }

        private void ClearRewardWallet(CookingGamePanel panel)
        {
            CookingRewardWallet rewardWallet = ResolveRewardWallet(panel);
            if (rewardWallet == null)
            {
                Debug.LogWarning("초기화할 CookingRewardWallet을 찾지 못했습니다.", panel);
                return;
            }

            Undo.RecordObject(rewardWallet, "Clear Cooking Reward Wallet");
            rewardWallet.ClearForDebug();
            EditorUtility.SetDirty(rewardWallet);
            Debug.Log($"보상 재화를 초기화했습니다. {rewardWallet.BuildDebugSummary()}", rewardWallet);
        }

        private void ClearCookingDebugInfo(CookingGamePanel panel)
        {
            if (panel == null)
                return;

            if (EditorUtility.DisplayDialog(
                    "정보 초기화",
                    "CookingGamePanel에 저장된 진행 정보, 결과, 선택 후보, 요리 지식, 보상 재화, 손님 만남 기록을 모두 초기화할까요?",
                    "초기화",
                    "취소") == false)
                return;

            CookingKnowledgeStore knowledgeStore = ResolveKnowledgeStore(panel);
            if (knowledgeStore != null)
                Undo.RecordObject(knowledgeStore, "Clear Cooking Debug Info");

            CookingRewardWallet rewardWallet = ResolveRewardWallet(panel);
            if (rewardWallet != null)
                Undo.RecordObject(rewardWallet, "Clear Cooking Debug Info");

            NpcEncounterDirector encounterDirector = panel.GetComponentInChildren<NpcEncounterDirector>(true);
            if (encounterDirector == null)
                encounterDirector = Object.FindFirstObjectByType<NpcEncounterDirector>();
            if (encounterDirector != null)
                Undo.RecordObject(encounterDirector, "Clear Cooking Debug Info");

            Undo.RecordObject(panel, "Clear Cooking Debug Info");
            panel.ClearStoredInfoForDebug();
            EditorUtility.SetDirty(panel);

            if (knowledgeStore != null)
                EditorUtility.SetDirty(knowledgeStore);

            if (rewardWallet != null)
                EditorUtility.SetDirty(rewardWallet);

            if (encounterDirector != null)
                EditorUtility.SetDirty(encounterDirector);

            Debug.Log("CookingGamePanel과 손님에 저장된 정보가 모두 초기화되었습니다.", panel);
        }

        private void SelectOrCreateOverlayRoot(CookingGamePanel panel)
        {
            Canvas canvas = FindBestCanvas(panel, GetViewReferences());
            if (canvas == null)
            {
                Debug.LogWarning("보상 Overlay Root를 만들 Canvas를 찾지 못했습니다.", panel);
                return;
            }

            Transform overlayRoot = GetOrCreateOverlayRoot(canvas);
            Selection.activeGameObject = overlayRoot.gameObject;
            EditorGUIUtility.PingObject(overlayRoot.gameObject);
        }

        private void MoveRewardViewToOverlayRoot(CookingGamePanel panel)
        {
            GameObject rewardView = GetReference<GameObject>(_rewardView);
            if (rewardView == null)
            {
                Debug.LogWarning("Reward View가 연결되어 있지 않습니다.", panel);
                return;
            }

            Canvas canvas = FindBestCanvas(panel, GetViewReferences());
            if (canvas == null)
            {
                Debug.LogWarning("Reward View를 옮길 Canvas를 찾지 못했습니다.", panel);
                return;
            }

            Transform overlayRoot = GetOrCreateOverlayRoot(canvas);
            if (rewardView.transform.parent == overlayRoot)
            {
                Selection.activeGameObject = rewardView;
                EditorGUIUtility.PingObject(rewardView);
                Debug.Log("Reward View가 이미 Overlay Root 아래에 있습니다.", rewardView);
                return;
            }

            Undo.SetTransformParent(rewardView.transform, overlayRoot, "Move Reward View To Cooking Overlay Root");
            rewardView.transform.localRotation = Quaternion.identity;
            rewardView.transform.localScale = Vector3.one;

            RectTransform rect = rewardView.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-24f, -24f);
            }

            EditorUtility.SetDirty(rewardView);
            MarkSceneDirty(rewardView);
            Selection.activeGameObject = rewardView;
            EditorGUIUtility.PingObject(rewardView);
            Debug.Log("Reward View를 CookingRewardOverlayRoot 아래로 이동했습니다.", rewardView);
        }

        private CookingFlowRunner ResolveFlowRunner(CookingGamePanel panel)
        {
            CookingFlowRunner runner = GetReference<CookingFlowRunner>(_flowRunner);
            if (runner != null)
                return runner;

            return panel != null ? panel.GetComponentInChildren<CookingFlowRunner>(true) : null;
        }

        private NpcConversationRunner ResolveNpcRunner()
        {
            NpcConversationRunner runner = GetReference<NpcConversationRunner>(_npcRunner);
            return runner != null ? runner : Object.FindFirstObjectByType<NpcConversationRunner>();
        }

        private CookingKnowledgeStore ResolveKnowledgeStore(CookingGamePanel panel)
        {
            CookingKnowledgeStore store = GetReference<CookingKnowledgeStore>(_knowledgeStore);
            if (store != null)
                return store;

            return panel != null ? panel.GetComponentInChildren<CookingKnowledgeStore>(true) : null;
        }

        private CookingRewardWallet ResolveRewardWallet(CookingGamePanel panel)
        {
            CookingRewardWallet wallet = GetReference<CookingRewardWallet>(_rewardWallet);
            if (wallet != null)
                return wallet;

            return panel != null ? panel.GetComponentInChildren<CookingRewardWallet>(true) : null;
        }

        private GameObject[] GetViewReferences()
        {
            return new[]
            {
                GetReference<GameObject>(_npcConversationView),
                GetReference<GameObject>(_recipeSelectionView),
                GetReference<GameObject>(_inventoryView),
                GetReference<GameObject>(_preparationView),
                GetReference<GameObject>(_resultView)
            };
        }

        private T GetReference<T>(SerializedProperty property)
            where T : Object
        {
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static Canvas FindBestCanvas(CookingGamePanel panel, IReadOnlyList<GameObject> views)
        {
            Canvas canvas = FindCanvasFromViews(views);
            if (canvas == null && panel != null)
                canvas = panel.GetComponentInParent<Canvas>(true);
            if (canvas == null)
                canvas = Object.FindFirstObjectByType<Canvas>();

            return canvas != null && canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
        }

        private static Canvas FindCanvasFromViews(IReadOnlyList<GameObject> views)
        {
            if (views == null)
                return null;

            for (int i = 0; i < views.Count; i++)
            {
                GameObject view = views[i];
                if (view == null)
                    continue;

                Canvas canvas = view.GetComponentInParent<Canvas>(true);
                if (canvas != null)
                    return canvas;
            }

            return null;
        }

        private static Transform FindOverlayRoot(Canvas canvas)
        {
            if (canvas == null)
                return null;

            return canvas.transform.Find(OverlayRootName);
        }

        private static Transform GetOrCreateOverlayRoot(Canvas canvas)
        {
            Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            Transform existing = rootCanvas.transform.Find(OverlayRootName);
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return existing;
            }

            GameObject rootObject = new GameObject(OverlayRootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Cooking Reward Overlay Root");

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(rootCanvas.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localRotation = Quaternion.identity;
            rootRect.localScale = Vector3.one;
            rootRect.SetAsLastSibling();

            EditorUtility.SetDirty(rootCanvas);
            MarkSceneDirty(rootCanvas.gameObject);
            return rootRect;
        }

        private static void DrawReadonlyObject(string label, Object value)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(label, value, value != null ? value.GetType() : typeof(Object), true);
            }
        }

        private static void DrawViewLine(string label, GameObject view)
        {
            EditorGUILayout.LabelField(label, DescribeView(view));
        }

        private static void DrawViewLine(string label, GameObject view, string contractName, bool hasContract)
        {
            EditorGUILayout.LabelField(label, $"{DescribeView(view)} / {contractName}: {(hasContract ? "OK" : "없음")}");
        }

        private static string DescribeView(GameObject view)
        {
            if (view == null)
                return "없음";

            Canvas canvas = view.GetComponentInParent<Canvas>(true);
            string active = view.activeInHierarchy ? "active" : "inactive";
            string canvasName = canvas != null ? canvas.name : "Canvas 없음";
            return $"{view.name} / {active} / {canvasName}";
        }

        private static string DescribeObject(Object value)
        {
            return value != null ? value.name : "없음";
        }

        private static bool HasContract<T>(GameObject view)
            where T : class
        {
            return view != null
                   && (view.GetComponent<T>() != null || view.GetComponentInChildren<T>(true) != null);
        }

        private static string DescribeIngredientSource(GameObject inventoryView)
        {
            ICookingIngredientSource source = FindContract<ICookingIngredientSource>(inventoryView);
            if (source != null)
            {
                bool supportsQuantity = source is ICookingIngredientQuantitySource;
                return $"{source.SourceName} / OK / 수량 {(supportsQuantity ? "지원" : "미지원")}";
            }

            return "없음 / FlowRunner 카탈로그 fallback 사용";
        }

        private static T FindContract<T>(GameObject view)
            where T : class
        {
            if (view == null)
                return null;

            T contract = view.GetComponent<T>();
            if (contract != null)
                return contract;

            contract = view.GetComponentInChildren<T>(true);
            if (contract != null)
                return contract;

            MonoBehaviour[] parents = view.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < parents.Length; i++)
            {
                if (parents[i] is T parentContract)
                    return parentContract;
            }

            return null;
        }

        private static void AppendViewReport(StringBuilder builder, string label, GameObject view)
        {
            if (view == null)
            {
                builder.AppendLine($"{label}: 없음");
                return;
            }

            Canvas canvas = view.GetComponentInParent<Canvas>(true);
            RectTransform rect = view.transform as RectTransform;
            builder.AppendLine($"{label}: {GetHierarchyPath(view.transform)}");
            builder.AppendLine($"  activeSelf={view.activeSelf}, activeInHierarchy={view.activeInHierarchy}");
            builder.AppendLine($"  canvas={(canvas != null ? GetHierarchyPath(canvas.transform) : "없음")}");

            if (rect != null)
            {
                builder.AppendLine(
                    $"  anchorMin={rect.anchorMin}, anchorMax={rect.anchorMax}, pivot={rect.pivot}, anchoredPosition={rect.anchoredPosition}, sizeDelta={rect.sizeDelta}");
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "없음";

            Stack<string> names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static void MarkSceneDirty(GameObject targetObject)
        {
            if (targetObject != null && targetObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(targetObject.scene);
        }
    }
}
