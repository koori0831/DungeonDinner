using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Runtime.Systems;
using Work.Core.EventBus;
using Work.NPC.Code.Runtime;
using Work.TimeSystem;
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
        [SerializeField] private GameTimeService gameTimeService;
        [SerializeField] private NpcEncounterDirector encounterDirector;
        [SerializeField] private float offset_x = -5;

        private float _hide_x = 0f;
        private Action _selectAfterAction;
        private Action _endAction;
        private Func<string> _dispatchStatusProvider;
        private Tween _moveTween;
        private Tween _fadeDelayTween;
        private bool _isSubscribedToTime;

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

            ResolveTimeReferences();
            SetStatusText();
        }

        public void SetStatusText()
        {
            ResolveTimeReferences();

            string status = gameTimeService != null
                ? GameTimeDisplayFormatter.FormatCompact(
                    gameTimeService.CurrentDay,
                    gameTimeService.CurrentTimeOfDay)
                : "날짜/시간 : 이용 불가";
            status += NEXT_LINE;
            status += "다음 영업 : ";
            status += BuildNextBusinessStatus();
            status += NEXT_LINE;
            status += "파견 : ";
            string dispatchStatus = _dispatchStatusProvider?.Invoke();
            status += string.IsNullOrWhiteSpace(dispatchStatus) ? ALREADY_DONE_TEXT : dispatchStatus;
            status += NEXT_LINE;
            status += "모험 : ";
            status += ALREADY_DONE_TEXT;
            status += NEXT_LINE;

            if (statusText != null)
                statusText.text = status;
        }

        private void OnEnable()
        {
            ResolveTimeReferences();
            SubscribeTimeEvents();
            SetStatusText();
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
            UnsubscribeTimeEvents();
        }

        private string BuildNextBusinessStatus()
        {
            if (encounterDirector == null)
                return "상태 확인 필요";

            int encountersStarted = encounterDirector.EncountersStartedToday;
            int maxEncounters = encounterDirector.MaxEncountersPerDay;
            string progress = $"접대 {encountersStarted}/{maxEncounters}";

            if (encounterDirector.CanStartEncounter())
                return $"가능 · {progress}";

            if (encounterDirector.IsBusinessDayComplete && gameTimeService != null)
            {
                int remainingTime = GameTimeDisplayFormatter.GetTimeUntilNextDay(
                    gameTimeService.CurrentTimeOfDay);
                return $"{remainingTime}시간 후 · {progress}";
            }

            return $"손님 조건 확인 필요 · {progress}";
        }

        private void ResolveTimeReferences()
        {
            if (gameTimeService == null)
                gameTimeService = FindFirstObjectByType<GameTimeService>();
            if (encounterDirector == null)
                encounterDirector = FindFirstObjectByType<NpcEncounterDirector>();
        }

        private void SubscribeTimeEvents()
        {
            if (_isSubscribedToTime)
                return;

            Bus<GameTimeAdvancedEvent>.Events += HandleTimeAdvanced;
            _isSubscribedToTime = true;
        }

        private void UnsubscribeTimeEvents()
        {
            if (_isSubscribedToTime == false)
                return;

            Bus<GameTimeAdvancedEvent>.Events -= HandleTimeAdvanced;
            _isSubscribedToTime = false;
        }

        private void HandleTimeAdvanced(GameTimeAdvancedEvent _)
        {
            SetStatusText();
        }

        private void KillMoveTween()
        {
            _moveTween?.Kill(false);
            _moveTween = null;
            root?.DOKill(false);
        }
    }
}
