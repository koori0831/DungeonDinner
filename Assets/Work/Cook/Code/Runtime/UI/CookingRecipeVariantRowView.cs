using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.Cook.Code.Runtime.UI
{
    public sealed class CookingRecipeVariantRowView : MonoBehaviour
    {
        private Button _headerButton;
        private TextMeshProUGUI _headerText;
        private GameObject _expandedRoot;
        private TextMeshProUGUI _detailText;
        private Button _cookButton;
        private TextMeshProUGUI _cookButtonText;
        private bool _expanded;

        public static CookingRecipeVariantRowView Create(Transform parent, TMP_FontAsset font)
        {
            GameObject root = new GameObject("VariantRow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.08f, 0.72f);
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            root.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CookingRecipeVariantRowView view = root.AddComponent<CookingRecipeVariantRowView>();
            view.Build(font);
            return view;
        }

        public void Bind(
            CookingRecipeVariantPresentationModel model,
            Action<string> cookRequested)
        {
            if (model == null)
                return;
            _headerText.text = $"{model.DisplayName}\n<size=75%>{model.Summary}</size>";
            _detailText.text = model.Details;
            _cookButton.interactable = model.CanReplay;
            _cookButtonText.text = model.CanReplay ? "이 변형으로 요리" : "재조리 정보 없음";
            _cookButton.onClick.RemoveAllListeners();
            if (model.CanReplay)
                _cookButton.onClick.AddListener(() => cookRequested?.Invoke(model.VariantId));
            SetExpanded(false);
        }

        private void Build(TMP_FontAsset font)
        {
            _headerButton = CreateButton(transform, "VariantHeader", out _headerText, font);
            _headerButton.onClick.AddListener(() => SetExpanded(_expanded == false));
            _expandedRoot = new GameObject("Expanded", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _expandedRoot.transform.SetParent(transform, false);
            VerticalLayoutGroup expandedLayout = _expandedRoot.GetComponent<VerticalLayoutGroup>();
            expandedLayout.spacing = 6f;
            expandedLayout.childControlHeight = true;
            expandedLayout.childControlWidth = true;
            expandedLayout.childForceExpandHeight = false;
            _expandedRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _detailText = CreateText(_expandedRoot.transform, "Details", font, 16f);
            _cookButton = CreateButton(_expandedRoot.transform, "CookVariant", out _cookButtonText, font);
        }

        private void SetExpanded(bool value)
        {
            _expanded = value;
            if (_expandedRoot != null)
                _expandedRoot.SetActive(value);
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            out TextMeshProUGUI label,
            TMP_FontAsset font)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.86f, 0.72f, 0.45f, 0.9f);
            buttonObject.GetComponent<LayoutElement>().minHeight = 46f;
            Button button = buttonObject.GetComponent<Button>();
            label = CreateText(buttonObject.transform, "Label", font, 17f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            label.color = new Color(0.12f, 0.09f, 0.05f, 1f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            button.targetGraphic = buttonObject.GetComponent<Image>();
            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, float size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }
    }
}
