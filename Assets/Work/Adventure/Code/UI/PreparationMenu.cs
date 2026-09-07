using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Runtime.Systems;
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
        private Func<string> _dispatchStatusProvider;
        private Tween _moveTween;
        private Tween _fadeDelayTween;

        private bool _isCanAction = true;
        private const string ALREADY_DONE_TEXT = "가능";
        private const string NEXT_LINE = "\n";

        public void Init(
            Action selectAfterAction,
            Action endAction,
            Func<string> dispatchStatusProvider = null)
        {
            _selectAfterAction = selectAfterAction;
            _endAction = endAction;
            _dispatchStatusProvider = dispatchStatusProvider;
            if (root != null)
                _hide_x = root.anchoredPosition.x;
        }

        public void SetStatusText()
        {
            string status = "파견 : ";
            string dispatchStatus = _dispatchStatusProvider?.Invoke();
            status += string.IsNullOrWhiteSpace(dispatchStatus) ? ALREADY_DONE_TEXT : dispatchStatus;
            status += NEXT_LINE;
            status += "모험 : ";
            status += ALREADY_DONE_TEXT;
            status += NEXT_LINE;

            if (statusText != null)
                statusText.text = status;
        }

        public void ShowUI(Action callback = null)
        {
            _isCanAction = true;
            SetStatusText();
            if (root == null)
            {
                callback?.Invoke();
                return;
            }

            KillMoveTween();
            _moveTween = root.DOAnchorPos(new Vector2(offset_x, root.anchoredPosition.y), 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => callback?.Invoke());
        }

        public void HideUI(Action callback = null)
        {
            _isCanAction = false;
            SetStatusText();
            if (root == null)
            {
                callback?.Invoke();
                return;
            }

            KillMoveTween();
            _moveTween = root.DOAnchorPos(new Vector2(_hide_x, root.anchoredPosition.y), 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => callback?.Invoke());
        }

        /// <summary>
        /// 모험 버튼 선택
        /// </summary>
        public void SelectAdventure()
        {
            if (_isCanAction == false) return;
            SelectAction(PreparationEnum.Adventure);
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
                    _fadeDelayTween?.Kill(false);
                    _fadeDelayTween = DOVirtual.DelayedCall(1, () =>
                            Bus<OnFadeOutEvent>.Raise(new OnFadeOutEvent()))
                        .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                }));
            });

        }

        /// <summary>
        /// 준비를 마치고 다음 음식점 운영 시작
        /// </summary>
        public void SelectNextBusiness()
        {
            HideUI();
            Bus<CookingBusinessResumeRequestedEvent>.Raise(new CookingBusinessResumeRequestedEvent());
        }

        /// <summary>
        /// 기존 씬/프리팹 UnityEvent 호환용 진입점입니다.
        /// </summary>
        public void SelectNextDay()
        {
            SelectNextBusiness();
        }

        private void OnDisable()
        {
            KillMoveTween();
            _fadeDelayTween?.Kill(false);
            _fadeDelayTween = null;
        }

        private void KillMoveTween()
        {
            _moveTween?.Kill(false);
            _moveTween = null;
            root?.DOKill(false);
        }
    }
}
