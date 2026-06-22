using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public sealed class CookingRewardToastView : MonoBehaviour, ICookingRewardView
    {
        [Header("Flow")]
        [SerializeField] private CookingGamePanel gamePanel;
        [SerializeField] private CookingRewardWallet rewardWallet;

        [Header("Layout References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleField;
        [SerializeField] private TextMeshProUGUI rewardField;
        [SerializeField] private TextMeshProUGUI balanceField;

        [Header("Default Layout")]
        [SerializeField] private bool buildDefaultLayoutWhenMissing = true;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Color panelColor = new Color(0.05f, 0.04f, 0.03f, 0.92f);
        [SerializeField] private Color positiveColor = new Color(0.92f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.72f, 0.68f, 0.60f, 1f);
        [SerializeField, Min(0.1f)] private float visibleDuration = 3f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.22f;

        [Header("Text")]
        [SerializeField] private string titleText = "보상 획득";
        [SerializeField] private string noRewardText = "보상 없음";
        [SerializeField] private string balancePrefix = "소지금";

        private static Sprite _generatedFallbackSprite;
        private CookingGamePanel _subscribedPanel;
        private Coroutine _hideRoutine;

        private void Awake()
        {
            EnsureReferences();
            EnsureLayout();
            HideImmediate();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureLayout();
            SubscribePanelEvents();
            BindCurrentBalance();
        }

        private void OnDisable()
        {
            UnsubscribePanelEvents();
        }

        public void Initialize(
            CookingGamePanel owner,
            CookingRewardWallet wallet,
            TMP_FontAsset defaultFontAsset = null)
        {
            gamePanel = owner;
            rewardWallet = wallet;

            if (defaultFontAsset != null)
                SetFontAsset(defaultFontAsset);

            EnsureLayout();

            if (isActiveAndEnabled)
            {
                SubscribePanelEvents();
                BindCurrentBalance();
            }
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            if (value == null)
                return;

            fontAsset = value;
            ApplyFontToExistingTexts();
        }

        public void Show(CookingRewardGrant grant)
        {
            EnsureReferences();
            EnsureLayout();

            if (grant == null)
                return;

            transform.SetAsLastSibling();

            SetText(titleField, BuildTitleText(grant));
            SetText(rewardField, grant.Amount > 0 ? $"+{grant.Amount}" : noRewardText);
            SetText(balanceField, $"{balancePrefix} {grant.BalanceAfter}");

            if (rewardField != null)
                rewardField.color = grant.Amount > 0 ? positiveColor : emptyColor;

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _hideRoutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            SetAlpha(1f);
            yield return new WaitForSeconds(visibleDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(1f - t);
                yield return null;
            }

            HideImmediate();
            _hideRoutine = null;
        }

        private void BindCurrentBalance()
        {
            if (rewardWallet == null || canvasGroup == null || canvasGroup.alpha > 0f)
                return;

            SetText(balanceField, $"{balancePrefix} {rewardWallet.Balance}");
        }

        private string BuildTitleText(CookingRewardGrant grant)
        {
            if (grant == null)
                return titleText;

            switch (grant.Result)
            {
                case NpcConversationResult.Perfect:
                    return "완벽한 접대";
                case NpcConversationResult.Correct:
                    return "요청 충족";
                case NpcConversationResult.Similar:
                    return "비슷한 요리";
                case NpcConversationResult.Disgusting:
                case NpcConversationResult.Wrong:
                default:
                    return titleText;
            }
        }

        private void EnsureReferences()
        {
            if (gamePanel == null)
                gamePanel = GetComponentInParent<CookingGamePanel>();

            if (rewardWallet == null && gamePanel != null)
                rewardWallet = gamePanel.RewardWallet;

            if (rewardWallet == null)
                rewardWallet = GetComponentInParent<CookingRewardWallet>();
        }

        private void EnsureLayout()
        {
            if (buildDefaultLayoutWhenMissing == false)
                return;

            if (canvasGroup != null
                && titleField != null
                && rewardField != null
                && balanceField != null)
            {
                ApplyExistingUiAssetSprites();
                return;
            }

            BuildDefaultLayout();
        }

        private void BuildDefaultLayout()
        {
            RectTransform rect = EnsureRectTransform(gameObject);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(360f, 126f);

            canvasGroup = GetOrAdd<CanvasGroup>(gameObject);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image background = GetOrAdd<Image>(gameObject);
            ApplyUiAssetSprite(background, panelSprite);
            background.color = panelSprite != null ? Color.white : panelColor;
            background.raycastTarget = false;

            VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(gameObject);
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleField = CreateText(transform, "Title", titleText, 15f, TextAlignmentOptions.Left);
            AddLayoutElement(titleField.gameObject, -1f, 22f, -1f, 0f);

            rewardField = CreateText(transform, "Reward", noRewardText, 26f, TextAlignmentOptions.Left);
            rewardField.color = positiveColor;
            AddLayoutElement(rewardField.gameObject, -1f, 34f, -1f, 0f);

            balanceField = CreateText(transform, "Balance", $"{balancePrefix} 0", 14f, TextAlignmentOptions.Left);
            AddLayoutElement(balanceField.gameObject, -1f, 22f, -1f, 0f);
        }

        private void SubscribePanelEvents()
        {
            if (_subscribedPanel == gamePanel)
                return;

            UnsubscribePanelEvents();

            if (gamePanel == null)
                return;

            gamePanel.RewardGranted += HandleRewardGranted;
            _subscribedPanel = gamePanel;
        }

        private void UnsubscribePanelEvents()
        {
            if (_subscribedPanel == null)
                return;

            _subscribedPanel.RewardGranted -= HandleRewardGranted;
            _subscribedPanel = null;
        }

        private void HandleRewardGranted(CookingRewardGrant grant)
        {
            Show(grant);
        }

        private void HideImmediate()
        {
            SetAlpha(0f);
        }

        private void SetAlpha(float value)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = Mathf.Clamp01(value);
        }

        private void ApplyFontToExistingTexts()
        {
            if (fontAsset == null)
                return;

            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].font = fontAsset;
            }
        }

        private TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            if (fontAsset != null)
                label.font = fontAsset;
            label.color = Color.white;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static RectTransform EnsureRectTransform(GameObject target)
        {
            RectTransform rect = target.transform as RectTransform;
            if (rect != null)
                return rect;

            return target.AddComponent<RectTransform>();
        }

        private static LayoutElement AddLayoutElement(
            GameObject target,
            float preferredWidth,
            float preferredHeight,
            float flexibleWidth,
            float flexibleHeight)
        {
            LayoutElement element = GetOrAdd<LayoutElement>(target);
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null)
                field.text = text;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            if (target.TryGetComponent(out T component))
                return component;

            return target.AddComponent<T>();
        }

        private static void ApplyGeneratedSprite(Image image)
        {
            if (image == null)
                return;

            if (image.sprite == null)
                image.sprite = GetGeneratedFallbackSprite();

            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private void ApplyExistingUiAssetSprites()
        {
            Image background = GetComponent<Image>();
            ApplyUiAssetSprite(background, panelSprite);
            if (background != null && panelSprite != null)
            {
                background.color = Color.white;
            }
        }

        private void ApplyUiAssetSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                return;
            }

            ApplyGeneratedSprite(image);
        }

        private static Sprite GetGeneratedFallbackSprite()
        {
            if (_generatedFallbackSprite != null)
                return _generatedFallbackSprite;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "GeneratedCookingRewardToastSpriteTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            _generatedFallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _generatedFallbackSprite.name = "GeneratedCookingRewardToastSprite";
            return _generatedFallbackSprite;
        }
    }
}
