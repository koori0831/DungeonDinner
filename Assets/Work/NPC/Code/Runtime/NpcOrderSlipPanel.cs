using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcOrderSlipPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Generated UI")]
        [SerializeField] private bool createGeneratedUi = true;
        [SerializeField] private bool visibleOnStart;
        [SerializeField] private bool visibleOnConversationStart = true;
        [SerializeField] private Vector2 panelSize = new Vector2(320f, 360f);
        [SerializeField] private Vector2 defaultAnchoredPosition = new Vector2(360f, 0f);

        [Header("Motion")]
        [SerializeField, Min(0f)] private float dragTopHeight = 48f;
        [SerializeField, Min(0f)] private float horizontalOverhang = 80f;
        [SerializeField, Min(0f)] private float verticalOverhang = 80f;
        [SerializeField, Min(0.001f)] private float characterDelay = 0.025f;
        [SerializeField, Min(1)] private int maxEntries = 12;

        private readonly Queue<string> _queuedEntries = new Queue<string>();
        private readonly List<string> _completedEntries = new List<string>();
        private readonly StringBuilder _displayedText = new StringBuilder();
        private RectTransform _root;
        private Canvas _canvas;
        private Coroutine _typingRoutine;
        private bool _isDragging;
        private int _entrySequence;

        public static NpcOrderSlipPanel GetOrCreateGeneratedPanel()
        {
            NpcOrderSlipPanel existing = FindFirstObjectByType<NpcOrderSlipPanel>();
            if (existing != null)
                return existing;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "NpcOrderSlipCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            GameObject panelObject = new GameObject("NpcOrderSlipPanel", typeof(RectTransform));
            panelObject.transform.SetParent(canvas.transform, false);
            return panelObject.AddComponent<NpcOrderSlipPanel>();
        }

        private void Awake()
        {
            _root = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            ResolveFont();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (createGeneratedUi && contentText == null)
                BuildGeneratedUi();

            ApplyDefaultLayout();
            SetVisible(visibleOnStart);
            RefreshContentText();
        }

        public void ResetForConversation(string eventId = "", string npcId = "")
        {
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
                _typingRoutine = null;
            }

            _queuedEntries.Clear();
            _completedEntries.Clear();
            _displayedText.Clear();
            _entrySequence = 0;
            RefreshContentText();

            if (visibleOnConversationStart)
                SetVisible(true);
        }

        public void AppendOrderClues(IEnumerable<string> clues)
        {
            if (clues == null)
                return;

            foreach (string clue in clues)
            {
                string normalized = NormalizeClue(clue);
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                _entrySequence++;
                _queuedEntries.Enqueue($"{_entrySequence:00}  {normalized}");
            }

            if (_queuedEntries.Count > 0 && _typingRoutine == null)
                _typingRoutine = StartCoroutine(TypeQueuedEntriesRoutine());
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = IsPointerInDragArea(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragging == false || _root == null)
                return;

            float scaleFactor = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Vector2 position = _root.anchoredPosition;
            position.x += eventData.delta.x / scaleFactor;
            position.y += eventData.delta.y / scaleFactor;
            position.x = ClampHorizontalPosition(position.x);
            position.y = ClampVerticalPosition(position.y);
            _root.anchoredPosition = position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        private IEnumerator TypeQueuedEntriesRoutine()
        {
            while (_queuedEntries.Count > 0)
            {
                string entry = _queuedEntries.Dequeue();
                if (_displayedText.Length > 0)
                    _displayedText.AppendLine();

                for (int i = 0; i < entry.Length; i++)
                {
                    _displayedText.Append(entry[i]);
                    RefreshContentText();
                    yield return new WaitForSeconds(characterDelay);
                }

                _completedEntries.Add(entry);
                TrimCompletedEntries();
            }

            _typingRoutine = null;
        }

        private void TrimCompletedEntries()
        {
            if (_completedEntries.Count <= maxEntries)
                return;

            while (_completedEntries.Count > maxEntries)
                _completedEntries.RemoveAt(0);

            _displayedText.Clear();
            for (int i = 0; i < _completedEntries.Count; i++)
            {
                if (i > 0)
                    _displayedText.AppendLine();

                _displayedText.Append(_completedEntries[i]);
            }

            RefreshContentText();
        }

        private void RefreshContentText()
        {
            if (contentText == null)
                return;

            contentText.text = _displayedText.Length > 0 ? _displayedText.ToString() : "...";
        }

        private bool IsPointerInDragArea(PointerEventData eventData)
        {
            if (_root == null || eventData == null)
                return false;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _root,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint) == false)
            {
                return false;
            }

            return localPoint.x >= 0f
                   && localPoint.x <= _root.rect.width
                   && localPoint.y <= 0f
                   && localPoint.y >= -_root.rect.height;
        }

        private float ClampHorizontalPosition(float x)
        {
            if (_root == null || _root.parent is RectTransform parent == false)
                return x;

            float minX = -horizontalOverhang;
            float maxX = Mathf.Max(0f, parent.rect.width - _root.rect.width) + horizontalOverhang;
            return Mathf.Clamp(x, minX, maxX);
        }

        private float ClampVerticalPosition(float y)
        {
            if (_root == null || _root.parent is RectTransform parent == false)
                return y;

            float minY = -Mathf.Max(0f, parent.rect.height - _root.rect.height) - verticalOverhang;
            float maxY = verticalOverhang;
            return Mathf.Clamp(y, minY, maxY);
        }

        private void ApplyDefaultLayout()
        {
            if (_root == null)
                return;

            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = panelSize;
            _root.anchoredPosition = new Vector2(
                ClampHorizontalPosition(defaultAnchoredPosition.x),
                defaultAnchoredPosition.y);
        }

        private void BuildGeneratedUi()
        {
            _root = transform as RectTransform;
            if (_root == null)
                return;

            Image background = GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();

            background.color = new Color(0.96f, 0.92f, 0.78f, 0.96f);
            background.raycastTarget = true;

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            GameObject headerObject = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerObject.transform.SetParent(transform, false);
            RectTransform header = headerObject.GetComponent<RectTransform>();
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, dragTopHeight);
            header.anchoredPosition = Vector2.zero;
            headerObject.GetComponent<Image>().color = new Color(0.22f, 0.19f, 0.16f, 0.95f);

            titleText = CreateText(header, "Title", "주문 명세서", 20f, TextAlignmentOptions.MidlineLeft);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(18f, 0f);
            titleRect.offsetMax = new Vector2(-18f, 0f);
            titleText.color = new Color(1f, 0.94f, 0.78f, 1f);

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(RectMask2D));
            bodyObject.transform.SetParent(transform, false);
            RectTransform body = bodyObject.GetComponent<RectTransform>();
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(18f, 18f);
            body.offsetMax = new Vector2(-18f, -dragTopHeight - 16f);

            contentText = CreateText(body, "Content", string.Empty, 17f, TextAlignmentOptions.TopLeft);
            RectTransform content = contentText.rectTransform;
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            contentText.color = new Color(0.12f, 0.09f, 0.06f, 1f);
            contentText.textWrappingMode = TextWrappingModes.Normal;
            contentText.overflowMode = TextOverflowModes.Overflow;
        }

        private TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.richText = true;

            if (fontAsset != null)
                text.font = fontAsset;

            return text;
        }

        private void ResolveFont()
        {
            if (fontAsset != null)
                return;

#if UNITY_EDITOR
            fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/MangoDdobak-B(otf) SDF.asset");
#endif
            if (fontAsset == null)
                fontAsset = TMP_Settings.defaultFontAsset;
        }

        private static string NormalizeClue(string clue)
        {
            return string.IsNullOrWhiteSpace(clue)
                ? string.Empty
                : clue.Trim().Replace("\r", " ").Replace("\n", " ");
        }
    }
}
