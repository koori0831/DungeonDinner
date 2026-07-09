using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using Work.Adventure.Code.UI;
using Work.Core.EventBus;

namespace Work.Adventure.Code.DialogMethod
{
    [Serializable]
    public class DeleteAdventureItem : AdventrueDialogEvent
    {
        [SerializeField] private AdventureItemSO itemSo;
        [SerializeField] private ItemLogStatusEnum status;

        public override void Init(RectTransform root)
        {

        }

        public override void RaiseEvent()
        {
            Bus<OnUseAdventureItem>.Raise(new OnUseAdventureItem(itemSo));
            Bus<OnMinusLogCreateEvent>.Raise(new OnMinusLogCreateEvent(new ItemLogData(itemSo.ItemName, status, itemSo.ItemIcon)));
        }
    }
}
