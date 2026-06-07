using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Work.Chat.Code
{
    public class ChatGroupInfo
    {
        public ChatGroupField Group { get; set; }
        public int ChatCount => Group.ChatCount;
        public bool IsUserChat { get; set; }

        public ChatGroupInfo(ChatGroupField group, bool isUserChat)
        {
            Group = group;
            IsUserChat = isUserChat;
        }
    }

    public class ChatPanel : MonoBehaviour
    {
        [SerializeField] private ChatGroupField userChatGroupPrefab, npcChatGroupPrefab;
        [SerializeField] private RectTransform contentTrm;

        private List<ChatGroupInfo> _chatGroups = new List<ChatGroupInfo>();
        private bool? beforeChatIsUser = null;
        private VerticalLayoutGroup _contentLayoutGroup;

        private void Awake()
        {
            if (contentTrm != null)
                _contentLayoutGroup = contentTrm.GetComponent<VerticalLayoutGroup>();
        }

        public void Update()
        {
            if(Keyboard.current.uKey.wasPressedThisFrame)
            {
                AddChat("하이여", true);
            }
            else if(Keyboard.current.nKey.wasPressedThisFrame)
            {
                AddChat("바이여", false);
            }
        }

        public void AddChat(string chat, bool isUserChat)
        {
            ChatGroupField chatGroup;
            if (beforeChatIsUser == null || beforeChatIsUser != isUserChat)
            {
                chatGroup = Instantiate(isUserChat ? userChatGroupPrefab : npcChatGroupPrefab, contentTrm);
                chatGroup.SetWidth(GetContentWidth());
                _chatGroups.Add(new ChatGroupInfo(chatGroup, isUserChat));
            }
            else
            {
                chatGroup = _chatGroups.Last().Group;
                chatGroup.SetWidth(GetContentWidth());
            }

            chatGroup.AddChat(chat, isUserChat);
            RefreshContentHeight();

            beforeChatIsUser = isUserChat;
        }

        public ChatTextField GetChat(int index)
        {
            int chatIndex = index;
            for (int i = 0; i < _chatGroups.Count; i++)
            {
                int chatCount = _chatGroups[i].ChatCount;
                if (chatIndex > chatCount)
                {
                    chatIndex -= chatCount;
                }
                else
                {
                    return _chatGroups[index].Group.GetChat(chatIndex);
                }
            }

            Debug.LogWarning("Not find chat");
            return default;
        }

        private void RefreshContentHeight()
        {
            if (contentTrm == null)
                return;

            float height = GetContentVerticalPadding();
            for (int i = 0; i < _chatGroups.Count; i++)
            {
                height += _chatGroups[i].Group.Height;
                if (i < _chatGroups.Count - 1)
                    height += GetContentSpacing();
            }

            contentTrm.sizeDelta = new Vector2(contentTrm.sizeDelta.x, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTrm);
        }

        private float GetContentWidth()
        {
            if (contentTrm == null)
                return 0f;

            float width = contentTrm.rect.width;
            if (width > 0f)
                return width;

            if (contentTrm.parent is RectTransform parentRect)
                return parentRect.rect.width;

            return contentTrm.sizeDelta.x;
        }

        private float GetContentVerticalPadding()
        {
            if (_contentLayoutGroup == null)
                return 0f;

            return _contentLayoutGroup.padding.top + _contentLayoutGroup.padding.bottom;
        }

        private float GetContentSpacing()
        {
            return _contentLayoutGroup != null ? _contentLayoutGroup.spacing : 0f;
        }
    }
}
