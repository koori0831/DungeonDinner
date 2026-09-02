using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace Work.Cook.Code.Editor.PreviewLab
{
    /// <summary>
    /// Preview Lab 코드/에셋이 Player 빌드 그래프에 섞이는 것을 빌드 직전에 차단한다.
    /// </summary>
    internal sealed class CookingUiPreviewBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (TryValidateIsolation(out string message) == false)
                throw new BuildFailedException(message);
        }

        [MenuItem("Tools/Dungeon Dinner/Cooking UI Preview Lab/Validate Player Isolation")]
        private static void ValidateFromMenu()
        {
            bool valid = TryValidateIsolation(out string message);
            if (valid)
                Debug.Log($"[Cooking UI Preview] {message}");
            else
                Debug.LogError($"[Cooking UI Preview] {message}");

            EditorUtility.DisplayDialog(
                "Cooking UI Preview Player Isolation",
                message,
                "확인");
        }

        internal static bool TryValidateIsolation(out string message)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (scene == null || scene.enabled == false || string.IsNullOrWhiteSpace(scene.path))
                    continue;

                if (IsPreviewPath(scene.path))
                {
                    message = $"Preview Lab 씬이 Player Build Settings에 활성화되어 있습니다: {scene.path}";
                    return false;
                }

                string[] dependencies = AssetDatabase.GetDependencies(scene.path, true);
                for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                {
                    string dependency = NormalizePath(dependencies[dependencyIndex]);
                    if (IsPreviewPath(dependency) == false)
                        continue;

                    message = $"Player 빌드 씬이 Preview Lab 에셋을 참조합니다. scene={scene.path}, dependency={dependency}";
                    return false;
                }
            }

            UnityEditor.Compilation.Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            for (int assemblyIndex = 0; assemblyIndex < playerAssemblies.Length; assemblyIndex++)
            {
                string[] sourceFiles = playerAssemblies[assemblyIndex].sourceFiles;
                for (int sourceIndex = 0; sourceIndex < sourceFiles.Length; sourceIndex++)
                {
                    string sourcePath = NormalizePath(sourceFiles[sourceIndex]);
                    if (IsPreviewPath(sourcePath) == false)
                        continue;

                    message = $"Preview Lab 스크립트가 Player 어셈블리에 포함되어 있습니다: {sourcePath}";
                    return false;
                }
            }

            message = "검증 완료: Preview Lab 씬/에셋/스크립트가 Player 빌드 그래프에 포함되지 않습니다.";
            return true;
        }

        private static bool IsPreviewPath(string path)
        {
            string normalized = NormalizePath(path);
            string previewRoot = NormalizePath(CookingUiPreviewWindow.PreviewRootPath);
            return normalized.StartsWith(previewRoot + "/", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalized, previewRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        }
    }
}
