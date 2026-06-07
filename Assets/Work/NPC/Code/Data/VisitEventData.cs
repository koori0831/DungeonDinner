using System;
using System.Collections.Generic;

namespace Work.NPC.Code.Data
{
    public enum VisitEventType
    {
        Normal,
        Special,
        Sequence,
        Request
    }

    public enum VisitEventRepeatMode
    {
        Once,
        Cycle,
        Repeat
    }

    public sealed class VisitEventData
    {
        public string EventId { get; }
        public string NpcId { get; }
        public string RegionId { get; }
        public IReadOnlyList<string> StartGroups { get; }
        public int QuestionLimit { get; }
        public IReadOnlyList<string> AvailableQuestionCategories { get; }
        public VisitEventType EventType { get; }
        public int Priority { get; }
        public VisitEventRepeatMode RepeatMode { get; }
        public int CooldownDays { get; }
        public int RequiredNpcVisits { get; }
        public int RequiredAffinity { get; }
        public int RequiredCorrectCount { get; }
        public string RequiredLastResult { get; }
        public IReadOnlyList<string> RequiredEventIds { get; }
        public string SequenceGroup { get; }
        public int SequenceIndex { get; }
        public string CorrectRecipeId { get; }
        public IReadOnlyList<string> AllowedFoodTypes { get; }
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> PreferredTags { get; }
        public IReadOnlyList<string> AvoidTags { get; }
        public IReadOnlyList<string> DisgustingTags { get; }

        public VisitEventData(
            string eventId,
            string npcId,
            string regionId,
            IReadOnlyList<string> startGroups,
            int questionLimit,
            IReadOnlyList<string> availableQuestionCategories,
            VisitEventType eventType,
            int priority,
            VisitEventRepeatMode repeatMode,
            int cooldownDays,
            int requiredNpcVisits,
            int requiredAffinity,
            int requiredCorrectCount,
            string requiredLastResult,
            IReadOnlyList<string> requiredEventIds,
            string sequenceGroup,
            int sequenceIndex,
            string correctRecipeId,
            IReadOnlyList<string> allowedFoodTypes,
            IReadOnlyList<string> requiredTags,
            IReadOnlyList<string> preferredTags,
            IReadOnlyList<string> avoidTags,
            IReadOnlyList<string> disgustingTags)
        {
            EventId = eventId;
            NpcId = npcId;
            RegionId = regionId;
            StartGroups = startGroups;
            QuestionLimit = questionLimit;
            AvailableQuestionCategories = availableQuestionCategories;
            EventType = eventType;
            Priority = priority;
            RepeatMode = repeatMode;
            CooldownDays = cooldownDays;
            RequiredNpcVisits = requiredNpcVisits;
            RequiredAffinity = requiredAffinity;
            RequiredCorrectCount = requiredCorrectCount;
            RequiredLastResult = requiredLastResult;
            RequiredEventIds = requiredEventIds;
            SequenceGroup = sequenceGroup;
            SequenceIndex = sequenceIndex;
            CorrectRecipeId = correctRecipeId;
            AllowedFoodTypes = allowedFoodTypes;
            RequiredTags = requiredTags;
            PreferredTags = preferredTags;
            AvoidTags = avoidTags;
            DisgustingTags = disgustingTags;
        }

        public static VisitEventData FromRow(IReadOnlyDictionary<string, string> row)
        {
            return new VisitEventData(
                CsvRowReader.Get(row, "EventId"),
                CsvRowReader.Get(row, "NpcId"),
                CsvRowReader.Get(row, "RegionId"),
                CsvRowReader.GetList(row, "StartGroups"),
                CsvRowReader.GetInt(row, "QuestionLimit", 1),
                CsvRowReader.GetList(row, "AvailableQuestionCategories"),
                GetEnum(row, "EventType", VisitEventType.Normal),
                CsvRowReader.GetInt(row, "Priority"),
                GetEnum(row, "RepeatMode", VisitEventRepeatMode.Cycle),
                CsvRowReader.GetInt(row, "CooldownDays"),
                CsvRowReader.GetInt(row, "RequiredNpcVisits"),
                CsvRowReader.GetInt(row, "RequiredAffinity"),
                CsvRowReader.GetInt(row, "RequiredCorrectCount"),
                CsvRowReader.Get(row, "RequiredLastResult"),
                CsvRowReader.GetList(row, "RequiredEventIds"),
                CsvRowReader.Get(row, "SequenceGroup"),
                CsvRowReader.GetInt(row, "SequenceIndex"),
                CsvRowReader.Get(row, "CorrectRecipeId"),
                CsvRowReader.GetList(row, "AllowedFoodTypes"),
                CsvRowReader.GetList(row, "RequiredTags"),
                CsvRowReader.GetList(row, "PreferredTags"),
                CsvRowReader.GetList(row, "AvoidTags"),
                CsvRowReader.GetList(row, "DisgustingTags"));
        }

        private static T GetEnum<T>(IReadOnlyDictionary<string, string> row, string key, T fallback)
            where T : struct
        {
            string value = CsvRowReader.Get(row, key);
            return Enum.TryParse(value, true, out T result) ? result : fallback;
        }
    }
}
