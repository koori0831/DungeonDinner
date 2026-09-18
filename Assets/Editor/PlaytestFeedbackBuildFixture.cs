using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.Cook.Code.Runtime.UI;

// Validation assets are referenced only by the opt-in Development player.
public sealed class PlaytestFeedbackBuildFixture : IProcessSceneWithReport
{
    public int callbackOrder => 900;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || (report.summary.options & BuildOptions.Development) == 0 || scene.name != "TitleScene") return;
        var root = new GameObject("FeedbackValidationFixture");
        SceneManager.MoveGameObjectToScene(root, scene);
        var probe = root.AddComponent<PlaytestFeedbackProbe>();
        var serialized = new SerializedObject(probe);
        serialized.FindProperty("standaloneOverlayPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Work/Cook/Prefabs/UI/CookingMiniGameOverlayRoot.prefab");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(false);
    }
}
