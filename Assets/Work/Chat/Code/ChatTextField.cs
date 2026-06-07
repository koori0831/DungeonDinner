using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Chat.Code
{
    public class ChatTextField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image image;
        [SerializeField] private Vector2 padding = new Vector2(28f, 18f);
        [SerializeField] private float minWidth = 95f;
        [SerializeField] private float minHeight = 50f;
        [SerializeField] private float maxWidth = 520f;
        [SerializeField] private float startScale = 0.35f;
        [SerializeField] private float overScale = 1.08f;
        [SerializeField] private float underScale = 0.97f;
        [SerializeField] private float growDuration = 0.22f;
        [SerializeField] private float shrinkDuration = 0.07f;
        [SerializeField] private float settleDuration = 0.06f;

        private Vector3 _defaultScale;

        public string Chat { get; private set; }

        private void Awake()
        {
            _defaultScale = transform.localScale;
        }

        public void EnableUI(string chat, bool isUserChat)
        {
            SetText(chat, isUserChat);
            
        }

        public void SetText(string script,bool isUserChat)
        {
            Chat = script;
            text.text = script;
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
                rectTransform.pivot = new Vector2(isUserChat ? 1f : 0f, rectTransform.pivot.y);
            
            text.color = isUserChat ? Color.white : Color.black;
            image.color = !isUserChat ? Color.white : Color.black;
            ResizeToText();
        }

        public void SetMaxWidth(float width)
        {
            maxWidth = Mathf.Max(minWidth, width);
            if (string.IsNullOrEmpty(Chat) == false)
                ResizeToText();
        }

        public float Height
        {
            get
            {
                RectTransform rectTransform = transform as RectTransform;
                return rectTransform != null ? rectTransform.sizeDelta.y : 0f;
            }
        }

        private void ResizeToText()
        {
            if (text == null)
                return;

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
                return;

            float textMaxWidth = Mathf.Max(1f, maxWidth - padding.x);
            Vector2 preferred = text.GetPreferredValues(Chat, textMaxWidth, 0f);

            float bubbleWidth = Mathf.Clamp(preferred.x + padding.x, minWidth, maxWidth);
            float wrappedTextWidth = Mathf.Max(1f, bubbleWidth - padding.x);
            preferred = text.GetPreferredValues(Chat, wrappedTextWidth, 0f);

            float bubbleHeight = Mathf.Max(minHeight, preferred.y + padding.y);
            rectTransform.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

            RectTransform textRectTransform = text.rectTransform;
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = new Vector2(padding.x * 0.5f, padding.y * 0.5f);
            textRectTransform.offsetMax = new Vector2(-padding.x * 0.5f, -padding.y * 0.5f);
        }

        public void PlayAppearAnimation()
        {
            transform.DOKill();
            transform.localScale = _defaultScale * startScale;

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(transform);
            sequence.Append(transform.DOScale(_defaultScale * overScale, growDuration).SetEase(Ease.OutBack));
            sequence.Append(transform.DOScale(_defaultScale * underScale, shrinkDuration).SetEase(Ease.InOutQuad));
            sequence.Append(transform.DOScale(_defaultScale, settleDuration).SetEase(Ease.OutQuad));
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
