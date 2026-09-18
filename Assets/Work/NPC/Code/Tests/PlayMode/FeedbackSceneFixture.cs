using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonDinner.Npc.PlayModeTests
{
    internal static class FeedbackSceneFixture
    {
        public static IEnumerator Load(string path)
        {
            void Configure(Scene scene, LoadSceneMode mode)
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour != null && behaviour.GetType().FullName == "Work.Adventure.Code.PreparationManager")
                            behaviour.GetType().GetField("startWithAdventure", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(behaviour, false);
            }
            SceneManager.sceneLoaded += Configure;
            try
            {
#if UNITY_EDITOR
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
                yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
#endif
            }
            finally { SceneManager.sceneLoaded -= Configure; }
        }
    }
}
