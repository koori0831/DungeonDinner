using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Work.Cook.Code.Editor
{
    [InitializeOnLoad]
    public static class CookingBusinessFlowValidation
    {
        private const string RequestPath = "Temp/CookingBusinessFlowValidation.request";
        private const string ResultPath = "Temp/CookingBusinessFlowValidation.txt";
        private static readonly TestRunnerApi Api;

        static CookingBusinessFlowValidation()
        {
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.RegisterCallbacks(new ResultWriter());
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            Run();
        }

        [MenuItem("Tools/Dungeon Dinner/Validate Business Resume")]
        public static void Run()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!SceneManager.GetSceneAt(i).isDirty) continue;
                File.WriteAllText(ResultPath, "BLOCKED: Save the modified scene before running business validation.");
                return;
            }

            File.WriteAllText(ResultPath, "RUNNING");
            Api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                categoryNames = new[] { "CookingBusinessFlow" }
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (!File.Exists(ResultPath) || File.ReadAllText(ResultPath) != "RUNNING") return;
                TestRunnerApi.SaveResultToFile(result, "Temp/CookingBusinessFlowValidation.xml");
                File.WriteAllText(ResultPath,
                    $"{result.ResultState}: passed={result.PassCount}, failed={result.FailCount}, skipped={result.SkipCount}\n{result.Message}");
            }
        }
    }
}
