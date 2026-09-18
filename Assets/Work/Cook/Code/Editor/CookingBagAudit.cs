using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Info;
using Work.Cook.Code.Runtime.UI;
using Object = UnityEngine.Object;

namespace Work.Cook.Code.Editor
{
    public static class CookingBagAudit
    {
        [InitializeOnLoadMethod]
        private static void CheckRequest()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists("Temp/CookingBagAudit.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete("Temp/CookingBagAudit.request");
                Run();
            };
        }

        [MenuItem("Tools/Dungeon Dinner/Validate Bag Layout")]
        public static void Run()
        {
            // Popup behavior, real pointer dragging and all six resolutions share the current regression driver.
            PlaytestFeedbackValidation.Run();
        }
    }
}
