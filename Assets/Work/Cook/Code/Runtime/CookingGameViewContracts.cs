using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public interface ICookingIngredientSource
    {
        event Action IngredientsChanged;
        string SourceName { get; }
        IReadOnlyList<IngredientSO> GetAvailableIngredients(CookingGamePanel owner, CookingFlowRunner runner);
    }

    public interface ICookingIngredientQuantitySource
    {
        int GetAvailableIngredientQuantity(
            IngredientSO ingredient,
            CookingGamePanel owner,
            CookingFlowRunner runner);
    }

    public interface ICookingIngredientIconSource
    {
        Sprite GetAvailableIngredientIcon(
            IngredientSO ingredient,
            CookingGamePanel owner,
            CookingFlowRunner runner);
    }

    public interface ICookingIngredientConsumer
    {
        bool CanConsumeIngredients(
            IReadOnlyList<IngredientSO> ingredients,
            CookingGamePanel owner,
            CookingFlowRunner runner,
            out string reason);

        bool TryConsumeIngredients(
            IReadOnlyList<IngredientSO> ingredients,
            CookingGamePanel owner,
            CookingFlowRunner runner,
            out string reason);
    }

    public interface ICookingRecipeSelectionView
    {
        void Initialize(CookingGamePanel owner, CookingFlowRunner runner, CookingKnowledgeStore knowledgeStore);
        void Refresh();
    }

    public interface ICookingIngredientSelectionView
    {
        void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null);
        void SetIngredientSource(ICookingIngredientSource source);
        void SetSelectionLimits(int minCount, int maxCount = 0);
        void SetFontAsset(TMP_FontAsset value);
        void SetSearchQuery(string query);
        ICookingIngredientSource GetCurrentIngredientSource();
        void ToggleIngredient(IngredientSO ingredient);
        void RemoveIngredient(IngredientSO ingredient);
        void ClearSelection();
        void ConfirmSelection();
        void Refresh();
    }

    public interface ICookingPreparationView
    {
        void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null);
        void SetFontAsset(TMP_FontAsset value);
        void Refresh();
    }

    public interface ICookingResultView
    {
        void Initialize(CookingGamePanel owner, CookingFlowRunner runner, TMP_FontAsset defaultFontAsset = null);
        void SetFontAsset(TMP_FontAsset value);
        void Refresh();
    }

    public interface ICookingKnowledgeUpdateView
    {
        void Initialize(CookingGamePanel owner, CookingKnowledgeStore store, TMP_FontAsset defaultFontAsset = null);
        void SetFontAsset(TMP_FontAsset value);
        bool ShowPendingUpdates(Action completed);
    }

    public interface ICookingRewardView
    {
        void Initialize(CookingGamePanel owner, CookingRewardWallet wallet, TMP_FontAsset defaultFontAsset = null);
        void SetFontAsset(TMP_FontAsset value);
    }
}
