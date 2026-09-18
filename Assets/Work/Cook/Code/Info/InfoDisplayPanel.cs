using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [Serializable]
    public class InfoDisplayPanel : MonoBehaviour, IDisplayInfo
    {
        [SerializeField] protected ViewHaveInfoEnum viewInfo;
        [SerializeField] protected Image iconImage;
        [SerializeField] protected TextMeshProUGUI nameField, descriptionField;
        [SerializeField] protected Button backBtn;
        [SerializeField] protected Button previousBtn, nextBtn;
        [SerializeField] private CanvasGroup transitionCanvasGroup;
        [SerializeField, Min(0f)] private float pageTransitionDuration = 0.12f;
        [SerializeField, Min(0f)] private float pageTransitionSlideDistance = 16f;

        public ViewHaveInfoEnum ViewInfo => viewInfo;

        private Action _previousAction;
        private Action _nextAction;
        private CancellationTokenSource _transitionCancellationTokenSource;
        private RectTransform _transitionRoot;
        private Vector2 _transitionBasePosition;
        private int _transitionDirection;
        private ScrollRect _guideScroll;
        protected virtual bool UseFieldGuideLayout => true;

        public virtual void InitializeDisplay(Action backAction)
        {
            if (UseFieldGuideLayout)
                EnsureFieldGuideLayout();
            ClearDisplayText();

            if (backBtn == null)
            {
                Debug.LogWarning("InfoDisplayPanel needs a back button before it can bind back navigation.", this);
            }
            else
                backBtn.onClick.AddListener(() => backAction?.Invoke());

            BindNavigationButtons();
        }

        public void SetSiblingNavigation(Action previousAction, Action nextAction, bool hasPrevious, bool hasNext)
        {
            _previousAction = previousAction;
            _nextAction = nextAction;

            BindNavigationButtons();

            SetButtonVisible(previousBtn, hasPrevious);
            SetButtonVisible(nextBtn, hasNext);
        }

        public virtual void Enable(InfoDictionaryEntryData displayInfo)
        {
            gameObject.SetActive(true);
            if (displayInfo == null)
                return;

            EnsureTransitionReferences();
            int direction = _transitionDirection;
            _transitionDirection = 0;

            if (iconImage != null)
            {
                iconImage.sprite = displayInfo.Icon;
                iconImage.enabled = displayInfo.Icon != null;
                iconImage.preserveAspect = true;
            }

            if (nameField != null)
                nameField.text = displayInfo.DisplayName;

            if (descriptionField != null)
                descriptionField.text = displayInfo.Description;

            if (_guideScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_guideScroll.content);
                _guideScroll.StopMovement();
                _guideScroll.verticalNormalizedPosition = 1f;
                RefreshReadingScrollbar(_guideScroll);
            }

            PlayPageTransition(direction);
        }

        private void EnsureFieldGuideLayout()
        {
            if (_guideScroll != null || descriptionField == null || nameField == null)
                return;
            var ink = new Color(0.23f, 0.18f, 0.14f);
            var background = GetComponent<Image>();
            if (background != null)
            {
                background.sprite = null;
                background.color = new Color(0.97f, 0.94f, 0.86f);
            }
            Place(nameField.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(118, -118), new Vector2(-20, -22));
            nameField.fontSize = 28;
            nameField.enableAutoSizing = true;
            nameField.fontSizeMin = 20;
            nameField.fontSizeMax = 28;
            nameField.textWrappingMode = TextWrappingModes.Normal;
            nameField.fontStyle = FontStyles.Bold;
            nameField.color = ink;
            nameField.alignment = TextAlignmentOptions.MidlineLeft;
            nameField.raycastTarget = false;
            if (iconImage != null)
            {
                Place(iconImage.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(20, -114), new Vector2(108, -26));
                iconImage.raycastTarget = false;
            }
            var scrollObject = new GameObject("GuideReadingArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(transform, false);
            Place((RectTransform)scrollObject.transform, Vector2.zero, Vector2.one,
                new Vector2(18, 66), new Vector2(-18, -138));
            scrollObject.GetComponent<Image>().color = new Color(1, 1, 1, 0.35f);
            _guideScroll = scrollObject.GetComponent<ScrollRect>();
            _guideScroll.horizontal = false;
            _guideScroll.movementType = ScrollRect.MovementType.Clamped;
            _guideScroll.scrollSensitivity = 28;
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            var viewport = (RectTransform)viewportObject.transform;
            Place(viewport, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8));
            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            bodyObject.transform.SetParent(viewport, false);
            var body = (RectTransform)bodyObject.transform;
            body.anchorMin = new Vector2(0, 1);
            body.anchorMax = Vector2.one;
            body.pivot = new Vector2(0.5f, 1);
            body.sizeDelta = Vector2.zero;
            body.anchoredPosition = Vector2.zero;
            var layout = bodyObject.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 4, 8, 16);
            bodyObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            descriptionField.transform.SetParent(body, false);
            descriptionField.fontSize = 18;
            descriptionField.enableAutoSizing = false;
            descriptionField.color = ink;
            descriptionField.alignment = TextAlignmentOptions.TopLeft;
            descriptionField.textWrappingMode = TextWrappingModes.Normal;
            descriptionField.overflowMode = TextOverflowModes.Overflow;
            descriptionField.lineSpacing = 8;
            descriptionField.raycastTarget = false;
            _guideScroll.viewport = viewport;
            _guideScroll.content = body;
            AddReadingScrollbar(_guideScroll, 10);
            if (backBtn != null)
            {
                Place((RectTransform)backBtn.transform, Vector2.zero, Vector2.zero,
                    new Vector2(20, 18), new Vector2(58, 52));
                if (backBtn.targetGraphic != null)
                    backBtn.targetGraphic.color = new Color(0.45f, 0.31f, 0.18f);
            }
            previousBtn = previousBtn != null ? previousBtn : CreateGuideNavigation("PreviousEntry", "이전", 0.36f);
            nextBtn = nextBtn != null ? nextBtn : CreateGuideNavigation("NextEntry", "다음", 0.69f);
        }

        public static void AddReadingScrollbar(ScrollRect scroll, float topInset)
        {
            if (scroll.verticalScrollbar != null)
                return;
            var track = new GameObject("ReadingProgress", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            track.transform.SetParent(scroll.transform, false);
            Place((RectTransform)track.transform, new Vector2(1, 0), Vector2.one,
                new Vector2(-7, 10), new Vector2(-2, -topInset));
            track.GetComponent<Image>().color = new Color(0.85f, 0.80f, 0.69f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(track.transform, false);
            var handleRect = (RectTransform)handle.transform;
            Place(handleRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handle.GetComponent<Image>().color = new Color(0.53f, 0.39f, 0.24f);
            var scrollbar = track.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        internal static void RefreshReadingScrollbar(ScrollRect scroll)
        {
            if (scroll.verticalScrollbar == null || scroll.content == null || scroll.viewport == null)
                return;
            float height = scroll.content.rect.height;
            scroll.verticalScrollbar.gameObject.SetActive(height > scroll.viewport.rect.height + 1);
            scroll.verticalScrollbar.size = height > 0 ? Mathf.Clamp01(scroll.viewport.rect.height / height) : 1;
            scroll.verticalScrollbar.SetValueWithoutNotify(scroll.verticalNormalizedPosition);
        }

        private Button CreateGuideNavigation(string objectName, string label, float anchor)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            Place((RectTransform)go.transform, new Vector2(anchor, 0), new Vector2(anchor + 0.26f, 0),
                new Vector2(0, 18), new Vector2(0, 52));
            go.GetComponent<Image>().color = new Color(0.84f, 0.77f, 0.62f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.font = nameField.font;
            text.fontSize = 16;
            text.color = new Color(0.23f, 0.18f, 0.14f);
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            text.raycastTarget = false;
            return button;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = insetMin;
            rect.offsetMax = insetMax;
        }

        public virtual void Disable()
        {
            CancelPageTransition();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CancelPageTransition();
        }

        private void ClearDisplayText()
        {
            if (nameField != null)
                nameField.text = string.Empty;

            if (descriptionField != null)
                descriptionField.text = string.Empty;
        }

        private void BindNavigationButtons()
        {
            if (previousBtn != null)
            {
                previousBtn.onClick.RemoveListener(InvokePrevious);
                previousBtn.onClick.AddListener(InvokePrevious);
            }

            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveListener(InvokeNext);
                nextBtn.onClick.AddListener(InvokeNext);
            }
        }

        private void InvokePrevious()
        {
            _transitionDirection = -1;
            _previousAction?.Invoke();
        }

        private void InvokeNext()
        {
            _transitionDirection = 1;
            _nextAction?.Invoke();
        }

        private void EnsureTransitionReferences()
        {
            if (transitionCanvasGroup == null)
                transitionCanvasGroup = GetComponent<CanvasGroup>();

            if (_transitionRoot == null)
            {
                if (descriptionField != null)
                    _transitionRoot = descriptionField.transform as RectTransform;
                else if (nameField != null)
                    _transitionRoot = nameField.transform as RectTransform;
                else
                    _transitionRoot = transform as RectTransform;
            }

            if (_transitionRoot != null)
                _transitionBasePosition = _transitionRoot.anchoredPosition;
        }

        private void PlayPageTransition(int direction)
        {
            if (transitionCanvasGroup == null || _transitionRoot == null || pageTransitionDuration <= 0f)
                return;

            CancelPageTransition();

            _transitionCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            PageTransitionAsync(direction, _transitionCancellationTokenSource).Forget();
        }

        private async UniTask PageTransitionAsync(int direction, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            direction = direction == 0 ? 1 : Math.Sign(direction);
            float elapsed = 0f;
            Vector2 from = _transitionBasePosition + new Vector2(pageTransitionSlideDistance * direction, 0f);

            try
            {
                transitionCanvasGroup.alpha = 0.72f;
                _transitionRoot.anchoredPosition = from;

                while (elapsed < pageTransitionDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / pageTransitionDuration);
                    float eased = 1f - Mathf.Pow(1f - t, 2f);
                    transitionCanvasGroup.alpha = Mathf.Lerp(0.72f, 1f, eased);
                    _transitionRoot.anchoredPosition = Vector2.Lerp(from, _transitionBasePosition, eased);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                transitionCanvasGroup.alpha = 1f;
                _transitionRoot.anchoredPosition = _transitionBasePosition;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (_transitionCancellationTokenSource == cancellationTokenSource)
                {
                    _transitionCancellationTokenSource.Dispose();
                    _transitionCancellationTokenSource = null;
                }
            }
        }

        private void CancelPageTransition()
        {
            if (_transitionCancellationTokenSource == null)
                return;

            _transitionCancellationTokenSource.Cancel();
            _transitionCancellationTokenSource.Dispose();
            _transitionCancellationTokenSource = null;
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }
    }
}
