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
        private ScrollRect _scrollRect;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

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
            HashSet<GameObject> destroyedObjects = new HashSet<GameObject>();

            foreach (InfoSelectBtn button in _selectButtons)
            {
                if (button == null)
                    continue;

                DestroyGeneratedButton(button, destroyedObjects);
            }

            ClearGeneratedButtonChildren(destroyedObjects);

            _selectButtons.Clear();
            ResizeContentHeight(0);
        }

        private void ClearGeneratedButtonChildren(HashSet<GameObject> destroyedObjects)
        {
            GridLayoutGroup gridLayout = ResolveGridLayout();
            if (gridLayout == null)
                return;

            InfoSelectBtn[] buttons = gridLayout.GetComponentsInChildren<InfoSelectBtn>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                InfoSelectBtn button = buttons[i];
                if (button == null)
                    continue;

                DestroyGeneratedButton(button, destroyedObjects);
            }
        }

        private void DestroyGeneratedButton(InfoSelectBtn button, HashSet<GameObject> destroyedObjects)
        {
            if (button == null)
                return;

            GameObject target = button.gameObject;
            if (target == null)
                return;

            if (destroyedObjects != null && destroyedObjects.Add(target) == false)
                return;

            if (Application.isPlaying == true)
            {
                target.SetActive(false);
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
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
            RefreshLayout(gridLayout, resizeTarget);
        }

        private void RefreshLayout(GridLayoutGroup gridLayout, RectTransform resizeTarget)
        {
            RectTransform gridRect = gridLayout.transform as RectTransform;
            if (gridRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

            LayoutRebuilder.ForceRebuildLayoutImmediate(resizeTarget);
            Canvas.ForceUpdateCanvases();

            ScrollRect scrollRect = ResolveScrollRect();
            if (scrollRect == null)
                return;

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.horizontalNormalizedPosition = 0f;
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

            ScrollRect scrollRect = ResolveScrollRect();
            if (scrollRect != null && scrollRect.content != null)
                return scrollRect.content;

            if (gridLayout != null && gridLayout.transform.parent is RectTransform parentRect)
                return parentRect;

            return content as RectTransform;
        }

        private ScrollRect ResolveScrollRect()
        {
            if (_scrollRect != null)
                return _scrollRect;

            _scrollRect = GetComponent<ScrollRect>();
            return _scrollRect;
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
