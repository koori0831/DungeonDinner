using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.MaterialAcquisition.Code.Integration
{
    [DisallowMultipleComponent]
    public sealed class PreparationPhaseView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private TextMeshProUGUI dispatchReasonText;
        [SerializeField] private TextMeshProUGUI adventureReasonText;
        [SerializeField] private TextMeshProUGUI nextDayReasonText;

        [Header("Buttons")]
        [SerializeField] private Button dispatchButton;
        [SerializeField] private Button adventureButton;
        [SerializeField] private Button nextDayButton;
        [SerializeField] private Button closeButton;

        [Header("Labels")]
        [SerializeField] private string titleLabel = "오늘의 준비";

        private PreparationPhaseController _controller;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            BindButtons();
            Hide();
        }

        private void OnEnable()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        public void Bind(PreparationPhaseController controller)
        {
            _controller = controller;
            BindButtons();
        }

        public void Show()
        {
            SetActive(root, true);
        }

        public void Hide()
        {
            SetActive(root, false);
        }

        public void Refresh(PreparationPhaseViewData data)
        {
            SetText(titleText, titleLabel);
            SetText(dayText, data.DayText);
            SetText(summaryText, BuildSummaryText(data));
            SetText(dispatchReasonText, data.CanOpenDispatch ? string.Empty : data.DispatchReason);
            SetText(adventureReasonText, data.CanOpenAdventure ? string.Empty : data.AdventureReason);
            SetText(nextDayReasonText, data.CanAdvanceDay ? string.Empty : data.AdvanceDayReason);

            SetInteractable(dispatchButton, data.CanOpenDispatch);
            SetInteractable(adventureButton, data.CanOpenAdventure);
            SetInteractable(nextDayButton, data.CanAdvanceDay);
        }

        private string BuildSummaryText(PreparationPhaseViewData data)
        {
            if (string.IsNullOrWhiteSpace(data.SummaryText) == false)
                return data.SummaryText;

            return $"진행 중인 파견 {data.ActiveDispatchTaskCount}건 / 복귀 가능 {data.ReadyDispatchTaskCount}건";
        }

        private void BindButtons()
        {
            if (dispatchButton != null)
            {
                dispatchButton.onClick.RemoveListener(HandleDispatchClicked);
                dispatchButton.onClick.AddListener(HandleDispatchClicked);
            }

            if (adventureButton != null)
            {
                adventureButton.onClick.RemoveListener(HandleAdventureClicked);
                adventureButton.onClick.AddListener(HandleAdventureClicked);
            }

            if (nextDayButton != null)
            {
                nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
                nextDayButton.onClick.AddListener(HandleNextDayClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
                closeButton.onClick.AddListener(HandleCloseClicked);
            }
        }

        private void UnbindButtons()
        {
            if (dispatchButton != null)
                dispatchButton.onClick.RemoveListener(HandleDispatchClicked);

            if (adventureButton != null)
                adventureButton.onClick.RemoveListener(HandleAdventureClicked);

            if (nextDayButton != null)
                nextDayButton.onClick.RemoveListener(HandleNextDayClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        private void HandleDispatchClicked()
        {
            _controller?.RequestDispatch();
        }

        private void HandleAdventureClicked()
        {
            _controller?.RequestAdventure();
        }

        private void HandleNextDayClicked()
        {
            _controller?.AdvanceToNextDay();
        }

        private void HandleCloseClicked()
        {
            _controller?.HidePreparationView();
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
                selectable.interactable = interactable;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
