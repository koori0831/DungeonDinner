using DG.Tweening;
using System;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime
{
    public class MoveLayoutUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField] private float time = 0.5f;
        private Vector2 _defaultPosition = Vector2.zero;
        private LayoutElement _myElement;
        private void Awake()
        {
            _defaultPosition = root.anchoredPosition;
            _myElement = GetComponent<LayoutElement>();
        }

        public void Move()
        {
            _myElement.ignoreLayout = true;
            root.DOAnchorPos(offset, time).SetEase(Ease.OutBack);
        }

        public void ResetPos()
        { 
            root.DOAnchorPos(_defaultPosition, time).SetEase(Ease.OutBack).OnComplete(() => _myElement.ignoreLayout = false);
        }
    }
}