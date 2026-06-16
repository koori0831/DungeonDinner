using System;
using System.Collections.Generic;

namespace Work.NPC.Code.Data
{
    public enum NpcConversationResult
    {
        Disgusting,
        Wrong,
        Similar,
        Correct,
        Perfect
    }

    public sealed class DialogueLineData
    {
        public string EventId { get; }
        public string Group { get; }
        public string QuestionCategory { get; }
        public int LineOrder { get; }
        public string Speaker { get; }
        public string Text { get; }
        public bool IsPlayer => string.Equals(Speaker, "Player", StringComparison.OrdinalIgnoreCase);

        public DialogueLineData(
            string eventId,
            string group,
            string questionCategory,
            int lineOrder,
            string speaker,
            string text)
        {
            EventId = eventId;
            Group = group;
            QuestionCategory = questionCategory;
            LineOrder = lineOrder;
            Speaker = speaker;
            Text = text;
        }

        public static DialogueLineData FromRow(IReadOnlyDictionary<string, string> row)
        {
            return new DialogueLineData(
                CsvRowReader.Get(row, "EventId"),
                CsvRowReader.Get(row, "Group"),
                CsvRowReader.Get(row, "QuestionCategory"),
                CsvRowReader.GetInt(row, "LineOrder"),
                CsvRowReader.Get(row, "Speaker"),
                CsvRowReader.Get(row, "Text"));
        }
    }
}
