using UnityEngine;
using Work.Core.EventBus;

namespace Work.TimeSystem
{
    /// <summary>
    /// 게임 전체에서 누적 시간의 유일한 쓰기 지점입니다.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameTimeService : MonoBehaviour
    {
        [SerializeField, Min(0)] private int initialTotalElapsedTime;
        [SerializeField] private bool persistTime = true;
        [SerializeField] private string saveKey = GameTimeRepository.DefaultSaveKey;

        private GameTimeState _state;
        private GameTimeRepository _repository;

        public int TotalElapsedTime => EnsureState().TotalElapsedTime;
        public int CurrentDay => EnsureState().CurrentDay;
        public int CurrentTimeOfDay => EnsureState().CurrentTimeOfDay;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_state != null)
            {
                return;
            }

            _repository = new GameTimeRepository(saveKey);
            int totalElapsedTime = initialTotalElapsedTime;

            if (persistTime)
            {
                totalElapsedTime = _repository.Load(initialTotalElapsedTime).TotalElapsedTime;
            }

            _state = new GameTimeState(totalElapsedTime);
        }

        public GameTimeChange AdvanceTime(int amount, GameTimeActivityType activityType)
        {
            GameTimeChange change = EnsureState().Advance(amount);

            if (persistTime)
            {
                _repository.Save(change.CurrentTotalTime);
            }

            Bus<GameTimeAdvancedEvent>.Raise(
                new GameTimeAdvancedEvent(
                    change.PreviousTotalTime,
                    change.CurrentTotalTime,
                    change.PreviousDay,
                    change.CurrentDay,
                    change.CurrentTimeOfDay,
                    amount,
                    activityType));

            if (change.DidDayChange)
            {
                Bus<GameDayChangedEvent>.Raise(
                    new GameDayChangedEvent(
                        change.PreviousDay,
                        change.CurrentDay,
                        change.CurrentTotalTime));
            }

            return change;
        }

        [ContextMenu("Reset Saved Game Time")]
        public void ResetSavedTime()
        {
            EnsureRepository().Delete();
            _state = new GameTimeState(initialTotalElapsedTime);
        }

        private GameTimeState EnsureState()
        {
            if (_state == null)
            {
                Initialize();
            }

            return _state;
        }

        private GameTimeRepository EnsureRepository()
        {
            if (_repository == null)
            {
                _repository = new GameTimeRepository(saveKey);
            }

            return _repository;
        }
    }
}
