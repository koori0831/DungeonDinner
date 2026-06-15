using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryScrollViewField : MonoBehaviour
    {
        [SerializeField] private InfoSelectBtn selectBtnPrefab;
        [SerializeField] private Transform content;
        [SerializeField] private RectTransform contentResizeTarget;
        [SerializeField] private int columnsPerRow = 3;

        private readonly List<InfoSelectBtn> _selectButtons = new List<InfoSelectBtn>();

        public void InitializeField(IReadOnlyList<InfoDictionaryEntryData> entries, Action<InfoDictionaryEntryData> action)
        {
            ClearButtons();

            if (entries == null || entries.Count == 0)
                return;

            if (selectBtnPrefab == null)
            {
                Debug.LogWarning("InfoDictionaryScrollViewField needs a select button prefab before it can build entries.", this);
                return;
            }

            if (content == null)
            {
                Debug.LogWarning("InfoDictionaryScrollViewField needs a content transform before it can build entries.", this);
                return;
            }

            GridLayoutGroup gridLayout = ResolveGridLayout();
            if (gridLayout == null)
            {
                Debug.LogWarning("InfoDictionaryScrollViewField needs a GridLayoutGroup under content before it can build entries.", this);
                return;
            }

            Transform buttonParent = gridLayout.transform;

            for (int i = 0; i < entries.Count; i++)
            {
                InfoDictionaryEntryData entry = entries[i];
                if (entry == null)
                    continue;

                InfoSelectBtn btn = Instantiate(selectBtnPrefab, buttonParent);
                _selectButtons.Add(btn);
                btn.InitializeBtn(entry, action);
            }

            ResizeContentHeight(_selectButtons.Count);
        }

        private void ClearButtons()
        {
            foreach (InfoSelectBtn button in _selectButtons)
            {
                if (button == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(button.gameObject);
                else
                    DestroyImmediate(button.gameObject);
            }

            _selectButtons.Clear();
            ResizeContentHeight(0);
        }

        private void ResizeContentHeight(int buttonCount)
        {
            if (content == null)
                return;

            GridLayoutGroup gridLayout = ResolveGridLayout();
            if (gridLayout == null)
            {
                Debug.LogWarning("InfoDictionaryScrollViewField content needs a GridLayoutGroup to calculate content height.", this);
                return;
            }

            RectTransform resizeTarget = ResolveResizeTarget(gridLayout);
            if (resizeTarget == null)
            {
                Debug.LogWarning("InfoDictionaryScrollViewField needs a RectTransform resize target to calculate content height.", this);
                return;
            }

            int columnCount = Mathf.Max(1, columnsPerRow);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columnCount;

            int rowCount = buttonCount > 0 ? Mathf.CeilToInt(buttonCount / (float)columnCount) : 0;
            float spacingHeight = rowCount > 1 ? gridLayout.spacing.y * (rowCount - 1) : 0f;
            float contentHeight = gridLayout.padding.top
                + gridLayout.padding.bottom
                + (gridLayout.cellSize.y * rowCount)
                + spacingHeight;

            resizeTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        }

        private GridLayoutGroup ResolveGridLayout()
        {
            if (content == null)
                return null;

            GridLayoutGroup gridLayout = content.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
                return gridLayout;

            return content.GetComponentInChildren<GridLayoutGroup>(true);
        }

        private RectTransform ResolveResizeTarget(GridLayoutGroup gridLayout)
        {
            if (contentResizeTarget != null)
                return contentResizeTarget;

            ScrollRect scrollRect = GetComponent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
                return scrollRect.content;

            if (gridLayout != null && gridLayout.transform.parent is RectTransform parentRect)
                return parentRect;

            return content as RectTransform;
        }

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}
