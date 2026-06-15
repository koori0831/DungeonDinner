using System;
using Work.Core.ObjectPool.RunTime;
using UnityEngine.UIElements;

namespace Work.Core.ObjectPool.Editor
{
    public class PoolItemUI
    {
        private Label _nameLabel;
        private Button _deleteBtn;
        private VisualElement _rootElement;

        public event Action<PoolItemUI> OnDeleteEvent;
        public event Action<PoolItemUI> OnSelectEvent;

        public string Name
        {
            get=>_nameLabel.text;
            set=>_nameLabel.text = value;
        }

        public PoolItemSO poolItem;

        public bool IsActive
        {
            get => _rootElement.ClassListContains("active");
            set
            {
                if (value)
                    _rootElement.AddToClassList("active");
                else
                    _rootElement.RemoveFromClassList("active");
            }
        }

        public PoolItemUI(VisualElement root, PoolItemSO poolItem)
        {
            this.poolItem = poolItem;
            _rootElement = root.Q<VisualElement>("PoolItem");
            _nameLabel = root.Q<Label>("ItemName");
            _deleteBtn = _rootElement.Q<Button>("DeleteBtn");
            _deleteBtn.RegisterCallback<ClickEvent>(evt =>
            {
                OnDeleteEvent?.Invoke(this);
                evt.StopPropagation();
            });
            _rootElement.RegisterCallback<ClickEvent>(evt =>
            {
                OnSelectEvent?.Invoke(this);
                evt.StopPropagation();
            });
        }
    }
}
