using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Editor.PreviewLab
{
    /// <summary>
    /// CI/로컬 배치 모드에서 Preview Lab의 컴파일 경계와 CookTestScene 연결을 검증한다.
    /// Unity -executeMethod로 호출하기 위해 public 진입점을 둔다.
    /// </summary>
    public static class CookingUiPreviewBatchVerification
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Run()
        {
            if (CookingUiPreviewBuildGuard.TryValidateIsolation(out string isolationMessage) == false)
                throw new InvalidOperationException(isolationMessage);

            Scene scene = EditorSceneManager.OpenScene(
                CookingUiPreviewWindow.CookTestScenePath,
                OpenSceneMode.Single);
            if (scene.IsValid() == false || scene.isLoaded == false)
                throw new InvalidOperationException("CookTestScene을 검증용으로 열지 못했습니다.");

            CookingGamePanel panel = CookingUiPreviewDriver.FindPanel();
            if (panel == null)
                throw new InvalidOperationException("CookTestScene에서 CookingGamePanel을 찾지 못했습니다.");
            if (panel.FlowRunner == null)
                throw new InvalidOperationException("CookTestScene CookingGamePanel에 FlowRunner가 연결되지 않았습니다.");
            if (panel.MiniGameView == null)
                throw new InvalidOperationException("CookTestScene CookingGamePanel에 MiniGameView가 연결되지 않았습니다.");

            CookingMiniGameRouterView router = panel.MiniGameView.GetComponentInChildren<CookingMiniGameRouterView>(true);
            if (router == null)
                throw new InvalidOperationException("CookTestScene에서 CookingMiniGameRouterView를 찾지 못했습니다.");

            FieldInfo optionField = typeof(CookingGamePanel).GetField("_pendingMiniGameOption", PrivateInstance);
            MethodInfo completionMethod = typeof(CookingMiniGameRouterView).GetMethod(
                "HandleControllerCompleted",
                PrivateInstance);
            if (optionField == null || completionMethod == null)
                throw new InvalidOperationException("강제 판정용 Editor Reflection 진입점이 런타임 구현과 맞지 않습니다.");

            Debug.Log(
                $"[Cooking UI Preview] 배치 검증 완료. {isolationMessage} " +
                $"scene={scene.path}, panel={panel.name}, router={router.name}",
                panel);
        }
    }
}
