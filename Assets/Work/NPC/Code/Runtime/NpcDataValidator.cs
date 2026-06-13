using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Work.NPC.Code.Data;

namespace Work.NPC.Code.Runtime
{
    public enum NpcDataValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class NpcDataValidationIssue
    {
        public NpcDataValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string Context { get; }

        public NpcDataValidationIssue(
            NpcDataValidationSeverity severity,
            string code,
            string message,
            string context = "")
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Context = context ?? string.Empty;
        }

        public string ToLogLine()
        {
            return string.IsNullOrWhiteSpace(Context)
                ? $"[{Severity}] {Code}: {Message}"
                : $"[{Severity}] {Code}: {Message} ({Context})";
        }
    }

    public sealed class NpcDataValidationReport
    {
        private readonly List<NpcDataValidationIssue> _issues;

        public NpcDataValidationReport(IEnumerable<NpcDataValidationIssue> issues)
        {
            _issues = issues?.ToList() ?? new List<NpcDataValidationIssue>();
        }

        public IReadOnlyList<NpcDataValidationIssue> Issues => _issues;
        public int ErrorCount => _issues.Count(issue => issue.Severity == NpcDataValidationSeverity.Error);
        public int WarningCount => _issues.Count(issue => issue.Severity == NpcDataValidationSeverity.Warning);
        public int InfoCount => _issues.Count(issue => issue.Severity == NpcDataValidationSeverity.Info);
        public bool HasErrors => ErrorCount > 0;

        public string BuildSummary(int maxIssueLines = 80)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("NPC Data Validation");
            builder.Append("Errors: ");
            builder.Append(ErrorCount);
            builder.Append("   Warnings: ");
            builder.Append(WarningCount);
            builder.Append("   Infos: ");
            builder.AppendLine(InfoCount.ToString());

            if (_issues.Count == 0)
            {
                builder.Append("No NPC data validation issues found.");
                return builder.ToString();
            }

            int visibleCount = Mathf.Max(1, maxIssueLines);
            for (int i = 0; i < _issues.Count && i < visibleCount; i++)
                builder.AppendLine(_issues[i].ToLogLine());

            if (_issues.Count > visibleCount)
            {
                builder.Append("... ");
                builder.Append(_issues.Count - visibleCount);
                builder.AppendLine(" more issue(s).");
            }

            return builder.ToString();
        }

        public void LogToUnityConsole()
        {
            if (_issues.Count == 0)
            {
                Debug.Log(BuildSummary());
                return;
            }

            for (int i = 0; i < _issues.Count; i++)
            {
                string line = $"NPC data validation: {_issues[i].ToLogLine()}";
                switch (_issues[i].Severity)
                {
                    case NpcDataValidationSeverity.Error:
                        Debug.LogError(line);
                        break;
                    case NpcDataValidationSeverity.Warning:
                        Debug.LogWarning(line);
                        break;
                    default:
                        Debug.Log(line);
                        break;
                }
            }
        }
    }

    public static class NpcDataValidator
    {
        private static readonly string[] RequiredResultGroups =
        {
            "Result_Correct",
            "Result_Wrong",
            "Result_Disgusting"
        };

        private static readonly string[] OptionalResultGroups =
        {
            "Result_Perfect",
            "Result_Similar"
        };

        public static NpcDataValidationReport Validate(
            NpcConversationDatabase database,
            IEnumerable<NpcDataValidationIssue> loadIssues = null)
        {
            List<NpcDataValidationIssue> issues = new List<NpcDataValidationIssue>();
            if (loadIssues != null)
                issues.AddRange(loadIssues);

            if (database == null)
            {
                AddError(issues, "DatabaseMissing", "NPC conversation database is null.");
                return new NpcDataValidationReport(issues);
            }

            ValidateNpcReferences(database, issues);
            ValidateQuestionCategories(database, issues);
            ValidateRegionPools(database, issues);
            ValidateRequiredEventIds(database, issues);
            ValidateSequenceEvents(database, issues);
            ValidateRequestEvents(database, issues);
            ValidateRequestStateRules(database, issues);
            ValidateRequestFlowAuthoring(database, issues);
            ValidateDialogueReferences(database, issues);
            ValidateDialogueLines(database, issues);
            ValidateStartDialogueGroups(database, issues);
            ValidateQuestionDialogueGroups(database, issues);
            ValidateResultDialogueGroups(database, issues);
            ValidateOrderContracts(database, issues);
            ValidateNonCookingEventContracts(database, issues);
            ValidateRegionEventCoverage(database, issues);

            return new NpcDataValidationReport(issues);
        }

        private static void ValidateNpcReferences(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                if (string.IsNullOrWhiteSpace(visitEvent.NpcId))
                {
                    AddError(issues, "VisitEventNpcIdEmpty", "Visit event has no NPC ID.", $"event={visitEvent.EventId}");
                    continue;
                }

                if (database.Npcs.ContainsKey(visitEvent.NpcId) == false)
                {
                    AddError(
                        issues,
                        "VisitEventNpcMissing",
                        "Visit event references an NPC that does not exist.",
                        $"event={visitEvent.EventId}, npc={visitEvent.NpcId}");
                }
            }
        }

        private static void ValidateQuestionCategories(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (QuestionCategoryData category in database.QuestionCategories.Values)
            {
                if (string.IsNullOrWhiteSpace(category.DialogueGroup))
                {
                    AddWarning(
                        issues,
                        "QuestionDialogueGroupEmpty",
                        "Question category has no dialogue group.",
                        $"category={category.CategoryId}");
                }
            }
        }

        private static void ValidateRegionPools(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (RegionPoolEntryData entry in database.RegionPoolEntries)
            {
                if (database.Npcs.ContainsKey(entry.NpcId) == false)
                {
                    AddError(
                        issues,
                        "RegionPoolNpcMissing",
                        "Region pool references an NPC that does not exist.",
                        $"region={entry.RegionId}, npc={entry.NpcId}");
                }

                if (entry.Weight <= 0)
                {
                    AddWarning(
                        issues,
                        "RegionPoolWeightInvalid",
                        "Region pool weight should be greater than 0.",
                        $"region={entry.RegionId}, npc={entry.NpcId}, weight={entry.Weight}");
                }
            }

            foreach (IGrouping<string, RegionPoolEntryData> duplicateGroup in database.RegionPoolEntries
                         .GroupBy(entry => $"{entry.RegionId}|{entry.NpcId}", StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                RegionPoolEntryData first = duplicateGroup.First();
                AddWarning(
                    issues,
                    "RegionPoolDuplicateNpc",
                    "The same NPC appears more than once in the same region pool.",
                    $"region={first.RegionId}, npc={first.NpcId}, count={duplicateGroup.Count()}");
            }
        }

        private static void ValidateRequiredEventIds(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                foreach (string requiredEventId in visitEvent.RequiredEventIds)
                {
                    if (string.IsNullOrWhiteSpace(requiredEventId))
                        continue;

                    if (string.Equals(visitEvent.EventId, requiredEventId, StringComparison.OrdinalIgnoreCase))
                    {
                        AddError(
                            issues,
                            "RequiredEventSelfReference",
                            "Visit event requires itself.",
                            $"event={visitEvent.EventId}");
                        continue;
                    }

                    if (database.VisitEvents.ContainsKey(requiredEventId))
                        continue;

                    AddError(
                        issues,
                        "RequiredEventMissing",
                        "Visit event requires an event that does not exist.",
                        $"event={visitEvent.EventId}, required={requiredEventId}");
                }
            }
        }

        private static void ValidateSequenceEvents(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            List<VisitEventData> sequenceEvents = database.VisitEvents.Values
                .Where(IsSequenceEvent)
                .ToList();

            foreach (VisitEventData visitEvent in sequenceEvents)
            {
                if (visitEvent.EventType != VisitEventType.Sequence)
                {
                    AddWarning(
                        issues,
                        "SequenceTypeMismatch",
                        "Event has SequenceGroup but EventType is not Sequence.",
                        $"event={visitEvent.EventId}, eventType={visitEvent.EventType}, sequenceGroup={visitEvent.SequenceGroup}");
                }

                if (visitEvent.RepeatMode != VisitEventRepeatMode.Once)
                {
                    AddWarning(
                        issues,
                        "SequenceRepeatMode",
                        "Sequence event should use RepeatMode Once.",
                        $"event={visitEvent.EventId}, repeatMode={visitEvent.RepeatMode}");
                }

                if (string.IsNullOrWhiteSpace(visitEvent.SequenceGroup))
                {
                    AddWarning(
                        issues,
                        "SequenceGroupEmpty",
                        "Sequence event needs SequenceGroup.",
                        $"event={visitEvent.EventId}");
                }

                if (visitEvent.SequenceIndex <= 0)
                {
                    AddWarning(
                        issues,
                        "SequenceIndexInvalid",
                        "Sequence event needs SequenceIndex greater than 0.",
                        $"event={visitEvent.EventId}");
                }
            }

            foreach (IGrouping<string, VisitEventData> group in sequenceEvents
                         .Where(visitEvent => string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false)
                         .GroupBy(visitEvent => visitEvent.SequenceGroup, StringComparer.OrdinalIgnoreCase))
            {
                ValidateSequenceGroup(group.Key, group.ToList(), issues);
            }
        }

        private static void ValidateSequenceGroup(
            string sequenceGroup,
            List<VisitEventData> events,
            List<NpcDataValidationIssue> issues)
        {
            foreach (IGrouping<int, VisitEventData> indexGroup in events.GroupBy(visitEvent => visitEvent.SequenceIndex))
            {
                if (indexGroup.Key <= 0 || indexGroup.Count() <= 1)
                    continue;

                string eventIds = string.Join("|", indexGroup.Select(visitEvent => visitEvent.EventId));
                AddWarning(
                    issues,
                    "SequenceDuplicateIndex",
                    "Duplicate SequenceIndex in group.",
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

                AddWarning(
                    issues,
                    "SequenceIndexGap",
                    "Sequence index gap detected.",
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

                AddWarning(
                    issues,
                    "SequenceVisitDelay",
                    "Sequence visit requirement may delay the next chain step.",
                    $"sequenceGroup={sequenceGroup}, previous={previousEvent.EventId}, current={currentEvent.EventId}, requiredNpcVisits={currentEvent.RequiredNpcVisits}, expectedMax={expectedMaximumVisitRequirement}");
            }
        }

        private static void ValidateRequestEvents(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            HashSet<string> npcIdsWithRequestEvent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                if (visitEvent.EventType != VisitEventType.Request)
                    continue;

                npcIdsWithRequestEvent.Add(visitEvent.NpcId);

                if (database.Npcs.TryGetValue(visitEvent.NpcId, out NpcData npc) == false)
                    continue;

                if (npc.RequestAvailable == false)
                {
                    AddWarning(
                        issues,
                        "RequestEventForUnavailableNpc",
                        "Request event exists for an NPC without request availability.",
                        $"event={visitEvent.EventId}, npc={visitEvent.NpcId}");
                }

                if (visitEvent.RepeatMode != VisitEventRepeatMode.Once)
                {
                    AddWarning(
                        issues,
                        "RequestRepeatMode",
                        "Request event should use RepeatMode Once.",
                        $"event={visitEvent.EventId}, repeatMode={visitEvent.RepeatMode}");
                }

                if (string.Equals(npc.RequestUnlockEvent, visitEvent.EventId, StringComparison.OrdinalIgnoreCase))
                {
                    AddError(
                        issues,
                        "RequestUnlockSelfReference",
                        "NPC request unlock event cannot be the Request event itself.",
                        $"npc={npc.NpcId}, requestEvent={visitEvent.EventId}");
                }
            }

            foreach (NpcData npc in database.Npcs.Values)
            {
                if (npc.RequestAvailable == false)
                    continue;

                if (string.IsNullOrWhiteSpace(npc.RequestUnlockEvent) == false
                    && database.VisitEvents.ContainsKey(npc.RequestUnlockEvent) == false)
                {
                    AddError(
                        issues,
                        "RequestUnlockEventMissing",
                        "NPC request unlock event does not exist.",
                        $"npc={npc.NpcId}, requestUnlockEvent={npc.RequestUnlockEvent}");
                }

                if (npcIdsWithRequestEvent.Contains(npc.NpcId))
                    continue;

                AddWarning(
                    issues,
                    "RequestEventMissing",
                    "NPC has request availability but no Request visit event.",
                    $"npc={npc.NpcId}");
            }
        }

        private static void ValidateRequestFlowAuthoring(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (NpcData npc in database.Npcs.Values)
            {
                if (npc.RequestAvailable == false)
                    continue;

                List<VisitEventData> npcEvents = database.VisitEvents.Values
                    .Where(visitEvent => string.Equals(visitEvent.NpcId, npc.NpcId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                List<VisitEventData> requestEvents = npcEvents
                    .Where(visitEvent => visitEvent.EventType == VisitEventType.Request)
                    .ToList();

                if (requestEvents.Count == 0)
                    continue;

                foreach (VisitEventData requestEvent in requestEvents)
                    ValidateRequestOfferAuthoringRules(issues, requestEvent);

                if (requestEvents.Any(TransitionsRequestStateAfterSuccessToReady) == false)
                {
                    AddWarning(
                        issues,
                        "RequestSuccessTransitionMissing",
                        "Request event should advance to ReadyToComplete after a successful result.",
                        $"npc={npc.NpcId}, requestEvents={JoinEventIds(requestEvents)}");
                }

                if (npcEvents.Any(IsRequestCompletionEvent) == false)
                {
                    AddWarning(
                        issues,
                        "RequestCompletionEventMissing",
                        "NPC request flow has no completion event from ReadyToComplete to Completed.",
                        $"npc={npc.NpcId}");
                }

                if (npcEvents.Any(IsRequestEpilogueEvent) == false)
                {
                    AddInfo(
                        issues,
                        "RequestEpilogueEventMissing",
                        "NPC request flow has no epilogue event from EpilogueAvailable to EpilogueCompleted.",
                        $"npc={npc.NpcId}");
                }
            }
        }

        private static void ValidateRequestOfferAuthoringRules(
            List<NpcDataValidationIssue> issues,
            VisitEventData requestEvent)
        {
            if (HasRequestState(requestEvent.RequiredRequestState, NpcRequestState.Unlocked) == false)
            {
                AddWarning(
                    issues,
                    "RequestOfferRequiredState",
                    "Request offer event usually needs RequiredRequestState=Unlocked.",
                    $"event={requestEvent.EventId}, value={requestEvent.RequiredRequestState}");
            }

            if (HasRequestState(requestEvent.BlockedAtRequestState, NpcRequestState.Offered) == false)
            {
                AddWarning(
                    issues,
                    "RequestOfferBlockedState",
                    "Request offer event usually needs BlockedAtRequestState=Offered to prevent replay.",
                    $"event={requestEvent.EventId}, value={requestEvent.BlockedAtRequestState}");
            }

            if (HasRequestState(requestEvent.RequestStateAfterEncounter, NpcRequestState.Offered) == false)
            {
                AddWarning(
                    issues,
                    "RequestOfferAfterEncounterState",
                    "Request offer event usually needs RequestStateAfterEncounter=Offered.",
                    $"event={requestEvent.EventId}, value={requestEvent.RequestStateAfterEncounter}");
            }
        }

        private static bool TransitionsRequestStateAfterSuccessToReady(VisitEventData visitEvent)
        {
            return HasRequestState(visitEvent.RequestStateAfterSuccessResult, NpcRequestState.ReadyToComplete);
        }

        private static bool IsRequestCompletionEvent(VisitEventData visitEvent)
        {
            return HasRequestState(visitEvent.RequiredRequestState, NpcRequestState.ReadyToComplete)
                   && HasRequestState(visitEvent.RequestStateAfterEncounter, NpcRequestState.Completed);
        }

        private static bool IsRequestEpilogueEvent(VisitEventData visitEvent)
        {
            return HasRequestState(visitEvent.RequiredRequestState, NpcRequestState.EpilogueAvailable)
                   && HasRequestState(visitEvent.RequestStateAfterEncounter, NpcRequestState.EpilogueCompleted);
        }

        private static bool HasRequestState(string value, NpcRequestState expectedState)
        {
            return NpcRequestStateUtility.TryParse(value, out NpcRequestState actualState)
                   && actualState == expectedState;
        }

        private static string JoinEventIds(IEnumerable<VisitEventData> visitEvents)
        {
            return string.Join("|", visitEvents.Select(visitEvent => visitEvent.EventId));
        }

        private static void ValidateDialogueReferences(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (string dialogueEventId in database.DialogueEventIds)
            {
                if (database.VisitEvents.ContainsKey(dialogueEventId))
                    continue;

                AddError(
                    issues,
                    "DialogueEventMissing",
                    "Dialogue line references a visit event that does not exist.",
                    $"event={dialogueEventId}");
            }
        }

        private static void ValidateDialogueLines(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (string eventId in database.DialogueEventIds)
            {
                foreach (string group in database.GetDialogueGroups(eventId))
                {
                    IReadOnlyList<DialogueLineData> lines = database.GetDialogueLines(eventId, group);
                    ValidateDialogueLineGroup(database, issues, eventId, group, lines);
                }
            }
        }

        private static void ValidateDialogueLineGroup(
            NpcConversationDatabase database,
            List<NpcDataValidationIssue> issues,
            string eventId,
            string group,
            IReadOnlyList<DialogueLineData> lines)
        {
            foreach (DialogueLineData line in lines)
            {
                string context =
                    $"event={eventId}, group={group}, order={line.LineOrder}, speaker={ValueOrNone(line.Speaker)}";

                if (line.LineOrder <= 0)
                {
                    AddError(
                        issues,
                        "DialogueLineOrderInvalid",
                        "Dialogue line order should be greater than 0.",
                        context);
                }

                if (string.IsNullOrWhiteSpace(line.Speaker))
                {
                    AddError(
                        issues,
                        "DialogueSpeakerEmpty",
                        "Dialogue line has no speaker.",
                        context);
                }
                else if (line.IsPlayer == false && database.Npcs.ContainsKey(line.Speaker) == false)
                {
                    AddError(
                        issues,
                        "DialogueSpeakerMissing",
                        "Dialogue line references an NPC speaker that does not exist.",
                        context);
                }

                if (string.IsNullOrWhiteSpace(line.Text))
                {
                    AddWarning(
                        issues,
                        "DialogueTextEmpty",
                        "Dialogue line has no text.",
                        context);
                }

                if (CountOccurrences(line.Text, "**") % 2 != 0)
                {
                    AddError(
                        issues,
                        "DialogueBoldMarkerUnbalanced",
                        "Dialogue line has an unmatched bold marker '**'.",
                        context);
                }
            }

            foreach (IGrouping<int, DialogueLineData> duplicateGroup in lines
                         .Where(line => line.LineOrder > 0)
                         .GroupBy(line => line.LineOrder)
                         .Where(lineGroup => lineGroup.Count() > 1))
            {
                AddError(
                    issues,
                    "DialogueLineOrderDuplicate",
                    "Dialogue line order is duplicated in the same event/group.",
                    $"event={eventId}, group={group}, order={duplicateGroup.Key}, count={duplicateGroup.Count()}");
            }

            List<int> orders = lines
                .Where(line => line.LineOrder > 0)
                .Select(line => line.LineOrder)
                .Distinct()
                .OrderBy(order => order)
                .ToList();

            for (int i = 0; i < orders.Count; i++)
            {
                int expectedOrder = i + 1;
                if (orders[i] == expectedOrder)
                    continue;

                AddWarning(
                    issues,
                    "DialogueLineOrderGap",
                    "Dialogue line order has a gap. Runtime will still sort by order, but authoring may be confusing.",
                    $"event={eventId}, group={group}, expected={expectedOrder}, actual={orders[i]}");
                break;
            }
        }

        private static void ValidateStartDialogueGroups(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                if (visitEvent.StartGroups.Count == 0)
                {
                    AddWarning(
                        issues,
                        "StartGroupsEmpty",
                        "Visit event has no start dialogue groups.",
                        $"event={visitEvent.EventId}");
                    continue;
                }

                foreach (string group in visitEvent.StartGroups)
                {
                    if (database.HasDialogueGroup(visitEvent.EventId, group))
                        continue;

                    AddWarning(
                        issues,
                        "StartDialogueGroupMissing",
                        "Start dialogue group not found.",
                        $"event={visitEvent.EventId}, group={group}");
                }
            }
        }

        private static void ValidateQuestionDialogueGroups(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                if (visitEvent.QuestionLimit <= 0)
                    continue;

                if (visitEvent.AvailableQuestionCategories.Count == 0)
                {
                    AddWarning(
                        issues,
                        "QuestionCategoriesEmpty",
                        "Event has QuestionLimit but no question categories.",
                        $"event={visitEvent.EventId}");
                    continue;
                }

                if (visitEvent.QuestionLimit > visitEvent.AvailableQuestionCategories.Count)
                {
                    AddWarning(
                        issues,
                        "QuestionLimitExceedsCategories",
                        "QuestionLimit is greater than available question category count.",
                        $"event={visitEvent.EventId}, questionLimit={visitEvent.QuestionLimit}, categories={visitEvent.AvailableQuestionCategories.Count}");
                }

                foreach (string categoryId in visitEvent.AvailableQuestionCategories)
                {
                    if (database.QuestionCategories.TryGetValue(categoryId, out QuestionCategoryData category) == false)
                    {
                        AddError(
                            issues,
                            "QuestionCategoryMissing",
                            "Question category not found.",
                            $"event={visitEvent.EventId}, category={categoryId}");
                        continue;
                    }

                    if (database.HasDialogueGroup(visitEvent.EventId, category.DialogueGroup))
                        continue;

                    AddWarning(
                        issues,
                        "QuestionDialogueGroupMissing",
                        "Question dialogue group not found.",
                        $"event={visitEvent.EventId}, category={categoryId}, group={category.DialogueGroup}");
                }
            }
        }

        private static void ValidateResultDialogueGroups(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                if (NpcVisitEventRules.RequiresCookingStep(visitEvent) == false)
                    continue;

                bool hasAnyResultGroup = database.GetDialogueGroups(visitEvent.EventId)
                    .Any(group => group.StartsWith("Result_", StringComparison.OrdinalIgnoreCase));

                if (hasAnyResultGroup == false)
                {
                    AddWarning(
                        issues,
                        "ResultDialogueGroupsMissing",
                        "Visit event has no result dialogue groups.",
                        $"event={visitEvent.EventId}");
                    continue;
                }

                foreach (string group in RequiredResultGroups)
                {
                    if (database.HasDialogueGroup(visitEvent.EventId, group))
                        continue;

                    AddWarning(
                        issues,
                        "RequiredResultDialogueGroupMissing",
                        "Common result dialogue group not found.",
                        $"event={visitEvent.EventId}, group={group}");
                }

                foreach (string group in OptionalResultGroups)
                {
                    if (database.HasDialogueGroup(visitEvent.EventId, group))
                        continue;

                    AddInfo(
                        issues,
                        "OptionalResultDialogueGroupMissing",
                        "Optional result dialogue group not found.",
                        $"event={visitEvent.EventId}, group={group}");
                }
            }
        }

        private static void ValidateRegionEventCoverage(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (RegionPoolEntryData entry in database.RegionPoolEntries)
            {
                if (database.Npcs.ContainsKey(entry.NpcId) == false)
                    continue;

                bool hasMatchingEvent = database.VisitEvents.Values.Any(visitEvent =>
                    string.Equals(visitEvent.NpcId, entry.NpcId, StringComparison.OrdinalIgnoreCase)
                    && IsRegionMatched(visitEvent.RegionId, entry.RegionId));

                if (hasMatchingEvent)
                    continue;

                AddWarning(
                    issues,
                    "RegionPoolNpcHasNoEvents",
                    "NPC is in a region pool but has no visit event for that region.",
                    $"region={entry.RegionId}, npc={entry.NpcId}");
            }
        }

        private static void ValidateOrderContracts(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                if (NpcVisitEventRules.RequiresCookingStep(visitEvent) == false)
                    continue;

                if (string.IsNullOrWhiteSpace(visitEvent.CorrectRecipeId))
                {
                    AddWarning(
                        issues,
                        "OrderRecipeMissing",
                        "Visit event has no CorrectRecipeId. Recipe matching cannot produce Perfect.",
                        $"event={visitEvent.EventId}");
                }

                if (visitEvent.AllowedFoodTypes.Count == 0)
                {
                    AddWarning(
                        issues,
                        "OrderFoodTypesEmpty",
                        "Visit event has no AllowedFoodTypes. Any submitted food type can match.",
                        $"event={visitEvent.EventId}");
                }

                if (visitEvent.RequiredTags.Count == 0)
                {
                    AddWarning(
                        issues,
                        "OrderRequiredTagsEmpty",
                        "Visit event has no RequiredTags. Tag matching may be too loose.",
                        $"event={visitEvent.EventId}");
                }

                ValidateDuplicateValues(issues, visitEvent.EventId, "AllowedFoodTypes", visitEvent.AllowedFoodTypes);
                ValidateDuplicateValues(issues, visitEvent.EventId, "RequiredTags", visitEvent.RequiredTags);
                ValidateDuplicateValues(issues, visitEvent.EventId, "PreferredTags", visitEvent.PreferredTags);
                ValidateDuplicateValues(issues, visitEvent.EventId, "AvoidTags", visitEvent.AvoidTags);
                ValidateDuplicateValues(issues, visitEvent.EventId, "DisgustingTags", visitEvent.DisgustingTags);

                ValidateOverlappingValues(
                    issues,
                    visitEvent.EventId,
                    "RequiredTags",
                    visitEvent.RequiredTags,
                    "AvoidTags",
                    visitEvent.AvoidTags);
                ValidateOverlappingValues(
                    issues,
                    visitEvent.EventId,
                    "RequiredTags",
                    visitEvent.RequiredTags,
                    "DisgustingTags",
                    visitEvent.DisgustingTags);
                ValidateOverlappingValues(
                    issues,
                    visitEvent.EventId,
                    "PreferredTags",
                    visitEvent.PreferredTags,
                    "AvoidTags",
                    visitEvent.AvoidTags);
                ValidateOverlappingValues(
                    issues,
                    visitEvent.EventId,
                    "PreferredTags",
                    visitEvent.PreferredTags,
                    "DisgustingTags",
                    visitEvent.DisgustingTags);
                ValidateOverlappingValues(
                    issues,
                    visitEvent.EventId,
                    "AvoidTags",
                    visitEvent.AvoidTags,
                    "DisgustingTags",
                    visitEvent.DisgustingTags);
            }
        }

        private static void ValidateNonCookingEventContracts(
            NpcConversationDatabase database,
            List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                if (NpcVisitEventRules.RequiresCookingStep(visitEvent))
                    continue;

                bool hasOrderData =
                    string.IsNullOrWhiteSpace(visitEvent.CorrectRecipeId) == false
                    || visitEvent.AllowedFoodTypes.Count > 0
                    || visitEvent.RequiredTags.Count > 0
                    || visitEvent.PreferredTags.Count > 0
                    || visitEvent.AvoidTags.Count > 0
                    || visitEvent.DisgustingTags.Count > 0;

                if (hasOrderData == false)
                    continue;

                AddInfo(
                    issues,
                    "NonCookingEventHasOrderData",
                    "Event does not require cooking, so order contract data will be ignored.",
                    $"event={visitEvent.EventId}, eventType={visitEvent.EventType}");
            }
        }

        private static void ValidateRequestStateRules(NpcConversationDatabase database, List<NpcDataValidationIssue> issues)
        {
            foreach (VisitEventData visitEvent in database.VisitEvents.Values)
            {
                bool hasRequired = ValidateRequestStateValue(
                    issues,
                    visitEvent,
                    "RequiredRequestState",
                    visitEvent.RequiredRequestState,
                    allowLocked: true,
                    out NpcRequestState requiredState);
                bool hasBlocked = ValidateRequestStateValue(
                    issues,
                    visitEvent,
                    "BlockedAtRequestState",
                    visitEvent.BlockedAtRequestState,
                    allowLocked: false,
                    out NpcRequestState blockedState);
                ValidateRequestStateValue(
                    issues,
                    visitEvent,
                    "RequestStateAfterEncounter",
                    visitEvent.RequestStateAfterEncounter,
                    allowLocked: false,
                    out _);
                bool hasSuccessState = ValidateRequestStateValue(
                    issues,
                    visitEvent,
                    "RequestStateAfterSuccessResult",
                    visitEvent.RequestStateAfterSuccessResult,
                    allowLocked: false,
                    out _);

                ValidateRequestSuccessResults(issues, visitEvent, hasSuccessState);

                if (hasRequired
                    && hasBlocked
                    && NpcRequestStateUtility.GetRank(requiredState) >= NpcRequestStateUtility.GetRank(blockedState))
                {
                    AddError(
                        issues,
                        "RequestStateRangeEmpty",
                        "RequiredRequestState and BlockedAtRequestState leave no selectable state.",
                        $"event={visitEvent.EventId}, required={requiredState}, blockedAt={blockedState}");
                }
            }
        }

        private static bool ValidateRequestStateValue(
            List<NpcDataValidationIssue> issues,
            VisitEventData visitEvent,
            string columnName,
            string value,
            bool allowLocked,
            out NpcRequestState state)
        {
            state = NpcRequestState.Locked;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (NpcRequestStateUtility.TryParse(value, out state) == false)
            {
                AddError(
                    issues,
                    "RequestStateInvalid",
                    "Visit event references an unknown NPC request state.",
                    $"event={visitEvent.EventId}, column={columnName}, value={value}");
                return false;
            }

            if (allowLocked == false && state == NpcRequestState.Locked)
            {
                AddError(
                    issues,
                    "RequestStateLockedInvalid",
                    "This request state column cannot use Locked.",
                    $"event={visitEvent.EventId}, column={columnName}");
                return false;
            }

            return true;
        }

        private static void ValidateRequestSuccessResults(
            List<NpcDataValidationIssue> issues,
            VisitEventData visitEvent,
            bool hasSuccessState)
        {
            if (visitEvent.RequestSuccessResults.Count > 0 && hasSuccessState == false)
            {
                AddWarning(
                    issues,
                    "RequestSuccessResultsUnused",
                    "RequestSuccessResults is set but RequestStateAfterSuccessResult is empty.",
                    $"event={visitEvent.EventId}");
            }

            foreach (string resultName in visitEvent.RequestSuccessResults)
            {
                if (Enum.TryParse(resultName, true, out NpcConversationResult _))
                    continue;

                AddError(
                    issues,
                    "RequestSuccessResultInvalid",
                    "Visit event references an unknown NPC conversation result.",
                    $"event={visitEvent.EventId}, result={resultName}");
            }
        }

        private static void ValidateDuplicateValues(
            List<NpcDataValidationIssue> issues,
            string eventId,
            string listName,
            IReadOnlyList<string> values)
        {
            List<string> duplicates = values
                .Where(value => string.IsNullOrWhiteSpace(value) == false)
                .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicates.Count == 0)
                return;

            AddWarning(
                issues,
                "OrderValueDuplicate",
                "Visit event has duplicated order contract values.",
                $"event={eventId}, list={listName}, values={JoinValues(duplicates)}");
        }

        private static void ValidateOverlappingValues(
            List<NpcDataValidationIssue> issues,
            string eventId,
            string leftName,
            IReadOnlyList<string> leftValues,
            string rightName,
            IReadOnlyList<string> rightValues)
        {
            List<string> overlaps = leftValues
                .Where(value => string.IsNullOrWhiteSpace(value) == false)
                .Select(value => value.Trim())
                .Intersect(
                    rightValues
                        .Where(value => string.IsNullOrWhiteSpace(value) == false)
                        .Select(value => value.Trim()),
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (overlaps.Count == 0)
                return;

            AddWarning(
                issues,
                "OrderValueOverlap",
                "Visit event has overlapping order contract values.",
                $"event={eventId}, left={leftName}, right={rightName}, values={JoinValues(overlaps)}");
        }

        private static bool IsSequenceEvent(VisitEventData visitEvent)
        {
            return visitEvent.EventType == VisitEventType.Sequence
                   || string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false;
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

        private static void AddInfo(
            List<NpcDataValidationIssue> issues,
            string code,
            string message,
            string context = "")
        {
            issues.Add(new NpcDataValidationIssue(NpcDataValidationSeverity.Info, code, message, context));
        }

        private static void AddWarning(
            List<NpcDataValidationIssue> issues,
            string code,
            string message,
            string context = "")
        {
            issues.Add(new NpcDataValidationIssue(NpcDataValidationSeverity.Warning, code, message, context));
        }

        private static void AddError(
            List<NpcDataValidationIssue> issues,
            string code,
            string message,
            string context = "")
        {
            issues.Add(new NpcDataValidationIssue(NpcDataValidationSeverity.Error, code, message, context));
        }

        private static string JoinValues(IReadOnlyList<string> values)
        {
            return values != null && values.Count > 0 ? string.Join("|", values) : "None";
        }

        private static int CountOccurrences(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return 0;

            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }
}
