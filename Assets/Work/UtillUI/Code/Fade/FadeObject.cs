using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Work.Core.EventBus;

namespace Work.UtillUI.Code.Fade
{
    public readonly record struct OnFadeInEvent(Action callback = null) : IEvent;
    public readonly record struct OnFadeOutEvent(Action callback = null) : IEvent;

    public enum FadeState
    {
        Left = 1,
        Right = 2,
        FillFromLeft = 3,
        FillFromRight = 4
    }
    [Serializable]
    public class FadeObjectPosInfo
    {
        public float xPos = 0;
        public float width = 0;
    }

    public class FadeObject : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private FadeObjectPosInfo fillInfo;
        [SerializeField] private FadeObjectPosInfo leftInfo, rightInfo;
        [SerializeField] private float fadeTime = 0.5f;
        [SerializeField] private FadeState startFadeState = FadeState.FillFromLeft;
        private FadeState _currentState = FadeState.FillFromLeft;
        private Sequence _transition;
        private IDisposable _inputLock;

        private void Awake()
        {
            Bus<OnFadeOutEvent>.Events += Clear;
            Bus<OnFadeInEvent>.Events += Fill;
            _currentState = startFadeState;
            Clear(new OnFadeOutEvent());
        }

        private void OnDestroy()
        {
            KillTransition();
            ReleaseInput();
            Bus<OnFadeOutEvent>.Events -= Clear;
            Bus<OnFadeInEvent>.Events -= Fill;
        }

        private void OnDisable()
        {
            KillTransition();
            ReleaseInput();
        }

        public void Fill(OnFadeInEvent evt)
        {
            _inputLock ??= GameUiInput.Acquire();
            bool alreadyFilled = _currentState == FadeState.FillFromRight || _currentState == FadeState.FillFromLeft;
            // The opening clear keeps its previous state until the tween finishes.
            // A start click during that tween must replace it and still complete its callback.
            if (alreadyFilled && (_transition == null || _transition.IsActive() == false || _transition.IsPlaying() == false))
            {
                evt.callback?.Invoke();
                return;
            }
            if (root == null)
            {
                ReleaseInput();
                evt.callback?.Invoke();
                return;
            }

            KillTransition();
            _transition = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .Join(root.DOAnchorPos(new Vector2(fillInfo.xPos, root.anchoredPosition.y), fadeTime))
                .Join(root.DOSizeDelta(new Vector2(fillInfo.width, root.sizeDelta.y), fadeTime))
                .OnComplete(() =>
                {
                    _currentState = _currentState == FadeState.Left || _currentState == FadeState.FillFromLeft
                        ? FadeState.FillFromLeft
                        : FadeState.FillFromRight;
                    evt.callback?.Invoke();
                });
        }

        public void Clear(OnFadeOutEvent evt)
        {
            _inputLock ??= GameUiInput.Acquire();
            if (_currentState == FadeState.Right || _currentState == FadeState.Left)
            {
                ReleaseInput();
                evt.callback?.Invoke();
                return;
            }
            if (root == null)
            {
                ReleaseInput();
                evt.callback?.Invoke();
                return;
            }

            KillTransition();

            if (_currentState == FadeState.FillFromRight)
            {
                _transition = BuildClearSequence(leftInfo, FadeState.Left, evt.callback);

            }
            else if (_currentState == FadeState.FillFromLeft)
            {
                _transition = BuildClearSequence(rightInfo, FadeState.Right, evt.callback);
            }
        }

        private Sequence BuildClearSequence(FadeObjectPosInfo target, FadeState completedState, Action callback)
        {
            return DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .Join(root.DOAnchorPos(new Vector2(target.xPos, root.anchoredPosition.y), fadeTime))
                .Join(root.DOSizeDelta(new Vector2(target.width, root.sizeDelta.y), fadeTime))
                .OnComplete(() =>
                {
                    _currentState = completedState;
                    ReleaseInput();
                    callback?.Invoke();
                });
        }

        private void KillTransition()
        {
            _transition?.Kill(false);
            _transition = null;
            root?.DOKill(false);
        }

        private void ReleaseInput()
        {
            var lease = _inputLock;
            _inputLock = null;
            lease?.Dispose();
        }
    }
}
