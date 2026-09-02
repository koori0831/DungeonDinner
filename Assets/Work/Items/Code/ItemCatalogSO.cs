using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Items.Code
{
    /// <summary>
    /// 저장된 ItemId를 실제 아이템 에셋으로 복원하기 위한 공용 카탈로그입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Items/Item Catalog")]
    public sealed class ItemCatalogSO : ScriptableObject
    {
        [SerializeField] private List<ItemDataSO> items = new List<ItemDataSO>();

        private readonly Dictionary<string, ItemDataSO> _itemsById =
            new Dictionary<string, ItemDataSO>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<ItemDataSO> Items => items;

        private void OnEnable()
        {
            RebuildIndex();
        }

        private void OnValidate()
        {
            RebuildIndex();
        }

        public void RebuildIndex()
        {
            _itemsById.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                ItemDataSO item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                if (_itemsById.ContainsKey(item.ItemId) == false)
                {
                    _itemsById.Add(item.ItemId, item);
                }
            }
        }

        public bool TryFindItem(string itemId, out ItemDataSO item)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                item = null;
                return false;
            }

            if (_itemsById.Count == 0 && items.Count > 0)
            {
                RebuildIndex();
            }

            return _itemsById.TryGetValue(itemId.Trim(), out item);
        }

        public ItemDataSO FindItem(string itemId)
        {
            return TryFindItem(itemId, out ItemDataSO item) ? item : null;
        }

        public List<string> BuildValidationMessages()
        {
            List<string> messages = new List<string>();
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < items.Count; i++)
            {
                ItemDataSO item = items[i];
                if (item == null)
                {
                    messages.Add($"Item catalog entry {i} is empty.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    messages.Add($"Item has an empty ItemId: {item.name}");
                    continue;
                }

                if (ids.Add(item.ItemId.Trim()) == false)
                {
                    messages.Add($"Duplicate ItemId: {item.ItemId}");
                }
            }

            return messages;
        }
    }
}
