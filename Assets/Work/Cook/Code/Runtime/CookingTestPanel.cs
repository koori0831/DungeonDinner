using System;
using UnityEngine;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingTestPanel : MonoBehaviour
    {
        [Obsolete("CookingTestPanel is kept only for scene compatibility. Use CookingGamePanel APIs with a custom UI instead.")]
        public event Action<DishResult> DishSubmitted;

        public void Open()
        {
            Debug.LogWarning("CookingTestPanel is disabled. Use CookingGamePanel APIs with a custom UI instead.", this);
        }

        public void Close()
        {
        }

        public void Toggle()
        {
            Open();
        }

        [Obsolete("CookingTestPanel no longer submits dishes. Use CookingGamePanel.HandCurrentDishToNpc instead.")]
        public void RaiseDishSubmittedForCompatibility(DishResult result)
        {
            DishSubmitted?.Invoke(result);
        }
    }
}
