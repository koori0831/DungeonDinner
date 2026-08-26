using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class CookingIngredientProgressPipView : MonoBehaviour
    {
        [SerializeField] private Image plateImage;
        [SerializeField] private Image ingredientIconImage;
        [SerializeField] private TextMeshProUGUI stateField;

        public void Bind(IngredientSO ingredient, bool completed, bool current, CookingUiPresentationSettingsSO settings)
        {
            Color baseColor = settings != null ? settings.ParchmentColor : Color.white;
            if (plateImage != null)
            {
                if (completed)
                    plateImage.color = settings != null ? settings.PositiveColor : Color.yellow;
                else if (current)
                    plateImage.color = baseColor;
                else
                    plateImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.36f);
            }

            if (ingredientIconImage != null)
            {
                ingredientIconImage.sprite = CookingTempVisualUtility.ResolveIngredientIcon(ingredient);
                ingredientIconImage.enabled = ingredientIconImage.sprite != null;
                ingredientIconImage.preserveAspect = true;
                ingredientIconImage.color = completed || current ? Color.white : new Color(1f, 1f, 1f, 0.42f);
            }

            if (stateField != null)
            {
                stateField.text = completed ? "O" : current ? ">" : string.Empty;
                stateField.color = completed ? new Color(0.18f, 0.1f, 0.04f, 1f) : Color.white;
                if (settings?.FontAsset != null)
                    stateField.font = settings.FontAsset;
            }
        }
    }
}
