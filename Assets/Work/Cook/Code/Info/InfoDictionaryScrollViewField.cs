using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Work.Cook.Code.Info
{
    public class InfoDictionaryScrollViewField : MonoBehaviour
    {
        [SerializeField] private InfoSelectBtn selectBtnPrefab;
        [SerializeField] private Transform content;
        [SerializeField] private RectTransform contentResizeTarget;
        [SerializeField] private int columnsPerRow = 3;
        [SerializeField] private int centeredHorizontalPadding = 34;

        private readonly List<InfoSelectBtn> _selectButtons = new List<InfoSelectBtn>();
        private ScrollRect _scrollRect;
        private float _lastViewportWidth;

        public void SetCategoryHeading(string category, int count)
        {
            var scroll = ResolveScrollRect();
            if (scroll == null || scroll.viewport == null || selectBtnPrefab == null)
                return;
            var sample = selectBtnPrefab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (sample == null)
                return;
            var background = GetComponent<Image>();
            if (background != null)
            {
                background.sprite = null;
                background.color = new Color(0.97f, 0.94f, 0.86f);
            }
            var go = new GameObject("CategoryHeading", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = sample.font;
            text.fontSize = 22;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.26f, 0.19f, 0.13f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.text = category + "  <size=70%>" + count + "종</size>";
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24, -52);
            rect.offsetMax = new Vector2(-24, -8);
            scroll.viewport.offsetMax = new Vector2(scroll.viewport.offsetMax.x, -60);
            InfoDisplayPanel.AddReadingScrollbar(scroll, 64);
        }

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void LateUpdate()
        {
            var scroll = ResolveScrollRect();
            if (scroll == null || scroll.viewport == null)
                return;
            float width = scroll.viewport.rect.width;
            if (width <= 0 || Mathf.Abs(width - _lastViewportWidth) < 0.5f)
                return;
            _lastViewportWidth = width;
            float position = scroll.verticalNormalizedPosition;
            ResizeContentHeight(_selectButtons.Count);
            scroll.verticalNormalizedPosition = position;
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

            var scroll = ResolveScrollRect();
            if (scroll != null && scroll.viewport != null)
            {
                resizeTarget.anchorMin = new Vector2(0, 1);
                resizeTarget.anchorMax = new Vector2(1, 1);
                resizeTarget.sizeDelta = new Vector2(0, resizeTarget.sizeDelta.y);
                resizeTarget.anchoredPosition = new Vector2(0, resizeTarget.anchoredPosition.y);
            }
            int horizontalPadding = Mathf.Max(0, centeredHorizontalPadding);
            float availableWidth = scroll != null && scroll.viewport != null
                ? scroll.viewport.rect.width : resizeTarget.rect.width;
            int fittingColumns = Mathf.FloorToInt((availableWidth - horizontalPadding * 2 + gridLayout.spacing.x)
                / (gridLayout.cellSize.x + gridLayout.spacing.x));
            int columnCount = Mathf.Clamp(fittingColumns, 1, Mathf.Max(1, columnsPerRow));
            gridLayout.padding.left = horizontalPadding;
            gridLayout.padding.right = horizontalPadding;
            gridLayout.padding.top = 20;
            gridLayout.padding.bottom = 20;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
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
            InfoDisplayPanel.RefreshReadingScrollbar(scrollRect);
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
