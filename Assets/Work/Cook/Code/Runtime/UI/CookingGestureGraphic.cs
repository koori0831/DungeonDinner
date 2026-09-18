using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime.UI
{
    public enum CookingGesture { Tap, Flip, Slice, Scrub, Stir, Pour, DragRight, Discard }
    public enum CookingFeedbackState { None, Mistake, Success }

    /// <summary>Resolution independent action drawings. Never participates in pointer raycasts.</summary>
    public sealed class CookingGestureGraphic : MaskableGraphic
    {
        [SerializeField] private CookingGesture action;
        [SerializeField] private bool destinationOnly;
        private bool _demonstrating;
        private float _started;
        private CookingFeedbackState _feedback;
        private float _feedbackUntil;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            maskable = false;
        }

        public void SetAction(CookingGesture value)
        {
            action = value;
            _demonstrating = true;
            _started = Time.unscaledTime;
            gameObject.SetActive(true);
            SetVerticesDirty();
        }

        public void CompleteDemonstration() { _demonstrating = false; SetVerticesDirty(); }
        public void ShowFeedback(CookingFeedbackState value)
        {
            _feedback = value;
            _feedbackUntil = Time.unscaledTime + 0.5f;
            SetVerticesDirty();
        }
        public void ClearFeedback() { _feedback = CookingFeedbackState.None; SetVerticesDirty(); }
        public void Hide() { _demonstrating = false; ClearFeedback(); }

        private void Update() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float unit = Mathf.Min(rect.width, rect.height);
            Vector2 center = rect.center;
            Color ink = new Color(0.42f, 0.24f, 0.11f, 0.9f);
            float thickness = Mathf.Max(2f, unit * 0.009f);
            if (destinationOnly)
            {
                DrawBin(vh, center, unit * 0.2f, thickness, ink);
                return;
            }
            if (Time.unscaledTime < _feedbackUntil && _feedback != CookingFeedbackState.None)
            {
                float offset = _feedback == CookingFeedbackState.Mistake ? Mathf.Sin(Time.unscaledTime * 65f) * 7f : 0f;
                center.x += offset;
                float r = unit * 0.075f;
                Color feedback = _feedback == CookingFeedbackState.Mistake ? new Color(0.75f, 0.21f, 0.14f) : new Color(0.28f, 0.5f, 0.3f);
                if (_feedback == CookingFeedbackState.Mistake)
                {
                    Line(vh, center + new Vector2(-r, -r), center + new Vector2(r, r), thickness * 2f, feedback);
                    Line(vh, center + new Vector2(-r, r), center + new Vector2(r, -r), thickness * 2f, feedback);
                }
                else
                {
                    Line(vh, center + new Vector2(-r, 0), center + new Vector2(-r * 0.2f, -r), thickness * 2f, feedback);
                    Line(vh, center + new Vector2(-r * 0.2f, -r), center + new Vector2(r, r), thickness * 2f, feedback);
                }
                return;
            }
            if (!_demonstrating) return;
            float t = Mathf.Repeat((Time.unscaledTime - _started) * 0.55f, 1f);
            float radius = unit * 0.34f;
            Vector2 start, end, cursor;
            switch (action)
            {
                case CookingGesture.Tap:
                    Ring(vh, center, unit * (0.095f + t * 0.045f), thickness, ink);
                    Ring(vh, center, unit * 0.065f, thickness, ink);
                    return;
                case CookingGesture.Flip:
                case CookingGesture.Stir:
                    float sweep = action == CookingGesture.Flip ? 290f : 335f;
                    Arc(vh, center, radius, 15f, sweep, thickness, ink);
                    end = center + Direction(sweep) * radius;
                    Arrow(vh, end - Direction(sweep + 90f) * 16f, end, thickness, ink);
                    cursor = center + Direction(t * sweep + 15f) * radius;
                    break;
                case CookingGesture.Slice:
                    start = center + new Vector2(-unit * 0.22f, unit * 0.28f);
                    end = center + new Vector2(-unit * 0.22f, -unit * 0.28f);
                    Arrow(vh, start, end, thickness, ink);
                    cursor = Vector2.Lerp(start, end, t);
                    break;
                case CookingGesture.Scrub:
                    start = center + new Vector2(-unit * 0.27f, 0);
                    end = center + new Vector2(unit * 0.27f, 0);
                    Arrow(vh, start, end, thickness, ink);
                    Arrow(vh, end + Vector2.down * 22f, start + Vector2.down * 22f, thickness, ink);
                    cursor = Vector2.Lerp(start, end, Mathf.PingPong(t * 2f, 1f));
                    break;
                default:
                    start = center + new Vector2(-unit * 0.27f, -unit * 0.12f);
                    end = action == CookingGesture.Discard ? center + new Vector2(unit * 0.34f, unit * 0.32f)
                        : action == CookingGesture.Pour ? center + Vector2.up * unit * 0.14f
                        : center + Vector2.right * unit * 0.28f;
                    Arrow(vh, start, end, thickness, ink);
                    cursor = Vector2.Lerp(start, end, t);
                    break;
            }
            Ring(vh, cursor, unit * 0.045f, thickness * 1.4f, new Color(1f, 0.88f, 0.51f));
        }

        private static Vector2 Direction(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
        private static void Ring(VertexHelper vh, Vector2 center, float radius, float width, Color color) => Arc(vh, center, radius, 0, 360, width, color);
        private static void Arc(VertexHelper vh, Vector2 center, float radius, float start, float end, float width, Color color)
        {
            const int segments = 48;
            for (int i = 0; i < segments; i++)
                Line(vh, center + Direction(Mathf.Lerp(start, end, (float)i / segments)) * radius,
                    center + Direction(Mathf.Lerp(start, end, (float)(i + 1) / segments)) * radius, width, color);
        }
        private static void Arrow(VertexHelper vh, Vector2 start, Vector2 end, float width, Color color)
        {
            Line(vh, start, end, width, color);
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Line(vh, end, end - direction * 14f + perpendicular * 9f, width, color);
            Line(vh, end, end - direction * 14f - perpendicular * 9f, width, color);
        }
        private static void DrawBin(VertexHelper vh, Vector2 center, float size, float width, Color color)
        {
            Vector2 a = center + new Vector2(-size, size), b = center + new Vector2(size, size);
            Vector2 c = center + new Vector2(size * 0.7f, -size), d = center + new Vector2(-size * 0.7f, -size);
            Line(vh, a, b, width, color); Line(vh, b, c, width, color);
            Line(vh, c, d, width, color); Line(vh, d, a, width, color);
            Line(vh, a + Vector2.up * 7f, b + Vector2.up * 7f, width, color);
            Line(vh, center + new Vector2(-size * 0.4f, size + 12f), center + new Vector2(size * 0.4f, size + 12f), width, color);
            for (int i = -1; i <= 1; i++) Line(vh, center + new Vector2(i * size * 0.4f, size * 0.6f), center + new Vector2(i * size * 0.3f, -size * 0.6f), width, color);
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
            int i = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero); vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero); vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
