using DG.Tweening;
using System;
using UnityEngine;
using Work.Core.EventBus;

namespace Work.Adventure.Code.UI
{
    public readonly record struct OnSelectPreparationEvent(PreparationEnum preparationType) : IEvent;

    public enum PreparationEnum
    {
        Adventure,
        Dispatch
    }

    public class PreparationMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private float offset_x = -5;

        private float _hide_x = 0f;
        private Action _selectAfterAction;
        private Action _endAction;

        public void Init(Action selectAfterAction, Action endAction)
        {
            _selectAfterAction = selectAfterAction;
            _endAction = endAction;
            _hide_x = root.anchoredPosition.x;
        }

        public void ShowUI(Action callback = null)
        {
            root.DOAnchorPos(new Vector2(0 + offset_x, root.anchoredPosition.y), 0.5f).OnComplete(() => callback?.Invoke());
        }

        public void HideUI(Action callback = null)
        {
            root.DOAnchorPos(new Vector2(_hide_x, root.anchoredPosition.y), 0.5f).OnComplete(() => callback?.Invoke());
        }

        /// <summary>
        /// 모험 버튼 선택
        /// </summary>
        public void SelectAdventure()
        {
            SelectAction(PreparationEnum.Adventure);
        }

        /// <summary>
        /// 파견 버튼 선택
        /// </summary>
        public void SelectDispatch()
        {
            SelectAction(PreparationEnum.Dispatch);
        }

        public void SelectAction(PreparationEnum preparationType)
        {
            _selectAfterAction?.Invoke();
            HideUI();
            Bus<OnSelectPreparationEvent>.Raise(new OnSelectPreparationEvent(preparationType));
        }

        /// <summary>
        /// 다음날 버튼 선택 
        /// </summary>
        public void SelectNextDay()
        {
            HideUI();
        }
    }
}