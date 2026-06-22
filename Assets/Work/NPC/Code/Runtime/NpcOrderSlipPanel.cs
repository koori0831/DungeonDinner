using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcOrderSlipPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Generated UI")]
        [SerializeField] private bool createGeneratedUi = true;
        [SerializeField] private bool visibleOnStart;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite labelSprite;
        [SerializeField] private Vector2 panelSize = new Vector2(320f, 360f);
        [SerializeField] private Vector2 pinnedAnchoredPosition = new Vector2(1586f, -24f);
        [SerializeField, Min(0f)] private float referencePanelSidePadding = 14f;
        [SerializeField, Min(0f)] private float referencePanelTopPadding = 24f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float headerHeight = 48f;
        [SerializeField, Min(0.01f)] private float enterDuration = 0.38f;
        [SerializeField, Min(0f)] private float enterBounceDistance = 24f;
        [SerializeField, Min(0f)] private float hiddenTopPadding = 36f;
        [SerializeField, Min(0.001f)] private float characterDelay = 0.025f;
        [SerializeField, Min(1)] private int maxEntries = 12;

        private readonly Queue<string> _queuedEntries = new Queue<string>();
        private readonly List<string> _completedEntries = new List<string>();
        private readonly StringBuilder _displayedText = new StringBuilder();
        private RectTransform _root;
        private CancellationTokenSource _animationCancellationTokenSource;
        private bool _isProcessingQueue;
        private bool _hasEntered;
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
            ResolveFont();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (createGeneratedUi && contentText == null)
                BuildGeneratedUi();

            ApplyDefaultLayout();
            SetVisible(visibleOnStart);
            if (visibleOnStart == false)
            {
                MoveToHiddenPosition();
            }

            RefreshContentText();
        }

        private void OnDestroy()
        {
            CancelAnimation();
        }

        public void ResetForConversation(string eventId = "", string npcId = "")
        {
            CancelAnimation();

            _queuedEntries.Clear();
            _completedEntries.Clear();
            _displayedText.Clear();
            _entrySequence = 0;
            _hasEntered = false;
            _isProcessingQueue = false;
            ApplyDefaultLayout();
            MoveToHiddenPosition();
            SetVisible(false);
            RefreshContentText();
        }

        public void AppendOrderClues(IEnumerable<string> clues)
        {
            if (clues == null)
                return;

            bool startedFirstEntry = false;
            foreach (string clue in clues)
            {
                string normalized = NormalizeClue(clue);
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                _entrySequence++;
                string entry = $"{_entrySequence:00}  {normalized}";
                if (_hasEntered == false && startedFirstEntry == false)
                {
                    AppendVisibleCompletedEntry(entry);
                    _hasEntered = true;
                    startedFirstEntry = true;
                    StartProcessingQueue(true);
                    continue;
                }

                _queuedEntries.Enqueue(entry);
            }

            if (_queuedEntries.Count > 0 && _isProcessingQueue == false)
            {
                StartProcessingQueue(false);
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            if (visible == true)
            {
                EnsureTopRightFallbackPosition();
                BringToFront();
            }
        }

        /// <summary>
        /// 주문 명세서 UI 배경 Sprite 설정
        /// </summary>
        /// <param name="panel">패널 배경 Sprite</param>
        /// <param name="label">라벨 배경 Sprite</param>
        public void SetVisualSprites(Sprite panel, Sprite label)
        {
            panelSprite = panel;
            labelSprite = label;
            ApplyExistingVisualSprites();
        }

        private void ApplyExistingVisualSprites()
        {
            Image background = GetComponent<Image>();
            ApplyUiSprite(background, panelSprite);
            if (background != null && panelSprite != null)
            {
                background.color = Color.white;
            }

            Transform header = transform.Find("Header");
            Image headerImage = header != null ? header.GetComponent<Image>() : null;
            if (headerImage != null)
            {
                headerImage.enabled = false;
            }
        }

        /// <summary>
        /// 주문 명세서를 UI 최상단으로 이동
        /// </summary>
        public void BringToFront()
        {
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// 기준 패널의 오른쪽에 주문 명세서 위치 고정
        /// </summary>
        /// <param name="referencePanel">위치 기준 패널</param>
        public void PinToReferencePanel(RectTransform referencePanel)
        {
            if (_root == null)
            {
                return;
            }

            RectTransform parentRect = _root.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            ApplyDefaultLayout();
            pinnedAnchoredPosition = GetTopRightPosition(parentRect);

            ApplyPinnedPositionForCurrentVisibility();
        }

        private void ApplyPinnedPositionForCurrentVisibility()
        {
            if (_hasEntered == true && canvasGroup != null && canvasGroup.alpha > 0f)
            {
                _root.anchoredPosition = pinnedAnchoredPosition;
                return;
            }

            MoveToHiddenPosition();
        }

        private Vector2 GetTopRightPosition(RectTransform parentRect)
        {
            if (parentRect == null)
            {
                return pinnedAnchoredPosition;
            }

            float targetX = Mathf.Max(
                referencePanelSidePadding,
                parentRect.rect.width - panelSize.x - referencePanelSidePadding);
            return new Vector2(targetX, -referencePanelTopPadding);
        }

        private void EnsureTopRightFallbackPosition()
        {
            RectTransform parentRect = _root != null ? _root.parent as RectTransform : null;
            if (parentRect == null)
            {
                return;
            }

            pinnedAnchoredPosition = GetTopRightPosition(parentRect);
        }

        private void StartProcessingQueue(bool playEntryAnimation)
        {
            CancelAnimation();
            _animationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ProcessQueuedEntriesAsync(playEntryAnimation, _animationCancellationTokenSource).Forget();
        }

        private async UniTask ProcessQueuedEntriesAsync(
            bool playEntryAnimation,
            CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            _isProcessingQueue = true;
            try
            {
                if (playEntryAnimation == true)
                {
                    await PlayEntryAnimationAsync(cancellationToken);
                }
                else
                {
                    SetVisible(true);
                    ApplyDefaultLayout();
                }

                while (_queuedEntries.Count > 0)
                {
                    await TypeQueuedEntryAsync(_queuedEntries.Dequeue(), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 새 손님 시작 또는 패널 초기화로 연출이 취소되는 정상 흐름
            }
            finally
            {
                _isProcessingQueue = false;
                if (_animationCancellationTokenSource == cancellationTokenSource)
                {
                    _animationCancellationTokenSource.Dispose();
                    _animationCancellationTokenSource = null;
                }
            }
        }

        private async UniTask PlayEntryAnimationAsync(CancellationToken cancellationToken)
        {
            EnsureTopRightFallbackPosition();
            Vector2 target = pinnedAnchoredPosition;
            MoveToHiddenPosition();
            Vector2 start = _root.anchoredPosition;
            SetVisible(true);
            BringToFront();
            await AnimateAnchoredPositionAsync(start, target, enterDuration, cancellationToken);
            _root.anchoredPosition = target;
        }

        private async UniTask AnimateAnchoredPositionAsync(
            Vector2 from,
            Vector2 to,
            float duration,
            CancellationToken cancellationToken)
        {
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float eased = EaseOutBounce(t);
                _root.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private static float EaseOutBounce(float t)
        {
            const float BOUNCE_SCALE = 7.5625f;
            const float BOUNCE_DIVISOR = 2.75f;

            if (t < 1f / BOUNCE_DIVISOR)
            {
                return BOUNCE_SCALE * t * t;
            }

            if (t < 2f / BOUNCE_DIVISOR)
            {
                float shifted = t - 1.5f / BOUNCE_DIVISOR;
                return BOUNCE_SCALE * shifted * shifted + 0.75f;
            }

            if (t < 2.5f / BOUNCE_DIVISOR)
            {
                float shifted = t - 2.25f / BOUNCE_DIVISOR;
                return BOUNCE_SCALE * shifted * shifted + 0.9375f;
            }

            float finalShifted = t - 2.625f / BOUNCE_DIVISOR;
            return BOUNCE_SCALE * finalShifted * finalShifted + 0.984375f;
        }

        private async UniTask TypeQueuedEntryAsync(string entry, CancellationToken cancellationToken)
        {
            if (_displayedText.Length > 0)
            {
                _displayedText.AppendLine();
            }

            int delayMilliseconds = Mathf.Max(1, Mathf.RoundToInt(characterDelay * 1000f));
            for (int i = 0; i < entry.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _displayedText.Append(entry[i]);
                RefreshContentText();
                await UniTask.Delay(delayMilliseconds, cancellationToken: cancellationToken);
            }

            AppendCompletedEntry(entry);
        }

        private void AppendCompletedEntry(string entry)
        {
            _completedEntries.Add(entry);
            TrimCompletedEntries();
        }

        private void AppendVisibleCompletedEntry(string entry)
        {
            if (_displayedText.Length > 0)
            {
                _displayedText.AppendLine();
            }

            _displayedText.Append(entry);
            AppendCompletedEntry(entry);
            RefreshContentText();
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

        private void CancelAnimation()
        {
            if (_animationCancellationTokenSource == null)
            {
                return;
            }

            _animationCancellationTokenSource.Cancel();
            _animationCancellationTokenSource.Dispose();
            _animationCancellationTokenSource = null;
        }

        private void ApplyDefaultLayout()
        {
            if (_root == null)
                return;

            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = panelSize;
            EnsureTopRightFallbackPosition();
            _root.anchoredPosition = pinnedAnchoredPosition;
        }

        private void MoveToHiddenPosition()
        {
            if (_root == null)
            {
                return;
            }

            float hiddenY = pinnedAnchoredPosition.y + panelSize.y + hiddenTopPadding + enterBounceDistance;
            _root.anchoredPosition = new Vector2(pinnedAnchoredPosition.x, hiddenY);
        }

        private void BuildGeneratedUi()
        {
            _root = transform as RectTransform;
            if (_root == null)
                return;

            Image background = GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();

            ApplyUiSprite(background, panelSprite);
            background.color = panelSprite != null ? Color.white : new Color(0.96f, 0.92f, 0.78f, 0.96f);
            background.raycastTarget = true;

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            GameObject headerObject = new GameObject("Header", typeof(RectTransform));
            headerObject.transform.SetParent(transform, false);
            RectTransform header = headerObject.GetComponent<RectTransform>();
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, headerHeight);
            header.anchoredPosition = Vector2.zero;

            titleText = CreateText(header, "Title", "주문 명세서", 20f, TextAlignmentOptions.Center);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText.color = Color.black;

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(RectMask2D));
            bodyObject.transform.SetParent(transform, false);
            RectTransform body = bodyObject.GetComponent<RectTransform>();
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(18f, 18f);
            body.offsetMax = new Vector2(-18f, -headerHeight - 16f);

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

        private static void ApplyUiSprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
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
