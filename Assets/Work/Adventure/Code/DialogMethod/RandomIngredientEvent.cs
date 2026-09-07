using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Work.Adventure.Code.UI;
using Work.Cook.Code.Data;
using Work.Core.EventBus;
using Work.Players.Code.Inventory;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using Random = UnityEngine.Random;

namespace Work.Adventure.Code.DialogMethod
{
    [Serializable]
    public class ImageAndItem
    {
        public IngredientItemDataSO itemDataSO;
        public Image imagePrefab;
    }

    [Serializable]
    public class RandomIngredientEvent : AdventrueDialogEvent
    {
        [SerializeField] private List<ImageAndItem> randomItemList = new List<ImageAndItem>();

        private RectTransform _root;

        public override void Init(RectTransform root)
        {
            _root = root;
        }

        public override void RaiseEvent()
        {
            if (randomItemList == null || randomItemList.Count == 0)
                return;
            int randomCount = UnityEngine.Random.Range(2, 6);
            List<ImageAndItem> tempList = new List<ImageAndItem>();

            for (int i = 0; i < randomCount; i++)
            {
                tempList.Add(randomItemList[Random.Range(0, randomItemList.Count)]);
            }

            for (int i = 0; i < randomCount; i++)
            {
                ImageAndItem data = tempList[i];
                if (data == null || data.itemDataSO == null)
                    continue;
                Image image = MonoBehaviour.Instantiate(data.imagePrefab, _root);
                image.rectTransform.anchoredPosition = new Vector2(Random.Range(-100f,100),0);
                Bus<InventoryItemAddRequestedEvent>.Raise(new InventoryItemAddRequestedEvent(data.itemDataSO, 1));
                Bus<OnPlusLogCreateEvent>.Raise(new OnPlusLogCreateEvent(new ItemLogData(data.itemDataSO.DisplayName, ItemLogStatusEnum.Add, data.itemDataSO.Icon)));
            }
        }
    }
}
