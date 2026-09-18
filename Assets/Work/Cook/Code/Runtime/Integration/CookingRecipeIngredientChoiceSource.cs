using System.Collections.Generic;
using UnityEngine;
using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.Integration
{
    public sealed class CookingRecipeIngredientChoiceSource : MonoBehaviour,
        ICookingIngredientSource,
        ICookingIngredientQuantitySource,
        ICookingIngredientIconSource,
        ICookingIngredientConsumer,
        ICookingRecipePlanSource
    {
        private readonly List<IngredientSO> _candidates = new List<IngredientSO>();

        [SerializeField] private string sourceName = "레시피 재료 후보";

        private ICookingIngredientSource _backingSource;

        public string SourceName => sourceName;
        public CookingRecipeStartPlan Plan { get; private set; }
        public ICookingIngredientSource BackingSource => _backingSource;

        public IReadOnlyList<IngredientSO> GetAvailableIngredients(CookingGamePanel owner, CookingFlowRunner runner)
        {
            return _candidates;
        }

        public int GetAvailableIngredientQuantity(
            IngredientSO ingredient,
            CookingGamePanel owner,
            CookingFlowRunner runner)
        {
            if (ingredient == null || _candidates.Contains(ingredient) == false)
                return 0;
            ICookingIngredientQuantitySource quantities = _backingSource as ICookingIngredientQuantitySource;
            return quantities != null
                ? Mathf.Max(0, quantities.GetAvailableIngredientQuantity(ingredient, owner, runner))
                : 0;
        }

        public Sprite GetAvailableIngredientIcon(
            IngredientSO ingredient,
            CookingGamePanel owner,
            CookingFlowRunner runner)
        {
            ICookingIngredientIconSource icons = _backingSource as ICookingIngredientIconSource;
            Sprite icon = icons?.GetAvailableIngredientIcon(ingredient, owner, runner);
            return icon != null ? icon : CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
        }

        public int GetRequiredIngredientQuantity(IngredientSO ingredient)
        {
            return Plan != null ? Plan.GetRequiredQuantity(ingredient) : 0;
        }

        public bool IsSelectionValid(
            IReadOnlyList<IngredientSO> selectedIngredients,
            CookingGamePanel owner,
            CookingFlowRunner runner,
            out string reason)
        {
            if (Plan == null)
            {
                reason = "레시피 시작 계획이 없습니다.";
                return false;
            }
            return Plan.IsSelectionValid(
                selectedIngredients,
                ingredient => GetAvailableIngredientQuantity(ingredient, owner, runner),
                out reason);
        }

        public bool CanConsumeIngredients(
            IReadOnlyList<IngredientSO> ingredients,
            CookingGamePanel owner,
            CookingFlowRunner runner,
            out string reason)
        {
            ICookingIngredientConsumer consumer = _backingSource as ICookingIngredientConsumer;
            if (consumer == null)
            {
                reason = string.Empty;
                return true;
            }
            return consumer.CanConsumeIngredients(ingredients, owner, runner, out reason);
        }

        public bool TryConsumeIngredients(
            IReadOnlyList<IngredientSO> ingredients,
            CookingGamePanel owner,
            CookingFlowRunner runner,
            out string reason)
        {
            ICookingIngredientConsumer consumer = _backingSource as ICookingIngredientConsumer;
            if (consumer == null)
            {
                reason = string.Empty;
                return true;
            }
            return consumer.TryConsumeIngredients(ingredients, owner, runner, out reason);
        }

        public void SetPlan(CookingRecipeStartPlan plan, ICookingIngredientSource backingSource)
        {
            Plan = plan;
            _backingSource = backingSource;
            _candidates.Clear();
            if (plan?.Candidates != null)
            {
                for (int i = 0; i < plan.Candidates.Count; i++)
                {
                    IngredientSO candidate = plan.Candidates[i];
                    if (candidate != null && _candidates.Contains(candidate) == false)
                        _candidates.Add(candidate);
                }
            }
            NotifyIngredientsChanged();
        }

        public void SetCandidates(IReadOnlyList<IngredientSO> candidates)
        {
            Plan = null;
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
            bool changed = _candidates.Count > 0 || Plan != null || _backingSource != null;
            _candidates.Clear();
            Plan = null;
            _backingSource = null;
            if (changed)
                NotifyIngredientsChanged();
        }

        private void NotifyIngredientsChanged()
        {
            Bus<CookingIngredientSourceChangedEvent>.Raise(new CookingIngredientSourceChangedEvent(this));
        }
    }
}
