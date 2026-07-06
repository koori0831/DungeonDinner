using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 Temp UI 생성을 위한 공통 유틸리티
    /// </summary>
    internal static class DispatchDefaultUiUtility
    {
        private static Sprite _generatedFallbackSprite;

        /// <summary>
        /// 대상 게임 오브젝트의 RectTransform을 전체 부모 영역으로 확장
        /// </summary>
        /// <param name="gameObject">대상 게임 오브젝트</param>
        /// <returns>확장된 RectTransform</returns>
        public static RectTransform EnsureStretchRect(GameObject gameObject)
        {
            RectTransform rectTransform = GetOrAdd<RectTransform>(gameObject);
            StretchToParent(rectTransform);
            return rectTransform;
        }

        /// <summary>
        /// 컴포넌트가 없으면 추가해서 반환
        /// </summary>
        /// <typeparam name="T">컴포넌트 타입</typeparam>
        /// <param name="gameObject">대상 게임 오브젝트</param>
        /// <returns>컴포넌트 인스턴스</returns>
        public static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Image에 기본 흰색 스프라이트 적용
        /// </summary>
        /// <param name="image">대상 Image</param>
        public static void ApplyGeneratedSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetGeneratedFallbackSprite();
            image.type = Image.Type.Sliced;
        }

        /// <summary>
        /// 자식 오브젝트 전체 제거
        /// </summary>
        /// <param name="root">자식 제거 대상</param>
        public static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying == true)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        /// <summary>
        /// UI 텍스트 생성
        /// </summary>
        public static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TMP_FontAsset fontAsset,
            TextAlignmentOptions alignment,
            Color color,
            bool wrap)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text ?? string.Empty;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = wrap == true ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            if (fontAsset != null)
            {
                label.font = fontAsset;
            }

            return label;
        }

        /// <summary>
        /// 기본 버튼 생성
        /// </summary>
        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Color color,
            TMP_FontAsset fontAsset,
            UnityAction action)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            ApplyGeneratedSprite(image);
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 0.65f);
            button.colors = colors;

            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            TextMeshProUGUI text = CreateText(
                buttonObject.transform,
                "Label",
                label,
                16f,
                fontAsset,
                TextAlignmentOptions.Center,
                Color.white,
                true);
            RectTransform textRect = text.rectTransform;
            StretchToParent(textRect);
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            return button;
        }

        /// <summary>
        /// LayoutElement 적용
        /// </summary>
        public static void AddLayoutElement(GameObject gameObject, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
        {
            if (gameObject == null)
            {
                return;
            }

            LayoutElement layoutElement = GetOrAdd<LayoutElement>(gameObject);
            if (preferredWidth >= 0f)
            {
                layoutElement.preferredWidth = preferredWidth;
            }

            if (preferredHeight >= 0f)
            {
                layoutElement.preferredHeight = preferredHeight;
            }

            if (flexibleWidth >= 0f)
            {
                layoutElement.flexibleWidth = flexibleWidth;
            }

            if (flexibleHeight >= 0f)
            {
                layoutElement.flexibleHeight = flexibleHeight;
            }
        }

        /// <summary>
        /// RectTransform을 부모 전체 영역으로 확장
        /// </summary>
        public static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private static Sprite GetGeneratedFallbackSprite()
        {
            if (_generatedFallbackSprite != null)
            {
                return _generatedFallbackSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "DispatchGeneratedWhiteSpriteTexture";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _generatedFallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _generatedFallbackSprite.name = "DispatchGeneratedWhiteSprite";
            return _generatedFallbackSprite;
        }
    }
}
