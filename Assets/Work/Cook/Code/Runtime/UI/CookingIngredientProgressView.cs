using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class CookingIngredientProgressView : MonoBehaviour
    {
        [SerializeField] private RectTransform pipRoot;
        [SerializeField] private CookingIngredientProgressPipView pipTemplate;
        [SerializeField] private TextMeshProUGUI overflowField;
        [SerializeField] private CookingUiPresentationSettingsSO settings;
        [SerializeField, Min(1)] private int maximumVisiblePips = 6;

        public void SetSettings(CookingUiPresentationSettingsSO value)
        {
            settings = value;
        }

        public void Bind(IReadOnlyList<IngredientSO> ingredients, int preparedCount)
        {
            EnsureReferences();
            ClearInstances();

            if (pipRoot == null || pipTemplate == null)
                return;

            int count = ingredients?.Count ?? 0;
            int visibleCount = Mathf.Min(count, maximumVisiblePips);
            int clampedPrepared = Mathf.Clamp(preparedCount, 0, count);
            for (int i = 0; i < visibleCount; i++)
            {
                CookingIngredientProgressPipView pip = Instantiate(pipTemplate, pipRoot);
                pip.gameObject.name = $"IngredientPip{i + 1}";
                pip.gameObject.SetActive(true);
                pip.Bind(ingredients[i], i < clampedPrepared, i == clampedPrepared, settings);
            }

            if (overflowField != null)
            {
                int hiddenCount = Mathf.Max(0, count - visibleCount);
                overflowField.text = hiddenCount > 0 ? $"+{hiddenCount}" : string.Empty;
                overflowField.gameObject.SetActive(hiddenCount > 0);
                if (settings?.FontAsset != null)
                    overflowField.font = settings.FontAsset;
            }

            Canvas.ForceUpdateCanvases();
            if (pipRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(pipRoot);
        }

        private void EnsureReferences()
        {
            if (pipRoot == null)
                pipRoot = transform as RectTransform;
        }

        private void ClearInstances()
        {
            if (pipRoot == null)
                return;

            for (int i = pipRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = pipRoot.GetChild(i);
                if (pipTemplate != null && child == pipTemplate.transform)
                    continue;
                if (overflowField != null && child == overflowField.transform)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
