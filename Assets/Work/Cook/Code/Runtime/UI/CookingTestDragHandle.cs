using UnityEngine;
using UnityEngine.EventSystems;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingTestDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform _target;
        private Canvas _canvas;
        private Vector2 _dragOffset;

        public void Initialize(RectTransform target, Canvas canvas)
        {
            _target = target;
            _canvas = canvas;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_target == null)
                return;

            if (TryGetParentPoint(eventData, out Vector2 parentPoint))
                _dragOffset = _target.anchoredPosition - parentPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_target == null)
                return;

            if (TryGetParentPoint(eventData, out Vector2 parentPoint))
                _target.anchoredPosition = parentPoint + _dragOffset;
        }

        private bool TryGetParentPoint(PointerEventData eventData, out Vector2 parentPoint)
        {
            parentPoint = Vector2.zero;
            if (_target == null || _target.parent == null)
                return false;

            Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _target.parent as RectTransform,
                eventData.position,
                camera,
                out parentPoint);
        }
    }
}
