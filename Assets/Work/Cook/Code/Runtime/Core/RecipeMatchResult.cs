using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.Core
{
    public sealed class RecipeMatchResult
    {
        public RecipeSO Recipe { get; }
        public bool IsMatched => Recipe != null;
        public string Reason { get; }

        public RecipeMatchResult(RecipeSO recipe, string reason)
        {
            Recipe = recipe;
            Reason = reason;
        }
    }
}
