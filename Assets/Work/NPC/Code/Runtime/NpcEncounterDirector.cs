using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
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

        [Header("Encounter Intro")]
        [SerializeField] private Transform npcRiseTarget;
        [SerializeField] private bool playNpcRiseBeforeConversation;
        [SerializeField] private float npcRiseStartEulerX = 110f;
        [SerializeField] private float npcRiseEndEulerX;
        [SerializeField, Min(0f)] private float npcRiseDuration = 0.55f;
        [SerializeField] private Ease npcRiseEase = Ease.OutBack;

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
        private Tween _npcRiseTween;
        private bool _isStartingEncounter;

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
            ReconcileRequestStatesFromPlayedRequestEvents();

            if (runner != null)
            {
                runner.ResultDialogueStarted += HandleResultDialogueStarted;
                runner.ConversationCompleted += HandleConversationCompleted;
            }
        }

        private void OnDestroy()
        {
            _npcRiseTween?.Kill();
            _npcRiseTween = null;

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
            if (_history == null)
            {
                _history = persistHistory
                    ? NpcEncounterHistory.Load(historySaveKey)
                    : NpcEncounterHistory.CreateUnsaved();
            }

            _history.Clear();
            _activeEventId = string.Empty;
            _encountersStartedToday = 0;
            _lastAffinityChangeSummary = string.Empty;
            _lastRequestUnlockSummary = string.Empty;
            SyncDailySessionState(true);
        }

        public void LogEncounterHistory()
        {
            Debug.Log(GetEncounterHistorySummary());
        }

        public NpcDataValidationReport ValidateNpcData(bool logToConsole = true)
        {
            if (_database == null)
                _database = NpcConversationDatabase.LoadFromResources(resourceFolder);

            NpcDataValidationReport report = _database.ValidateData();
            if (logToConsole)
                report.LogToUnityConsole();

            return report;
        }

        public string GetNpcDataValidationSummary()
        {
            if (_database == null)
                return "NPC data is not loaded.";

            NpcDataValidationReport report = _database.LastValidationReport ?? _database.ValidateData();
            return report.BuildSummary(20);
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

        public string GetCurrentOrLastNpcId()
        {
            if (runner != null && string.IsNullOrWhiteSpace(runner.CurrentNpcId) == false)
                return runner.CurrentNpcId;

            return _history?.LastNpcId ?? string.Empty;
        }

        public string GetCurrentNpcRequestStateSummary()
        {
            return GetNpcRequestStateSummary(GetCurrentOrLastNpcId());
        }

        public string GetNpcRequestStateSummary(string npcId)
        {
            if (_database == null || _history == null)
                return "NPC data is not loaded.";

            if (string.IsNullOrWhiteSpace(npcId))
                return "Request: No active NPC.";

            if (_database.Npcs.TryGetValue(npcId, out NpcData npc) && npc.RequestAvailable == false)
                return $"Request: {npcId} / Not configured.";

            return _history.BuildNpcRequestDebugSummary(npcId);
        }

        public string GetCurrentNpcRequestFlowSummary()
        {
            return GetNpcRequestFlowSummary(GetCurrentOrLastNpcId());
        }

        public string GetNpcRequestFlowSummary(string npcId)
        {
            if (_database == null || _history == null)
                return "NPC data is not loaded.";

            if (string.IsNullOrWhiteSpace(npcId))
                return "Request Flow: No active NPC.";

            if (_database.Npcs.TryGetValue(npcId, out NpcData npc) == false)
                return $"Request Flow: NPC data missing. npc={npcId}";

            if (npc.RequestAvailable == false)
                return $"Request Flow: {npc.DisplayName} ({npcId}) / Not configured.";

            NpcRequestState state = _history.GetNpcRequestState(npcId);
            StringBuilder builder = new StringBuilder();
            builder.Append("Request Flow: ");
            builder.Append(npc.DisplayName);
            builder.Append(" (");
            builder.Append(npcId);
            builder.Append(") / ");
            builder.AppendLine(state.ToString());
            builder.AppendLine(BuildRequestFlowActionHint(npcId, npc, state));
            builder.Append(BuildRequestFlowNextEventHint(npcId));
            return builder.ToString();
        }

        public bool AcceptNpcRequest(string npcId)
        {
            return TryAdvanceNpcRequestState(npcId, NpcRequestState.Accepted);
        }

        public bool SetNpcRequestInProgress(string npcId)
        {
            return TryAdvanceNpcRequestState(npcId, NpcRequestState.InProgress);
        }

        public bool MarkNpcRequestReadyToComplete(string npcId)
        {
            return TryAdvanceNpcRequestState(npcId, NpcRequestState.ReadyToComplete);
        }

        public bool CompleteNpcRequest(string npcId)
        {
            return TryAdvanceNpcRequestState(npcId, NpcRequestState.Completed);
        }

        public bool MarkNpcRequestEpilogueAvailable(string npcId)
        {
            return TryAdvanceNpcRequestState(npcId, NpcRequestState.EpilogueAvailable);
        }

        public bool CompleteNpcRequestEpilogue(string npcId)
        {
            return TryAdvanceNpcRequestState(npcId, NpcRequestState.EpilogueCompleted);
        }

        public bool AdvanceCurrentNpcRequestState(NpcRequestState targetState)
        {
            return TryAdvanceNpcRequestState(GetCurrentOrLastNpcId(), targetState);
        }

        public string GetCurrentNpcProgressSummary()
        {
            return GetNpcProgressSummary(GetCurrentOrLastNpcId());
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

        public bool StartEncounter()
        {
            return StartEncounterInternal(false);
        }

        public bool StartEncounterAndAdvanceDay()
        {
            return StartEncounterInternal(true);
        }

        public bool ForceStartEvent(string eventId, bool advanceDay = false)
        {
            if (runner == null)
            {
                Debug.LogError("NPC conversation runner is not assigned.");
                return false;
            }

            if (runner.HasActiveConversation)
            {
                Debug.LogWarning("NPC encounter already has an active conversation. Complete the current conversation before forcing another event.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(eventId))
            {
                Debug.LogWarning("Cannot force NPC event. Event ID is empty.");
                return false;
            }

            if (_database.TryGetVisitEvent(eventId.Trim(), out VisitEventData visitEvent) == false)
            {
                Debug.LogWarning($"Cannot force NPC event. Visit event not found: {eventId}");
                return false;
            }

            currentDay = Mathf.Max(1, currentDay);
            SyncDailySessionState();
            ReconcileRequestStatesFromPlayedRequestEvents();

            List<string> blockers = GetVisitEventBlockers(visitEvent, true);
            if (blockers.Count > 0)
            {
                Debug.LogWarning(
                    $"Forcing NPC event with blockers. event={visitEvent.EventId}, " +
                    $"blockers={string.Join(", ", blockers)}");
            }

            _activeEncounterDay = currentDay;
            _activeEventId = visitEvent.EventId;
            Debug.Log(
                $"NPC event forced: date={NpcImperialCalendar.FormatDayIndex(_activeEncounterDay)}, " +
                $"region={regionId}, npc={visitEvent.NpcId}, event={visitEvent.EventId}");
            runner.PlayEvent(visitEvent.EventId, _history.GetNpcAffinity(visitEvent.NpcId));
            RecordEncounter(visitEvent, _activeEncounterDay, advanceDay);
            return true;
        }

        public string GetEventCandidateDebugSummary(int maxLines = 24)
        {
            if (_database == null || _history == null)
                return "NPC data is not loaded.";

            SyncDailySessionState();
            ReconcileRequestStatesFromPlayedRequestEvents();

            IReadOnlyList<RegionPoolEntryData> poolEntries = _database.GetRegionPoolEntries(regionId);
            if (poolEntries.Count == 0)
                return $"Event Preview: No pool entries. region={regionId}";

            StringBuilder builder = new StringBuilder();
            builder.Append("Event Preview: ");
            builder.Append(CurrentDateText);
            builder.Append(" / ");
            builder.AppendLine(regionId);

            int lineCount = 0;
            int readyCount = 0;
            foreach (RegionPoolEntryData entry in poolEntries)
            {
                if (lineCount >= maxLines)
                    break;

                List<string> entryBlockers = GetPoolEntryBlockers(entry);
                IReadOnlyList<VisitEventData> events = _database.GetVisitEvents(entry.RegionId, entry.NpcId);
                if (events.Count == 0)
                {
                    AppendCandidateLine(builder, entry.NpcId, "NoEvents", "BLOCKED", "no region events");
                    lineCount++;
                    continue;
                }

                foreach (VisitEventData visitEvent in events
                             .OrderByDescending(NpcVisitEventRules.GetPriorityTypeRank)
                             .ThenByDescending(candidate => candidate.Priority)
                             .ThenBy(candidate => candidate.EventId))
                {
                    if (lineCount >= maxLines)
                        break;

                    List<string> blockers = new List<string>(entryBlockers);
                    blockers.AddRange(GetVisitEventBlockers(visitEvent, true));

                    string state = blockers.Count == 0 ? "READY" : "BLOCKED";
                    if (blockers.Count == 0)
                        readyCount++;

                    AppendCandidateLine(
                        builder,
                        visitEvent.NpcId,
                        visitEvent.EventId,
                        state,
                        blockers.Count == 0 ? visitEvent.EventType.ToString() : string.Join(", ", blockers));
                    lineCount++;
                }
            }

            if (lineCount >= maxLines)
                builder.AppendLine($"... {Mathf.Max(0, CountPreviewableEvents(poolEntries) - maxLines)} more event(s).");

            builder.Append("Ready: ");
            builder.Append(readyCount);
            builder.Append(" / Shown: ");
            builder.Append(lineCount);
            return builder.ToString();
        }

        public bool CanStartEncounter()
        {
            if (runner == null)
                runner = FindFirstObjectByType<NpcConversationRunner>();

            if (runner == null || runner.HasActiveConversation == true || _isStartingEncounter == true)
                return false;

            SyncDailySessionState();
            ReconcileRequestStatesFromPlayedRequestEvents();

            return IsBusinessDayComplete == false
                   && TryPickVisitEvent(regionId, out _);
        }

        private bool StartEncounterInternal(bool advanceDay)
        {
            if (runner == null)
            {
                Debug.LogError("NPC conversation runner is not assigned.");
                return false;
            }

            if (runner.HasActiveConversation == true || _isStartingEncounter == true)
            {
                Debug.LogWarning("NPC encounter already has an active conversation. Complete the current conversation before starting another encounter.");
                return false;
            }

            SyncDailySessionState();
            ReconcileRequestStatesFromPlayedRequestEvents();

            if (IsBusinessDayComplete)
            {
                Debug.LogWarning(
                    $"NPC encounter skipped. Business day is complete. date={CurrentDateText}, " +
                    $"region={regionId}, encounters={EncountersStartedToday}/{MaxEncountersPerDay}");
                return false;
            }

            if (TryPickVisitEvent(regionId, out VisitEventData visitEvent) == false)
            {
                Debug.LogWarning($"NPC encounter failed. {GetEncounterFailureReason(regionId)}");
                return false;
            }

            _activeEncounterDay = currentDay;
            _activeEventId = visitEvent.EventId;
            Debug.Log(
                $"NPC encounter selected: date={NpcImperialCalendar.FormatDayIndex(_activeEncounterDay)}, " +
                $"region={regionId}, npc={visitEvent.NpcId}, event={visitEvent.EventId}");
            PlayEncounterConversationAfterIntro(visitEvent);
            RecordEncounter(visitEvent, _activeEncounterDay, advanceDay);
            return true;
        }

        private void PlayEncounterConversationAfterIntro(VisitEventData visitEvent)
        {
            if (visitEvent == null)
            {
                return;
            }

            if (playNpcRiseBeforeConversation == false || npcRiseTarget == null || npcRiseDuration <= 0f)
            {
                PlayEncounterConversation(visitEvent);
                return;
            }

            _isStartingEncounter = true;
            _npcRiseTween?.Kill();

            Quaternion startRotation = Quaternion.Euler(npcRiseStartEulerX, 0f, 0f);
            Quaternion endRotation = Quaternion.Euler(npcRiseEndEulerX, 0f, 0f);
            npcRiseTarget.localRotation = startRotation;

            _npcRiseTween = npcRiseTarget
                .DOLocalRotateQuaternion(endRotation, npcRiseDuration)
                .SetEase(npcRiseEase)
                .SetTarget(npcRiseTarget)
                .OnComplete(() => CompleteNpcRiseIntro(visitEvent, endRotation));
        }

        private void CompleteNpcRiseIntro(VisitEventData visitEvent, Quaternion endRotation)
        {
            if (npcRiseTarget != null)
            {
                npcRiseTarget.localRotation = endRotation;
            }

            PlayEncounterConversation(visitEvent);
        }

        private void PlayEncounterConversation(VisitEventData visitEvent)
        {
            _isStartingEncounter = false;
            _npcRiseTween = null;

            if (runner == null || visitEvent == null)
            {
                return;
            }

            runner.PlayEvent(visitEvent.EventId, _history.GetNpcAffinity(visitEvent.NpcId));
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

        private List<string> GetPoolEntryBlockers(RegionPoolEntryData entry)
        {
            List<string> blockers = new List<string>();

            if (entry.MinDay > currentDay)
                blockers.Add($"minDay {currentDay}/{entry.MinDay}");

            if (IsNpcBlockedForBusinessDay(entry.NpcId, entry.RegionId))
                blockers.Add("same day npc");

            if (IsNpcCooldownReady(entry) == false)
            {
                int elapsedDays = GetElapsedDaysSince(_history.GetNpcLastVisitDay(entry.NpcId));
                blockers.Add($"npc cooldown {elapsedDays}/{Mathf.Max(0, entry.CooldownDays)}");
            }

            return blockers;
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

            NpcRequestState state = _history.GetNpcRequestState(npcId);
            int unlockLevel = Mathf.Max(0, npc.RequestUnlockLevel);
            int affinity = _history.GetNpcAffinity(npcId);
            int level = NpcAffinityUtility.GetLevel(affinity);

            if (state != NpcRequestState.Locked)
                return _history.BuildNpcRequestDebugSummary(npcId);

            string eventRequirement = string.IsNullOrWhiteSpace(npc.RequestUnlockEvent)
                ? string.Empty
                : $", event={npc.RequestUnlockEvent}";
            return $"Request: Locked (Lv.{level}/{unlockLevel}{eventRequirement})";
        }

        private string BuildRequestFlowActionHint(string npcId, NpcData npc, NpcRequestState state)
        {
            switch (state)
            {
                case NpcRequestState.Locked:
                {
                    int affinity = _history.GetNpcAffinity(npcId);
                    int level = NpcAffinityUtility.GetLevel(affinity);
                    int unlockLevel = Mathf.Max(0, npc.RequestUnlockLevel);
                    string eventRequirement = GetRequestUnlockEventRequirementText(npc);
                    return $"Next: unlock request. affinity Lv.{level}/{unlockLevel}, event={eventRequirement}";
                }
                case NpcRequestState.Unlocked:
                    return "Next: start encounter until the Request offer event appears.";
                case NpcRequestState.Offered:
                    return "Next: accept the request, then progress it through the external quest/cooking flow.";
                case NpcRequestState.Accepted:
                    return "Next: set InProgress when the external request flow starts.";
                case NpcRequestState.InProgress:
                    return "Next: mark ReadyToComplete when the request objective is fulfilled.";
                case NpcRequestState.ReadyToComplete:
                    return "Next: start encounter to play the completion event.";
                case NpcRequestState.Completed:
                    return "Next: make EpilogueAvailable when the later story condition is met.";
                case NpcRequestState.EpilogueAvailable:
                    return "Next: start encounter to play the epilogue event.";
                case NpcRequestState.EpilogueCompleted:
                    return "Next: request flow finished.";
                default:
                    return "Next: unknown request state.";
            }
        }

        private string GetRequestUnlockEventRequirementText(NpcData npc)
        {
            if (string.IsNullOrWhiteSpace(npc.RequestUnlockEvent))
                return "none";

            return _history.HasPlayedEvent(npc.RequestUnlockEvent)
                ? "met"
                : $"needs {npc.RequestUnlockEvent}";
        }

        private string BuildRequestFlowNextEventHint(string npcId)
        {
            List<VisitEventData> requestEvents = _database
                .GetVisitEvents(regionId, npcId)
                .Where(visitEvent => visitEvent.EventType == VisitEventType.Request || HasRequestStateRule(visitEvent))
                .OrderBy(visitEvent => GetVisitEventBlockers(visitEvent, true).Count)
                .ThenByDescending(NpcVisitEventRules.GetPriorityTypeRank)
                .ThenBy(visitEvent => visitEvent.SequenceGroup)
                .ThenBy(visitEvent => visitEvent.SequenceIndex)
                .ThenByDescending(visitEvent => visitEvent.Priority)
                .ThenBy(visitEvent => visitEvent.EventId)
                .ToList();

            if (requestEvents.Count == 0)
                return $"Request Events ({regionId}): None configured.";

            StringBuilder builder = new StringBuilder();
            builder.Append("Request Events (");
            builder.Append(regionId);
            builder.AppendLine("):");

            int displayedCount = Mathf.Min(3, requestEvents.Count);
            for (int i = 0; i < displayedCount; i++)
            {
                VisitEventData visitEvent = requestEvents[i];
                builder.Append("- ");
                builder.Append(visitEvent.EventId);
                builder.Append(" / ");
                builder.AppendLine(BuildRequirementStatus(visitEvent));
            }

            if (requestEvents.Count > displayedCount)
                builder.Append("... +").Append(requestEvents.Count - displayedCount).Append(" more");

            return builder.ToString();
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

            AddRequestStateBlockers(visitEvent, blockers);

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

        private static void AppendCandidateLine(
            StringBuilder builder,
            string npcId,
            string eventId,
            string state,
            string detail)
        {
            builder.Append(state);
            builder.Append(" | ");
            builder.Append(ValueOrNone(npcId));
            builder.Append(" | ");
            builder.Append(ValueOrNone(eventId));
            builder.Append(" | ");
            builder.AppendLine(ValueOrNone(detail));
        }

        private int CountPreviewableEvents(IReadOnlyList<RegionPoolEntryData> poolEntries)
        {
            int count = 0;
            foreach (RegionPoolEntryData entry in poolEntries)
                count += Mathf.Max(1, _database.GetVisitEvents(entry.RegionId, entry.NpcId).Count);

            return count;
        }

        private void HandleResultDialogueStarted(string eventId, NpcConversationResult result)
        {
            result = NpcConversationRunner.NormalizeResult(result);
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
            ApplyRequestStateAfterSuccessResult(visitEvent, result, _activeEncounterDay);

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
            ApplyRequestStateAfterEncounter(visitEvent, encounterDay);

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
            return NpcConversationRunner.NormalizeResult(result) switch
            {
                NpcConversationResult.Perfect => 3,
                NpcConversationResult.Correct => 2,
                NpcConversationResult.Similar => 1,
                NpcConversationResult.Wrong => 0,
                _ => 0
            };
        }

        private void ReconcileRequestStatesFromPlayedRequestEvents()
        {
            if (_database == null || _history == null)
                return;

            bool changed = false;
            foreach (VisitEventData visitEvent in _database.VisitEvents.Values)
            {
                if (_history.HasPlayedEvent(visitEvent.EventId) == false)
                    continue;

                changed |= TryApplyPlayedRequestStateAfterEncounter(visitEvent);
                changed |= TryApplyPlayedRequestStateAfterSuccessResult(visitEvent);
            }

            if (changed && persistHistory)
                _history.Save();
        }

        private void AddRequestStateBlockers(VisitEventData visitEvent, List<string> blockers)
        {
            if (_history == null)
            {
                if (HasRequestStateRule(visitEvent) || visitEvent.EventType == VisitEventType.Request)
                    blockers.Add("request history missing");

                return;
            }

            NpcRequestState currentState = _history.GetNpcRequestState(visitEvent.NpcId);
            bool hasRequiredState = TryGetRequestStateRule(
                visitEvent.RequiredRequestState,
                "RequiredRequestState",
                visitEvent.EventId,
                blockers,
                out NpcRequestState requiredState);
            bool hasBlockedState = TryGetRequestStateRule(
                visitEvent.BlockedAtRequestState,
                "BlockedAtRequestState",
                visitEvent.EventId,
                blockers,
                out NpcRequestState blockedState);

            if (hasRequiredState && NpcRequestStateUtility.IsAtLeast(currentState, requiredState) == false)
                blockers.Add($"request {currentState}/{requiredState}");

            if (hasBlockedState && NpcRequestStateUtility.IsBlockedAtOrAfter(currentState, blockedState))
                blockers.Add($"request blocked at {blockedState}");

            if (visitEvent.EventType != VisitEventType.Request || hasRequiredState || hasBlockedState)
                return;

            if (currentState == NpcRequestState.Locked)
            {
                blockers.Add("request locked");
                return;
            }

            if (currentState != NpcRequestState.Unlocked)
                blockers.Add($"request state {currentState}");
        }

        private static bool HasRequestStateRule(VisitEventData visitEvent)
        {
            return string.IsNullOrWhiteSpace(visitEvent.RequiredRequestState) == false
                   || string.IsNullOrWhiteSpace(visitEvent.BlockedAtRequestState) == false
                   || string.IsNullOrWhiteSpace(visitEvent.RequestStateAfterEncounter) == false
                   || string.IsNullOrWhiteSpace(visitEvent.RequestStateAfterSuccessResult) == false;
        }

        private static bool TryGetRequestStateRule(
            string value,
            string columnName,
            string eventId,
            List<string> blockers,
            out NpcRequestState state)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                state = NpcRequestState.Locked;
                return false;
            }

            if (NpcRequestStateUtility.TryParse(value, out state))
                return true;

            blockers.Add($"invalid {columnName} {value} on {eventId}");
            return false;
        }

        private void ApplyRequestStateAfterEncounter(VisitEventData visitEvent, int encounterDay)
        {
            if (_history == null || visitEvent == null)
                return;

            if (TryGetRequestStateAfterEncounter(visitEvent, out NpcRequestState targetState))
            {
                _history.TryAdvanceNpcRequestState(visitEvent.NpcId, targetState, encounterDay);
                return;
            }

            if (visitEvent.EventType == VisitEventType.Request)
                _history.TryMarkNpcRequestOffered(visitEvent.NpcId, encounterDay);
        }

        private bool TryApplyPlayedRequestStateAfterEncounter(VisitEventData visitEvent)
        {
            if (TryGetRequestStateAfterEncounter(visitEvent, out NpcRequestState targetState))
            {
                int playedDay = _history.GetEventLastPlayDay(visitEvent.EventId);
                return _history.TryAdvanceNpcRequestState(
                    visitEvent.NpcId,
                    targetState,
                    playedDay > 0 ? playedDay : currentDay);
            }

            if (visitEvent.EventType != VisitEventType.Request)
                return false;

            return _history.TryMarkNpcRequestOfferedFromPlayedEvent(
                visitEvent.NpcId,
                visitEvent.EventId,
                currentDay);
        }

        private void ApplyRequestStateAfterSuccessResult(
            VisitEventData visitEvent,
            NpcConversationResult result,
            int encounterDay)
        {
            if (_history == null || visitEvent == null)
                return;

            if (TryGetRequestStateAfterSuccessResult(visitEvent, result, out NpcRequestState targetState) == false)
                return;

            _history.TryAdvanceNpcRequestState(visitEvent.NpcId, targetState, encounterDay);
        }

        private bool TryApplyPlayedRequestStateAfterSuccessResult(VisitEventData visitEvent)
        {
            string lastResult = _history.GetEventLastResult(visitEvent.EventId);
            if (Enum.TryParse(lastResult, true, out NpcConversationResult result) == false)
                return false;

            result = NpcConversationRunner.NormalizeResult(result);
            if (TryGetRequestStateAfterSuccessResult(visitEvent, result, out NpcRequestState targetState) == false)
                return false;

            int resultDay = _history.GetEventLastPlayDay(visitEvent.EventId);
            return _history.TryAdvanceNpcRequestState(
                visitEvent.NpcId,
                targetState,
                resultDay > 0 ? resultDay : currentDay);
        }

        private static bool TryGetRequestStateAfterEncounter(VisitEventData visitEvent, out NpcRequestState targetState)
        {
            return NpcRequestStateUtility.TryParse(visitEvent.RequestStateAfterEncounter, out targetState)
                   && targetState != NpcRequestState.Locked;
        }

        private static bool TryGetRequestStateAfterSuccessResult(
            VisitEventData visitEvent,
            NpcConversationResult result,
            out NpcRequestState targetState)
        {
            if (NpcRequestStateUtility.TryParse(visitEvent.RequestStateAfterSuccessResult, out targetState) == false
                || targetState == NpcRequestState.Locked)
            {
                return false;
            }

            if (visitEvent.RequestSuccessResults.Count == 0)
                return IsDefaultSuccessfulRequestResult(result);

            foreach (string resultName in visitEvent.RequestSuccessResults)
            {
                if (Enum.TryParse(resultName, true, out NpcConversationResult successResult)
                    && NpcConversationRunner.NormalizeResult(successResult) == NpcConversationRunner.NormalizeResult(result))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDefaultSuccessfulRequestResult(NpcConversationResult result)
        {
            return result == NpcConversationResult.Perfect
                   || result == NpcConversationResult.Correct;
        }

        private bool TryAdvanceNpcRequestState(string npcId, NpcRequestState targetState)
        {
            if (_history == null)
            {
                Debug.LogWarning("Cannot advance NPC request state. History is not loaded.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(npcId))
            {
                Debug.LogWarning("Cannot advance NPC request state. No active or recent NPC.");
                return false;
            }

            if (targetState == NpcRequestState.Locked)
            {
                Debug.LogWarning("Cannot advance NPC request state to Locked.");
                return false;
            }

            if (_database == null || _database.Npcs.TryGetValue(npcId, out NpcData npc) == false)
            {
                Debug.LogWarning($"Cannot advance NPC request state. NPC data is missing. npc={npcId}");
                return false;
            }

            if (npc.RequestAvailable == false)
            {
                Debug.LogWarning($"Cannot advance NPC request state. Request is not configured. npc={npcId}");
                return false;
            }

            if (_history.TryAdvanceNpcRequestState(npcId, targetState, currentDay) == false)
            {
                Debug.Log(
                    $"NPC request state unchanged. npc={npcId}, target={targetState}, " +
                    $"current={_history.GetNpcRequestState(npcId)}");
                return false;
            }

            if (persistHistory)
                _history.Save();

            Debug.Log($"NPC request state advanced. {GetNpcRequestStateSummary(npcId)}");
            return true;
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

        public static bool RequiresCookingStep(VisitEventData visitEvent)
        {
            if (visitEvent == null)
                return false;

            return string.IsNullOrWhiteSpace(visitEvent.CorrectRecipeId) == false
                   || visitEvent.AllowedFoodTypes.Count > 0
                   || visitEvent.RequiredTags.Count > 0
                   || visitEvent.PreferredTags.Count > 0
                   || visitEvent.AvoidTags.Count > 0
                   || visitEvent.DisgustingTags.Count > 0;
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
