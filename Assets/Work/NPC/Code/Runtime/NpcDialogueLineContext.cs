using System;
using System.Collections.Generic;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcDialogueLineContext
    {
        public string EventId { get; }
        public string NpcId { get; }
        public string Group { get; }
        public string SpeakerId { get; }
        public string SpeakerName { get; }
        public bool IsPlayer { get; }
        public string Text { get; }
        public string DisplayText { get; }
        public IReadOnlyList<string> OrderHighlights { get; }
        private readonly List<Func<bool>> _presentationWaiters = new List<Func<bool>>();

        public NpcDialogueLineContext(
            string eventId,
            string npcId,
            string group,
            string speakerId,
            string speakerName,
            bool isPlayer,
            string text,
            string displayText,
            IReadOnlyList<string> orderHighlights = null)
        {
            EventId = eventId ?? string.Empty;
            NpcId = npcId ?? string.Empty;
            Group = group ?? string.Empty;
            SpeakerId = speakerId ?? string.Empty;
            SpeakerName = speakerName ?? string.Empty;
            IsPlayer = isPlayer;
            Text = text ?? string.Empty;
            DisplayText = string.IsNullOrWhiteSpace(displayText) ? Text : displayText;
            OrderHighlights = orderHighlights ?? Array.Empty<string>();
        }

        public void RegisterPresentationWaiter(Func<bool> isComplete)
        {
            if (isComplete != null)
                _presentationWaiters.Add(isComplete);
        }

        public bool IsPresentationComplete()
        {
            for (int i = 0; i < _presentationWaiters.Count; i++)
            {
                if (_presentationWaiters[i]?.Invoke() == false)
                    return false;
            }

            return true;
        }

        public string BuildDebugSummary()
        {
            return
                $"event={ValueOrNone(EventId)}, npc={ValueOrNone(NpcId)}, group={ValueOrNone(Group)}, " +
                $"speaker={ValueOrNone(SpeakerId)}, player={IsPlayer}, text={ValueOrNone(DisplayText)}";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }
}
