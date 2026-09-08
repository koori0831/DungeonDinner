using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Work.Chat.Code;
using Work.NPC.Code.Data;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcConversationView : MonoBehaviour
    {
        [SerializeField] private NpcConversationRunner runner;
        [SerializeField] private ChatPanel chatPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool resolveReferencesOnEnable = true;
        [SerializeField] private bool disableRunnerDirectChatOutput = true;
        [SerializeField] private bool visibleOnEnable = true;
        [SerializeField] private bool showWhenConversationStarted = true;
        [SerializeField] private bool showWhenDialogueLinePlayed = true;
        [SerializeField] private bool showWhenQuestionOptionsAvailable = true;
        [SerializeField] private bool showWhenOrderReady = true;
        [SerializeField] private bool hideWhenCookingStepReady;
        [SerializeField] private bool hideWhenConversationCompleted;
        [SerializeField] private bool clearChatHistoryWhenConversationStarted = true;
        [SerializeField] private bool clearChatHistoryWhenConversationCompleted;
        [SerializeField] private bool showSpeakerNameInBubble = true;
        [SerializeField] private bool completeTypingOnSubmit = true;
        [SerializeField] private string playerNameColor = "#000000";
        [SerializeField] private string npcNameColor = "#D6A85A";

        [Header("NPC Portrait")]
        [SerializeField] private NpcPortraitCatalogSO portraitCatalog;
        [SerializeField] private RectTransform portraitRoot;
        [SerializeField] private CanvasGroup portraitCanvasGroup;
        [SerializeField] private Image portraitImage;
        [SerializeField] private bool createPortraitViewIfMissing = true;
        [SerializeField] private bool hidePortraitWhenCookingStepReady = true;
        [SerializeField] private Vector2 portraitSize = new Vector2(520f, 520f);
        [SerializeField] private Vector2 portraitRestingPosition = new Vector2(18f, 0f);
        [SerializeField, Min(0f)] private float portraitSlideDistance = 80f;
        [SerializeField, Min(0.01f)] private float portraitEntranceDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float portraitExitDuration = 0.22f;
        [SerializeField, Range(0.5f, 1f)] private float portraitEntranceScale = 0.94f;

        [Header("Events")]
        [SerializeField] private UnityEvent conversationShown = new UnityEvent();
        [SerializeField] private UnityEvent conversationHidden = new UnityEvent();
        [SerializeField] private UnityEvent cookingStepReady = new UnityEvent();
        [SerializeField] private UnityEvent conversationCompleted = new UnityEvent();

        private bool _visible;
        private bool _hasSavedRunnerDirectOutput;
        private bool _savedRunnerDirectOutput;
        private Sequence _portraitSequence;
        private string _visiblePortraitNpcId;
        private bool _portraitIsVisible;

        public bool IsVisible => _visible;

        private void Awake()
        {
            ResolveReferences();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                Debug.LogError("NpcConversationView CanvasGroup is missing. Assign it in the inspector or add it to the prefab.", this);
            }

            EnsurePortraitPresentation();
            SetPortraitHiddenImmediate();
        }

        private void OnEnable()
        {
            if (resolveReferencesOnEnable)
                ResolveReferences();

            EnsurePortraitPresentation();
            SetRunnerSubscriptions(true);
            ApplyRunnerDirectOutputOverride();
            SetVisible(visibleOnEnable);

            if (visibleOnEnable && runner != null && runner.HasActiveConversation)
                ShowPortrait(runner.CurrentNpcId);
            else
                SetPortraitHiddenImmediate();
        }

        private void OnDisable()
        {
            KillPortraitSequence();
            SetPortraitHiddenImmediate();
            RestoreRunnerDirectOutputOverride();
            SetRunnerSubscriptions(false);
        }

        public void Bind(NpcConversationRunner newRunner, ChatPanel newChatPanel)
        {
            if (runner == newRunner && chatPanel == newChatPanel)
                return;

            RestoreRunnerDirectOutputOverride();
            SetRunnerSubscriptions(false);

            runner = newRunner;
            chatPanel = newChatPanel;

            SetRunnerSubscriptions(isActiveAndEnabled);
            if (isActiveAndEnabled)
                ApplyRunnerDirectOutputOverride();
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            bool changed = _visible != visible;
            _visible = visible;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (visible)
            {
                if (runner != null && runner.HasActiveConversation)
                    ShowPortrait(runner.CurrentNpcId);
            }
            else
            {
                HidePortrait();
            }

            if (changed == false)
                return;

            if (visible)
                conversationShown.Invoke();
            else
                conversationHidden.Invoke();
        }

        private void Update()
        {
            if (completeTypingOnSubmit == false || chatPanel == null)
                return;

            bool submitted = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                submitted = true;

            if (Keyboard.current != null
                && (Keyboard.current.spaceKey.wasPressedThisFrame
                    || Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                submitted = true;
            }

            if (submitted)
                chatPanel.CompleteActiveTyping();
        }

        private void ResolveReferences()
        {
            if (runner == null)
                runner = FindFirstObjectByType<NpcConversationRunner>();

            if (chatPanel == null)
                chatPanel = FindFirstObjectByType<ChatPanel>();

            if (portraitCatalog == null)
                portraitCatalog = NpcPortraitCatalogSO.LoadDefault();
        }

        private void SetRunnerSubscriptions(bool subscribe)
        {
            if (runner == null)
                return;

            if (subscribe)
            {
                runner.ConversationStarted += HandleConversationStarted;
                runner.DialogueLinePlayed += HandleDialogueLinePlayed;
                runner.QuestionOptionsUpdated += HandleQuestionOptionsUpdated;
                runner.OrderReady += HandleOrderReady;
                runner.CookingStepReady += HandleCookingStepReady;
                runner.ConversationCompleted += HandleConversationCompleted;
                return;
            }

            runner.ConversationStarted -= HandleConversationStarted;
            runner.DialogueLinePlayed -= HandleDialogueLinePlayed;
            runner.QuestionOptionsUpdated -= HandleQuestionOptionsUpdated;
            runner.OrderReady -= HandleOrderReady;
            runner.CookingStepReady -= HandleCookingStepReady;
            runner.ConversationCompleted -= HandleConversationCompleted;
        }

        private void HandleConversationStarted()
        {
            if (clearChatHistoryWhenConversationStarted == true)
                chatPanel?.ClearChats();

            if (showWhenConversationStarted == true)
                SetVisible(true);

            ShowPortrait(runner != null ? runner.CurrentNpcId : string.Empty);
        }

        private void ApplyRunnerDirectOutputOverride()
        {
            if (runner == null || disableRunnerDirectChatOutput == false || _hasSavedRunnerDirectOutput)
                return;

            _savedRunnerDirectOutput = runner.DirectChatPanelOutputEnabled;
            _hasSavedRunnerDirectOutput = true;
            runner.SetDirectChatPanelOutput(false);
        }

        private void RestoreRunnerDirectOutputOverride()
        {
            if (runner == null || _hasSavedRunnerDirectOutput == false)
                return;

            runner.SetDirectChatPanelOutput(_savedRunnerDirectOutput);
            _hasSavedRunnerDirectOutput = false;
        }

        private void HandleDialogueLinePlayed(NpcDialogueLineContext context)
        {
            if (showWhenDialogueLinePlayed)
                SetVisible(true);

            if (chatPanel == null || context == null)
                return;

            if (context.IsPlayer == false)
                ShowPortrait(context.NpcId);

            ChatTextField chat = chatPanel.AddChat(BuildBubbleText(context), context.IsPlayer);
            context.RegisterPresentationWaiter(() => chat == null || chat.IsTyping == false);
        }

        private void HandleQuestionOptionsUpdated(IReadOnlyList<QuestionCategoryData> options)
        {
            if (showWhenQuestionOptionsAvailable && options != null && options.Count > 0)
                SetVisible(true);
        }

        private void HandleOrderReady(NpcOrderContext orderContext)
        {
            if (showWhenOrderReady)
                SetVisible(true);
        }

        private void HandleCookingStepReady()
        {
            cookingStepReady.Invoke();

            if (hidePortraitWhenCookingStepReady)
                HidePortrait();

            if (hideWhenCookingStepReady)
                SetVisible(false);
        }

        private void HandleConversationCompleted()
        {
            conversationCompleted.Invoke();
            HidePortrait();

            if (clearChatHistoryWhenConversationCompleted == true)
                chatPanel?.ClearChats();

            if (hideWhenConversationCompleted)
                SetVisible(false);
        }

        private void EnsurePortraitPresentation()
        {
            if (portraitCatalog == null)
                portraitCatalog = NpcPortraitCatalogSO.LoadDefault();

            if (portraitRoot != null)
            {
                if (portraitCanvasGroup == null)
                    portraitCanvasGroup = portraitRoot.GetComponent<CanvasGroup>();
                if (portraitImage == null)
                    portraitImage = portraitRoot.GetComponentInChildren<Image>(true);
            }

            if (portraitRoot != null && portraitCanvasGroup != null && portraitImage != null)
                return;
            if (createPortraitViewIfMissing == false)
                return;

            RectTransform parent = ResolvePortraitParent();
            if (parent == null)
                return;

            GameObject rootObject = new GameObject(
                "NpcEntrancePortrait",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(LayoutElement));
            rootObject.layer = parent.gameObject.layer;
            portraitRoot = rootObject.GetComponent<RectTransform>();
            portraitRoot.SetParent(parent, false);
            portraitRoot.anchorMin = Vector2.zero;
            portraitRoot.anchorMax = Vector2.zero;
            portraitRoot.pivot = Vector2.zero;
            portraitRoot.sizeDelta = portraitSize;
            portraitRoot.anchoredPosition = portraitRestingPosition;
            portraitRoot.SetAsLastSibling();

            LayoutElement layoutElement = rootObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            portraitCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            portraitCanvasGroup.interactable = false;
            portraitCanvasGroup.blocksRaycasts = false;

            GameObject imageObject = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = rootObject.layer;
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(portraitRoot, false);
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            portraitImage = imageObject.GetComponent<Image>();
            portraitImage.raycastTarget = false;
            portraitImage.preserveAspect = true;
            portraitImage.color = Color.white;
        }

        private RectTransform ResolvePortraitParent()
        {
            if (chatPanel != null && chatPanel.transform.parent is RectTransform chatParent)
                return chatParent;

            Canvas canvas = chatPanel != null
                ? chatPanel.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        }

        private void ShowPortrait(string npcId)
        {
            EnsurePortraitPresentation();
            if (portraitRoot == null || portraitCanvasGroup == null || portraitImage == null)
                return;

            Sprite portrait = portraitCatalog != null ? portraitCatalog.GetPortrait(npcId) : null;
            if (portrait == null)
            {
                HidePortrait();
                return;
            }

            string normalizedNpcId = string.IsNullOrWhiteSpace(npcId) ? string.Empty : npcId.Trim();
            bool needsEntrance = _portraitIsVisible == false
                                 || string.Equals(_visiblePortraitNpcId, normalizedNpcId, System.StringComparison.OrdinalIgnoreCase) == false;

            portraitImage.sprite = portrait;
            portraitImage.enabled = true;
            _visiblePortraitNpcId = normalizedNpcId;
            if (needsEntrance == false)
                return;

            KillPortraitSequence();
            _portraitIsVisible = true;
            portraitRoot.gameObject.SetActive(true);
            portraitRoot.anchoredPosition = portraitRestingPosition + Vector2.left * portraitSlideDistance;
            portraitRoot.localScale = new Vector3(portraitEntranceScale, portraitEntranceScale, 1f);
            portraitCanvasGroup.alpha = 0f;

            _portraitSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _portraitSequence.Join(
                portraitRoot.DOAnchorPos(portraitRestingPosition, portraitEntranceDuration)
                    .SetEase(Ease.OutCubic));
            _portraitSequence.Join(portraitCanvasGroup.DOFade(1f, portraitEntranceDuration));
            _portraitSequence.Join(
                portraitRoot.DOScale(Vector3.one, portraitEntranceDuration)
                    .SetEase(Ease.OutBack));
            _portraitSequence.OnComplete(() => _portraitSequence = null);
        }

        private void HidePortrait()
        {
            if (portraitRoot == null || portraitCanvasGroup == null)
                return;
            if (_portraitIsVisible == false)
            {
                SetPortraitHiddenImmediate();
                return;
            }

            KillPortraitSequence();
            _portraitIsVisible = false;
            _visiblePortraitNpcId = string.Empty;
            _portraitSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _portraitSequence.Join(
                portraitRoot.DOAnchorPos(
                        portraitRestingPosition + Vector2.left * portraitSlideDistance,
                        portraitExitDuration)
                    .SetEase(Ease.InCubic));
            _portraitSequence.Join(portraitCanvasGroup.DOFade(0f, portraitExitDuration));
            _portraitSequence.Join(
                portraitRoot.DOScale(
                        new Vector3(portraitEntranceScale, portraitEntranceScale, 1f),
                        portraitExitDuration)
                    .SetEase(Ease.InCubic));
            _portraitSequence.OnComplete(() => _portraitSequence = null);
        }

        private void SetPortraitHiddenImmediate()
        {
            if (portraitRoot == null || portraitCanvasGroup == null)
                return;

            _portraitIsVisible = false;
            _visiblePortraitNpcId = string.Empty;
            portraitCanvasGroup.alpha = 0f;
            portraitCanvasGroup.interactable = false;
            portraitCanvasGroup.blocksRaycasts = false;
            portraitRoot.anchoredPosition = portraitRestingPosition + Vector2.left * portraitSlideDistance;
            portraitRoot.localScale = new Vector3(portraitEntranceScale, portraitEntranceScale, 1f);
        }

        private void KillPortraitSequence()
        {
            if (_portraitSequence == null)
                return;

            _portraitSequence.Kill(false);
            _portraitSequence = null;
        }

        private string BuildBubbleText(NpcDialogueLineContext context)
        {
            if (context == null)
                return string.Empty;

            if (showSpeakerNameInBubble == false || string.IsNullOrWhiteSpace(context.SpeakerName))
                return context.DisplayText;

            string color = context.IsPlayer ? playerNameColor : npcNameColor;
            return $"<size=75%><color={color}>{context.SpeakerName}</color></size>\n{context.DisplayText}";
        }
    }
}
