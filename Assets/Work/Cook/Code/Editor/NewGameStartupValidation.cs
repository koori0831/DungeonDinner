using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Work.Cook.Code.Editor
{
    [InitializeOnLoad]
    public static class NewGameStartupValidation
    {
        private const string RequestPath = "Temp/NewGameStartupValidation.request";
        private static TestRunnerApi _api;
        private static readonly ResultWriter Writer = new ResultWriter();

        static NewGameStartupValidation()
        {
            EditorApplication.update += CheckRequest;
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.RegisterCallbacks(Writer);
        }

        private static void CheckRequest()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            Run();
        }

        [MenuItem("Tools/Dungeon Dinner/Validate New Game Startup")]
        public static void Run()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!SceneManager.GetSceneAt(i).isDirty) continue;
                File.WriteAllText("Temp/NewGameStartupValidation.txt", "BLOCKED: Save the modified scene before running startup validation.");
                return;
            }
            File.WriteAllText("Temp/NewGameStartupValidation.txt", "RUNNING");
            _api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                categoryNames = new[] { "NewGameStartup" }
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!File.Exists("Temp/NewGameStartupValidation.txt")) return;
                if (File.ReadAllText("Temp/NewGameStartupValidation.txt") != "RUNNING") return;
                TestRunnerApi.SaveResultToFile(result, "Temp/NewGameStartupValidation.xml");
                File.WriteAllText("Temp/NewGameStartupValidation.txt",
                    $"{result.ResultState}: passed={result.PassCount}, failed={result.FailCount}, skipped={result.SkipCount}\n{result.Message}");
            }
        }
    }
}
