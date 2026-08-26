using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Editor
{
    public sealed partial class CookingGamePanelEditor
    {
        private static Transform FindOverlayRoot(Canvas canvas)
        {
            if (canvas == null)
                return null;

            return canvas.transform.Find(OverlayRootName);
        }

        private static Transform GetOrCreateOverlayRoot(Canvas canvas)
        {
            Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            Transform existing = rootCanvas.transform.Find(OverlayRootName);
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return existing;
            }

            GameObject rootObject = new GameObject(OverlayRootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Cooking Reward Overlay Root");

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(rootCanvas.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localRotation = Quaternion.identity;
            rootRect.localScale = Vector3.one;
            rootRect.SetAsLastSibling();

            EditorUtility.SetDirty(rootCanvas);
            MarkSceneDirty(rootCanvas.gameObject);
            return rootRect;
        }

        private static void DrawReadonlyObject(string label, Object value)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(label, value, value != null ? value.GetType() : typeof(Object), true);
            }
        }

        private static void DrawViewLine(string label, GameObject view)
        {
            EditorGUILayout.LabelField(label, DescribeView(view));
        }

        private static void DrawViewLine(string label, GameObject view, string contractName, bool hasContract)
        {
            EditorGUILayout.LabelField(label, $"{DescribeView(view)} / {contractName}: {(hasContract ? "OK" : "없음")}");
        }

        private static string DescribeView(GameObject view)
        {
            if (view == null)
                return "없음";

            Canvas canvas = view.GetComponentInParent<Canvas>(true);
            string active = view.activeInHierarchy ? "active" : "inactive";
            string canvasName = canvas != null ? canvas.name : "Canvas 없음";
            return $"{view.name} / {active} / {canvasName}";
        }

        private static string DescribeObject(Object value)
        {
            return value != null ? value.name : "없음";
        }

        private static bool HasContract<T>(GameObject view)
            where T : class
        {
            return view != null
                   && (view.GetComponent<T>() != null || view.GetComponentInChildren<T>(true) != null);
        }

        private static string DescribeIngredientSource(GameObject inventoryView)
        {
            ICookingIngredientSource source = FindContract<ICookingIngredientSource>(inventoryView);
            if (source != null)
            {
                bool supportsQuantity = source is ICookingIngredientQuantitySource;
                return $"{source.SourceName} / OK / 수량 {(supportsQuantity ? "지원" : "미지원")}";
            }

            return "없음 / FlowRunner 카탈로그 fallback 사용";
        }

        private static T FindContract<T>(GameObject view)
            where T : class
        {
            if (view == null)
                return null;

            T contract = view.GetComponent<T>();
            if (contract != null)
                return contract;

            contract = view.GetComponentInChildren<T>(true);
            if (contract != null)
                return contract;

            MonoBehaviour[] parents = view.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < parents.Length; i++)
            {
                if (parents[i] is T parentContract)
                    return parentContract;
            }

            return null;
        }

        private static void AppendViewReport(StringBuilder builder, string label, GameObject view)
        {
            if (view == null)
            {
                builder.AppendLine($"{label}: 없음");
                return;
            }

            Canvas canvas = view.GetComponentInParent<Canvas>(true);
            RectTransform rect = view.transform as RectTransform;
            builder.AppendLine($"{label}: {GetHierarchyPath(view.transform)}");
            builder.AppendLine($"  activeSelf={view.activeSelf}, activeInHierarchy={view.activeInHierarchy}");
            builder.AppendLine($"  canvas={(canvas != null ? GetHierarchyPath(canvas.transform) : "없음")}");

            if (rect != null)
            {
                builder.AppendLine(
                    $"  anchorMin={rect.anchorMin}, anchorMax={rect.anchorMax}, pivot={rect.pivot}, anchoredPosition={rect.anchoredPosition}, sizeDelta={rect.sizeDelta}");
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "없음";

            Stack<string> names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static void MarkSceneDirty(GameObject targetObject)
        {
            if (targetObject != null && targetObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(targetObject.scene);
        }
    }
}