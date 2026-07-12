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
        private FadeState _currentState = FadeState.Left;

        private void Awake()
        {
            Bus<OnFadeOutEvent>.Events += Clear;
            Bus<OnFadeInEvent>.Events += Fill;
        }

        private void OnDestroy()
        {
            Bus<OnFadeOutEvent>.Events -= Clear;
            Bus<OnFadeInEvent>.Events -= Fill;
        }

        private void Update()
        {
            if(Keyboard.current.iKey.wasPressedThisFrame)
            {
                Fill(new OnFadeInEvent());
            }

            if (Keyboard.current.oKey.wasPressedThisFrame)
            {
                Clear(new OnFadeOutEvent());
            }
        }


        public void Fill(OnFadeInEvent evt)
        {
            if (_currentState == FadeState.FillFromRight || _currentState == FadeState.FillFromLeft)
                return;

            root.DOAnchorPos(new Vector2(fillInfo.xPos, root.anchoredPosition.y), fadeTime);
            root.DOSizeDelta(new Vector2(fillInfo.width, root.sizeDelta.y), fadeTime).OnComplete(() =>
            { 
                _currentState = _currentState == FadeState.Left ? FadeState.FillFromLeft : FadeState.FillFromRight ;
                evt.callback?.Invoke();
            });
        }

        public void Clear(OnFadeOutEvent evt)
        {
            if (_currentState == FadeState.Right || _currentState == FadeState.Left)
                return;

            if (_currentState == FadeState.FillFromRight)
            {
                root.DOAnchorPos(new Vector2(leftInfo.xPos, root.anchoredPosition.y), fadeTime);
                root.DOSizeDelta(new Vector2(leftInfo.width, root.sizeDelta.y), fadeTime).OnComplete(() =>
                {
                    _currentState = FadeState.Left;
                    evt.callback?.Invoke();
                });

            }
            else if (_currentState == FadeState.FillFromLeft)
            {
                root.DOAnchorPos(new Vector2(rightInfo.xPos, root.anchoredPosition.y), fadeTime);
                root.DOSizeDelta(new Vector2(rightInfo.width, root.sizeDelta.y), fadeTime).OnComplete(() =>
                {
                    _currentState = FadeState.Right;
                    evt.callback?.Invoke();
                });
            }
        }
    }
}