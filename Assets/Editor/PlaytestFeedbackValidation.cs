using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Work.Cook.Code.Runtime.UI;

[InitializeOnLoad]
public static class PlaytestFeedbackValidation
{
    private const string Request = "DungeonDinner.FeedbackValidation";
    static PlaytestFeedbackValidation() => EditorApplication.playModeStateChanged += OnPlayMode;

    [MenuItem("Tools/Dungeon Dinner/Validate Feedback With Real Input")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Work/Title/Scene/TitleScene.unity");
        SessionState.SetBool(Request, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Request, false)) return;
        SessionState.SetBool(Request, false);
        PlaytestFeedbackProbe.EditorResolution = SetGameViewResolution;
        PlaytestFeedbackProbe.EditorCompleted = passed =>
        {
            EditorApplication.ExitPlaymode();
            if (Application.isBatchMode)
                EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        };
        var probe = new GameObject("PlaytestFeedbackProbe").AddComponent<PlaytestFeedbackProbe>();
        var data = new SerializedObject(probe);
        data.FindProperty("standaloneOverlayPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Work/Cook/Prefabs/UI/CookingMiniGameOverlayRoot.prefab");
        data.ApplyModifiedPropertiesWithoutUndo();
        probe.BeginValidation();
    }

    private static void SetGameViewResolution(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        object sizes = singleton.GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kindType = assembly.GetType("UnityEditor.GameViewSizeType");
        int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        int custom = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        int selected = -1;
        for (int i = 0; i < builtIn + custom; i++)
        {
            var item = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            if ((int)sizeType.GetProperty("width").GetValue(item) == width && (int)sizeType.GetProperty("height").GetValue(item) == height)
            { selected = i; break; }
        }
        if (selected < 0)
        {
            var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(kindType, "FixedResolution"), (object)width, height, "Feedback " + width + "x" + height });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            selected = builtIn + custom;
        }
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(gameView, selected);
        gameView.Repaint();
    }



    public static void ApplyAndBuild()
    {
        PlaytestFeedbackInstaller.Apply();
        BuildWindows();
    }

    [MenuItem("Tools/Dungeon Dinner/Build Feedback Windows Player")]
    public static void BuildWindows()
    {
        Directory.CreateDirectory("Builds/PlaytestFeedback");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/PlaytestFeedback/DungeonDinner.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        File.WriteAllText("Builds/PlaytestFeedback/build-report.txt", report.summary.result + "\nerrors=" + report.summary.totalErrors + "\nsize=" + report.summary.totalSize);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed");
    }
}
