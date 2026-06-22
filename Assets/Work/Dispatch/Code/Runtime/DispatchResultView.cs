using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Items.Code;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 완료 후 인벤토리 지급 결과를 표시하는 Temp UI
    /// </summary>
    public sealed class DispatchResultView : MonoBehaviour
    {
        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI summaryField;
        [SerializeField] private RectTransform entryRoot;
        [SerializeField] private Button confirmButton;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.58f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.07f, 0.06f, 0.97f);
        [SerializeField] private Color entryColor = new Color(0.18f, 0.14f, 0.10f, 0.96f);
        [SerializeField] private Color confirmButtonColor = new Color(0.52f, 0.38f, 0.20f, 1f);

        [Header("Text")]
        [SerializeField] private string titleText = "파견 완료";
        [SerializeField] private string emptyRewardText = "획득한 재료가 없습니다.";
        [SerializeField] private string confirmText = "확인";

        private Action _closedCallback;

        /// <summary>
        /// 기본 폰트 지정
        /// </summary>
        /// <param name="value">적용할 TMP 폰트</param>
        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
            {
                return;
            }

            fontAsset = value;
            ApplyFont(titleField);
            ApplyFont(summaryField);
        }

        /// <summary>
        /// 파견 결과 표시
        /// </summary>
        /// <param name="result">표시할 파견 결과</param>
        /// <param name="closedCallback">결과창 닫힘 콜백</param>
        public void Show(DispatchRewardResult result, Action closedCallback)
        {
            EnsureLayout();
            _closedCallback = closedCallback;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            BindResult(result);
        }

        /// <summary>
        /// 결과창 숨김
        /// </summary>
        public void Hide()
        {
            EnsureLayout();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void Awake()
        {
            EnsureLayout();
            BindButton();
            Hide();
        }

        private void OnEnable()
        {
            EnsureLayout();
            BindButton();
        }

        private void BindResult(DispatchRewardResult result)
        {
            string pointName = result?.Point != null ? result.Point.DisplayName : "파견";
            SetText(titleField, $"{pointName} 완료");
            SetText(summaryField, BuildSummaryText(result));
            RebuildEntries(result?.Entries);
        }

        private string BuildSummaryText(DispatchRewardResult result)
        {
            if (result == null || result.Entries.Count == 0)
            {
                return emptyRewardText;
            }

            if (result.RemainingAmount > 0)
            {
                return $"획득 {result.AddedAmount}개 / 미획득 {result.RemainingAmount}개";
            }

            return $"획득 {result.AddedAmount}개";
        }

        private void RebuildEntries(IReadOnlyList<DispatchRewardResultEntry> entries)
        {
            DispatchDefaultUiUtility.ClearChildren(entryRoot);

            if (entryRoot == null)
            {
                return;
            }

            if (entries == null || entries.Count == 0)
            {
                TextMeshProUGUI empty = DispatchDefaultUiUtility.CreateText(
                    entryRoot,
                    "EmptyReward",
                    emptyRewardText,
                    15f,
                    fontAsset,
                    TextAlignmentOptions.Center,
                    Color.white,
                    true);
                DispatchDefaultUiUtility.AddLayoutElement(empty.gameObject, -1f, 44f, -1f, 0f);
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                DispatchRewardResultEntry entry = entries[i];
                if (entry != null)
                {
                    CreateEntry(entryRoot, entry);
                }
            }
        }

        private void CreateEntry(Transform parent, DispatchRewardResultEntry entry)
        {
            GameObject entryObject = new GameObject("RewardEntry", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            entryObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(entryObject, -1f, 58f, -1f, 0f);

            Image background = entryObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(background);
            background.color = entryColor;

            HorizontalLayoutGroup layoutGroup = entryObject.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 8, 8);
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            CreateIcon(entryObject.transform, entry.Item);

            TextMeshProUGUI label = DispatchDefaultUiUtility.CreateText(
                entryObject.transform,
                "RewardText",
                BuildEntryText(entry),
                15f,
                fontAsset,
                TextAlignmentOptions.MidlineLeft,
                Color.white,
                true);
            DispatchDefaultUiUtility.AddLayoutElement(label.gameObject, 0f, -1f, 1f, -1f);
        }

        private void CreateIcon(Transform parent, ItemDataSO item)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(iconObject, 38f, 38f, 0f, 0f);

            Image image = iconObject.GetComponent<Image>();
            image.sprite = ItemIconUtility.ResolveIcon(item);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private string BuildEntryText(DispatchRewardResultEntry entry)
        {
            string itemName = entry.Item != null ? entry.Item.DisplayName : "Missing Item";
            if (entry.RemainingAmount > 0)
            {
                return $"{itemName}  +{entry.AddedAmount} / 미획득 {entry.RemainingAmount}  보유 {entry.CurrentInventoryAmount}";
            }

            return $"{itemName}  +{entry.AddedAmount}  보유 {entry.CurrentInventoryAmount}";
        }

        private void EnsureLayout()
        {
            if (buildDefaultLayoutWhenMissing == false)
            {
                return;
            }

            if (canvasGroup != null
                && overlayImage != null
                && titleField != null
                && summaryField != null
                && entryRoot != null
                && confirmButton != null)
            {
                return;
            }

            DispatchDefaultUiUtility.ClearChildren(transform);
            DispatchDefaultUiUtility.EnsureStretchRect(gameObject);
            canvasGroup = DispatchDefaultUiUtility.GetOrAdd<CanvasGroup>(gameObject);

            overlayImage = DispatchDefaultUiUtility.GetOrAdd<Image>(gameObject);
            DispatchDefaultUiUtility.ApplyGeneratedSprite(overlayImage);
            overlayImage.color = overlayColor;
            overlayImage.raycastTarget = true;

            RectTransform panel = CreatePanel();
            titleField = DispatchDefaultUiUtility.CreateText(panel, "Title", titleText, 25f, fontAsset, TextAlignmentOptions.Center, Color.white, true);
            DispatchDefaultUiUtility.AddLayoutElement(titleField.gameObject, -1f, 38f, -1f, 0f);

            summaryField = DispatchDefaultUiUtility.CreateText(panel, "Summary", string.Empty, 16f, fontAsset, TextAlignmentOptions.Center, new Color(0.88f, 0.76f, 0.58f, 1f), true);
            DispatchDefaultUiUtility.AddLayoutElement(summaryField.gameObject, -1f, 34f, -1f, 0f);

            entryRoot = CreateEntryRoot(panel);
            confirmButton = DispatchDefaultUiUtility.CreateButton(panel, "ConfirmButton", confirmText, confirmButtonColor, fontAsset, HandleConfirmClicked);
            DispatchDefaultUiUtility.AddLayoutElement(confirmButton.gameObject, -1f, 44f, -1f, 0f);
            BindButton();
        }

        private RectTransform CreatePanel()
        {
            GameObject panelObject = new GameObject("DispatchResultPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(transform, false);

            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(560f, 420f);

            Image image = panelObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(image);
            image.color = panelColor;

            VerticalLayoutGroup layoutGroup = panelObject.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(22, 22, 20, 20);
            layoutGroup.spacing = 12f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            return panel;
        }

        private RectTransform CreateEntryRoot(Transform parent)
        {
            GameObject viewportObject = new GameObject("RewardViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);
            DispatchDefaultUiUtility.AddLayoutElement(viewportObject, -1f, 0f, -1f, 1f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            DispatchDefaultUiUtility.ApplyGeneratedSprite(viewportImage);
            viewportImage.color = new Color(0f, 0f, 0f, 0.18f);

            RectTransform content = new GameObject("RewardEntries", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewportObject.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = viewportObject.GetComponent<RectTransform>();
            scrollRect.content = content;
            return content;
        }

        private void BindButton()
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        private void HandleConfirmClicked()
        {
            Action closedCallback = _closedCallback;
            _closedCallback = null;
            Hide();
            closedCallback?.Invoke();
        }

        private void SetText(TextMeshProUGUI field, string text)
        {
            if (field == null)
            {
                return;
            }

            field.text = text ?? string.Empty;
        }

        private void ApplyFont(TextMeshProUGUI field)
        {
            if (field == null || fontAsset == null)
            {
                return;
            }

            field.font = fontAsset;
        }
    }
}
