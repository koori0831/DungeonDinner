using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime.UI
{
    public class MoveLayoutUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField] private float time = 0.5f;
        private Vector2 _defaultPosition = Vector2.zero;
        private LayoutElement _myElement;
        private Tween _moveTween;
        private void Awake()
        {
            _defaultPosition = root.anchoredPosition;
            _myElement = GetComponent<LayoutElement>();
        }

        public void Move()
        {
            _myElement.ignoreLayout = true;
            AnimateTo(offset, null);
        }

        public void ResetPos()
        { 
            AnimateTo(_defaultPosition, () => _myElement.ignoreLayout = false);
        }

        private void OnDisable()
        {
            KillMoveTween();
            if (_myElement != null)
                _myElement.ignoreLayout = false;
        }

        private void AnimateTo(Vector2 target, System.Action completed)
        {
            if (root == null)
            {
                completed?.Invoke();
                return;
            }

            KillMoveTween();
            _moveTween = root.DOAnchorPos(target, time)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => completed?.Invoke());
        }

        private void KillMoveTween()
        {
            _moveTween?.Kill(false);
            _moveTween = null;
            root?.DOKill(false);
        }
    }
}
