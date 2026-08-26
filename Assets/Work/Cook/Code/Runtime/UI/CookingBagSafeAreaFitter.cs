using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.UI
{
    [ExecuteAlways]
    public sealed class CookingBagSafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform bagRoot;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private List<RectTransform> avoidanceRects = new List<RectTransform>();
        [SerializeField, Min(0f)] private float padding = 12f;
        [SerializeField, Range(0.1f, 1f)] private float fallbackHeightRatio = 0.6f;
        [SerializeField] private bool fitOnEnable = true;
        [SerializeField] private bool fitEveryFrame;

        private readonly Vector3[] _corners = new Vector3[4];

        private void Reset()
        {
            bagRoot = transform as RectTransform;
            targetCanvas = GetComponentInParent<Canvas>(true);
        }

        private void OnEnable()
        {
            if (fitOnEnable == true)
                Fit();
        }

        private void LateUpdate()
        {
            if (fitEveryFrame == true)
                Fit();
        }

        public void SetAvoidanceViews(params GameObject[] views)
        {
            avoidanceRects.Clear();

            if (views != null)
            {
                for (int i = 0; i < views.Length; i++)
                    AddAvoidanceView(views[i]);
            }

            Fit();
        }

        public void AddAvoidanceView(GameObject view)
        {
            if (view == null)
                return;

            RectTransform rect = view.transform as RectTransform;
            if (rect == null)
                rect = view.GetComponentInChildren<RectTransform>(true);

            if (rect != null && avoidanceRects.Contains(rect) == false)
                avoidanceRects.Add(rect);
        }

        public void Fit()
        {
            EnsureReferences();
            if (bagRoot == null || targetCanvas == null)
                return;

            if (bagRoot.GetComponent<CookingIngredientSelectionView>() != null)
                return;

            RectTransform canvasRect = targetCanvas.rootCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Rect safe = canvasRect.rect;
            safe.yMax = Mathf.Lerp(safe.yMin, safe.yMax, fallbackHeightRatio);

            Vector2 canvasCenter = canvasRect.rect.center;
            for (int i = 0; i < avoidanceRects.Count; i++)
            {
                RectTransform avoidance = avoidanceRects[i];
                if (avoidance == null || avoidance.gameObject.activeInHierarchy == false)
                    continue;

                Rect localRect = GetLocalRectInCanvas(canvasRect, avoidance);
                if (localRect.width <= 0f || localRect.height <= 0f)
                    continue;

                if (localRect.xMax <= canvasCenter.x)
                    safe.xMin = Mathf.Max(safe.xMin, localRect.xMax + padding);
                else if (localRect.xMin >= canvasCenter.x)
                    safe.xMax = Mathf.Min(safe.xMax, localRect.xMin - padding);
                else if (localRect.center.y >= canvasCenter.y)
                    safe.yMax = Mathf.Min(safe.yMax, localRect.yMin - padding);
                else
                    safe.yMin = Mathf.Max(safe.yMin, localRect.yMax + padding);
            }

            if (safe.width <= padding || safe.height <= padding)
                return;

            ApplySafeRect(canvasRect, safe);
        }

        private void EnsureReferences()
        {
            if (bagRoot == null)
                bagRoot = transform as RectTransform;

            if (targetCanvas == null)
                targetCanvas = GetComponentInParent<Canvas>(true);
        }

        private Rect GetLocalRectInCanvas(RectTransform canvasRect, RectTransform target)
        {
            target.GetWorldCorners(_corners);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < _corners.Length; i++)
            {
                Vector2 localPoint = canvasRect.InverseTransformPoint(_corners[i]);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void ApplySafeRect(RectTransform canvasRect, Rect safe)
        {
            Rect canvasLocal = canvasRect.rect;
            Vector2 anchorMin = new Vector2(
                Mathf.InverseLerp(canvasLocal.xMin, canvasLocal.xMax, safe.xMin),
                Mathf.InverseLerp(canvasLocal.yMin, canvasLocal.yMax, safe.yMin));
            Vector2 anchorMax = new Vector2(
                Mathf.InverseLerp(canvasLocal.xMin, canvasLocal.xMax, safe.xMax),
                Mathf.InverseLerp(canvasLocal.yMin, canvasLocal.yMax, safe.yMax));

            bagRoot.anchorMin = anchorMin;
            bagRoot.anchorMax = anchorMax;
            bagRoot.offsetMin = Vector2.zero;
            bagRoot.offsetMax = Vector2.zero;
            bagRoot.localRotation = Quaternion.identity;
            bagRoot.localScale = Vector3.one;
        }
    }
}
