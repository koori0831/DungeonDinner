using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Work.Items.Code.Editor.Tests
{
    public sealed class ItemCatalogSOTests
    {
        [Test]
        public void TryFindItem_IsCaseInsensitive()
        {
            ItemDataSO item = CreateItem("Moss_Mushroom");
            ItemCatalogSO catalog = CreateCatalog(item);

            bool found = catalog.TryFindItem("moss_mushroom", out ItemDataSO result);

            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(item));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(item);
        }

        [Test]
        public void Validation_ReportsDuplicateIds()
        {
            ItemDataSO first = CreateItem("duplicate");
            ItemDataSO second = CreateItem("DUPLICATE");
            ItemCatalogSO catalog = CreateCatalog(first, second);

            Assert.That(catalog.BuildValidationMessages(), Has.Some.Contains("Duplicate ItemId"));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        private static ItemDataSO CreateItem(string itemId)
        {
            ItemDataSO item = ScriptableObject.CreateInstance<ItemDataSO>();
            SerializedObject serializedItem = new SerializedObject(item);
            serializedItem.FindProperty("itemId").stringValue = itemId;
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static ItemCatalogSO CreateCatalog(params ItemDataSO[] items)
        {
            ItemCatalogSO catalog = ScriptableObject.CreateInstance<ItemCatalogSO>();
            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty itemsProperty = serializedCatalog.FindProperty("items");
            itemsProperty.arraySize = items.Length;

            for (int i = 0; i < items.Length; i++)
            {
                itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            catalog.RebuildIndex();
            return catalog;
        }
    }
}
