using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Chat.Code
{
    public class ChatTextField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image image;
        [SerializeField] private Sprite playerBubbleSprite;
        [SerializeField] private Sprite otherBubbleSprite;
        [SerializeField] private Vector2 padding = new Vector2(88f, 32f);
        [SerializeField] private Vector2 bubbleExtraSize = new Vector2(52f, 16f);
        [SerializeField] private float minWidth = 200f;
        [SerializeField] private float minHeight = 72f;
        [SerializeField] private float maxWidth = 700f;
        [SerializeField] private float startScale = 0.35f;
        [SerializeField] private float overScale = 1.08f;
        [SerializeField] private float underScale = 0.97f;
        [SerializeField] private float growDuration = 0.22f;
        [SerializeField] private float shrinkDuration = 0.07f;
        [SerializeField] private float settleDuration = 0.06f;
        [SerializeField] private bool useTypewriter = true;
        [SerializeField, Min(1f)] private float charactersPerSecond = 28f;
        [SerializeField, Min(0f)] private float wordPause = 0.03f;
        [SerializeField, Min(0f)] private float commaPause = 0.11f;
        [SerializeField, Min(0f)] private float sentencePause = 0.2f;
        [SerializeField] private bool finishTypingOnDisable = true;

        private Vector3 _defaultScale;
        private Coroutine _typingRoutine;
        private int _visibleCharacterCount;

        public string Chat { get; private set; }
        public bool IsTyping { get; private set; }

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
            StopTyping(false);
            Chat = script;
            text.richText = true;
            text.text = script;
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
                rectTransform.pivot = new Vector2(isUserChat ? 1f : 0f, rectTransform.pivot.y);

            ApplyBubbleVisual(isUserChat);
            ResizeToText();
            StartTyping();
        }

        private void ApplyBubbleVisual(bool isUserChat)
        {
            Sprite bubbleSprite = isUserChat == true ? playerBubbleSprite : otherBubbleSprite;
            if (bubbleSprite != null && image != null)
            {
                image.sprite = bubbleSprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.color = Color.white;
                text.color = isUserChat == true ? Color.black : Color.white;
                return;
            }

            text.color = isUserChat == true ? Color.white : Color.black;
            image.color = isUserChat == false ? Color.white : Color.black;
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

            float textMaxWidth = Mathf.Max(1f, maxWidth - padding.x - bubbleExtraSize.x);
            Vector2 preferred = text.GetPreferredValues(Chat, textMaxWidth, 0f);

            float bubbleWidth = Mathf.Clamp(preferred.x + padding.x + bubbleExtraSize.x, minWidth, maxWidth);
            float wrappedTextWidth = Mathf.Max(1f, bubbleWidth - padding.x - bubbleExtraSize.x);
            preferred = text.GetPreferredValues(Chat, wrappedTextWidth, 0f);

            float bubbleHeight = Mathf.Max(minHeight, preferred.y + padding.y + bubbleExtraSize.y);
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

        public void CompleteTyping()
        {
            StopTyping(true);
        }

        private void StartTyping()
        {
            if (text == null)
                return;

            text.ForceMeshUpdate();
            _visibleCharacterCount = text.textInfo.characterCount;
            if (useTypewriter == false || _visibleCharacterCount <= 0)
            {
                text.maxVisibleCharacters = int.MaxValue;
                IsTyping = false;
                return;
            }

            text.maxVisibleCharacters = 0;
            IsTyping = true;
            _typingRoutine = StartCoroutine(TypeTextRoutine());
        }

        private IEnumerator TypeTextRoutine()
        {
            float interval = 1f / Mathf.Max(1f, charactersPerSecond);
            for (int i = 1; i <= _visibleCharacterCount; i++)
            {
                text.maxVisibleCharacters = i;
                yield return new WaitForSeconds(interval + GetTypingPause(i));
            }

            text.maxVisibleCharacters = int.MaxValue;
            _typingRoutine = null;
            IsTyping = false;
        }

        private float GetTypingPause(int visibleCharacterIndex)
        {
            if (text == null
                || visibleCharacterIndex <= 0
                || visibleCharacterIndex > text.textInfo.characterCount)
            {
                return 0f;
            }

            char character = text.textInfo.characterInfo[visibleCharacterIndex - 1].character;
            if (char.IsWhiteSpace(character))
                return wordPause;

            return character switch
            {
                ',' or '\uFF0C' or ';' or '\uFF1B' or ':' or '\uFF1A' => commaPause,
                '.' or '\u3002' or '!' or '\uFF01' or '?' or '\uFF1F' or '~' or '\u2026' => sentencePause,
                _ => 0f
            };
        }

        private void StopTyping(bool revealAll)
        {
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
                _typingRoutine = null;
            }

            if (text != null && revealAll)
                text.maxVisibleCharacters = int.MaxValue;

            IsTyping = false;
        }

        private void OnDestroy()
        {
            transform.DOKill();
            StopTyping(true);
        }

        private void OnDisable()
        {
            if (finishTypingOnDisable)
                StopTyping(true);
        }
    }
}
