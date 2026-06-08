using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [RequireComponent(typeof(Button))]
    public class InfoBockmarkBtn : MonoBehaviour
    {
        private Button _button;
        private float _defaultXValue;
        private RectTransform _rect => gameObject != null ? transform as RectTransform : null;

        [SerializeField] private float offset;
        [SerializeField] private float maxMoveDistance;
        [SerializeField] private float moveTime;

        public void Awake()
        {
            InitializeBtn(() => Debug.Log($"버튼 눌림 : {gameObject.name} "));
        }

        public void InitializeBtn(Action buttonEvent)
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => buttonEvent.Invoke());
            _defaultXValue = _rect.anchoredPosition.x + offset;
        }

        public void MouseEnter()
        {
            Debug.Log("마우스 진입");
            _rect.DOAnchorPosX(_defaultXValue + maxMoveDistance, moveTime);
        }

        public void MouseExit()
        {
            Debug.Log("마우스 탈출");
            _rect.DOAnchorPosX(_defaultXValue, moveTime);
        }
    }
}