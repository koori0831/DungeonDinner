using UnityEngine;
using UnityEngine.EventSystems;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingPopupDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CookingIngredientBagPopup popup;
        public void OnBeginDrag(PointerEventData evt) => popup.BeginDrag(evt);
        public void OnDrag(PointerEventData evt) => popup.Drag(evt);
        public void OnEndDrag(PointerEventData evt) => popup.EndDrag(evt);
    }
}
