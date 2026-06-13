using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Work.NPC.Code.Data;

namespace Work.NPC.Code.Runtime
{
    [Serializable]
    public sealed class NpcEncounterHistory
    {
        private const int MaxRecentEncounterRecords = 20;

        [SerializeField] private List<NpcHistoryRecord> npcRecords = new List<NpcHistoryRecord>();
        [SerializeField] private List<VisitEventHistoryRecord> eventRecords = new List<VisitEventHistoryRecord>();
        [SerializeField] private List<RecentEncounterRecord> recentEncounters = new List<RecentEncounterRecord>();
        [SerializeField] private string lastNpcId;
        [SerializeField] private string lastEventId;
        [SerializeField] private string lastRegionId;
        [SerializeField] private int lastEncounterDay;

        [NonSerialized] private string _saveKey;

        public static NpcEncounterHistory Load(string saveKey)
        {
            string json = PlayerPrefs.GetString(saveKey, string.Empty);
            NpcEncounterHistory history = string.IsNullOrWhiteSpace(json)
                ? new NpcEncounterHistory()
                : JsonUtility.FromJson<NpcEncounterHistory>(json);

            if (history == null)
                history = new NpcEncounterHistory();

            history.Normalize();
            history._saveKey = saveKey;
            return history;
        }

        public static NpcEncounterHistory CreateUnsaved()
        {
            NpcEncounterHistory history = new NpcEncounterHistory();
            history.Normalize();
            return history;
        }

        public int GetNpcVisitCount(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            return record?.visitCount ?? 0;
        }

        public int GetNpcLastVisitDay(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            return record?.lastVisitDay ?? 0;
        }

        public int GetNpcAffinity(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            return Mathf.Max(0, record?.affinity ?? 0);
        }

        public int GetNpcCorrectCount(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            if (record == null)
                return 0;

            return record.correctCount + record.perfectCount;
        }

        public string GetNpcLastResult(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            return record?.lastResult ?? string.Empty;
        }

        public bool IsNpcRequestUnlocked(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            return GetRequestStateRank(ResolveRequestState(record)) >= GetRequestStateRank(NpcRequestState.Unlocked);
        }

        public int GetNpcRequestUnlockedDay(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            return record?.requestUnlockedDay ?? 0;
        }

        public NpcRequestState GetNpcRequestState(string npcId)
        {
            return ResolveRequestState(FindNpcRecord(npcId));
        }

        public int GetNpcRequestStateDay(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            return GetRequestStateDay(record, ResolveRequestState(record));
        }

        public string BuildNpcRequestDebugSummary(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return "Request: No active NPC.";

            NpcHistoryRecord record = FindNpcRecord(npcId);
            if (record == null)
                return $"Request: {npcId} / Locked";

            return
                $"Request: {npcId} / {BuildRequestStateSummary(record)}\n" +
                $"Unlocked={FormatHistoryDay(record.requestUnlockedDay)}   " +
                $"Offered={FormatHistoryDay(record.requestOfferedDay)}   " +
                $"Accepted={FormatHistoryDay(record.requestAcceptedDay)}\n" +
                $"Progress={FormatHistoryDay(record.requestInProgressDay)}   " +
                $"Ready={FormatHistoryDay(record.requestReadyToCompleteDay)}   " +
                $"Complete={FormatHistoryDay(record.requestCompletedDay)}\n" +
                $"Epilogue={FormatHistoryDay(record.requestEpilogueAvailableDay)}   " +
                $"Done={FormatHistoryDay(record.requestEpilogueCompletedDay)}";
        }

        public string LastNpcId => lastNpcId;
        public string LastEventId => lastEventId;
        public string LastRegionId => lastRegionId;
        public int LastEncounterDay => lastEncounterDay;

        public IReadOnlyList<string> GetRecentNpcIds(string regionId, int count)
        {
            return GetRecentIds(regionId, count, record => record.npcId);
        }

        public IReadOnlyList<string> GetRecentEventIds(string regionId, int count)
        {
            return GetRecentIds(regionId, count, record => record.eventId);
        }

        public int GetEncounterCountOnDay(string regionId, int day)
        {
            if (day <= 0 || recentEncounters.Count == 0)
                return 0;

            int count = 0;
            foreach (RecentEncounterRecord record in recentEncounters)
            {
                if (record.day != day)
                    continue;

                if (IsSameRegion(record.regionId, regionId) == false)
                    continue;

                count++;
            }

            return count;
        }

        public bool HasNpcEncounterOnDay(string npcId, string regionId, int day)
        {
            if (day <= 0 || string.IsNullOrWhiteSpace(npcId) || recentEncounters.Count == 0)
                return false;

            foreach (RecentEncounterRecord record in recentEncounters)
            {
                if (record.day != day)
                    continue;

                if (IsSameRegion(record.regionId, regionId) == false)
                    continue;

                if (string.Equals(record.npcId, npcId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public int GetEventPlayCount(string eventId)
        {
            VisitEventHistoryRecord record = FindEventRecord(eventId);
            return record?.playCount ?? 0;
        }

        public int GetEventLastPlayDay(string eventId)
        {
            VisitEventHistoryRecord record = FindEventRecord(eventId);
            return record?.lastPlayDay ?? 0;
        }

        public string GetEventLastResult(string eventId)
        {
            VisitEventHistoryRecord record = FindEventRecord(eventId);
            return record?.lastResult ?? string.Empty;
        }

        public bool HasPlayedEvent(string eventId)
        {
            return GetEventPlayCount(eventId) > 0;
        }

        public bool TryUnlockNpcRequest(string npcId, int currentDay)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return false;

            NpcHistoryRecord npcRecord = FindOrCreateNpcRecord(npcId);
            return TryAdvanceNpcRequestState(npcRecord, NpcRequestState.Unlocked, currentDay);
        }

        public bool TryMarkNpcRequestOffered(string npcId, int currentDay)
        {
            return TryAdvanceNpcRequestState(npcId, NpcRequestState.Offered, currentDay);
        }

        public bool TryMarkNpcRequestOfferedFromPlayedEvent(string npcId, string eventId, int currentDay)
        {
            if (string.IsNullOrWhiteSpace(npcId)
                || string.IsNullOrWhiteSpace(eventId)
                || HasPlayedEvent(eventId) == false)
            {
                return false;
            }

            int playedDay = GetEventLastPlayDay(eventId);
            return TryAdvanceNpcRequestState(
                npcId,
                NpcRequestState.Offered,
                playedDay > 0 ? playedDay : currentDay);
        }

        public bool TryAdvanceNpcRequestState(string npcId, NpcRequestState targetState, int currentDay)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return false;

            NpcHistoryRecord npcRecord = FindOrCreateNpcRecord(npcId);
            return TryAdvanceNpcRequestState(npcRecord, targetState, currentDay);
        }

        public void RecordEncounter(VisitEventData visitEvent, int currentDay, string encounterRegionId = null)
        {
            if (visitEvent == null)
                return;

            string actualRegionId = string.IsNullOrWhiteSpace(encounterRegionId)
                ? visitEvent.RegionId
                : encounterRegionId;

            NpcHistoryRecord npcRecord = FindOrCreateNpcRecord(visitEvent.NpcId);
            npcRecord.visitCount++;
            npcRecord.lastVisitDay = currentDay;

            lastNpcId = visitEvent.NpcId;
            lastEventId = visitEvent.EventId;
            lastRegionId = actualRegionId;
            lastEncounterDay = currentDay;
            AddRecentEncounter(visitEvent, currentDay, actualRegionId);

            VisitEventHistoryRecord eventRecord = FindOrCreateEventRecord(visitEvent.EventId);
            eventRecord.npcId = visitEvent.NpcId;
            eventRecord.playCount++;
            eventRecord.lastPlayDay = currentDay;
        }

        public void RecordResult(
            string npcId,
            string eventId,
            NpcConversationResult result,
            int affinityDelta,
            int currentDay)
        {
            NpcHistoryRecord npcRecord = FindOrCreateNpcRecord(npcId);
            npcRecord.affinity = Mathf.Max(0, npcRecord.affinity + affinityDelta);
            npcRecord.lastResult = result.ToString();
            npcRecord.lastResultDay = currentDay;
            AddResultCount(npcRecord, result);

            VisitEventHistoryRecord eventRecord = FindOrCreateEventRecord(eventId);
            eventRecord.npcId = npcId;
            eventRecord.lastResult = result.ToString();
            eventRecord.lastResultDay = currentDay;
            AddResultCount(eventRecord, result);
        }

        public string BuildDebugSummary()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("NPC Encounter History");

            foreach (NpcHistoryRecord record in npcRecords)
            {
                builder.Append(record.npcId);
                builder.Append(": visits=");
                builder.Append(record.visitCount);
                builder.Append(", lastDate=");
                builder.Append(FormatHistoryDay(record.lastVisitDay));
                builder.Append(", affinity=");
                builder.Append(record.affinity);
                builder.Append(", lastResult=");
                builder.Append(string.IsNullOrWhiteSpace(record.lastResult) ? "None" : record.lastResult);
                builder.Append(", request=");
                builder.Append(BuildRequestStateSummary(record));

                builder.AppendLine();
            }

            if (recentEncounters.Count > 0)
            {
                builder.Append("Recent: ");
                int startIndex = Math.Max(0, recentEncounters.Count - 5);
                for (int i = startIndex; i < recentEncounters.Count; i++)
                {
                    RecentEncounterRecord record = recentEncounters[i];
                    if (i > startIndex)
                        builder.Append(" > ");

                    builder.Append(FormatHistoryDay(record.day));
                    builder.Append(":");
                    builder.Append(record.npcId);
                    builder.Append("/");
                    builder.Append(record.eventId);
                }

                builder.AppendLine();
            }

            builder.Append("LastEncounter: npc=");
            builder.Append(string.IsNullOrWhiteSpace(lastNpcId) ? "None" : lastNpcId);
            builder.Append(", event=");
            builder.Append(string.IsNullOrWhiteSpace(lastEventId) ? "None" : lastEventId);
            builder.Append(", region=");
            builder.Append(string.IsNullOrWhiteSpace(lastRegionId) ? "None" : lastRegionId);
            builder.Append(", date=");
            builder.AppendLine(FormatHistoryDay(lastEncounterDay));

            return builder.ToString();
        }

        private static string FormatHistoryDay(int dayIndex)
        {
            return dayIndex > 0 ? NpcImperialCalendar.FormatDayIndex(dayIndex) : "None";
        }

        public void Save()
        {
            if (string.IsNullOrWhiteSpace(_saveKey))
                return;

            PlayerPrefs.SetString(_saveKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            npcRecords.Clear();
            eventRecords.Clear();
            recentEncounters.Clear();
            lastNpcId = string.Empty;
            lastEventId = string.Empty;
            lastRegionId = string.Empty;
            lastEncounterDay = 0;

            if (string.IsNullOrWhiteSpace(_saveKey) == false)
            {
                PlayerPrefs.DeleteKey(_saveKey);
                PlayerPrefs.Save();
            }
        }

        private void Normalize()
        {
            if (npcRecords == null)
                npcRecords = new List<NpcHistoryRecord>();

            if (eventRecords == null)
                eventRecords = new List<VisitEventHistoryRecord>();

            if (recentEncounters == null)
                recentEncounters = new List<RecentEncounterRecord>();

            foreach (NpcHistoryRecord record in npcRecords)
            {
                NormalizeRequestState(record);
            }

            TrimRecentEncounters();
        }

        private IReadOnlyList<string> GetRecentIds(
            string regionId,
            int count,
            Func<RecentEncounterRecord, string> selector)
        {
            if (count <= 0 || recentEncounters.Count == 0)
                return new List<string>();

            List<string> result = new List<string>();
            for (int i = recentEncounters.Count - 1; i >= 0 && result.Count < count; i--)
            {
                RecentEncounterRecord record = recentEncounters[i];
                if (IsSameRegion(record.regionId, regionId) == false)
                    continue;

                string id = selector(record);
                if (string.IsNullOrWhiteSpace(id) || result.Contains(id))
                    continue;

                result.Add(id);
            }

            return result;
        }

        private static bool IsSameRegion(string recordRegionId, string targetRegionId)
        {
            return string.IsNullOrWhiteSpace(targetRegionId)
                   || string.Equals(recordRegionId, targetRegionId, StringComparison.OrdinalIgnoreCase);
        }

        private void AddRecentEncounter(VisitEventData visitEvent, int currentDay, string actualRegionId)
        {
            recentEncounters.Add(new RecentEncounterRecord
            {
                npcId = visitEvent.NpcId,
                eventId = visitEvent.EventId,
                regionId = actualRegionId,
                day = currentDay
            });

            TrimRecentEncounters();
        }

        private void TrimRecentEncounters()
        {
            int overflowCount = recentEncounters.Count - MaxRecentEncounterRecords;
            if (overflowCount > 0)
                recentEncounters.RemoveRange(0, overflowCount);
        }

        private NpcHistoryRecord FindNpcRecord(string npcId)
        {
            return npcRecords.Find(record => record.npcId == npcId);
        }

        private VisitEventHistoryRecord FindEventRecord(string eventId)
        {
            return eventRecords.Find(record => record.eventId == eventId);
        }

        private NpcHistoryRecord FindOrCreateNpcRecord(string npcId)
        {
            NpcHistoryRecord record = FindNpcRecord(npcId);
            if (record != null)
                return record;

            record = new NpcHistoryRecord { npcId = npcId };
            npcRecords.Add(record);
            return record;
        }

        private VisitEventHistoryRecord FindOrCreateEventRecord(string eventId)
        {
            VisitEventHistoryRecord record = FindEventRecord(eventId);
            if (record != null)
                return record;

            record = new VisitEventHistoryRecord { eventId = eventId };
            eventRecords.Add(record);
            return record;
        }

        private static void AddResultCount(NpcHistoryRecord record, NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    record.perfectCount++;
                    break;
                case NpcConversationResult.Correct:
                    record.correctCount++;
                    break;
                case NpcConversationResult.Similar:
                    record.similarCount++;
                    break;
                case NpcConversationResult.Wrong:
                    record.wrongCount++;
                    break;
                case NpcConversationResult.Disgusting:
                    record.disgustingCount++;
                    break;
            }
        }

        private static void AddResultCount(VisitEventHistoryRecord record, NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    record.perfectCount++;
                    break;
                case NpcConversationResult.Correct:
                    record.correctCount++;
                    break;
                case NpcConversationResult.Similar:
                    record.similarCount++;
                    break;
                case NpcConversationResult.Wrong:
                    record.wrongCount++;
                    break;
                case NpcConversationResult.Disgusting:
                    record.disgustingCount++;
                    break;
            }
        }

        private static bool TryAdvanceNpcRequestState(
            NpcHistoryRecord record,
            NpcRequestState targetState,
            int currentDay)
        {
            if (record == null || targetState == NpcRequestState.Locked)
                return false;

            NormalizeRequestState(record);
            NpcRequestState currentState = ResolveRequestState(record);
            if (GetRequestStateRank(currentState) >= GetRequestStateRank(targetState))
                return false;

            ApplyRequestState(record, targetState, currentDay);
            return true;
        }

        private static void ApplyRequestState(NpcHistoryRecord record, NpcRequestState targetState, int currentDay)
        {
            int safeDay = Mathf.Max(1, currentDay);
            record.requestState = targetState;

            for (int state = (int)NpcRequestState.Unlocked; state <= (int)targetState; state++)
            {
                EnsureRequestStateDay(record, (NpcRequestState)state, safeDay);
            }

            record.requestUnlocked = true;
            if (record.requestUnlockedDay <= 0)
                record.requestUnlockedDay = safeDay;
        }

        private static void NormalizeRequestState(NpcHistoryRecord record)
        {
            if (record == null)
                return;

            if (record.requestState == NpcRequestState.Locked && record.requestUnlocked)
                record.requestState = NpcRequestState.Unlocked;

            if (record.requestState == NpcRequestState.Locked)
                return;

            record.requestUnlocked = true;
            if (record.requestUnlockedDay <= 0)
                record.requestUnlockedDay = FindFallbackRequestStateDay(record);
        }

        private static NpcRequestState ResolveRequestState(NpcHistoryRecord record)
        {
            if (record == null)
                return NpcRequestState.Locked;

            if (record.requestState != NpcRequestState.Locked)
                return record.requestState;

            return record.requestUnlocked ? NpcRequestState.Unlocked : NpcRequestState.Locked;
        }

        private static string BuildRequestStateSummary(NpcHistoryRecord record)
        {
            NpcRequestState state = ResolveRequestState(record);
            int day = GetRequestStateDay(record, state);
            if (day <= 0)
                return state.ToString();

            return $"{state}@{FormatHistoryDay(day)}";
        }

        private static int GetRequestStateDay(NpcHistoryRecord record, NpcRequestState state)
        {
            if (record == null)
                return 0;

            return state switch
            {
                NpcRequestState.Unlocked => record.requestUnlockedDay,
                NpcRequestState.Offered => record.requestOfferedDay,
                NpcRequestState.Accepted => record.requestAcceptedDay,
                NpcRequestState.InProgress => record.requestInProgressDay,
                NpcRequestState.ReadyToComplete => record.requestReadyToCompleteDay,
                NpcRequestState.Completed => record.requestCompletedDay,
                NpcRequestState.EpilogueAvailable => record.requestEpilogueAvailableDay,
                NpcRequestState.EpilogueCompleted => record.requestEpilogueCompletedDay,
                _ => 0
            };
        }

        private static void EnsureRequestStateDay(NpcHistoryRecord record, NpcRequestState state, int currentDay)
        {
            switch (state)
            {
                case NpcRequestState.Unlocked:
                    if (record.requestUnlockedDay <= 0)
                        record.requestUnlockedDay = currentDay;
                    break;
                case NpcRequestState.Offered:
                    if (record.requestOfferedDay <= 0)
                        record.requestOfferedDay = currentDay;
                    break;
                case NpcRequestState.Accepted:
                    if (record.requestAcceptedDay <= 0)
                        record.requestAcceptedDay = currentDay;
                    break;
                case NpcRequestState.InProgress:
                    if (record.requestInProgressDay <= 0)
                        record.requestInProgressDay = currentDay;
                    break;
                case NpcRequestState.ReadyToComplete:
                    if (record.requestReadyToCompleteDay <= 0)
                        record.requestReadyToCompleteDay = currentDay;
                    break;
                case NpcRequestState.Completed:
                    if (record.requestCompletedDay <= 0)
                        record.requestCompletedDay = currentDay;
                    break;
                case NpcRequestState.EpilogueAvailable:
                    if (record.requestEpilogueAvailableDay <= 0)
                        record.requestEpilogueAvailableDay = currentDay;
                    break;
                case NpcRequestState.EpilogueCompleted:
                    if (record.requestEpilogueCompletedDay <= 0)
                        record.requestEpilogueCompletedDay = currentDay;
                    break;
            }
        }

        private static int FindFallbackRequestStateDay(NpcHistoryRecord record)
        {
            int fallbackDay = record.requestUnlockedDay;
            fallbackDay = GetEarlierPositiveDay(fallbackDay, record.requestOfferedDay);
            fallbackDay = GetEarlierPositiveDay(fallbackDay, record.requestAcceptedDay);
            fallbackDay = GetEarlierPositiveDay(fallbackDay, record.requestInProgressDay);
            fallbackDay = GetEarlierPositiveDay(fallbackDay, record.requestReadyToCompleteDay);
            fallbackDay = GetEarlierPositiveDay(fallbackDay, record.requestCompletedDay);
            fallbackDay = GetEarlierPositiveDay(fallbackDay, record.requestEpilogueAvailableDay);
            fallbackDay = GetEarlierPositiveDay(fallbackDay, record.requestEpilogueCompletedDay);
            return fallbackDay > 0 ? fallbackDay : record.lastVisitDay;
        }

        private static int GetEarlierPositiveDay(int currentDay, int candidateDay)
        {
            if (candidateDay <= 0)
                return currentDay;

            if (currentDay <= 0)
                return candidateDay;

            return Math.Min(currentDay, candidateDay);
        }

        private static int GetRequestStateRank(NpcRequestState state)
        {
            return (int)state;
        }
    }

    [Serializable]
    public sealed class NpcHistoryRecord
    {
        public string npcId;
        public int visitCount;
        public int lastVisitDay;
        public int affinity;
        public string lastResult;
        public int lastResultDay;
        public int perfectCount;
        public int correctCount;
        public int similarCount;
        public int wrongCount;
        public int disgustingCount;
        public bool requestUnlocked;
        public int requestUnlockedDay;
        public NpcRequestState requestState;
        public int requestOfferedDay;
        public int requestAcceptedDay;
        public int requestInProgressDay;
        public int requestReadyToCompleteDay;
        public int requestCompletedDay;
        public int requestEpilogueAvailableDay;
        public int requestEpilogueCompletedDay;
    }

    [Serializable]
    public sealed class VisitEventHistoryRecord
    {
        public string eventId;
        public string npcId;
        public int playCount;
        public int lastPlayDay;
        public string lastResult;
        public int lastResultDay;
        public int perfectCount;
        public int correctCount;
        public int similarCount;
        public int wrongCount;
        public int disgustingCount;
    }

    [Serializable]
    public sealed class RecentEncounterRecord
    {
        public string npcId;
        public string eventId;
        public string regionId;
        public int day;
    }

    public static class NpcAffinityUtility
    {
        private static readonly int[] LevelThresholds = { 0, 1, 2, 4, 6, 9 };
        private static readonly string[] LevelLabels =
        {
            "낯선 손님",
            "안면 있음",
            "이름을 기억함",
            "믿고 맡김",
            "단골 후보",
            "특별 의뢰 가능"
        };

        public static int GetLevel(int affinity)
        {
            int safeAffinity = Mathf.Max(0, affinity);
            int level = 0;
            for (int i = 0; i < LevelThresholds.Length; i++)
            {
                if (safeAffinity >= LevelThresholds[i])
                    level = i;
            }

            return level;
        }

        public static string GetLabel(int affinity)
        {
            int level = GetLevel(affinity);
            return LevelLabels[Mathf.Clamp(level, 0, LevelLabels.Length - 1)];
        }

        public static int GetNextThreshold(int affinity)
        {
            int safeAffinity = Mathf.Max(0, affinity);
            foreach (int threshold in LevelThresholds)
            {
                if (threshold > safeAffinity)
                    return threshold;
            }

            return -1;
        }

        public static string BuildProgressText(int affinity)
        {
            int level = GetLevel(affinity);
            int nextThreshold = GetNextThreshold(affinity);
            string label = GetLabel(affinity);

            if (nextThreshold < 0)
                return $"Lv.{level} {label} ({affinity})";

            return $"Lv.{level} {label} ({affinity}/{nextThreshold})";
        }
    }

    public static class NpcImperialCalendar
    {
        public const string EraName = "제국력";

        private static readonly DateTime StartDate = new DateTime(975, 7, 14);

        public static DateTime ToDate(int dayIndex)
        {
            int safeDayIndex = Math.Max(1, dayIndex);
            return StartDate.AddDays(safeDayIndex - 1);
        }

        public static bool TryToDayIndex(int year, int month, int day, out int dayIndex)
        {
            dayIndex = 1;

            try
            {
                DateTime date = new DateTime(year, month, day);
                if (date < StartDate)
                    return false;

                dayIndex = (date - StartDate).Days + 1;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        public static string FormatDayIndex(int dayIndex)
        {
            DateTime date = ToDate(dayIndex);
            return $"{EraName} {date.Year}년 {date.Month}월 {date.Day}일";
        }
    }
}
