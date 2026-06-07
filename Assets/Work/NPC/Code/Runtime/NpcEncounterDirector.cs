using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Work.NPC.Code.Data;
using Random = UnityEngine.Random;

namespace Work.NPC.Code.Runtime
{
    [Serializable]
    public sealed class NpcAffinityChangeSummaryEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public sealed class NpcRequestUnlockSummaryEvent : UnityEvent<string>
    {
    }

    public sealed class NpcEncounterDirector : MonoBehaviour
    {
        [SerializeField] private NpcConversationRunner runner;
        [SerializeField] private string resourceFolder = "NPCData";
        [SerializeField] private string regionId = "MossCave";
        [SerializeField] private bool playOnStart;
        [SerializeField, Min(1)] private int currentDay = 1;
        [SerializeField] private bool persistHistory = true;
        [SerializeField] private string historySaveKey = "DungeonDinner.NpcEncounterHistory";
        [SerializeField] private bool continueDayFromHistoryOnLoad = true;
        [SerializeField] private bool ignoreNpcCooldownWhenPoolIsEmpty;
        [SerializeField, Min(1)] private int maxEncountersPerDay = 3;
        [SerializeField] private bool blockSameNpcOncePerDay = true;
        [SerializeField] private bool avoidImmediateRepeat = true;
        [SerializeField, Min(0)] private int recentNpcRepeatBlockCount = 2;
        [SerializeField, Min(0)] private int recentEventRepeatBlockCount = 3;

        [Header("Events")]
        [SerializeField] private NpcAffinityChangeSummaryEvent affinityChanged = new NpcAffinityChangeSummaryEvent();
        [SerializeField] private NpcAffinityChangeSummaryEvent affinityLevelChanged = new NpcAffinityChangeSummaryEvent();
        [SerializeField] private NpcRequestUnlockSummaryEvent requestUnlocked = new NpcRequestUnlockSummaryEvent();

        private NpcConversationDatabase _database;
        private NpcEncounterHistory _history;
        private int _activeEncounterDay = 1;
        private string _activeEventId;
        private int _sessionDay;
        private string _sessionRegionId;
        private int _encountersStartedToday;
        private string _lastAffinityChangeSummary;
        private string _lastRequestUnlockSummary;

        public event Action<NpcAffinityChangeContext> AffinityChanged;
        public event Action<NpcAffinityChangeContext> AffinityLevelChanged;
        public event Action<NpcRequestUnlockContext> RequestUnlocked;

        public string RegionId => regionId;
        public int CurrentDay => currentDay;
        public DateTime CurrentDate => NpcImperialCalendar.ToDate(currentDay);
        public string CurrentDateText => NpcImperialCalendar.FormatDayIndex(currentDay);
        public int MaxEncountersPerDay => Mathf.Max(1, maxEncountersPerDay);
        public int EncountersStartedToday
        {
            get
            {
                SyncDailySessionState();
                return _encountersStartedToday;
            }
        }
        public int RemainingEncountersToday => Mathf.Max(0, MaxEncountersPerDay - EncountersStartedToday);
        public bool IsBusinessDayComplete => RemainingEncountersToday <= 0;
        public string LastAffinityChangeSummary => string.IsNullOrWhiteSpace(_lastAffinityChangeSummary)
            ? "None"
            : _lastAffinityChangeSummary;
        public string LastRequestUnlockSummary => string.IsNullOrWhiteSpace(_lastRequestUnlockSummary)
            ? "None"
            : _lastRequestUnlockSummary;

        private void Awake()
        {
            currentDay = Mathf.Max(1, currentDay);

            if (runner == null)
                runner = FindFirstObjectByType<NpcConversationRunner>();

            _database = NpcConversationDatabase.LoadFromResources(resourceFolder);
            _history = persistHistory
                ? NpcEncounterHistory.Load(historySaveKey)
                : NpcEncounterHistory.CreateUnsaved();

            SyncCurrentDayFromHistory();

            if (runner != null)
            {
                runner.ResultDialogueStarted += HandleResultDialogueStarted;
                runner.ConversationCompleted += HandleConversationCompleted;
            }
        }

        private void OnDestroy()
        {
            if (runner != null)
            {
                runner.ResultDialogueStarted -= HandleResultDialogueStarted;
                runner.ConversationCompleted -= HandleConversationCompleted;
            }
        }

        private void Start()
        {
            if (playOnStart)
                StartEncounter();
        }

        public void SetRegion(string newRegionId)
        {
            regionId = newRegionId;
            SyncDailySessionState();
        }

        public void SetCurrentDay(int newCurrentDay)
        {
            currentDay = Mathf.Max(1, newCurrentDay);
            SyncDailySessionState();
        }

        public bool TrySetCurrentImperialDate(int year, int month, int day)
        {
            if (NpcImperialCalendar.TryToDayIndex(year, month, day, out int dayIndex) == false)
            {
                Debug.LogWarning(
                    $"Invalid NPC imperial date. date={NpcImperialCalendar.EraName} {year}년 {month}월 {day}일, " +
                    $"campaignStart={NpcImperialCalendar.FormatDayIndex(1)}");
                return false;
            }

            SetCurrentDay(dayIndex);
            return true;
        }

        public void AdvanceDay()
        {
            currentDay++;
            SyncDailySessionState();
        }

        public void ClearEncounterHistory()
        {
            _history?.Clear();
            SyncDailySessionState(true);
        }

        public void LogEncounterHistory()
        {
            Debug.Log(GetEncounterHistorySummary());
        }

        public string GetEncounterHistorySummary()
        {
            return _history?.BuildDebugSummary() ?? "NPC encounter history is not loaded.";
        }

        public string GetBusinessDaySummary()
        {
            SyncDailySessionState();
            return $"{CurrentDateText} / {regionId} / 손님 {EncountersStartedToday}/{MaxEncountersPerDay}";
        }

        public string GetLastAffinityChangeSummary()
        {
            return LastAffinityChangeSummary;
        }

        public string GetLastRequestUnlockSummary()
        {
            return LastRequestUnlockSummary;
        }

        public bool IsNpcRequestUnlocked(string npcId)
        {
            return _history != null && _history.IsNpcRequestUnlocked(npcId);
        }

        public string GetCurrentNpcProgressSummary()
        {
            string npcId = runner != null && string.IsNullOrWhiteSpace(runner.CurrentNpcId) == false
                ? runner.CurrentNpcId
                : _history?.LastNpcId;

            return GetNpcProgressSummary(npcId);
        }

        public string GetNpcProgressSummary(string npcId)
        {
            if (_database == null || _history == null)
                return "NPC data is not loaded.";

            if (string.IsNullOrWhiteSpace(npcId))
                return "No active NPC.";

            int affinity = _history.GetNpcAffinity(npcId);
            int visits = _history.GetNpcVisitCount(npcId);
            int correctCount = _history.GetNpcCorrectCount(npcId);
            string lastResult = _history.GetNpcLastResult(npcId);

            StringBuilder builder = new StringBuilder();
            builder.Append("Relation: ");
            builder.Append(NpcAffinityUtility.BuildProgressText(affinity));
            builder.AppendLine();
            builder.Append("Visits: ");
            builder.Append(visits);
            builder.Append("   Correct+: ");
            builder.Append(correctCount);
            builder.Append("   Last: ");
            builder.AppendLine(string.IsNullOrWhiteSpace(lastResult) ? "None" : lastResult);
            builder.AppendLine(BuildRequestSummary(npcId));
            builder.Append(BuildNextSequenceSummary(npcId));
            return builder.ToString();
        }

        public void StartEncounter()
        {
            StartEncounterInternal(false);
        }

        public void StartEncounterAndAdvanceDay()
        {
            StartEncounterInternal(true);
        }

        private void StartEncounterInternal(bool advanceDay)
        {
            if (runner == null)
            {
                Debug.LogError("NPC conversation runner is not assigned.");
                return;
            }

            if (runner.HasActiveConversation)
            {
                Debug.LogWarning("NPC encounter already has an active conversation. Complete the current conversation before starting another encounter.");
                return;
            }

            SyncDailySessionState();
            if (IsBusinessDayComplete)
            {
                Debug.LogWarning(
                    $"NPC encounter skipped. Business day is complete. date={CurrentDateText}, " +
                    $"region={regionId}, encounters={EncountersStartedToday}/{MaxEncountersPerDay}");
                return;
            }

            if (TryPickVisitEvent(regionId, out VisitEventData visitEvent) == false)
            {
                Debug.LogWarning($"NPC encounter failed. {GetEncounterFailureReason(regionId)}");
                return;
            }

            _activeEncounterDay = currentDay;
            _activeEventId = visitEvent.EventId;
            Debug.Log(
                $"NPC encounter selected: date={NpcImperialCalendar.FormatDayIndex(_activeEncounterDay)}, " +
                $"region={regionId}, npc={visitEvent.NpcId}, event={visitEvent.EventId}");
            runner.PlayEvent(visitEvent.EventId, _history.GetNpcAffinity(visitEvent.NpcId));
            RecordEncounter(visitEvent, _activeEncounterDay, advanceDay);
        }

        public bool TryPickVisitEvent(string targetRegionId, out VisitEventData visitEvent)
        {
            visitEvent = null;
            currentDay = Mathf.Max(1, currentDay);
            SyncDailySessionState();

            IReadOnlyList<RegionPoolEntryData> poolEntries = _database.GetRegionPoolEntries(targetRegionId);
            if (poolEntries.Count == 0)
                return false;

            bool ignoreCooldownsForThisPick = false;
            List<RegionPoolEntryData> validEntries = AvoidRecentRepeatEntries(
                targetRegionId,
                GetValidPoolEntries(poolEntries, true, true));

            if (validEntries.Count == 0 && ignoreNpcCooldownWhenPoolIsEmpty)
            {
                ignoreCooldownsForThisPick = true;
                validEntries = AvoidRecentRepeatEntries(
                    targetRegionId,
                    GetValidPoolEntries(poolEntries, false, false));
            }

            if (validEntries.Count == 0)
                return false;

            if (TryPickPriorityVisitEvent(
                    targetRegionId,
                    validEntries,
                    ignoreCooldownsForThisPick == false,
                    out visitEvent))
            {
                return true;
            }

            RegionPoolEntryData selectedEntry = PickWeighted(validEntries);
            IReadOnlyList<VisitEventData> candidates = GetSelectableVisitEvents(
                targetRegionId,
                selectedEntry.NpcId,
                ignoreCooldownsForThisPick == false);
            if (candidates.Count == 0)
                return false;

            candidates = AvoidRecentRepeatEvents(targetRegionId, candidates);

            if (ignoreCooldownsForThisPick)
            {
                Debug.Log(
                    $"NPC encounter cooldown fallback used. region={targetRegionId}, date={CurrentDateText}, " +
                    "npc and event cooldowns were ignored for this debug pick.");
            }

            visitEvent = PickVisitEvent(candidates);
            return true;
        }

        public string GetEncounterFailureReason(string targetRegionId)
        {
            if (_database == null)
                return "NPC database is not loaded.";

            IReadOnlyList<RegionPoolEntryData> poolEntries = _database.GetRegionPoolEntries(targetRegionId);
            if (poolEntries.Count == 0)
                return $"Region pool is empty or disabled. region={targetRegionId}";

            int minDayBlocked = 0;
            int dayRepeatBlocked = 0;
            int cooldownBlocked = 0;
            int eventBlocked = 0;

            foreach (RegionPoolEntryData entry in poolEntries)
            {
                if (entry.MinDay > currentDay)
                {
                    minDayBlocked++;
                    continue;
                }

                if (IsNpcBlockedForBusinessDay(entry.NpcId, entry.RegionId))
                {
                    dayRepeatBlocked++;
                    continue;
                }

                if (IsNpcCooldownReady(entry) == false)
                {
                    cooldownBlocked++;
                    continue;
                }

                if (GetSelectableVisitEvents(entry.RegionId, entry.NpcId).Count == 0)
                    eventBlocked++;
            }

            int historyLastDay = _history?.LastEncounterDay ?? 0;
            string historyLastDate = historyLastDay > 0
                ? NpcImperialCalendar.FormatDayIndex(historyLastDay)
                : "None";

            return
                $"No selectable NPC event. region={targetRegionId}, date={CurrentDateText}, " +
                $"pool={poolEntries.Count}, minDayBlocked={minDayBlocked}, " +
                $"dayRepeatBlocked={dayRepeatBlocked}, " +
                $"cooldownBlocked={cooldownBlocked}, eventBlocked={eventBlocked}, " +
                $"historyLastDate={historyLastDate}, " +
                $"ignoreCooldownWhenPoolIsEmpty={ignoreNpcCooldownWhenPoolIsEmpty}" +
                BuildBlockedPriorityFailureHint(targetRegionId, poolEntries);
        }

        private void SyncCurrentDayFromHistory()
        {
            if (continueDayFromHistoryOnLoad == false || _history == null)
                return;

            int lastEncounterDay = _history.LastEncounterDay;
            if (lastEncounterDay <= 0 || currentDay > lastEncounterDay)
                return;

            int previousCurrentDay = currentDay;
            currentDay = lastEncounterDay + 1;

            Debug.Log(
                $"NPC encounter date synced from saved history. " +
                $"date={NpcImperialCalendar.FormatDayIndex(previousCurrentDay)}->{CurrentDateText}, " +
                $"historyLastDate={NpcImperialCalendar.FormatDayIndex(lastEncounterDay)}");
        }

        private void SyncDailySessionState(bool force = false)
        {
            currentDay = Mathf.Max(1, currentDay);

            bool sameDay = _sessionDay == currentDay;
            bool sameRegion = string.Equals(_sessionRegionId, regionId, StringComparison.OrdinalIgnoreCase);
            if (force == false && sameDay && sameRegion)
                return;

            _sessionDay = currentDay;
            _sessionRegionId = regionId;
            _encountersStartedToday = _history?.GetEncounterCountOnDay(regionId, currentDay) ?? 0;
        }

        private List<RegionPoolEntryData> GetValidPoolEntries(
            IReadOnlyList<RegionPoolEntryData> poolEntries,
            bool enforceNpcCooldown,
            bool enforceEventCooldown)
        {
            List<RegionPoolEntryData> validEntries = new List<RegionPoolEntryData>();

            foreach (RegionPoolEntryData entry in poolEntries)
            {
                if (entry.MinDay > currentDay)
                    continue;

                if (IsNpcBlockedForBusinessDay(entry.NpcId, entry.RegionId))
                    continue;

                if (enforceNpcCooldown && IsNpcCooldownReady(entry) == false)
                    continue;

                if (GetSelectableVisitEvents(entry.RegionId, entry.NpcId, enforceEventCooldown).Count == 0)
                    continue;

                validEntries.Add(entry);
            }

            return validEntries;
        }

        private bool TryPickPriorityVisitEvent(
            string targetRegionId,
            IReadOnlyList<RegionPoolEntryData> validEntries,
            bool enforceEventCooldown,
            out VisitEventData visitEvent)
        {
            visitEvent = null;

            List<VisitEventData> priorityCandidates = new List<VisitEventData>();
            foreach (RegionPoolEntryData entry in validEntries)
            {
                priorityCandidates.AddRange(_database
                    .GetVisitEvents(targetRegionId, entry.NpcId)
                    .Where(NpcVisitEventRules.IsPriorityVisitEvent)
                    .Where(candidate => IsVisitEventEligible(candidate, enforceEventCooldown)));
            }

            priorityCandidates = priorityCandidates
                .GroupBy(candidate => candidate.EventId)
                .Select(group => group.First())
                .ToList();

            if (priorityCandidates.Count == 0)
                return false;

            visitEvent = PickVisitEvent(GetHighestPriorityEvents(priorityCandidates));
            Debug.Log(
                $"NPC priority event selected: region={targetRegionId}, npc={visitEvent.NpcId}, " +
                $"event={visitEvent.EventId}, type={visitEvent.EventType}");
            return true;
        }

        private string BuildRequestSummary(string npcId)
        {
            if (_database.Npcs.TryGetValue(npcId, out NpcData npc) == false)
                return "Request: NPC data missing.";

            if (npc.RequestAvailable == false)
                return "Request: Not configured.";

            bool unlocked = _history.IsNpcRequestUnlocked(npcId);
            int unlockLevel = Mathf.Max(0, npc.RequestUnlockLevel);
            int affinity = _history.GetNpcAffinity(npcId);
            int level = NpcAffinityUtility.GetLevel(affinity);

            if (unlocked)
            {
                int unlockDay = _history.GetNpcRequestUnlockedDay(npcId);
                string unlockDate = unlockDay > 0
                    ? NpcImperialCalendar.FormatDayIndex(unlockDay)
                    : "Unknown date";
                return $"Request: Unlocked ({unlockDate})";
            }

            string eventRequirement = string.IsNullOrWhiteSpace(npc.RequestUnlockEvent)
                ? string.Empty
                : $", event={npc.RequestUnlockEvent}";
            return $"Request: Locked (Lv.{level}/{unlockLevel}{eventRequirement})";
        }

        private string BuildNextSequenceSummary(string npcId)
        {
            VisitEventData nextPriorityEvent = _database.GetVisitEvents(regionId, npcId)
                .Where(NpcVisitEventRules.IsPriorityVisitEvent)
                .Where(visitEvent => _history.HasPlayedEvent(visitEvent.EventId) == false)
                .OrderByDescending(NpcVisitEventRules.GetPriorityTypeRank)
                .ThenBy(visitEvent => string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) ? "zz" : visitEvent.SequenceGroup)
                .ThenBy(visitEvent => visitEvent.SequenceIndex)
                .ThenByDescending(visitEvent => visitEvent.Priority)
                .FirstOrDefault();

            if (nextPriorityEvent == null)
                return "Next Priority: None or completed.";

            string requirementStatus = BuildRequirementStatus(nextPriorityEvent);
            return $"Next Priority: {nextPriorityEvent.EventId}\n{requirementStatus}";
        }

        private string BuildRequirementStatus(VisitEventData visitEvent)
        {
            List<string> blockers = GetVisitEventBlockers(visitEvent, true);

            if (blockers.Count == 0)
                return "Status: Ready";

            return $"Status: Blocked - {string.Join(", ", blockers)}";
        }

        private List<string> GetVisitEventBlockers(VisitEventData visitEvent, bool enforceEventCooldown)
        {
            List<string> blockers = new List<string>();

            int playCount = _history.GetEventPlayCount(visitEvent.EventId);
            if (NpcVisitEventRules.IsOneShotEvent(visitEvent) && playCount > 0)
                blockers.Add("already played");

            if (IsRequestEventUnlocked(visitEvent) == false)
                blockers.Add("request locked");

            int visits = _history.GetNpcVisitCount(visitEvent.NpcId);
            if (visits < visitEvent.RequiredNpcVisits)
                blockers.Add($"visits {visits}/{visitEvent.RequiredNpcVisits}");

            int affinity = _history.GetNpcAffinity(visitEvent.NpcId);
            if (affinity < visitEvent.RequiredAffinity)
                blockers.Add($"affinity {affinity}/{visitEvent.RequiredAffinity}");

            int correctCount = _history.GetNpcCorrectCount(visitEvent.NpcId);
            if (correctCount < visitEvent.RequiredCorrectCount)
                blockers.Add($"correct {correctCount}/{visitEvent.RequiredCorrectCount}");

            if (IsRequiredLastResultMatched(visitEvent) == false)
            {
                string lastResult = _history.GetNpcLastResult(visitEvent.NpcId);
                blockers.Add($"lastResult {ValueOrNone(lastResult)}/{visitEvent.RequiredLastResult}");
            }

            foreach (string requiredEventId in visitEvent.RequiredEventIds)
            {
                if (_history.HasPlayedEvent(requiredEventId) == false)
                    blockers.Add($"needs {requiredEventId}");
            }

            if (IsSequenceReady(visitEvent) == false)
                blockers.Add("previous sequence step");

            if (enforceEventCooldown)
                AddEventCooldownBlocker(visitEvent, playCount, blockers);

            return blockers;
        }

        private void AddEventCooldownBlocker(VisitEventData visitEvent, int playCount, List<string> blockers)
        {
            int eventCooldownDays = Mathf.Max(0, visitEvent.CooldownDays);
            int elapsedEventDays = GetElapsedDaysSince(_history.GetEventLastPlayDay(visitEvent.EventId));
            if (playCount > 0 && elapsedEventDays < eventCooldownDays)
                blockers.Add($"cooldown {elapsedEventDays}/{eventCooldownDays}");
        }

        private string BuildBlockedPriorityFailureHint(
            string targetRegionId,
            IReadOnlyList<RegionPoolEntryData> poolEntries)
        {
            List<string> hints = new List<string>();
            HashSet<string> seenEventIds = new HashSet<string>();

            foreach (RegionPoolEntryData entry in poolEntries)
            {
                foreach (VisitEventData visitEvent in _database.GetVisitEvents(targetRegionId, entry.NpcId))
                {
                    if (NpcVisitEventRules.IsPriorityVisitEvent(visitEvent) == false)
                        continue;

                    if (_history.HasPlayedEvent(visitEvent.EventId))
                        continue;

                    if (seenEventIds.Add(visitEvent.EventId) == false)
                        continue;

                    string status = BuildRequirementStatus(visitEvent);
                    if (status == "Status: Ready")
                        continue;

                    hints.Add($"{visitEvent.EventId} [{status}]");
                    if (hints.Count >= 3)
                        break;
                }

                if (hints.Count >= 3)
                    break;
            }

            if (hints.Count == 0)
                return string.Empty;

            return $", blockedPriorityHints={string.Join(" | ", hints)}";
        }

        private List<RegionPoolEntryData> AvoidRecentRepeatEntries(
            string targetRegionId,
            List<RegionPoolEntryData> entries)
        {
            if (avoidImmediateRepeat == false || entries.Count <= 1)
                return entries;

            if (_history.LastEncounterDay != currentDay
                || string.Equals(_history.LastRegionId, targetRegionId, StringComparison.OrdinalIgnoreCase) == false)
            {
                return entries;
            }

            HashSet<string> blockedNpcIds = new HashSet<string>(
                _history.GetRecentNpcIds(targetRegionId, recentNpcRepeatBlockCount));
            if (blockedNpcIds.Count == 0)
                return entries;

            List<RegionPoolEntryData> filteredEntries = entries
                .Where(entry => blockedNpcIds.Contains(entry.NpcId) == false)
                .ToList();

            return filteredEntries.Count > 0 ? filteredEntries : entries;
        }

        private IReadOnlyList<VisitEventData> AvoidRecentRepeatEvents(
            string targetRegionId,
            IReadOnlyList<VisitEventData> events)
        {
            if (avoidImmediateRepeat == false || events.Count <= 1)
                return events;

            if (_history.LastEncounterDay != currentDay
                || string.Equals(_history.LastRegionId, targetRegionId, StringComparison.OrdinalIgnoreCase) == false)
            {
                return events;
            }

            HashSet<string> blockedEventIds = new HashSet<string>(
                _history.GetRecentEventIds(targetRegionId, recentEventRepeatBlockCount));
            if (blockedEventIds.Count == 0)
                return events;

            List<VisitEventData> filteredEvents = events
                .Where(visitEvent => blockedEventIds.Contains(visitEvent.EventId) == false)
                .ToList();

            return filteredEvents.Count > 0 ? filteredEvents : events;
        }

        private bool IsNpcCooldownReady(RegionPoolEntryData entry)
        {
            if (_history.GetNpcVisitCount(entry.NpcId) == 0)
                return true;

            int cooldownDays = Mathf.Max(0, entry.CooldownDays);
            return GetElapsedDaysSince(_history.GetNpcLastVisitDay(entry.NpcId)) >= cooldownDays;
        }

        private bool IsNpcBlockedForBusinessDay(string npcId, string entryRegionId)
        {
            if (blockSameNpcOncePerDay == false || _history == null)
                return false;

            return _history.HasNpcEncounterOnDay(npcId, entryRegionId, currentDay);
        }

        private IReadOnlyList<VisitEventData> GetSelectableVisitEvents(
            string targetRegionId,
            string npcId,
            bool enforceEventCooldown = true)
        {
            List<VisitEventData> eligibleEvents = _database.GetVisitEvents(targetRegionId, npcId)
                .Where(visitEvent => IsVisitEventEligible(visitEvent, enforceEventCooldown))
                .ToList();

            if (eligibleEvents.Count == 0)
                return eligibleEvents;

            List<VisitEventData> unplayedEvents = eligibleEvents
                .Where(visitEvent => _history.HasPlayedEvent(visitEvent.EventId) == false)
                .ToList();

            if (unplayedEvents.Count > 0)
                return GetHighestPriorityEvents(unplayedEvents);

            List<VisitEventData> repeatableEvents = eligibleEvents
                .Where(visitEvent => NpcVisitEventRules.IsOneShotEvent(visitEvent) == false)
                .ToList();

            if (repeatableEvents.Count == 0)
                return repeatableEvents;

            int leastPlayCount = repeatableEvents.Min(visitEvent => _history.GetEventPlayCount(visitEvent.EventId));
            return GetHighestPriorityEvents(repeatableEvents
                .Where(visitEvent => _history.GetEventPlayCount(visitEvent.EventId) == leastPlayCount)
                .ToList());
        }

        private bool IsVisitEventEligible(VisitEventData visitEvent, bool enforceEventCooldown)
        {
            return GetVisitEventBlockers(visitEvent, enforceEventCooldown).Count == 0;
        }

        private int GetElapsedDaysSince(int previousDay)
        {
            if (previousDay <= 0)
                return int.MaxValue;

            if (currentDay < previousDay)
                return int.MaxValue;

            return currentDay - previousDay;
        }

        private bool IsRequestEventUnlocked(VisitEventData visitEvent)
        {
            return visitEvent.EventType != VisitEventType.Request
                   || (_history != null && _history.IsNpcRequestUnlocked(visitEvent.NpcId));
        }

        private bool IsRequiredLastResultMatched(VisitEventData visitEvent)
        {
            if (string.IsNullOrWhiteSpace(visitEvent.RequiredLastResult))
                return true;

            return string.Equals(
                _history.GetNpcLastResult(visitEvent.NpcId),
                visitEvent.RequiredLastResult,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSequenceReady(VisitEventData visitEvent)
        {
            if (string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) || visitEvent.SequenceIndex <= 1)
                return true;

            foreach (VisitEventData otherEvent in _database.VisitEvents.Values)
            {
                if (otherEvent.SequenceGroup != visitEvent.SequenceGroup)
                    continue;

                if (otherEvent.SequenceIndex <= 0 || otherEvent.SequenceIndex >= visitEvent.SequenceIndex)
                    continue;

                if (_history.HasPlayedEvent(otherEvent.EventId) == false)
                    return false;
            }

            return true;
        }

        private static IReadOnlyList<VisitEventData> GetHighestPriorityEvents(IReadOnlyList<VisitEventData> events)
        {
            if (events.Count == 0)
                return events;

            int highestPriority = events.Max(visitEvent => visitEvent.Priority);
            return events
                .Where(visitEvent => visitEvent.Priority == highestPriority)
                .ToList();
        }

        private static VisitEventData PickVisitEvent(IReadOnlyList<VisitEventData> candidates)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }

        private void HandleResultDialogueStarted(string eventId, NpcConversationResult result)
        {
            if (string.Equals(eventId, _activeEventId, StringComparison.OrdinalIgnoreCase) == false)
            {
                Debug.LogWarning(
                    $"Ignoring NPC result from an event that was not started by the encounter director. event={eventId}, activeEvent={_activeEventId}");
                return;
            }

            if (_database.TryGetVisitEvent(eventId, out VisitEventData visitEvent) == false)
            {
                Debug.LogWarning($"Cannot record NPC result. Visit event not found: {eventId}");
                return;
            }

            int affinityDelta = GetAffinityDelta(result);
            int beforeAffinity = _history.GetNpcAffinity(visitEvent.NpcId);
            int beforeLevel = NpcAffinityUtility.GetLevel(beforeAffinity);
            _history.RecordResult(visitEvent.NpcId, visitEvent.EventId, result, affinityDelta, _activeEncounterDay);
            int afterAffinity = _history.GetNpcAffinity(visitEvent.NpcId);
            int afterLevel = NpcAffinityUtility.GetLevel(afterAffinity);

            NpcAffinityChangeContext changeContext = new NpcAffinityChangeContext(
                visitEvent.NpcId,
                visitEvent.EventId,
                result,
                affinityDelta,
                beforeAffinity,
                afterAffinity,
                beforeLevel,
                afterLevel,
                NpcAffinityUtility.GetLabel(beforeAffinity),
                NpcAffinityUtility.GetLabel(afterAffinity),
                _activeEncounterDay,
                NpcImperialCalendar.FormatDayIndex(_activeEncounterDay));

            _lastAffinityChangeSummary = changeContext.BuildDebugSummary();
            AffinityChanged?.Invoke(changeContext);
            affinityChanged.Invoke(_lastAffinityChangeSummary);

            if (changeContext.LevelChanged)
            {
                AffinityLevelChanged?.Invoke(changeContext);
                affinityLevelChanged.Invoke(_lastAffinityChangeSummary);
            }

            TryUnlockRequest(visitEvent, afterAffinity);

            if (persistHistory)
                _history.Save();

            Debug.Log(
                $"NPC result recorded: npc={visitEvent.NpcId}, event={visitEvent.EventId}, result={result}, " +
                $"relation={changeContext.BuildShortSummary()}");
        }

        private void TryUnlockRequest(VisitEventData visitEvent, int currentAffinity)
        {
            if (visitEvent == null || _database == null || _history == null)
                return;

            if (_database.Npcs.TryGetValue(visitEvent.NpcId, out NpcData npc) == false)
                return;

            if (npc.RequestAvailable == false)
                return;

            if (_history.IsNpcRequestUnlocked(npc.NpcId))
                return;

            int unlockLevel = Mathf.Max(0, npc.RequestUnlockLevel);
            int currentLevel = NpcAffinityUtility.GetLevel(currentAffinity);
            if (currentLevel < unlockLevel)
                return;

            if (string.IsNullOrWhiteSpace(npc.RequestUnlockEvent) == false
                && _history.HasPlayedEvent(npc.RequestUnlockEvent) == false)
            {
                return;
            }

            if (_history.TryUnlockNpcRequest(npc.NpcId, _activeEncounterDay) == false)
                return;

            NpcRequestUnlockContext unlockContext = new NpcRequestUnlockContext(
                npc.NpcId,
                npc.DisplayName,
                visitEvent.EventId,
                unlockLevel,
                currentAffinity,
                currentLevel,
                _activeEncounterDay,
                NpcImperialCalendar.FormatDayIndex(_activeEncounterDay));

            _lastRequestUnlockSummary = unlockContext.BuildDebugSummary();
            RequestUnlocked?.Invoke(unlockContext);
            requestUnlocked.Invoke(_lastRequestUnlockSummary);

            Debug.Log($"NPC request unlocked: {_lastRequestUnlockSummary}");
        }

        private void HandleConversationCompleted()
        {
            _activeEventId = string.Empty;
        }

        private void RecordEncounter(VisitEventData visitEvent, int encounterDay, bool advanceDay)
        {
            _history.RecordEncounter(visitEvent, encounterDay, regionId);
            _encountersStartedToday = _history.GetEncounterCountOnDay(regionId, encounterDay);

            if (persistHistory)
                _history.Save();

            if (advanceDay)
            {
                currentDay++;
                SyncDailySessionState(true);
            }
        }

        private static int GetAffinityDelta(NpcConversationResult result)
        {
            return result switch
            {
                NpcConversationResult.Perfect => 3,
                NpcConversationResult.Correct => 2,
                NpcConversationResult.Similar => 1,
                NpcConversationResult.Wrong => 0,
                NpcConversationResult.Disgusting => 0,
                _ => 0
            };
        }

        private static RegionPoolEntryData PickWeighted(IReadOnlyList<RegionPoolEntryData> entries)
        {
            int totalWeight = 0;
            foreach (RegionPoolEntryData entry in entries)
            {
                totalWeight += Mathf.Max(0, entry.Weight);
            }

            if (totalWeight <= 0)
                return entries[Random.Range(0, entries.Count)];

            int roll = Random.Range(0, totalWeight);
            int accumulated = 0;

            foreach (RegionPoolEntryData entry in entries)
            {
                accumulated += Mathf.Max(0, entry.Weight);
                if (roll < accumulated)
                    return entry;
            }

            return entries[entries.Count - 1];
        }
    }

    public static class NpcVisitEventRules
    {
        public static bool IsOneShotEvent(VisitEventData visitEvent)
        {
            return visitEvent.RepeatMode == VisitEventRepeatMode.Once
                   || visitEvent.EventType == VisitEventType.Request
                   || visitEvent.EventType == VisitEventType.Special
                   || visitEvent.EventType == VisitEventType.Sequence
                   || string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false
                   || IsFirstVisitEvent(visitEvent);
        }

        public static bool IsPriorityVisitEvent(VisitEventData visitEvent)
        {
            return visitEvent.EventType == VisitEventType.Request
                   || visitEvent.EventType == VisitEventType.Special
                   || visitEvent.EventType == VisitEventType.Sequence
                   || string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) == false;
        }

        public static int GetPriorityTypeRank(VisitEventData visitEvent)
        {
            return visitEvent.EventType switch
            {
                VisitEventType.Request => 3,
                VisitEventType.Special => 2,
                VisitEventType.Sequence => 1,
                _ => string.IsNullOrWhiteSpace(visitEvent.SequenceGroup) ? 0 : 1
            };
        }

        private static bool IsFirstVisitEvent(VisitEventData visitEvent)
        {
            return visitEvent.EventId.IndexOf("FirstVisit", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public sealed class NpcAffinityChangeContext
    {
        public string NpcId { get; }
        public string EventId { get; }
        public NpcConversationResult Result { get; }
        public int AffinityDelta { get; }
        public int BeforeAffinity { get; }
        public int AfterAffinity { get; }
        public int BeforeLevel { get; }
        public int AfterLevel { get; }
        public string BeforeLevelLabel { get; }
        public string AfterLevelLabel { get; }
        public int EncounterDay { get; }
        public string EncounterDateText { get; }
        public bool LevelChanged => BeforeLevel != AfterLevel;

        public NpcAffinityChangeContext(
            string npcId,
            string eventId,
            NpcConversationResult result,
            int affinityDelta,
            int beforeAffinity,
            int afterAffinity,
            int beforeLevel,
            int afterLevel,
            string beforeLevelLabel,
            string afterLevelLabel,
            int encounterDay,
            string encounterDateText)
        {
            NpcId = npcId ?? string.Empty;
            EventId = eventId ?? string.Empty;
            Result = result;
            AffinityDelta = affinityDelta;
            BeforeAffinity = Mathf.Max(0, beforeAffinity);
            AfterAffinity = Mathf.Max(0, afterAffinity);
            BeforeLevel = Mathf.Max(0, beforeLevel);
            AfterLevel = Mathf.Max(0, afterLevel);
            BeforeLevelLabel = beforeLevelLabel ?? string.Empty;
            AfterLevelLabel = afterLevelLabel ?? string.Empty;
            EncounterDay = Mathf.Max(1, encounterDay);
            EncounterDateText = encounterDateText ?? string.Empty;
        }

        public string BuildShortSummary()
        {
            string delta = AffinityDelta > 0 ? $"+{AffinityDelta}" : AffinityDelta.ToString();
            return $"{BeforeAffinity}->{AfterAffinity} ({delta}), Lv.{BeforeLevel}->{AfterLevel}";
        }

        public string BuildDebugSummary()
        {
            string levelText = LevelChanged
                ? $"Lv.{BeforeLevel} {BeforeLevelLabel} -> Lv.{AfterLevel} {AfterLevelLabel}"
                : $"Lv.{AfterLevel} {AfterLevelLabel}";

            return
                $"{EncounterDateText} / npc={ValueOrNone(NpcId)}, result={Result}, " +
                $"affinity={BeforeAffinity}->{AfterAffinity} ({FormatDelta(AffinityDelta)}), level={levelText}";
        }

        private static string FormatDelta(int delta)
        {
            return delta > 0 ? $"+{delta}" : delta.ToString();
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }

    public sealed class NpcRequestUnlockContext
    {
        public string NpcId { get; }
        public string DisplayName { get; }
        public string EventId { get; }
        public int UnlockLevel { get; }
        public int Affinity { get; }
        public int AffinityLevel { get; }
        public int EncounterDay { get; }
        public string EncounterDateText { get; }

        public NpcRequestUnlockContext(
            string npcId,
            string displayName,
            string eventId,
            int unlockLevel,
            int affinity,
            int affinityLevel,
            int encounterDay,
            string encounterDateText)
        {
            NpcId = npcId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            EventId = eventId ?? string.Empty;
            UnlockLevel = Mathf.Max(0, unlockLevel);
            Affinity = Mathf.Max(0, affinity);
            AffinityLevel = Mathf.Max(0, affinityLevel);
            EncounterDay = Mathf.Max(1, encounterDay);
            EncounterDateText = encounterDateText ?? string.Empty;
        }

        public string BuildDebugSummary()
        {
            return
                $"{EncounterDateText} / npc={ValueOrNone(DisplayName)} ({ValueOrNone(NpcId)}), " +
                $"event={ValueOrNone(EventId)}, affinity={Affinity}, level=Lv.{AffinityLevel}, requestLevel=Lv.{UnlockLevel}";
        }

        private static string ValueOrNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }
}
