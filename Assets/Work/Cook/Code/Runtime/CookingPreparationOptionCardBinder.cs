using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingPreparationOptionCardBinder : MonoBehaviour
    {
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private bool findGamePanelOnEnable = true;
        [SerializeField, Min(0)] private int optionIndex;

        [Header("UI")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private TextMeshProUGUI iconField;
        [SerializeField] private TextMeshProUGUI nameField;
        [SerializeField] private TextMeshProUGUI descriptionField;
        [SerializeField] private TextMeshProUGUI effectField;
        [SerializeField] private GameObject knownEffectObject;
        [SerializeField] private Button selectButton;

        [Header("Text")]
        [SerializeField] private string emptyText = "-";
        [SerializeField] private string unknownEffectText = "아직 결과를 모릅니다.";
        [SerializeField] private string knownEffectPrefix = "확인된 효과";

        private CookingGamePanel _subscribedPanel;

        private void Reset()
        {
            rootObject = gameObject;
            selectButton = GetComponentInChildren<Button>(true);
            gamePanel = GetComponentInParent<CookingGamePanel>();
        }

        private void Awake()
        {
            EnsureReferences();
            BindButton();
        }

        private void OnEnable()
        {
            EnsureReferences();
            BindButton();
            SubscribePanel();
            Refresh();
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

            Refresh();
        }

        public void SetOptionIndex(int value)
        {
            optionIndex = Mathf.Max(0, value);
            Refresh();
        }

        public void Refresh()
        {
            CookingGameSnapshot snapshot = gamePanel != null ? gamePanel.CurrentSnapshot : null;
            IngredientSO ingredient = snapshot?.CurrentIngredient;
            IngredientPreparationOption option = GetOption(ingredient);
            bool hasOption = ingredient != null && option != null;

            SetActive(rootObject, hasOption);

            if (hasOption == false)
            {
                SetText(iconField, emptyText);
                SetText(nameField, emptyText);
                SetText(descriptionField, string.Empty);
                SetText(effectField, string.Empty);
                SetActive(knownEffectObject, false);
                SetInteractable(selectButton, false);
                return;
            }

            SetText(iconField, BuildIconText(option));
            SetText(nameField, BuildNameText(option));
            SetText(descriptionField, BuildDescriptionText(option));

            bool known = gamePanel != null && gamePanel.IsPreparationEffectKnown(ingredient, option);
            SetActive(knownEffectObject, known);
            SetText(effectField, known ? BuildKnownEffectText(option) : unknownEffectText);
            SetInteractable(selectButton, true);
        }

        public void SelectOption()
        {
            gamePanel?.SelectCurrentPreparationByIndex(optionIndex);
        }

        private IngredientPreparationOption GetOption(IngredientSO ingredient)
        {
            if (gamePanel == null || ingredient == null)
                return null;

            IReadOnlyList<IngredientPreparationOption> options = gamePanel.GetPreparationOptions(ingredient);
            if (options == null || optionIndex >= options.Count)
                return null;

            return options[optionIndex];
        }

        private static string BuildIconText(IngredientPreparationOption option)
        {
            if (option == null)
                return string.Empty;

            string source = option.Method != null && string.IsNullOrWhiteSpace(option.Method.DisplayName) == false
                ? option.Method.DisplayName
                : option.DisplayName;

            return string.IsNullOrWhiteSpace(source) ? string.Empty : source.Substring(0, 1);
        }

        private static string BuildNameText(IngredientPreparationOption option)
        {
            if (option == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(option.DisplayName) == false)
                return option.DisplayName;

            return option.Method != null ? option.Method.DisplayName : string.Empty;
        }

        private static string BuildDescriptionText(IngredientPreparationOption option)
        {
            if (option == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(option.Description) == false)
                return option.Description;

            return option.Method != null ? option.Method.Description : string.Empty;
        }

        private string BuildKnownEffectText(IngredientPreparationOption option)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(knownEffectPrefix);

            if (option.QualityDelta != 0)
                builder.AppendLine($"Quality {option.QualityDelta:+#;-#;0}");

            AppendTags(builder, "Add", option.AddTags);
            AppendTags(builder, "Remove", option.RemoveTags);

            if (string.IsNullOrWhiteSpace(option.ResultNameModifier) == false)
                builder.AppendLine($"Name {option.ResultNameModifier}");

            if (option.CausesDisgusting)
                builder.AppendLine("Disgusting risk");

            if (option.AddsPoison)
                builder.AppendLine("Adds poison");

            return builder.Length > knownEffectPrefix.Length + 1
                ? builder.ToString()
                : $"{knownEffectPrefix}\nNo change";
        }

        private static void AppendTags(StringBuilder builder, string label, IReadOnlyList<FoodTagSO> tags)
        {
            if (tags == null || tags.Count == 0)
                return;

            List<string> names = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] != null)
                    names.Add(tags[i].DisplayName);
            }

            if (names.Count > 0)
                builder.AppendLine($"{label}: {string.Join(", ", names)}");
        }

        private void EnsureReferences()
        {
            if (rootObject == null)
                rootObject = gameObject;

            if (selectButton == null)
                selectButton = GetComponentInChildren<Button>(true);

            if (gamePanel != null || findGamePanelOnEnable == false)
                return;

            gamePanel = GetComponentInParent<CookingGamePanel>();
            if (gamePanel == null)
                gamePanel = FindFirstObjectByType<CookingGamePanel>();
        }

        private void BindButton()
        {
            if (selectButton == null)
                return;

            selectButton.onClick.RemoveListener(SelectOption);
            selectButton.onClick.AddListener(SelectOption);
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
            Refresh();
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}
