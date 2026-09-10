using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Work.Core.EventBus;

namespace Work.Adventure.Code.UI
{

    public readonly record struct OnEnableTooltipEvent(string value) : IEvent;
    public readonly record struct OnDisableTooltipEvent() : IEvent;

    public class TooltipUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float offset = 30f;

        private static readonly Vector2 PointerOffset = new Vector2(20f, -20f);
        private const float ScreenPadding = 8f;
        private readonly Vector3[] _worldCorners = new Vector3[4];
        private Canvas _canvas;

        public void Awake()
        {
            Bus<OnEnableTooltipEvent>.Events += HandleEnableEvent;
            Bus<OnDisableTooltipEvent>.Events += HandleDisableEvent;

            SetText("");
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Bus<OnEnableTooltipEvent>.Events -= HandleEnableEvent;
            Bus<OnDisableTooltipEvent>.Events -= HandleDisableEvent;
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (Mouse.current == null || root == null || !(root.parent is RectTransform parent))
                return;

            if (_canvas == null)
                _canvas = root.GetComponentInParent<Canvas>();
            if (_canvas == null)
                return;

            Canvas canvas = _canvas.rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenPosition = Mouse.current.position.ReadValue() + PointerOffset;

            // Pointer coordinates are pixels, while anchoredPosition uses the scaled parent's UI units.
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPosition, camera, out Vector3 position))
                return;
            root.position = position;

            root.GetWorldCorners(_worldCorners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, _worldCorners[0]);
            Vector2 max = min;
            for (int i = 1; i < _worldCorners.Length; i++)
            {
                Vector2 corner = RectTransformUtility.WorldToScreenPoint(camera, _worldCorners[i]);
                min = Vector2.Min(min, corner);
                max = Vector2.Max(max, corner);
            }

            Rect bounds = canvas.pixelRect;
            Vector2 correction = new Vector2(
                GetScreenCorrection(min.x, max.x, bounds.xMin + ScreenPadding, bounds.xMax - ScreenPadding),
                GetScreenCorrection(min.y, max.y, bounds.yMin + ScreenPadding, bounds.yMax - ScreenPadding));
            if (correction != Vector2.zero
                && RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPosition + correction, camera, out position))
                root.position = position;
        }

        private static float GetScreenCorrection(float min, float max, float lower, float upper)
        {
            if (min < lower || max - min > upper - lower)
                return lower - min;
            return max > upper ? upper - max : 0f;
        }

        private void HandleEnableEvent(OnEnableTooltipEvent evt)
        {
            gameObject.SetActive(true);
            SetText(evt.value);
            Canvas.ForceUpdateCanvases();
            UpdatePosition();
        }

        private void HandleDisableEvent(OnDisableTooltipEvent evt)
        {
            SetText("");
            gameObject.SetActive(false);
        }

        public void SetText(string message)
        {
            text.text = message;
            text.ForceMeshUpdate();

            Vector2 textSize = text.GetRenderedValues(false);
            Vector2 size = root.sizeDelta;
            size.x = textSize.x + offset; // 좌우 여백
            root.sizeDelta = size;
        }
    }
}
