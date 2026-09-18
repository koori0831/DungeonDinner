using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Editor.PreviewLab
{
    [InitializeOnLoad]
    internal static class CookingUiPreviewPlayModeCoordinator
    {
        private const string PendingScenarioPathKey = "DungeonDinner.CookingUiPreview.PendingScenarioPath";
        private const string PendingApplyKey = "DungeonDinner.CookingUiPreview.PendingApply";
        private static int _remainingApplyAttempts;

        static CookingUiPreviewPlayModeCoordinator()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        public static bool StartCookTestPreview(CookingUiPreviewScenario scenario)
        {
            if (scenario == null)
                return false;

            string scenarioPath = AssetDatabase.GetAssetPath(scenario);
            if (string.IsNullOrWhiteSpace(scenarioPath))
            {
                EditorUtility.DisplayDialog(
                    "Cooking UI Preview",
                    "Play Mode 자동 적용에는 저장된 Preview Scenario 에셋이 필요합니다.",
                    "확인");
                return false;
            }

            if (Application.isPlaying)
                return CookingUiPreviewDriver.Apply(CookingUiPreviewDriver.FindPanel(), scenario);

            if (SceneManager.GetActiveScene().path != CookingUiPreviewWindow.CookTestScenePath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                    return false;

                EditorSceneManager.OpenScene(CookingUiPreviewWindow.CookTestScenePath, OpenSceneMode.Single);
            }

            SessionState.SetString(PendingScenarioPathKey, scenarioPath);
            SessionState.SetBool(PendingApplyKey, true);
            EditorApplication.isPlaying = true;
            return true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || SessionState.GetBool(PendingApplyKey, false) == false)
                return;

            _remainingApplyAttempts = 240;
            EditorApplication.update -= TryApplyPendingScenario;
            EditorApplication.update += TryApplyPendingScenario;
        }

        private static void TryApplyPendingScenario()
        {
            if (Application.isPlaying == false)
            {
                StopWaiting();
                return;
            }

            CookingGamePanel panel = CookingUiPreviewDriver.FindPanel();
            if (panel == null)
            {
                if (--_remainingApplyAttempts <= 0)
                {
                    Debug.LogWarning("[Cooking UI Preview] Play Mode에서 CookingGamePanel을 찾지 못했습니다.");
                    StopWaiting();
                }
                return;
            }

            string scenarioPath = SessionState.GetString(PendingScenarioPathKey, string.Empty);
            CookingUiPreviewScenario scenario = AssetDatabase.LoadAssetAtPath<CookingUiPreviewScenario>(scenarioPath);
            if (scenario == null)
            {
                Debug.LogWarning($"[Cooking UI Preview] Pending Scenario를 불러오지 못했습니다: {scenarioPath}");
                StopWaiting();
                return;
            }

            CookingUiPreviewDriver.Apply(panel, scenario);
            StopWaiting();
            CookingUiPreviewWindow.RepaintOpenWindows();
        }

        private static void StopWaiting()
        {
            EditorApplication.update -= TryApplyPendingScenario;
            SessionState.SetBool(PendingApplyKey, false);
            _remainingApplyAttempts = 0;
        }
    }
}
