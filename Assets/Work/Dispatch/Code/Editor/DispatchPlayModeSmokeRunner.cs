using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Work.Adventure.Code;
using Work.Cook.Code.Runtime.UI;
using Work.Dispatch.Code.Runtime;
using Work.Dispatch.Code.UI;
using Work.TimeSystem;

namespace Work.Dispatch.Code.Editor
{
    /// <summary>
    /// CI/명령줄에서 실제 통합 씬을 잠시 실행해 파견 UI 초기화를 검증합니다.
    /// 결과는 Temp/DispatchPlayModeSmoke.txt에 기록됩니다.
    /// </summary>
    [InitializeOnLoad]
    public static class DispatchPlayModeSmokeRunner
    {
        private const string RunningKey = "DungeonDinner.DispatchPlayModeSmoke.Running";
        private const string ExitCodeKey = "DungeonDinner.DispatchPlayModeSmoke.ExitCode";
        private const string ScenePath = AdventureSceneIntegrationSetup.IntegrationScenePath;
        private static readonly List<string> RuntimeErrors = new List<string>();

        private static double _checkAt;
        private static int _exitCode;
        private static bool _checking;

        static DispatchPlayModeSmokeRunner()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            if (SessionState.GetBool(RunningKey, false))
            {
                Application.logMessageReceived -= HandleLog;
                Application.logMessageReceived += HandleLog;

                if (EditorApplication.isPlaying)
                {
                    BeginRuntimeCheck();
                }
            }
        }

        public static void Run()
        {
            SessionState.SetBool(RunningKey, true);
            RuntimeErrors.Clear();
            DeleteOldResult();

            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                FinishWithFailure(exception.ToString());
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (SessionState.GetBool(RunningKey, false) == false)
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BeginRuntimeCheck();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(RunningKey, false);
                EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, _exitCode));
            }
        }

        private static void BeginRuntimeCheck()
        {
            if (_checking)
            {
                return;
            }

            _checking = true;
            _checkAt = EditorApplication.timeSinceStartup + 1d;
            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
            EditorApplication.update -= CheckWhenReady;
            EditorApplication.update += CheckWhenReady;
        }

        private static void CheckWhenReady()
        {
            if (EditorApplication.timeSinceStartup < _checkAt)
            {
                return;
            }

            EditorApplication.update -= CheckWhenReady;

            try
            {
                GameObject root = GameObject.Find("DispatchUIRoot");
                Require(root != null, "DispatchUIRoot 씬 오브젝트가 없습니다.");

                UIDocument document = root.GetComponent<UIDocument>();
                DispatchManager manager = root.GetComponent<DispatchManager>();
                DispatchNpcQuery npcQuery = root.GetComponent<DispatchNpcQuery>();
                DispatchScreenPresenter presenter = root.GetComponent<DispatchScreenPresenter>();
                GameTimeService gameTime = root.GetComponent<GameTimeService>();
                PreparationManager preparation = UnityEngine.Object.FindFirstObjectByType<PreparationManager>();

                Require(document != null, "UIDocument가 없습니다.");
                Require(manager != null, "DispatchManager가 없습니다.");
                Require(npcQuery != null, "DispatchNpcQuery가 없습니다.");
                Require(presenter != null, "DispatchScreenPresenter가 없습니다.");
                Require(gameTime != null, "GameTimeService가 없습니다.");
                Require(preparation != null, "PreparationManager가 없습니다.");
                Require(manager.Catalog != null, "DispatchCatalog 참조가 없습니다.");
                Require(document.rootVisualElement.Q<VisualElement>("dispatch-root") != null,
                    "실행 중 dispatch-root를 생성하지 못했습니다.");

                preparation.SelectDispatch();
                Require(presenter.IsVisible, "준비 화면에서 파견 화면을 열지 못했습니다.");

                MethodInfo closeMethod = typeof(DispatchScreenPresenter).GetMethod(
                    "Close",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Require(closeMethod != null, "파견 닫기 동작을 찾지 못했습니다.");
                closeMethod.Invoke(presenter, null);
                Require(presenter.IsVisible == false, "파견 화면 닫기 후 UI가 남아 있습니다.");

                CookingGamePanel cookingPanel = UnityEngine.Object.FindFirstObjectByType<CookingGamePanel>(
                    FindObjectsInactive.Include);
                Require(cookingPanel != null, "CookingGamePanel이 없습니다.");
                Require(cookingPanel.PreparationView != null, "최신 조리 뷰 참조가 없습니다.");
                Require(
                    cookingPanel.PreparationView.GetComponent<CookingView>() != null,
                    "PreparationView가 최신 CookingView를 사용하지 않습니다.");

                cookingPanel.OpenRecipeSelection();
                Require(
                    cookingPanel.RecipeSelectionView != null
                    && cookingPanel.RecipeSelectionView.activeInHierarchy,
                    "레시피 선택 화면을 열지 못했습니다.");

                cookingPanel.OpenPreparation();
                Require(
                    cookingPanel.PreparationView.activeInHierarchy,
                    "조리 준비 화면을 열지 못했습니다.");
                RectTransform preparationRect = cookingPanel.PreparationView.transform as RectTransform;
                Require(
                    preparationRect != null
                    && preparationRect.rect.width > 0f
                    && preparationRect.rect.height > 0f,
                    "조리 준비 화면의 실제 크기가 0입니다.");

                if (RuntimeErrors.Count > 0)
                {
                    throw new InvalidOperationException(
                        "실행 중 오류 로그가 발생했습니다.\n" + string.Join("\n", RuntimeErrors));
                }

                Finish(true, "PASS: 통합 씬 파견 UI 열기/닫기 및 런타임 초기화 성공");
            }
            catch (Exception exception)
            {
                Finish(false, "FAIL: " + exception);
            }
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                && IsDispatchIntegrationLog(condition, stackTrace))
            {
                RuntimeErrors.Add($"[{type}] {condition}\n{stackTrace}");
            }
        }

        private static bool IsDispatchIntegrationLog(string condition, string stackTrace)
        {
            string text = (condition ?? string.Empty) + "\n" + (stackTrace ?? string.Empty);
            return text.Contains("Work.Dispatch", StringComparison.Ordinal)
                   || text.Contains("Work.TimeSystem", StringComparison.Ordinal)
                   || text.Contains("Work.Cook", StringComparison.Ordinal)
                   || text.Contains("Work.NPC", StringComparison.Ordinal)
                   || text.Contains("PreparationManager", StringComparison.Ordinal)
                   || text.Contains("AdventureManager", StringComparison.Ordinal)
                   || text.Contains("CookingBusinessFlowController", StringComparison.Ordinal)
                   || text.Contains("NpcEncounterDirector", StringComparison.Ordinal);
        }

        private static void Require(bool condition, string message)
        {
            if (condition == false)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Finish(bool success, string message)
        {
            _exitCode = success ? 0 : 1;
            SessionState.SetInt(ExitCodeKey, _exitCode);
            WriteResult(message);
            Application.logMessageReceived -= HandleLog;
            _checking = false;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
            else
            {
                SessionState.SetBool(RunningKey, false);
                EditorApplication.Exit(_exitCode);
            }
        }

        private static void FinishWithFailure(string message)
        {
            Finish(false, "FAIL: " + message);
        }

        private static void DeleteOldResult()
        {
            string path = GetResultPath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void WriteResult(string message)
        {
            string path = GetResultPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, message);
            UnityEngine.Debug.Log(message);
        }

        private static string GetResultPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/DispatchPlayModeSmoke.txt"));
        }
    }
}
