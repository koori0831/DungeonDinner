using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 인스펙터에서 구성한 파견 결과 UI에 보상 지급 결과 바인딩
    /// </summary>
    public sealed class DispatchResultView : MonoBehaviour
    {
        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI summaryField;
        [SerializeField] private TextMeshProUGUI emptyRewardField;
        [SerializeField] private RectTransform entryRoot;
        [SerializeField] private DispatchRewardResultEntryView entryPrefab;
        [SerializeField] private Button confirmButton;

        [Header("Text")]
        [SerializeField] private string emptyRewardText = "획득한 재료가 없습니다.";

        private Action _closedCallback;
        private bool _loggedMissingEntryPrefab;

        /// <summary>
        /// 파견 결과 표시
        /// </summary>
        /// <param name="result">표시할 파견 결과</param>
        /// <param name="closedCallback">결과창 닫힘 콜백</param>
        public void Show(DispatchRewardResult result, Action closedCallback)
        {
            _closedCallback = closedCallback;
            BindButton();
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
            BindButton();
            Hide();
        }

        private void OnEnable()
        {
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
                SetText(emptyRewardField, emptyRewardText);
                SetActive(emptyRewardField != null ? emptyRewardField.gameObject : null, true);
                return;
            }

            SetActive(emptyRewardField != null ? emptyRewardField.gameObject : null, false);

            for (int i = 0; i < entries.Count; i++)
            {
                DispatchRewardResultEntry entry = entries[i];
                if (entry != null)
                {
                    CreateEntry(entry);
                }
            }
        }

        private void CreateEntry(DispatchRewardResultEntry entry)
        {
            if (entryPrefab == null)
            {
                if (_loggedMissingEntryPrefab == false)
                {
                    Debug.LogWarning("DispatchResultView needs a reward entry prefab before it can build result entries.", this);
                    _loggedMissingEntryPrefab = true;
                }

                return;
            }

            DispatchRewardResultEntryView entryView = Instantiate(entryPrefab, entryRoot);
            entryView.name = "RewardEntry";
            entryView.Bind(entry);
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

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
            {
                field.text = text ?? string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
