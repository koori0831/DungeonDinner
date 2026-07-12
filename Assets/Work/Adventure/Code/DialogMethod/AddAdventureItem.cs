using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Work.Adventure.Code;
using Work.Adventure.Code.UI;
using Work.Core.EventBus;

namespace Work.Adventure.Code.DialogMethod
{
    [Serializable]
    public class AddAdventureItem : AdventrueDialogEvent
    {
        [SerializeField] private AdventureItemSO itemSo;

        public override void Init(RectTransform root)
        {

        }

        public override void RaiseEvent()
        {
            Bus<OnAddAdventureItem>.Raise(new OnAddAdventureItem(itemSo));
            Bus<OnPlusLogCreateEvent>.Raise(new OnPlusLogCreateEvent(new ItemLogData(itemSo.ItemName, ItemLogStatusEnum.Add, itemSo.ItemIcon)));
        }
    }
}
