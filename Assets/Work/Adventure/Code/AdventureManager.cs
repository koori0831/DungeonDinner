using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Work.Adventure.Code.UI;
using Work.Core.EventBus;
using Work.UtillUI.Code.Fade;
using Work.TimeSystem;
using Work.Players.Code.Inventory;
using System;
using Work.UtillUI.Code;

namespace Work.Adventure.Code
{
    public readonly record struct OnAddAdventureItemEvent(AdventureItemSO itemSO) : IEvent;
    public readonly record struct OnRemoveAdventureItemEvent(AdventureItemSO itemSO) : IEvent;
    public readonly record struct OnAddAdventureItemAfterEvent(AdventureItemSO itemSO, int count) : IEvent;
    public readonly record struct OnRemoveAdventureItemAfterEvent(AdventureItemSO itemSO, int count) : IEvent;

    public class AdventureManager : MonoBehaviour
    {
        [SerializeField] PreparationManager preparationManager;
        [SerializeField] private AdventureEventSO firstHarvestEvent;
        [SerializeField] private AdventureBackground background;
        [SerializeField] private AdventureDialogUI dialog;
        [SerializeField] private AdventureItemUI itemUI;
        [SerializeField] private GameTimeService gameTimeService;

        [SerializeField] private List<AdventureEventSO> eventList = new List<AdventureEventSO>();
        [SerializeField, Range(0f, 1f)] private float supplyEventChance = 0.35f;
        [SerializeField, Min(1)] private int maxEventsWithoutSupply = 3;
        [SerializeField, Min(1f)] private float missingToolWeight = 2f;
        private readonly AdventureEventSelector _eventSelector = new AdventureEventSelector();

        private AdventureEventSO _currentEvent;
        private Dictionary<string, int> _adventureItemDic = new Dictionary<string, int>();
        private bool _isAdventureRunning;
        private bool _initialized;
        private bool _firstHarvestPending = true;
        private IDisposable _walkingInput;
        private int _transitionVersion;
        public AdventurePhase Phase { get; private set; }
        public bool IsAdventureRunning => _isAdventureRunning;
        public bool FirstHarvestCompleted => !_firstHarvestPending;
        private Tween _startTransitionDelay;
        private Tween _stopTransitionDelay;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;
            if (gameTimeService == null)
                gameTimeService = FindFirstObjectByType<GameTimeService>();

            dialog.EventFinished += HandleEventFinished;
            dialog.BindContinuation(ProgressAdventure, StopAdventure);
            Bus<OnHaveItemEvent, BoolenReturnValue>.Events += HandleHaveItemCheckEvent;
            Bus<OnAddAdventureItemEvent>.Events += HandleAddAdventureItemEvent;
            Bus<OnRemoveAdventureItemEvent>.Events += HandleUseAdventureItemEvent;
        }

        private void OnDestroy()
        {
            KillTransitionDelays();
            if (dialog != null) dialog.EventFinished -= HandleEventFinished;
            Bus<OnAddAdventureItemEvent>.Events -= HandleAddAdventureItemEvent;
            Bus<OnRemoveAdventureItemEvent>.Events -= HandleUseAdventureItemEvent;
            Bus<OnHaveItemEvent, BoolenReturnValue>.Events -= HandleHaveItemCheckEvent;
        }

        private void HandleUseAdventureItemEvent(OnRemoveAdventureItemEvent item)
        {
            if (_adventureItemDic.TryGetValue(item.itemSO.ItemName, out int count) && count > 0)
            {
                _adventureItemDic[item.itemSO.ItemName] -= 1;
                Bus<OnRemoveAdventureItemAfterEvent>.Raise(new OnRemoveAdventureItemAfterEvent(item.itemSO, _adventureItemDic[item.itemSO.ItemName]));
            }
        }

        private void HandleAddAdventureItemEvent(OnAddAdventureItemEvent item)
        {
            if (_adventureItemDic.ContainsKey(item.itemSO.ItemName))
                _adventureItemDic[item.itemSO.ItemName] += 1;
            else
                _adventureItemDic.Add(item.itemSO.ItemName, 1);

            Bus<OnAddAdventureItemAfterEvent>.Raise(new OnAddAdventureItemAfterEvent(item.itemSO, _adventureItemDic[item.itemSO.ItemName]));
        }

        private BoolenReturnValue HandleHaveItemCheckEvent(OnHaveItemEvent evt)
        {
            bool isTrue = false;
            if (_adventureItemDic.ContainsKey(evt.itemSO.ItemName))
            {
                if (_adventureItemDic[evt.itemSO.ItemName] > 0)
                    isTrue = true;
            }

            BoolenReturnValue value = new BoolenReturnValue(isTrue);
            return value;
        }

        public void StartAdventure()
        {
            if (_isAdventureRunning || Phase == AdventurePhase.Exiting)
                return;

            _isAdventureRunning = true;
            Phase = AdventurePhase.Entering;
            int version = ++_transitionVersion;
            GameUiInput.SetContext(GameUiContext.Adventure);
            Bus<OnFadeInEvent>.Raise(new OnFadeInEvent(() =>
            {
                if (version != _transitionVersion || !_isAdventureRunning) return;
                itemUI.Enable();
                background.Enable();
                _startTransitionDelay?.Kill(false);
                _startTransitionDelay = DOVirtual.DelayedCall(
                        0.5f,
                        () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent(() => BeginNextEvent(version))))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }));
        }

        public void ProgressAdventure()
        {
            if (Phase != AdventurePhase.Choice || GameUiInput.IsBlocked) return;
            BeginNextEvent(_transitionVersion);
        }

        private void BeginNextEvent(int version)
        {
            if (!_isAdventureRunning || version != _transitionVersion) return;
            Phase = AdventurePhase.Walking;
            dialog.ResetDialog();
            _walkingInput?.Dispose();
            _walkingInput = GameUiInput.Acquire();
            background.Walking(() =>
            {
                if (!_isAdventureRunning || version != _transitionVersion) return;
                var inventory = FindFirstObjectByType<PlayerInventoryModule>();
                bool HasItem(AdventureItemSO item) => item != null
                    && _adventureItemDic.TryGetValue(item.ItemName, out int count) && count > 0;
                bool CanSelect(Options option) => option is LockedOption locked
                    ? locked.KeyItem != null && HasItem(locked.KeyItem) != locked.IsUnLockOption
                    : !(option is IngredientLockedOption ingredient) || ingredient.CanSelect(inventory);
                _currentEvent = _firstHarvestPending && firstHarvestEvent != null ? firstHarvestEvent : _eventSelector.Select(eventList, _currentEvent,
                    _adventureItemDic.Values.Sum(count => Mathf.Max(0, count)), HasItem, CanSelect,
                    supplyEventChance, maxEventsWithoutSupply, missingToolWeight, () => UnityEngine.Random.value);
                if (_currentEvent == null)
                {
                    Debug.LogWarning("진행할 어드벤처 이벤트가 없습니다.", this);
                    ExitAdventure();
                    return;
                }
                Phase = AdventurePhase.Dialog;
                dialog.StartDialog(_currentEvent);
                _walkingInput?.Dispose();
                _walkingInput = null;
            });
        }

        private void HandleEventFinished()
        {
            if (!_isAdventureRunning) return;
            if (_currentEvent == firstHarvestEvent) _firstHarvestPending = false;
            Phase = AdventurePhase.Choice;
        }

        public void StopAdventure()
        {
            if (!_isAdventureRunning || Phase != AdventurePhase.Choice || GameUiInput.IsBlocked)
                return;
            ExitAdventure();
        }

        private void ExitAdventure()
        {
            _isAdventureRunning = false;
            Phase = AdventurePhase.Exiting;
            int version = ++_transitionVersion;
            dialog.ResetDialog();
            _walkingInput?.Dispose();
            _walkingInput = null;
            if (gameTimeService != null)
                gameTimeService.AdvanceTime(1, GameTimeActivityType.Adventure);
            else
                Debug.LogError("GameTimeService가 없어 모험 시간을 반영하지 못했습니다.", this);

            Bus<OnFadeInEvent>.Raise(new OnFadeInEvent(() =>
            {
                if (version != _transitionVersion) return;
                itemUI.Disable();
                background.Disable();
                preparationManager.StopAdventure();
                _stopTransitionDelay?.Kill(false);
                _stopTransitionDelay = DOVirtual.DelayedCall(
                        0.5f,
                        () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent(() => Phase = AdventurePhase.Inactive)))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }));
        }

        private void OnDisable()
        {
            ++_transitionVersion;
            _isAdventureRunning = false;
            Phase = AdventurePhase.Inactive;
            _walkingInput?.Dispose();
            _walkingInput = null;
            KillTransitionDelays();
        }

        private void KillTransitionDelays()
        {
            _startTransitionDelay?.Kill(false);
            _startTransitionDelay = null;
            _stopTransitionDelay?.Kill(false);
            _stopTransitionDelay = null;
        }
    }

    public enum AdventurePhase { Inactive, Entering, Walking, Dialog, Choice, Exiting }
}
