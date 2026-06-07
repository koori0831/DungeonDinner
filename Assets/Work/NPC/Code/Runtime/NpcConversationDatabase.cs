using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Work.NPC.Code.Data;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcConversationDatabase
    {
        private readonly Dictionary<string, NpcData> _npcs = new Dictionary<string, NpcData>();
        private readonly Dictionary<string, VisitEventData> _visitEvents = new Dictionary<string, VisitEventData>();
        private readonly Dictionary<string, QuestionCategoryData> _questionCategories = new Dictionary<string, QuestionCategoryData>();
        private readonly List<RegionPoolEntryData> _regionPoolEntries = new List<RegionPoolEntryData>();
        private readonly Dictionary<string, Dictionary<string, List<DialogueLineData>>> _dialogueLines
            = new Dictionary<string, Dictionary<string, List<DialogueLineData>>>();

        public IReadOnlyDictionary<string, NpcData> Npcs => _npcs;
        public IReadOnlyDictionary<string, VisitEventData> VisitEvents => _visitEvents;
        public IReadOnlyDictionary<string, QuestionCategoryData> QuestionCategories => _questionCategories;
        public IReadOnlyList<RegionPoolEntryData> RegionPoolEntries => _regionPoolEntries;

        public static NpcConversationDatabase LoadFromResources(string resourceFolder = "NPCData")
        {
            NpcConversationDatabase database = new NpcConversationDatabase();
            database.Load(resourceFolder);
            return database;
        }

        public bool TryGetVisitEvent(string eventId, out VisitEventData visitEvent)
        {
            return _visitEvents.TryGetValue(eventId, out visitEvent);
        }

        public bool TryGetQuestionCategory(string categoryId, out QuestionCategoryData category)
        {
            return _questionCategories.TryGetValue(categoryId, out category);
        }

        public IReadOnlyList<DialogueLineData> GetDialogueLines(string eventId, string group)
        {
            if (_dialogueLines.TryGetValue(eventId, out Dictionary<string, List<DialogueLineData>> groupMap) == false)
                return new List<DialogueLineData>();

            if (groupMap.TryGetValue(group, out List<DialogueLineData> lines) == false)
                return new List<DialogueLineData>();

            return lines;
        }

        public bool HasDialogueGroup(string eventId, string group)
        {
            return _dialogueLines.TryGetValue(eventId, out Dictionary<string, List<DialogueLineData>> groupMap)
                   && groupMap.ContainsKey(group);
        }

        public IReadOnlyList<QuestionCategoryData> GetAvailableQuestionCategories(
            VisitEventData visitEvent,
            IEnumerable<string> usedCategoryIds)
        {
            HashSet<string> used = new HashSet<string>(usedCategoryIds);
            List<QuestionCategoryData> result = new List<QuestionCategoryData>();

            foreach (string categoryId in visitEvent.AvailableQuestionCategories)
            {
                if (used.Contains(categoryId))
                    continue;

                if (_questionCategories.TryGetValue(categoryId, out QuestionCategoryData category) == false)
                    continue;

                if (HasDialogueGroup(visitEvent.EventId, category.DialogueGroup) == false)
                    continue;

                result.Add(category);
            }

            return result;
        }

        public IReadOnlyList<RegionPoolEntryData> GetRegionPoolEntries(string regionId)
        {
            return _regionPoolEntries
                .Where(entry => entry.RegionId == regionId && entry.Weight > 0)
                .ToList();
        }

        public IReadOnlyList<VisitEventData> GetVisitEvents(string regionId, string npcId)
        {
            return _visitEvents.Values
                .Where(visitEvent => IsRegionMatched(visitEvent.RegionId, regionId)
                                     && visitEvent.NpcId == npcId)
                .ToList();
        }

        private void Load(string resourceFolder)
        {
            LoadNpcs(LoadRequired(resourceFolder, "NPCs"));
            LoadVisitEvents(LoadRequired(resourceFolder, "VisitEvents"));
            LoadQuestionCategories(LoadRequired(resourceFolder, "QuestionCategories"));
            LoadRegionPools(LoadRequired(resourceFolder, "RegionPools"));
            LoadDialogueLines(LoadRequired(resourceFolder, "DialogueLines"));
            ValidateLoadedData();
        }

        private void LoadNpcs(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                NpcData data = NpcData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.NpcId))
                    continue;

                _npcs[data.NpcId] = data;
            }
        }

        private void LoadVisitEvents(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                VisitEventData data = VisitEventData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.EventId))
                    continue;

                _visitEvents[data.EventId] = data;
            }
        }

        private void LoadQuestionCategories(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                QuestionCategoryData data = QuestionCategoryData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.CategoryId))
                    continue;

                _questionCategories[data.CategoryId] = data;
            }
        }

        private void LoadRegionPools(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                RegionPoolEntryData data = RegionPoolEntryData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.RegionId) || string.IsNullOrWhiteSpace(data.NpcId))
                    continue;

                _regionPoolEntries.Add(data);
            }
        }

        private void LoadDialogueLines(TextAsset textAsset)
        {
            IEnumerable<DialogueLineData> rows = CsvTableParser.Parse(textAsset)
                .Select(DialogueLineData.FromRow)
                .Where(line => string.IsNullOrWhiteSpace(line.EventId) == false
                               && string.IsNullOrWhiteSpace(line.Group) == false);

            foreach (DialogueLineData line in rows)
            {
                if (_dialogueLines.TryGetValue(line.EventId, out Dictionary<string, List<DialogueLineData>> groupMap) == false)
                {
                    groupMap = new Dictionary<string, List<DialogueLineData>>();
                    _dialogueLines[line.EventId] = groupMap;
                }

                if (groupMap.TryGetValue(line.Group, out List<DialogueLineData> lines) == false)
                {
                    lines = new List<DialogueLineData>();
                    groupMap[line.Group] = lines;
                }

                lines.Add(line);
            }

            foreach (Dictionary<string, List<DialogueLineData>> groupMap in _dialogueLines.Values)
            {
                foreach (List<DialogueLineData> lines in groupMap.Values)
                {
                    lines.Sort((a, b) => a.LineOrder.CompareTo(b.LineOrder));
                }
            }
        }

        private static TextAsset LoadRequired(string resourceFolder, string fileName)
        {
            string path = $"{resourceFolder}/{fileName}";
            TextAsset textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
                Debug.LogError($"NPC conversation data not found at Resources/{path}.csv");

            return textAsset;
        }

        private void ValidateLoadedData()
        {
            ValidateRequiredEventIds();
            ValidateSequenceEvents();
            ValidateRequestEvents();
            ValidateStartDialogueGroups();
            ValidateQuestionDialogueGroups();
        }

        private void ValidateRequiredEventIds()
        {
            foreach (VisitEventData visitEvent in _visitEvents.Values)
            {
                foreach (string requiredEventId in visitEvent.RequiredEventIds)
                {
                    if (_visitEvents.ContainsKey(requiredEventId))
                        continue;

                    Debug.LogWarning(
                        $"NPC data validation: required event not found. event={visitEvent.EventId}, required={requiredEventId}");
                }
            }
        }

        private void ValidateSequenceEvents()
        {
            List<VisitEventData> sequenceEvents = _visitEvents.Values
                .Where(IsSequenceEvent)
                .ToList();

            foreach (VisitEventData visitEvent in sequenceEvents)
            {
                if (visitEvent.EventType != VisitEventType.Sequence)
                {
                    Debug.LogWarning(
                        $"NPC data validation: event has SequenceGroup but EventType is not Sequence. " +
                        $"event={visitEvent.EventId}, eventType={visitEvent.EventType}, sequenceGroup={visitEvent.SequenceGroup}");
                }

                if (visitEvent.RepeatMode != VisitEventRepeatMode.Once)
                {
                    Debug.LogWarning(
                        $"NPC data validation: sequence event should use RepeatMode Once. " +
                        $"event={visitEvent.EventId}, repeatMode={visitEvent.RepeatMode}");
                }

                if (string.IsNullOrWhiteSpace(visitEvent.SequenceGroup))
                {
                    Debug.LogWarning(
                        $"NPC data validation: sequence event needs SequenceGroup. event={visitEvent.EventId}");
                }

                if (visitEvent.SequenceIndex <= 0)
                {
                    Debug.LogWarning(
                        $"NPC data validation: sequence event needs SequenceIndex greater than 0. event={visitEvent.EventId}");
                }
            }

            foreach (IGrouping<string, VisitEventData> group in sequenceEvents
                         .Where(visitEvent => string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false)
                         .GroupBy(visitEvent => visitEvent.SequenceGroup))
            {
                ValidateSequenceGroup(group.Key, group.ToList());
            }
        }

        private static void ValidateSequenceGroup(string sequenceGroup, List<VisitEventData> events)
        {
            foreach (IGrouping<int, VisitEventData> indexGroup in events.GroupBy(visitEvent => visitEvent.SequenceIndex))
            {
                if (indexGroup.Key <= 0 || indexGroup.Count() <= 1)
                    continue;

                string eventIds = string.Join("|", indexGroup.Select(visitEvent => visitEvent.EventId));
                Debug.LogWarning(
                    $"NPC data validation: duplicate SequenceIndex in group. " +
                    $"sequenceGroup={sequenceGroup}, sequenceIndex={indexGroup.Key}, events={eventIds}");
            }

            List<int> indexes = events
                .Where(visitEvent => visitEvent.SequenceIndex > 0)
                .Select(visitEvent => visitEvent.SequenceIndex)
                .Distinct()
                .OrderBy(index => index)
                .ToList();

            for (int i = 0; i < indexes.Count; i++)
            {
                int expectedIndex = i + 1;
                if (indexes[i] == expectedIndex)
                    continue;

                Debug.LogWarning(
                    $"NPC data validation: sequence index gap detected. " +
                    $"sequenceGroup={sequenceGroup}, expected={expectedIndex}, actual={indexes[i]}");
                break;
            }

            List<VisitEventData> orderedEvents = events
                .Where(visitEvent => visitEvent.SequenceIndex > 0)
                .OrderBy(visitEvent => visitEvent.SequenceIndex)
                .ToList();

            for (int i = 1; i < orderedEvents.Count; i++)
            {
                VisitEventData previousEvent = orderedEvents[i - 1];
                VisitEventData currentEvent = orderedEvents[i];
                int expectedMaximumVisitRequirement = previousEvent.RequiredNpcVisits + 1;
                if (currentEvent.RequiredNpcVisits <= expectedMaximumVisitRequirement)
                    continue;

                Debug.LogWarning(
                    $"NPC data validation: sequence visit requirement may delay the next chain step. " +
                    $"sequenceGroup={sequenceGroup}, previous={previousEvent.EventId}, current={currentEvent.EventId}, " +
                    $"requiredNpcVisits={currentEvent.RequiredNpcVisits}, expectedMax={expectedMaximumVisitRequirement}");
            }
        }

        private static bool IsSequenceEvent(VisitEventData visitEvent)
        {
            return visitEvent.EventType == VisitEventType.Sequence
                   || string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false;
        }

        private void ValidateRequestEvents()
        {
            foreach (VisitEventData visitEvent in _visitEvents.Values)
            {
                if (visitEvent.EventType != VisitEventType.Request)
                    continue;

                if (_npcs.TryGetValue(visitEvent.NpcId, out NpcData npc) == false)
                {
                    Debug.LogWarning(
                        $"NPC data validation: request event npc not found. event={visitEvent.EventId}, npc={visitEvent.NpcId}");
                    continue;
                }

                if (npc.RequestAvailable == false)
                {
                    Debug.LogWarning(
                        $"NPC data validation: request event exists for an NPC without request availability. " +
                        $"event={visitEvent.EventId}, npc={visitEvent.NpcId}");
                }

                if (visitEvent.RepeatMode != VisitEventRepeatMode.Once)
                {
                    Debug.LogWarning(
                        $"NPC data validation: request event should use RepeatMode Once. " +
                        $"event={visitEvent.EventId}, repeatMode={visitEvent.RepeatMode}");
                }
            }
        }

        private void ValidateQuestionDialogueGroups()
        {
            foreach (VisitEventData visitEvent in _visitEvents.Values)
            {
                if (visitEvent.QuestionLimit <= 0)
                    continue;

                if (visitEvent.AvailableQuestionCategories.Count == 0)
                {
                    Debug.LogWarning(
                        $"NPC data validation: event has QuestionLimit but no question categories. event={visitEvent.EventId}");
                    continue;
                }

                foreach (string categoryId in visitEvent.AvailableQuestionCategories)
                {
                    if (_questionCategories.TryGetValue(categoryId, out QuestionCategoryData category) == false)
                    {
                        Debug.LogWarning(
                            $"NPC data validation: question category not found. event={visitEvent.EventId}, category={categoryId}");
                        continue;
                    }

                    if (HasDialogueGroup(visitEvent.EventId, category.DialogueGroup))
                        continue;

                    Debug.LogWarning(
                        $"NPC data validation: question dialogue group not found. " +
                        $"event={visitEvent.EventId}, category={categoryId}, group={category.DialogueGroup}");
                }
            }
        }

        private void ValidateStartDialogueGroups()
        {
            foreach (VisitEventData visitEvent in _visitEvents.Values)
            {
                foreach (string group in visitEvent.StartGroups)
                {
                    if (HasDialogueGroup(visitEvent.EventId, group))
                        continue;

                    Debug.LogWarning(
                        $"NPC data validation: start dialogue group not found. event={visitEvent.EventId}, group={group}");
                }
            }
        }

        private static bool IsRegionMatched(string eventRegionId, string targetRegionId)
        {
            if (string.IsNullOrWhiteSpace(targetRegionId))
                return false;

            if (string.IsNullOrWhiteSpace(eventRegionId)
                || string.Equals(eventRegionId, "*", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventRegionId, "Any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string[] regionIds = eventRegionId.Split('|');
            foreach (string regionId in regionIds)
            {
                if (string.Equals(regionId.Trim(), targetRegionId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
