using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Work.Adventure.Code.UI;
using Work.Core.EventBus;
using Work.UtillUI.Code.Fade;

namespace Work.Adventure.Code
{
    public readonly record struct OnAddAdventureItemEvent(AdventureItemSO itemSO) : IEvent;
    public readonly record struct OnRemoveAdventureItemEvent(AdventureItemSO itemSO) : IEvent;
    public readonly record struct OnAddAdventureItemAfterEvent(AdventureItemSO itemSO, int count) : IEvent;
    public readonly record struct OnRemoveAdventureItemAfterEvent(AdventureItemSO itemSO, int count) : IEvent;

    public class AdventureManager : MonoBehaviour
    {
        [SerializeField] PreparationManager preparationManager;
        [SerializeField] private AdventureMapUI adventureMap;
        [SerializeField] private AdventureBackground background;
        [SerializeField] private AdventureDialogUI dialog;
        [SerializeField] private AdventureItemUI itemUI;

        [SerializeField] private List<AdventureEventSO> eventList = new List<AdventureEventSO>();

        private AdventureEventSO _currentEvent;
        private Dictionary<string, int> _adventureItemDic = new Dictionary<string, int>();

        public void Init()
        {
            adventureMap.Init(StartAdventure);
            Bus<OnHaveItemEvent, BoolenReturnValue>.Events += HandleHaveItemCheckEvent;
            Bus<OnAddAdventureItemEvent>.Events += HandleAddAdventureItemEvent;
            Bus<OnRemoveAdventureItemEvent>.Events += HandleUseAdventureItemEvent;
        }

        private void OnDestroy()
        {
            Bus<OnAddAdventureItemEvent>.Events -= HandleAddAdventureItemEvent;
            Bus<OnRemoveAdventureItemEvent>.Events -= HandleUseAdventureItemEvent;
            Bus<OnHaveItemEvent, BoolenReturnValue>.Events -= HandleHaveItemCheckEvent;
        }

        private void HandleUseAdventureItemEvent(OnRemoveAdventureItemEvent item)
        {
            if (_adventureItemDic.ContainsKey(item.itemSO.ItemName))
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

        public void OpenMap()
        {
            adventureMap.OpenMap();
        }

        public void StartAdventure()
        {
            Bus<OnFadeInEvent>.Raise(new OnFadeInEvent(() =>
            {
                itemUI.Enable();
                adventureMap.CloseMap();
                background.Enable();
                DOVirtual.DelayedCall(0.5f, () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent(ProgressAdventure)));
            }));
        }

        public void ProgressAdventure()
        {
            background.Walking(() =>
            {
                _currentEvent = _currentEvent != null ? eventList.Where(x => x != _currentEvent).ToList()[Random.Range(0, eventList.Count - 1)] : eventList[Random.Range(0, eventList.Count)];
                dialog.StartDialog(_currentEvent);
            });
        }

        public void StopAdventure()
        {
            Bus<OnFadeInEvent>.Raise(new OnFadeInEvent(() =>
            {
                itemUI.Disable();
                adventureMap.CloseMap();
                background.Disable();
                preparationManager.StopAdventure();
                DOVirtual.DelayedCall(0.5f, () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent()));
            }));
        }
    }
}