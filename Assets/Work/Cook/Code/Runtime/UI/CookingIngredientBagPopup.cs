using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Work.Cook.Code.Runtime.Events;
using Work.Core.EventBus;
using Work.UtillUI.Code;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingIngredientBagPopup : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel owner;
        [SerializeField] private RectTransform window;
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private CanvasGroup bodyGroup;
        [SerializeField] private Button openButton;
        [SerializeField] private Button collapseButton;
        [SerializeField] private Button closeButton;
        private Vector2 _expandedSize;
        private Vector2 _dragOffset;
        private Vector2 _openButtonHome;
        private int _pointer = int.MinValue;
        private bool _collapsed;
        private bool _closed;
        private CookingGameScreenState _screen;
        public bool IsCollapsed => _collapsed;
        public bool IsOpen => !_closed && _screen == CookingGameScreenState.Inventory;

        private void Awake()
        {
            if (windowGroup == null && window != null)
                {
                windowGroup = window.GetComponent<CanvasGroup>();
                if (windowGroup == null) windowGroup = window.gameObject.AddComponent<CanvasGroup>();
            }
            if (bodyGroup == null && window != null)
            {
                var body = window.Find("Body");
                bodyGroup = body.GetComponent<CanvasGroup>();
                if (bodyGroup == null) bodyGroup = body.gameObject.AddComponent<CanvasGroup>();
            }
            _expandedSize = new Vector2(720, 680);
            _openButtonHome = ((RectTransform)openButton.transform).anchoredPosition;
            openButton.onClick.AddListener(Open);
            collapseButton.onClick.AddListener(ToggleCollapsed);
            closeButton.onClick.AddListener(Close);
        }
        private void OnEnable()
        {
            Bus<CookingGameScreenChangedEvent>.Events += OnScreenChanged;
            if (owner != null) ApplyScreen(owner.CurrentScreen);
        }
        private void OnDisable()
        {
            Bus<CookingGameScreenChangedEvent>.Events -= OnScreenChanged;
            _pointer = int.MinValue;
        }
        private void LateUpdate() { if (IsOpen) ClampToScreen(); }
        private void OnScreenChanged(CookingGameScreenChangedEvent evt)
        {
            if (evt.Source == owner) ApplyScreen(owner.CurrentScreen);
        }
        private void ApplyScreen(CookingGameScreenState screen)
        {
            bool entered = _screen != screen;
            _screen = screen;
            openButton.gameObject.SetActive(screen == CookingGameScreenState.Inventory || screen == CookingGameScreenState.RecipeSelection);
            if (entered && screen == CookingGameScreenState.Inventory) { _closed = false; _collapsed = false; }
            Refresh();
        }
        public void Open()
        {
            if (GameUiInput.IsBlocked) return;
            if (owner != null && owner.CurrentScreen == CookingGameScreenState.RecipeSelection)
                Bus<CookingDirectIngredientSelectionOpenRequestedEvent>.Raise(new CookingDirectIngredientSelectionOpenRequestedEvent(owner));
            _closed = false; _collapsed = false; Refresh();
        }
        public void Close() { if (GameUiInput.IsBlocked) return; _closed = true; _pointer = int.MinValue; Refresh(); }
        public void ToggleCollapsed() { if (GameUiInput.IsBlocked) return; _collapsed = !_collapsed; Refresh(); }
        public void CollapseForGuide()
        {
            var canvas = window != null ? window.GetComponentInParent<Canvas>() : null;
            Rect bounds = canvas != null ? canvas.rootCanvas.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
            if (bounds.width / Mathf.Max(1, bounds.height) < 1.7f || bounds.width < 1500)
            { _collapsed = true; Refresh(); }
        }
        private void Refresh()
        {
            if (window == null || windowGroup == null || bodyGroup == null) return;
            bool visible = _screen == CookingGameScreenState.Inventory && !_closed;
            windowGroup.alpha = visible ? 1 : 0;
            windowGroup.interactable = windowGroup.blocksRaycasts = visible;
            bodyGroup.alpha = _collapsed ? 0 : 1;
            bodyGroup.interactable = bodyGroup.blocksRaycasts = !_collapsed;
            window.sizeDelta = new Vector2(_expandedSize.x, _collapsed ? 58 : _expandedSize.y);
            var label = collapseButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null) label.text = _collapsed ? "펼치기" : "접기";
            ((RectTransform)openButton.transform).anchoredPosition = _openButtonHome
                + Vector2.left * (owner != null ? owner.CookingReferenceWidth : 0f);
            ClampToScreen();
        }
        public void BeginDrag(PointerEventData evt)
        {
            if (!IsOpen || GameUiInput.IsBlocked || _pointer != int.MinValue) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.parent, evt.position, evt.pressEventCamera, out var point)) return;
            _pointer = evt.pointerId;
            _dragOffset = window.anchoredPosition - point;
        }
        public void Drag(PointerEventData evt)
        {
            if (_pointer != evt.pointerId) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.parent, evt.position, evt.pressEventCamera, out var point))
                window.anchoredPosition = point + _dragOffset;
            ClampToScreen();
        }
        public void EndDrag(PointerEventData evt) { if (_pointer == evt.pointerId) _pointer = int.MinValue; ClampToScreen(); }
        private void OnApplicationFocus(bool focused) { if (!focused) { _pointer = int.MinValue; ClampToScreen(); } }
        private void ClampToScreen()
        {
            if (window == null || !(window.parent is RectTransform parent)) return;
            Rect available = parent.rect;
            available.xMax -= owner != null ? owner.CookingReferenceWidth : 0f;
            float scale = Mathf.Min(1f, (available.width - 24f) / _expandedSize.x, (available.height - 24f) / _expandedSize.y);
            window.localScale = Vector3.one * Mathf.Max(0.4f, scale);
            Vector3[] corners = new Vector3[4]; window.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]), max = parent.InverseTransformPoint(corners[2]);
            Vector2 shift = Vector2.zero;
            if (min.x < available.xMin + 12) shift.x = available.xMin + 12 - min.x;
            else if (max.x > available.xMax - 12) shift.x = available.xMax - 12 - max.x;
            if (min.y < available.yMin + 12) shift.y = available.yMin + 12 - min.y;
            else if (max.y > available.yMax - 12) shift.y = available.yMax - 12 - max.y;
            window.anchoredPosition += shift;
        }
    }
}
