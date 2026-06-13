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
        private readonly List<NpcDataValidationIssue> _loadIssues = new List<NpcDataValidationIssue>();
        private readonly Dictionary<string, Dictionary<string, List<DialogueLineData>>> _dialogueLines
            = new Dictionary<string, Dictionary<string, List<DialogueLineData>>>();

        public IReadOnlyDictionary<string, NpcData> Npcs => _npcs;
        public IReadOnlyDictionary<string, VisitEventData> VisitEvents => _visitEvents;
        public IReadOnlyDictionary<string, QuestionCategoryData> QuestionCategories => _questionCategories;
        public IReadOnlyList<RegionPoolEntryData> RegionPoolEntries => _regionPoolEntries;
        public IEnumerable<string> DialogueEventIds => _dialogueLines.Keys;
        public NpcDataValidationReport LastValidationReport { get; private set; }

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

        public IReadOnlyList<string> GetDialogueGroups(string eventId)
        {
            if (_dialogueLines.TryGetValue(eventId, out Dictionary<string, List<DialogueLineData>> groupMap) == false)
                return new List<string>();

            return groupMap.Keys.ToList();
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

        public NpcDataValidationReport ValidateData(bool logIssuesToConsole = false)
        {
            LastValidationReport = NpcDataValidator.Validate(this, _loadIssues);

            if (logIssuesToConsole && LastValidationReport.Issues.Count > 0)
                LastValidationReport.LogToUnityConsole();

            return LastValidationReport;
        }

        private void Load(string resourceFolder)
        {
            LoadNpcs(LoadRequired(resourceFolder, "NPCs"));
            LoadVisitEvents(LoadRequired(resourceFolder, "VisitEvents"));
            LoadQuestionCategories(LoadRequired(resourceFolder, "QuestionCategories"));
            LoadRegionPools(LoadRequired(resourceFolder, "RegionPools"));
            LoadDialogueLines(LoadRequired(resourceFolder, "DialogueLines"));
            ValidateData(true);
        }

        private void LoadNpcs(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                NpcData data = NpcData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.NpcId))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "NpcIdEmpty",
                        "NPC row has no NPC ID.",
                        "file=NPCs");
                    continue;
                }

                if (_npcs.ContainsKey(data.NpcId))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "NpcIdDuplicate",
                        "NPC ID is duplicated. The later row overwrote the earlier one.",
                        $"file=NPCs, npc={data.NpcId}");
                }

                _npcs[data.NpcId] = data;
            }
        }

        private void LoadVisitEvents(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                VisitEventData data = VisitEventData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.EventId))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "VisitEventIdEmpty",
                        "Visit event row has no Event ID.",
                        "file=VisitEvents");
                    continue;
                }

                if (_visitEvents.ContainsKey(data.EventId))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "VisitEventIdDuplicate",
                        "Visit event ID is duplicated. The later row overwrote the earlier one.",
                        $"file=VisitEvents, event={data.EventId}");
                }

                _visitEvents[data.EventId] = data;
            }
        }

        private void LoadQuestionCategories(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                QuestionCategoryData data = QuestionCategoryData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.CategoryId))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "QuestionCategoryIdEmpty",
                        "Question category row has no Category ID.",
                        "file=QuestionCategories");
                    continue;
                }

                if (_questionCategories.ContainsKey(data.CategoryId))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "QuestionCategoryIdDuplicate",
                        "Question category ID is duplicated. The later row overwrote the earlier one.",
                        $"file=QuestionCategories, category={data.CategoryId}");
                }

                _questionCategories[data.CategoryId] = data;
            }
        }

        private void LoadRegionPools(TextAsset textAsset)
        {
            foreach (Dictionary<string, string> row in CsvTableParser.Parse(textAsset))
            {
                RegionPoolEntryData data = RegionPoolEntryData.FromRow(row);
                if (string.IsNullOrWhiteSpace(data.RegionId) || string.IsNullOrWhiteSpace(data.NpcId))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "RegionPoolKeyEmpty",
                        "Region pool row needs both RegionId and NpcId.",
                        $"file=RegionPools, region={data.RegionId}, npc={data.NpcId}");
                    continue;
                }

                _regionPoolEntries.Add(data);
            }
        }

        private void LoadDialogueLines(TextAsset textAsset)
        {
            IEnumerable<DialogueLineData> rows = CsvTableParser.Parse(textAsset)
                .Select(DialogueLineData.FromRow);

            foreach (DialogueLineData line in rows)
            {
                if (string.IsNullOrWhiteSpace(line.EventId) || string.IsNullOrWhiteSpace(line.Group))
                {
                    AddLoadIssue(
                        NpcDataValidationSeverity.Error,
                        "DialogueLineKeyEmpty",
                        "Dialogue line needs both EventId and Group.",
                        $"file=DialogueLines, event={line.EventId}, group={line.Group}");
                    continue;
                }

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

        private void AddLoadIssue(
            NpcDataValidationSeverity severity,
            string code,
            string message,
            string context = "")
        {
            _loadIssues.Add(new NpcDataValidationIssue(severity, code, message, context));
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
