using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    [RequireComponent(typeof(Button))]
    public class InfoBockmarkBtn : MonoBehaviour
    {
        private Button _button;
        private float _defaultXValue;
        private float _expandedXValue;
        private bool _isSelected;
        private bool _isPointerInside;

        public RectTransform Rect => gameObject != null ? transform as RectTransform : null;

        [SerializeField] private float offset;
        [SerializeField] private float maxMoveDistance;
        [SerializeField] private bool moveToCurrentLength;
        [SerializeField] private float moveLengthOffset;
        [SerializeField] private float collapsedVisibleLength = 35f;
        [SerializeField] private float moveTime;
        [SerializeField] private Image markImage;
        [SerializeField] private TextMeshProUGUI markName;
        [SerializeField] private bool hideNameWhenCollapsed = true;
        [SerializeField] private bool resizeLengthToFitText;
        [SerializeField] private RectTransform.Axis resizeAxis = RectTransform.Axis.Vertical;
        [SerializeField] private float minLength = 156.07f;
        [SerializeField] private float textLengthPadding = 56f;
        [SerializeField] private float maxLength = 260f;
        [SerializeField] private bool resizeTextFieldToFitText;
        [SerializeField] private float textFieldWidthPadding = 12f;

        public void InitializeBtn(Action buttonEvent, string markname, Sprite icon)
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogWarning("InfoBockmarkBtn needs a Button component before it can be initialized.", this);
                return;
            }

            _button.onClick.AddListener(() => buttonEvent?.Invoke());

            if (markImage != null)
                markImage.sprite = icon;

            if (markName != null)
                markName.text = markname;

            ResizeLengthToFitText();

            RectTransform rect = Rect;
            if (rect == null)
            {
                Debug.LogWarning("InfoBockmarkBtn needs a RectTransform before it can be initialized.", this);
                return;
            }

            _expandedXValue = rect.anchoredPosition.x + offset + maxMoveDistance;
            _defaultXValue = moveToCurrentLength
                ? GetCollapsedX(_expandedXValue)
                : rect.anchoredPosition.x + offset;
            _isSelected = false;
            _isPointerInside = false;
            ApplyCurrentState(true);
        }

        public void MouseEnter()
        {
            _isPointerInside = true;
            ApplyCurrentState(false);
        }

        public void MouseExit()
        {
            _isPointerInside = false;
            ApplyCurrentState(false);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            ApplyCurrentState(false);
        }

        private float GetSelectedX()
        {
            return _expandedXValue;
        }

        private void ApplyCurrentState(bool instant)
        {
            bool shouldExpand = _isSelected || _isPointerInside;
            SetNameVisible(shouldExpand);
            MoveToX(shouldExpand ? GetSelectedX() : _defaultXValue, instant);
        }

        private void MoveToX(float targetX, bool instant)
        {
            RectTransform rect = Rect;
            if (rect == null)
                return;

            rect.DOKill();

            if (instant)
            {
                rect.anchoredPosition = new Vector2(targetX, rect.anchoredPosition.y);
                return;
            }

            rect.DOAnchorPosX(targetX, moveTime);
        }

        private void ResizeLengthToFitText()
        {
            if (resizeLengthToFitText == false || markName == null)
                return;

            RectTransform rect = Rect;
            if (rect == null)
                return;

            markName.textWrappingMode = TextWrappingModes.NoWrap;
            markName.ForceMeshUpdate(true, true);

            float preferredTextWidth = Mathf.Ceil(markName.preferredWidth);
            float currentLength = GetAxisLength(rect, resizeAxis);
            float targetLength = preferredTextWidth + textLengthPadding;
            targetLength = Mathf.Max(targetLength, minLength, currentLength);

            if (maxLength > 0f)
                targetLength = Mathf.Min(targetLength, maxLength);

            rect.SetSizeWithCurrentAnchors(resizeAxis, targetLength);

            ResizeTextField(preferredTextWidth, targetLength);
        }

        private float GetCollapsedX(float expandedX)
        {
            if (moveToCurrentLength == false)
                return expandedX - maxMoveDistance;

            RectTransform rect = Rect;
            if (rect == null)
                return expandedX - maxMoveDistance;

            // The chapter bookmark prefab is rotated, so the visible tab length is whichever axis is resized.
            float markerLength = GetAxisLength(rect, resizeAxis);
            float visibleLength = collapsedVisibleLength > 0f
                ? collapsedVisibleLength
                : GetAxisLength(rect, GetOtherAxis(resizeAxis));
            float hiddenLength = Mathf.Max(0f, markerLength - visibleLength + moveLengthOffset);
            return expandedX - hiddenLength;
        }

        private void ResizeTextField(float preferredTextWidth, float targetLength)
        {
            if (resizeTextFieldToFitText == false || markName == null)
                return;

            RectTransform textRect = markName.rectTransform;
            if (textRect == null)
                return;

            float targetTextWidth = preferredTextWidth + textFieldWidthPadding;
            targetTextWidth = Mathf.Max(targetTextWidth, textRect.rect.width);
            targetTextWidth = Mathf.Min(targetTextWidth, targetLength);

            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetTextWidth);
        }

        private void SetNameVisible(bool isVisible)
        {
            if (hideNameWhenCollapsed == false || markName == null)
                return;

            markName.enabled = isVisible;
        }

        private static float GetAxisLength(RectTransform rect, RectTransform.Axis axis)
        {
            if (rect == null)
                return 0f;

            return axis == RectTransform.Axis.Horizontal
                ? Mathf.Abs(rect.rect.width)
                : Mathf.Abs(rect.rect.height);
        }

        private static RectTransform.Axis GetOtherAxis(RectTransform.Axis axis)
        {
            return axis == RectTransform.Axis.Horizontal
                ? RectTransform.Axis.Vertical
                : RectTransform.Axis.Horizontal;
        }
    }
}
