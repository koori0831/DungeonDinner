using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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
        [SerializeField] private bool showWhenDialogueLinePlayed = true;
        [SerializeField] private bool showWhenQuestionOptionsAvailable = true;
        [SerializeField] private bool showWhenOrderReady = true;
        [SerializeField] private bool hideWhenCookingStepReady;
        [SerializeField] private bool hideWhenConversationCompleted;
        [SerializeField] private bool showSpeakerNameInBubble = true;
        [SerializeField] private bool completeTypingOnSubmit = true;
        [SerializeField] private string playerNameColor = "#9FD4FF";
        [SerializeField] private string npcNameColor = "#D6A85A";

        [Header("Events")]
        [SerializeField] private UnityEvent conversationShown = new UnityEvent();
        [SerializeField] private UnityEvent conversationHidden = new UnityEvent();
        [SerializeField] private UnityEvent cookingStepReady = new UnityEvent();
        [SerializeField] private UnityEvent conversationCompleted = new UnityEvent();

        private bool _visible;
        private bool _hasSavedRunnerDirectOutput;
        private bool _savedRunnerDirectOutput;

        public bool IsVisible => _visible;

        private void Awake()
        {
            ResolveReferences();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (resolveReferencesOnEnable)
                ResolveReferences();

            SetRunnerSubscriptions(true);
            ApplyRunnerDirectOutputOverride();
            SetVisible(visibleOnEnable);
        }

        private void OnDisable()
        {
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
        }

        private void SetRunnerSubscriptions(bool subscribe)
        {
            if (runner == null)
                return;

            if (subscribe)
            {
                runner.DialogueLinePlayed += HandleDialogueLinePlayed;
                runner.QuestionOptionsUpdated += HandleQuestionOptionsUpdated;
                runner.OrderReady += HandleOrderReady;
                runner.CookingStepReady += HandleCookingStepReady;
                runner.ConversationCompleted += HandleConversationCompleted;
                return;
            }

            runner.DialogueLinePlayed -= HandleDialogueLinePlayed;
            runner.QuestionOptionsUpdated -= HandleQuestionOptionsUpdated;
            runner.OrderReady -= HandleOrderReady;
            runner.CookingStepReady -= HandleCookingStepReady;
            runner.ConversationCompleted -= HandleConversationCompleted;
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

            if (hideWhenCookingStepReady)
                SetVisible(false);
        }

        private void HandleConversationCompleted()
        {
            conversationCompleted.Invoke();

            if (hideWhenConversationCompleted)
                SetVisible(false);
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
