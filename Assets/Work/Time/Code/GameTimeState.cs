using System;

namespace Work.TimeSystem
{
    /// <summary>
    /// 누적 시간을 기준으로 날짜와 하루 안의 시간을 계산하는 순수 런타임 상태입니다.
    /// </summary>
    [Serializable]
    public sealed class GameTimeState
    {
        public const int TimeUnitsPerDay = 6;

        private int _totalElapsedTime;

        public int TotalElapsedTime => _totalElapsedTime;
        public int CurrentDay => _totalElapsedTime / TimeUnitsPerDay + 1;
        public int CurrentTimeOfDay => _totalElapsedTime % TimeUnitsPerDay;

        public GameTimeState(int totalElapsedTime = 0)
        {
            _totalElapsedTime = Math.Max(0, totalElapsedTime);
        }

        public GameTimeChange Advance(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "증가 시간은 1 이상이어야 합니다.");
            }

            int previousTotalTime = _totalElapsedTime;
            int previousDay = CurrentDay;

            checked
            {
                _totalElapsedTime += amount;
            }

            return new GameTimeChange(
                previousTotalTime,
                _totalElapsedTime,
                previousDay,
                CurrentDay,
                CurrentTimeOfDay);
        }
    }

    public readonly struct GameTimeChange
    {
        public int PreviousTotalTime { get; }
        public int CurrentTotalTime { get; }
        public int PreviousDay { get; }
        public int CurrentDay { get; }
        public int CurrentTimeOfDay { get; }
        public bool DidDayChange => PreviousDay != CurrentDay;

        public GameTimeChange(
            int previousTotalTime,
            int currentTotalTime,
            int previousDay,
            int currentDay,
            int currentTimeOfDay)
        {
            PreviousTotalTime = previousTotalTime;
            CurrentTotalTime = currentTotalTime;
            PreviousDay = previousDay;
            CurrentDay = currentDay;
            CurrentTimeOfDay = currentTimeOfDay;
        }
    }
}
