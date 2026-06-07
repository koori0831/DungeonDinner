using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Chat.Code
{
    public class ChatGroupField : MonoBehaviour
    {
        [SerializeField] private ChatTextField chatTextPrefab;
        [SerializeField] private float fallbackHorizontalPadding = 80f;

        private readonly List<ChatTextField> _chats = new List<ChatTextField>();
        private RectTransform _rectTransform;
        private VerticalLayoutGroup _layoutGroup;
        private LayoutElement _layoutElement;

        public int ChatCount => _chats.Count;
        public float Height => _rectTransform != null ? _rectTransform.sizeDelta.y : 0f;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _layoutGroup = GetComponent<VerticalLayoutGroup>();
        }

        public ChatTextField GetChat(int index) => _chats[index];

        public void SetWidth(float width)
        {
            EnsureReferences();

            if (_rectTransform == null)
                return;

            _rectTransform.sizeDelta = new Vector2(width, _rectTransform.sizeDelta.y);
            if (_layoutElement != null)
                _layoutElement.preferredWidth = width;

            RefreshBubbleMaxWidths();
            RefreshHeight();
        }

        public void AddChat(string chat, bool isUserChat)
        {
            EnsureReferences();

            ChatTextField newChat = Instantiate(chatTextPrefab, transform);
            newChat.SetMaxWidth(GetBubbleMaxWidth());
            newChat.SetText(chat, isUserChat);
            _chats.Add(newChat);

            RefreshHeight();
            newChat.PlayAppearAnimation();
        }

        private void RefreshBubbleMaxWidths()
        {
            float bubbleMaxWidth = GetBubbleMaxWidth();
            foreach (ChatTextField chat in _chats)
            {
                chat.SetMaxWidth(bubbleMaxWidth);
            }
        }

        private void RefreshHeight()
        {
            EnsureReferences();

            if (_rectTransform == null)
                return;

            float height = GetVerticalPadding();
            for (int i = 0; i < _chats.Count; i++)
            {
                height += _chats[i].Height;
                if (i < _chats.Count - 1)
                    height += GetSpacing();
            }

            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, height);
            if (_layoutElement != null)
            {
                _layoutElement.preferredWidth = _rectTransform.sizeDelta.x;
                _layoutElement.preferredHeight = height;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        }

        private float GetBubbleMaxWidth()
        {
            EnsureReferences();

            float width = _rectTransform != null ? _rectTransform.rect.width : 0f;
            if (width <= 0f && _rectTransform != null)
                width = _rectTransform.sizeDelta.x;

            int horizontalPadding = _layoutGroup != null
                ? _layoutGroup.padding.left + _layoutGroup.padding.right
                : Mathf.RoundToInt(fallbackHorizontalPadding);

            return Mathf.Max(120f, width - horizontalPadding);
        }

        private float GetVerticalPadding()
        {
            return _layoutGroup != null ? _layoutGroup.padding.top + _layoutGroup.padding.bottom : 0f;
        }

        private float GetSpacing()
        {
            return _layoutGroup != null ? _layoutGroup.spacing : 0f;
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_layoutGroup == null)
                _layoutGroup = GetComponent<VerticalLayoutGroup>();

            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
            }
        }
    }
}
