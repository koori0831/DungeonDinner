using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Work.Adventure.Code.UI
{
    public class AdventureBackground : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image moveBackground, defaultBackground;
        [SerializeField] private float offsetX = 120, offsetY = 65;
        [SerializeField] private float time = 0.6f;
        [SerializeField] private float scaleOffset = 0.3f;
        [SerializeField] private float fadeTime = 0.4f;

        private float _defaultScale;
        private Sequence _walkSequence;
        private Tween _defaultFadeTween;
        private Tween _moveFadeTween;


        private void Awake()
        {
            _defaultScale = moveBackground.rectTransform.localScale.x;
        }

        public void Walking(Action callback = null)
        {
            KillAllTweens();

            Sequence moveSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            for (int i = 1; i <= 3; i++)
            {
                // i가 홀수일 때는 왼쪽(-), 짝수일 때는 오른쪽(+)으로 이동
                float currentOffsetX = (i % 2 != 0) ? -offsetX : offsetX;
                float currentScale = _defaultScale + (scaleOffset * i);

                // 2. 바깥쪽으로 이동 및 크기 키우기 (동시 실행)
                moveSequence.Append(moveBackground.rectTransform.DOScale(currentScale, time));
                moveSequence.Join(moveBackground.rectTransform.DOAnchorPos(new Vector2(currentOffsetX, offsetY), time).SetEase(Ease.InBack));

                // 3. 다시 중앙(0, 0)으로 복귀
                moveSequence.Append(moveBackground.rectTransform.DOAnchorPos(Vector2.zero, time));
            }

            // 4. 모든 루프가 끝난 후 Idle() 실행
            moveSequence.Insert(0f, defaultBackground.DOFade(0, fadeTime));
            moveSequence.Insert(0f, moveBackground.DOFade(1, fadeTime));
            _walkSequence = moveSequence.OnComplete(() =>
            {
                _walkSequence = null;
                StartIdleVisuals();
                callback?.Invoke();
            });
        }    

        public void Idle()
        {
            KillAllTweens();
            StartIdleVisuals();
        }

        public void Enable()
        {
            root.gameObject.SetActive(true);
            Idle();
        }

        public void Disable()
        {
            KillAllTweens();
            root.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            KillAllTweens();
        }

        private void StartIdleVisuals()
        {
            _defaultFadeTween = defaultBackground.DOFade(1, fadeTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _moveFadeTween = moveBackground.DOFade(0, fadeTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            moveBackground.rectTransform.localScale = new Vector3(_defaultScale, _defaultScale, _defaultScale);
            moveBackground.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void KillAllTweens()
        {
            _walkSequence?.Kill(false);
            _walkSequence = null;
            _defaultFadeTween?.Kill(false);
            _defaultFadeTween = null;
            _moveFadeTween?.Kill(false);
            _moveFadeTween = null;
            defaultBackground?.DOKill(false);
            moveBackground?.DOKill(false);
            if (moveBackground != null)
                moveBackground.rectTransform.DOKill(false);
        }
    }
}
