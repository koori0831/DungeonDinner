using System.Collections.Generic;
using UnityEngine;

namespace Work.Items.Code
{
    /// <summary>
    /// 아이템 UI 표시용 아이콘 해석 및 Temp 아이콘 생성 유틸리티
    /// </summary>
    public static class ItemIconUtility
    {
        private const int ICON_SIZE = 32;
        private const int BORDER_SIZE = 3;

        private static readonly Dictionary<string, Sprite> GENERATED_ICONS = new Dictionary<string, Sprite>();

        /// <summary>
        /// 아이템 아이콘 또는 Temp 아이콘 반환
        /// </summary>
        /// <param name="item">표시할 아이템 데이터</param>
        /// <returns>표시용 아이콘</returns>
        public static Sprite ResolveIcon(ItemDataSO item)
        {
            if (item != null && item.Icon != null)
            {
                return item.Icon;
            }

            string key = item != null ? item.ItemId : "missing_item";
            string label = item != null ? item.DisplayName : "Missing Item";
            return GetOrCreateTempIcon(key, label, 0.70f, 0.50f, 0.28f);
        }

        /// <summary>
        /// 지정 키 기반 Temp 아이콘 반환
        /// </summary>
        /// <param name="key">아이콘 식별 키</param>
        /// <param name="label">색상 생성에 사용할 표시 이름</param>
        /// <param name="fallbackR">기본 R</param>
        /// <param name="fallbackG">기본 G</param>
        /// <param name="fallbackB">기본 B</param>
        /// <returns>Temp 아이콘</returns>
        public static Sprite GetOrCreateTempIcon(string key, string label, float fallbackR, float fallbackG, float fallbackB)
        {
            string safeKey = string.IsNullOrWhiteSpace(key) == false ? key : label;
            if (string.IsNullOrWhiteSpace(safeKey) == true)
            {
                safeKey = "temp_icon";
            }

            if (GENERATED_ICONS.TryGetValue(safeKey, out Sprite cachedSprite) == true)
            {
                return cachedSprite;
            }

            Color baseColor = BuildStableColor(safeKey, fallbackR, fallbackG, fallbackB);
            Texture2D texture = CreateIconTexture(safeKey, baseColor);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, ICON_SIZE, ICON_SIZE), new Vector2(0.5f, 0.5f));
            sprite.name = $"TempIcon_{safeKey}";
            GENERATED_ICONS.Add(safeKey, sprite);
            return sprite;
        }

        private static Texture2D CreateIconTexture(string key, Color baseColor)
        {
            Texture2D texture = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false);
            texture.name = $"TempIconTexture_{key}";
            texture.filterMode = FilterMode.Point;

            Color borderColor = Color.Lerp(baseColor, Color.black, 0.45f);
            Color highlightColor = Color.Lerp(baseColor, Color.white, 0.28f);
            Color shadowColor = Color.Lerp(baseColor, Color.black, 0.22f);

            for (int y = 0; y < ICON_SIZE; y++)
            {
                for (int x = 0; x < ICON_SIZE; x++)
                {
                    Color pixelColor = baseColor;
                    bool isBorder = x < BORDER_SIZE
                                    || y < BORDER_SIZE
                                    || x >= ICON_SIZE - BORDER_SIZE
                                    || y >= ICON_SIZE - BORDER_SIZE;

                    if (isBorder == true)
                    {
                        pixelColor = borderColor;
                    }
                    else if (x + y < ICON_SIZE)
                    {
                        pixelColor = highlightColor;
                    }
                    else if (x > y)
                    {
                        pixelColor = shadowColor;
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Color BuildStableColor(string key, float fallbackR, float fallbackG, float fallbackB)
        {
            if (string.IsNullOrWhiteSpace(key) == true)
            {
                return new Color(fallbackR, fallbackG, fallbackB, 1f);
            }

            int hash = 17;
            for (int i = 0; i < key.Length; i++)
            {
                hash = hash * 31 + key[i];
            }

            float hue = Mathf.Abs(hash % 360) / 360f;
            Color generated = Color.HSVToRGB(hue, 0.48f, 0.78f);
            return new Color(generated.r, generated.g, generated.b, 1f);
        }
    }
}
