using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Work.Adventure.Code.UI;
using Work.Core.EventBus;
using Work.UtillUI.Code.Fade;

namespace Work.Adventure.Code
{
    public readonly record struct OnAddAdventureItem(AdventureItemSO itemSO) : IEvent;
    public readonly record struct OnUseAdventureItem(AdventureItemSO itemSO) : IEvent;

    public class AdventureManager : MonoBehaviour
    {
        [SerializeField] PreparationManager preparationManager;
        [SerializeField] private AdventureMapUI adventureMap;
        [SerializeField] private AdventureBackground background;
        [SerializeField] private AdventureDialogUI dialog;

        [SerializeField] private List<AdventureEventSO> eventList = new List<AdventureEventSO>();

        private AdventureEventSO _currentEvent;
        private Dictionary<string, int> _adventureItemDic = new Dictionary<string, int>();

        public void Init()
        {
            adventureMap.Init(StartAdventure);
            Bus<OnHaveItemEvent, BoolenReturnValue>.Events += HandleHaveItemCheckEvent;
            Bus<OnAddAdventureItem>.Events += HandleAddAdventureItemEvent;
            Bus<OnUseAdventureItem>.Events += HandleUseAdventureItemEvent;
        }

        private void OnDestroy()
        {
            Bus<OnAddAdventureItem>.Events -= HandleAddAdventureItemEvent;
            Bus<OnUseAdventureItem>.Events -= HandleUseAdventureItemEvent;
            Bus<OnHaveItemEvent, BoolenReturnValue>.Events -= HandleHaveItemCheckEvent;
        }

        private void HandleUseAdventureItemEvent(OnUseAdventureItem item)
        {
            _adventureItemDic[item.itemSO.ItemName] -= 1;
        }

        private void HandleAddAdventureItemEvent(OnAddAdventureItem item)
        {
            if (_adventureItemDic.ContainsKey(item.itemSO.ItemName))
                _adventureItemDic[item.itemSO.ItemName] += 1;
            else
                _adventureItemDic.Add(item.itemSO.ItemName,1);
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
                adventureMap.CloseMap();
                background.Enable();
                DOVirtual.DelayedCall(0.5f, () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent(ProgressAdventure)));
                //여기 워킹 안에 이벤트 뽑는거 연결 
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
                adventureMap.CloseMap();
                background.Disable();
                preparationManager.StopAdventure();
                DOVirtual.DelayedCall(0.5f, () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent()));
                //여기 워킹 안에 이벤트 뽑는거 연결 
            }));
        }
    }
}