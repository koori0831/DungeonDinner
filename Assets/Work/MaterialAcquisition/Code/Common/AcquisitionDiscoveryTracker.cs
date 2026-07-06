using System;
using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.MaterialAcquisition.Code.Common
{
    public sealed class AcquisitionDiscoveryTracker : MonoBehaviour
    {
        [SerializeField]
        private List<string> discoveredIngredientIds = new List<string>();

        private HashSet<string> discoveredIngredientIdSet;

        public IReadOnlyList<string> DiscoveredIngredientIds => discoveredIngredientIds;

        private void Awake()
        {
            EnsureLookup();
        }

        public bool IsDiscovered(IngredientSO ingredient)
        {
            string id = GetIngredientId(ingredient);
            if (string.IsNullOrWhiteSpace(id) == true)
            {
                return false;
            }

            EnsureLookup();
            return discoveredIngredientIdSet.Contains(id);
        }

        public bool MarkDiscovered(IngredientSO ingredient)
        {
            string id = GetIngredientId(ingredient);
            if (string.IsNullOrWhiteSpace(id) == true)
            {
                return false;
            }

            EnsureLookup();
            if (discoveredIngredientIdSet.Add(id) == false)
            {
                return false;
            }

            discoveredIngredientIds.Add(id);
            return true;
        }

        public void ClearForDebug()
        {
            discoveredIngredientIds.Clear();
            EnsureLookup(true);
        }

        private static string GetIngredientId(IngredientSO ingredient)
        {
            if (ingredient == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(ingredient.IngredientId) == false)
            {
                return ingredient.IngredientId.Trim();
            }

            return ingredient.name;
        }

        private void EnsureLookup(bool forceRebuild = false)
        {
            if (discoveredIngredientIdSet != null && forceRebuild == false)
            {
                return;
            }

            discoveredIngredientIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (discoveredIngredientIds == null)
            {
                discoveredIngredientIds = new List<string>();
                return;
            }

            for (int i = discoveredIngredientIds.Count - 1; i >= 0; i--)
            {
                string id = discoveredIngredientIds[i];
                if (string.IsNullOrWhiteSpace(id) == true)
                {
                    discoveredIngredientIds.RemoveAt(i);
                    continue;
                }

                discoveredIngredientIds[i] = id.Trim();
            }

            for (int i = 0; i < discoveredIngredientIds.Count; i++)
            {
                discoveredIngredientIdSet.Add(discoveredIngredientIds[i]);
            }
        }

        private void OnValidate()
        {
            EnsureLookup(true);
        }
    }
}
