using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.Integration
{
    public sealed class CookingRecipeIngredientChoiceSource : MonoBehaviour, ICookingIngredientSource, ICookingIngredientQuantitySource
    {
        private readonly List<IngredientSO> _candidates = new List<IngredientSO>();

        [SerializeField] private string sourceName = "레시피 재료 후보";

        public string SourceName => sourceName;

        public IReadOnlyList<IngredientSO> GetAvailableIngredients(CookingGamePanel owner, CookingFlowRunner runner)
        {
            return _candidates;
        }

        public int GetAvailableIngredientQuantity(
            IngredientSO ingredient,
            CookingGamePanel owner,
            CookingFlowRunner runner)
        {
            return ingredient != null && _candidates.Contains(ingredient) ? 1 : 0;
        }

        public void SetCandidates(IReadOnlyList<IngredientSO> candidates)
        {
            _candidates.Clear();

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    IngredientSO candidate = candidates[i];
                    if (candidate != null && _candidates.Contains(candidate) == false)
                        _candidates.Add(candidate);
                }
            }

            NotifyIngredientsChanged();
        }

        public void Clear()
        {
            if (_candidates.Count == 0)
                return;

            _candidates.Clear();
            NotifyIngredientsChanged();
        }

        private void NotifyIngredientsChanged()
        {
            Bus<CookingIngredientSourceChangedEvent>.Raise(new CookingIngredientSourceChangedEvent(this));
        }
    }
}
