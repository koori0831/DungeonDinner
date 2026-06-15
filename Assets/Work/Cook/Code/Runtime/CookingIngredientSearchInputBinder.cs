using TMPro;
using UnityEngine;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingIngredientSearchInputBinder : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField] private bool bindInputOnEnable = true;
        [SerializeField] private bool clearWhenOpeningDirectSelection = true;
        [SerializeField] private bool onlyInteractableDuringInventory = true;

        private CookingGamePanel _subscribedPanel;
        private CookingGameScreenState _lastScreen = CookingGameScreenState.None;

        private void Reset()
        {
            inputField = GetComponent<TMP_InputField>();
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void Awake()
        {
            EnsureReferences();
            BindInput();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (bindInputOnEnable)
                BindInput();

            SubscribePanel();
            ApplySnapshot(gamePanel != null ? gamePanel.CurrentSnapshot : null);
        }

        private void OnDisable()
        {
            UnsubscribePanel();
        }

        public void SetGamePanel(CookingGamePanel value)
        {
            if (gamePanel == value)
                return;

            UnsubscribePanel();
            gamePanel = value;

            if (isActiveAndEnabled)
                SubscribePanel();

            ApplySnapshot(gamePanel != null ? gamePanel.CurrentSnapshot : null);
        }

        public void SetSearchQuery(string query)
        {
            EnsureReferences();

            string safeQuery = query ?? string.Empty;
            if (inputField != null && inputField.text != safeQuery)
                inputField.SetTextWithoutNotify(safeQuery);

            gamePanel?.SetIngredientSearchQuery(safeQuery);
        }

        public void ClearSearchQuery()
        {
            SetSearchQuery(string.Empty);
        }

        private void HandleValueChanged(string value)
        {
            gamePanel?.SetIngredientSearchQuery(value);
        }

        private void ApplySnapshot(CookingGameSnapshot snapshot)
        {
            if (inputField == null)
                return;

            if (onlyInteractableDuringInventory)
                inputField.interactable = snapshot != null
                                          && snapshot.Screen == CookingGameScreenState.Inventory;

            if (clearWhenOpeningDirectSelection
                && snapshot != null
                && snapshot.Screen == CookingGameScreenState.Inventory
                && _lastScreen != CookingGameScreenState.Inventory
                && string.IsNullOrEmpty(inputField.text) == false)
            {
                inputField.SetTextWithoutNotify(string.Empty);
                gamePanel?.SetIngredientSearchQuery(string.Empty);
            }

            _lastScreen = snapshot != null ? snapshot.Screen : CookingGameScreenState.None;
        }

        private void EnsureReferences()
        {
            if (inputField == null)
                inputField = GetComponent<TMP_InputField>();

            if (gamePanel != null || findGamePanelOnEnable == false)
                return;

            gamePanel = GetComponentInParent<CookingGamePanel>();
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void BindInput()
        {
            if (inputField == null)
                return;

            inputField.onValueChanged.RemoveListener(HandleValueChanged);
            inputField.onValueChanged.AddListener(HandleValueChanged);
        }

        private void SubscribePanel()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanel();

            if (gamePanel == null)
                return;

            gamePanel.SnapshotChanged += HandleSnapshotChanged;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanel()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.SnapshotChanged -= HandleSnapshotChanged;
            _subscribedPanel = null;
        }

        private void HandleSnapshotChanged(CookingGameSnapshot snapshot)
        {
            ApplySnapshot(snapshot);
        }
    }
}
