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

        [Header("Prefabs")]
        [SerializeField] private DispatchRewardRowView rewardRowPrefab;

        [Header("View Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;

        [Header("Text")]
        [SerializeField] private string emptyRewardText = "획득한 재료가 없습니다.";

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
            if (rewardRowPrefab != null)
            {
                DispatchRewardRowView row = Instantiate(rewardRowPrefab, parent);
                row.Bind(BuildEntryText(entry), ItemIconUtility.ResolveIcon(entry.Item));
                return;
            }

            Debug.LogError("DispatchResultView rewardRowPrefab is missing. Assign a reward row prefab.", this);
        }

        private string BuildEntryText(DispatchRewardResultEntry entry)
        {
            string itemName = entry.Item != null ? entry.Item.DisplayName : "Missing Item";
            if (entry.RemainingAmount > 0)
            {
                return $"{itemName}  +{entry.AddedAmount} / 미획득 {entry.RemainingAmount}";
            }

            return $"{itemName}  +{entry.AddedAmount}";
        }

        private void EnsureLayout()
        {
            if (HasRequiredLayoutReferences() == true)
            {
                return;
            }

            Debug.LogError("DispatchResultView is missing inspector layout references or rewardRowPrefab. Assign a prefab/inspector based result view.", this);
        }

        private bool HasRequiredLayoutReferences()
        {
            return canvasGroup != null
                   && overlayImage != null
                   && titleField != null
                   && summaryField != null
                   && entryRoot != null
                   && confirmButton != null
                   && rewardRowPrefab != null;
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
