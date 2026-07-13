using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Core.EventBus;

namespace Work.Adventure.Code.UI
{
    public class AdventureItemUI : MonoBehaviour
    {
        [SerializeField] private AdventureItemIconUI iconUIPrefab;

        private Dictionary<string, AdventureItemIconUI> adventureItemIconsDic = new Dictionary<string, AdventureItemIconUI>();

        public void Awake()
        {
            Bus<OnAddAdventureItemAfterEvent>.Events += HandleAddAdventureItemEvent;
            Bus<OnRemoveAdventureItemAfterEvent>.Events += HandleRemoveAdvnetureItemEvent;
        }

        public void OnDestroy()
        {
            Bus<OnAddAdventureItemAfterEvent>.Events -= HandleAddAdventureItemEvent;
            Bus<OnRemoveAdventureItemAfterEvent>.Events -= HandleRemoveAdvnetureItemEvent;
        }

        private void HandleRemoveAdvnetureItemEvent(OnRemoveAdventureItemAfterEvent item)
        {
            if(item.count == 0 && adventureItemIconsDic.ContainsKey(item.itemSO.ItemName))
            {
                AdventureItemIconUI ui = adventureItemIconsDic[item.itemSO.ItemName];
                Destroy(ui.gameObject);
                adventureItemIconsDic.Remove(item.itemSO.ItemName);
            }
            else if(adventureItemIconsDic.ContainsKey(item.itemSO.ItemName))
            {
                AdventureItemIconUI ui = adventureItemIconsDic[item.itemSO.ItemName];
                ui.SetCount(item.count);
            }
        }

        private void HandleAddAdventureItemEvent(OnAddAdventureItemAfterEvent item)
        {
            AdventureItemIconUI iconUI = Instantiate(iconUIPrefab,transform);
            iconUI.Init(item.itemSO.ItemIcon, item.count);

            adventureItemIconsDic.Add(item.itemSO.ItemName, iconUI);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }

        public void Enable()
        {
            gameObject.SetActive(true);
        }
    }
}