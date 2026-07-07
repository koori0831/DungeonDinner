using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using Work.Core.EventBus;
using Work.UtillUI.Code.Fade;

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
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private float offset_x = -5;

        private float _hide_x = 0f;
        private Action _selectAfterAction;
        private Action _endAction;

        private bool _isAdventureAlreadyDone = false;
        private bool _isCanAction = true;
        private const string ALREADY_DONE_TEXT = "가능";
        private const string NOT_ALREADY_DONE_TEXT = "이미완료함";
        private const string NEXT_LINE = "\n";

        public void Init(Action selectAfterAction, Action endAction)
        {
            _selectAfterAction = selectAfterAction;
            _endAction = endAction;
            _hide_x = root.anchoredPosition.x;
        }

        public void SetStatusText()
        {
            string status = "파견 : ";
            status += "가능"; // 나중에 파견쪽 만들어지면 추가
            status += NEXT_LINE;
            status += "모험 : ";
            status += _isAdventureAlreadyDone ? NOT_ALREADY_DONE_TEXT : ALREADY_DONE_TEXT;
            status += NEXT_LINE;

            statusText.text = status;
        }

        public void ShowUI(Action callback = null)
        {
            _isCanAction = true;
            SetStatusText();
            root.DOAnchorPos(new Vector2(0 + offset_x, root.anchoredPosition.y), 0.5f).OnComplete(() => callback?.Invoke()).SetEase(Ease.OutBack);
        }

        public void HideUI(Action callback = null)
        {
            _isCanAction = false;
            SetStatusText();
            root.DOAnchorPos(new Vector2(_hide_x, root.anchoredPosition.y), 0.5f).OnComplete(() => callback?.Invoke()).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 모험 버튼 선택
        /// </summary>
        public void SelectAdventure()
        {
            if (_isCanAction == false) return;
            SelectAction(PreparationEnum.Adventure);
            _isAdventureAlreadyDone = true;
        }

        /// <summary>
        /// 파견 버튼 선택
        /// </summary>
        public void SelectDispatch()
        {
            if (_isCanAction == false) return;
            SelectAction(PreparationEnum.Dispatch);
        }

        public void SelectAction(PreparationEnum preparationType)
        {
            HideUI(() =>
            {
                _selectAfterAction?.Invoke();
                Bus<OnFadeInEvent>.Raise(new OnFadeInEvent(() =>
                {
                    Bus<OnSelectPreparationEvent>.Raise(new OnSelectPreparationEvent(preparationType));
                    DOVirtual.DelayedCall(1, () => Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent()));
                }));
            });

        }

        /// <summary>
        /// 다음날 버튼 선택 
        /// </summary>
        public void SelectNextDay()
        {
            HideUI();
            _isAdventureAlreadyDone = false;
        }
    }
}