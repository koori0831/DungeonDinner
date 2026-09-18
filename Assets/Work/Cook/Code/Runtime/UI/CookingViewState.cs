namespace Work.Cook.Code.Runtime.UI
{
    /// <summary>
    /// 조리 뷰 내부 진행 상태
    /// </summary>
    public enum CookingViewState
    {
        None,
        IngredientFocus,
        CardSelect,
        CardCommit,
        IngredientInteraction,
        InteractionResult,
        CompleteCooking
    }
}
