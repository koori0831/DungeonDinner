using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Work.Cook.Code.Runtime.Core;
using Work.NPC.Code.Data;

namespace Work.Cook.Code.Data
{
    public enum CookingTagPresentationKind
    {
        Required,
        Preferred,
        Avoid,
        Danger
    }

    public enum CookingTagPresentationStatus
    {
        Neutral,
        Matched,
        Missing,
        Triggered
    }

    [Serializable]
    public sealed class CookingQualityVisual
    {
        [SerializeField] private DishCraftGrade quality;
        [SerializeField] private string displayName;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Sprite icon;

        public DishCraftGrade Quality => quality;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? quality.ToString() : displayName;
        public Color Color => color;
        public Sprite Icon => icon;

        public CookingQualityVisual(DishCraftGrade quality, string displayName, Color color, Sprite icon = null)
        {
            this.quality = quality;
            this.displayName = displayName;
            this.color = color;
            this.icon = icon;
        }

        public void SetIcon(Sprite value)
        {
            icon = value;
        }
    }

    [Serializable]
    public sealed class CookingReactionVisual
    {
        [SerializeField] private NpcConversationResult result;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string summary;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Sprite icon;

        public NpcConversationResult Result => result;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? result.ToString() : displayName;
        public string Summary => summary ?? string.Empty;
        public Color Color => color;
        public Sprite Icon => icon;

        public CookingReactionVisual(
            NpcConversationResult result,
            string displayName,
            string summary,
            Color color,
            Sprite icon = null)
        {
            this.result = result;
            this.displayName = displayName;
            this.summary = summary;
            this.color = color;
            this.icon = icon;
        }

        public void SetIcon(Sprite value)
        {
            icon = value;
        }
    }

    [Serializable]
    public sealed class CookingTagVisual
    {
        [SerializeField] private CookingTagPresentationKind kind;
        [SerializeField] private string displayName;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Sprite icon;

        public CookingTagPresentationKind Kind => kind;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
        public Color Color => color;
        public Sprite Icon => icon;

        public CookingTagVisual(CookingTagPresentationKind kind, string displayName, Color color, Sprite icon = null)
        {
            this.kind = kind;
            this.displayName = displayName;
            this.color = color;
            this.icon = icon;
        }

        public void SetIcon(Sprite value)
        {
            icon = value;
        }
    }

    [CreateAssetMenu(fileName = "CookingUiPresentationSettings", menuName = "Dungeon Dinner/Cooking/UI Presentation Settings")]
    public sealed class CookingUiPresentationSettingsSO : ScriptableObject
    {
        [Header("Typography")]
        [SerializeField] private TMP_FontAsset fontAsset;

        [Header("Dungeon Kitchen Palette")]
        [SerializeField] private Color backdropColor = new Color(0.035f, 0.022f, 0.014f, 0.9f);
        [SerializeField] private Color parchmentColor = new Color(0.83f, 0.68f, 0.43f, 1f);
        [SerializeField] private Color panelColor = new Color(0.14f, 0.085f, 0.045f, 0.98f);
        [SerializeField] private Color ironColor = new Color(0.16f, 0.17f, 0.18f, 1f);
        [SerializeField] private Color primaryTextColor = new Color(1f, 0.92f, 0.76f, 1f);
        [SerializeField] private Color secondaryTextColor = new Color(0.83f, 0.75f, 0.62f, 1f);
        [SerializeField] private Color positiveColor = new Color(0.95f, 0.74f, 0.27f, 1f);
        [SerializeField] private Color negativeColor = new Color(0.82f, 0.25f, 0.18f, 1f);
        [SerializeField] private Color missingColor = new Color(0.48f, 0.43f, 0.38f, 1f);

        [Header("Result Reveal")]
        [SerializeField, Min(0f)] private float backdropDuration = 0.4f;
        [SerializeField, Min(0f)] private float qualityDuration = 0.4f;
        [SerializeField, Min(0f)] private float reactionDuration = 0.5f;
        [SerializeField, Min(0f)] private float rewardDuration = 0.5f;
        [SerializeField, Min(0f)] private float actionDuration = 0.2f;
        [SerializeField, Min(0f)] private float cardHoverOffset = 20f;
        [SerializeField, Range(1f, 1.2f)] private float cardHoverScale = 1.04f;
        [SerializeField, Min(0.01f)] private float cardHoverDuration = 0.14f;

        [Header("Preparation Card Fan")]
        [SerializeField, Range(1, 7)] private int maxFanCardCount = 7;
        [SerializeField, Min(2)] private int scrollFallbackThreshold = 8;
        [SerializeField] private bool enableScrollFallback;
        [SerializeField, Range(0f, 30f)] private float maxFanAngle = 13f;
        [SerializeField, Min(1f)] private float minFanCardSpacing = 132f;
        [SerializeField, Min(1f)] private float maxFanCardSpacing = 220f;
        [SerializeField, Range(0.5f, 1f)] private float minFanCardScale = 0.86f;
        [SerializeField, Min(0f)] private float fanArcHeight = 70f;
        [SerializeField, Min(0f)] private float fanFocusLift = 68f;
        [SerializeField, Range(1f, 1.25f)] private float fanFocusScale = 1.08f;
        [SerializeField, Min(0f)] private float fanSelectedLift = 18f;
        [SerializeField, Min(0f)] private float fanNeighborSpread = 36f;
        [SerializeField, Min(0.01f)] private float fanTweenDuration = 0.16f;

        [Header("Visual Mappings")]
        [SerializeField] private List<CookingQualityVisual> qualityVisuals = new List<CookingQualityVisual>();
        [SerializeField] private List<CookingReactionVisual> reactionVisuals = new List<CookingReactionVisual>();
        [SerializeField] private List<CookingTagVisual> tagVisuals = new List<CookingTagVisual>();
        [SerializeField] private Sprite rewardIcon;
        [SerializeField] private Sprite npcPlaceholderIcon;

        [Header("Shared UI Skin")]
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite receiptSprite;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite primaryButtonSprite;
        [SerializeField] private Sprite secondaryButtonSprite;
        [SerializeField] private Sprite labelSprite;
        [SerializeField] private Sprite npcChatBubbleSprite;
        [SerializeField] private Sprite playerChatBubbleSprite;

        [Header("Optional Audio")]
        [SerializeField] private AudioClip dishRevealClip;
        [SerializeField] private AudioClip qualityStampClip;
        [SerializeField] private AudioClip rewardCountClip;

        public TMP_FontAsset FontAsset => fontAsset;
        public Color BackdropColor => backdropColor;
        public Color ParchmentColor => parchmentColor;
        public Color PanelColor => panelColor;
        public Color IronColor => ironColor;
        public Color PrimaryTextColor => primaryTextColor;
        public Color SecondaryTextColor => secondaryTextColor;
        public Color PositiveColor => positiveColor;
        public Color NegativeColor => negativeColor;
        public Color MissingColor => missingColor;
        public float BackdropDuration => Mathf.Max(0f, backdropDuration);
        public float QualityDuration => Mathf.Max(0f, qualityDuration);
        public float ReactionDuration => Mathf.Max(0f, reactionDuration);
        public float RewardDuration => Mathf.Max(0f, rewardDuration);
        public float ActionDuration => Mathf.Max(0f, actionDuration);
        public float TotalRevealDuration => BackdropDuration + QualityDuration + ReactionDuration + RewardDuration + ActionDuration;
        public float CardHoverOffset => Mathf.Max(0f, cardHoverOffset);
        public float CardHoverScale => Mathf.Clamp(cardHoverScale, 1f, 1.2f);
        public float CardHoverDuration => Mathf.Max(0.01f, cardHoverDuration);
        public int MaxFanCardCount => Mathf.Clamp(maxFanCardCount, 1, 7);
        public int ScrollFallbackThreshold => Mathf.Max(MaxFanCardCount + 1, scrollFallbackThreshold);
        public bool EnableScrollFallback => enableScrollFallback;
        public float MaxFanAngle => Mathf.Clamp(maxFanAngle, 0f, 30f);
        public float MinFanCardSpacing => Mathf.Max(1f, minFanCardSpacing);
        public float MaxFanCardSpacing => Mathf.Max(MinFanCardSpacing, maxFanCardSpacing);
        public float MinFanCardScale => Mathf.Clamp(minFanCardScale, 0.5f, 1f);
        public float FanArcHeight => Mathf.Max(0f, fanArcHeight);
        public float FanFocusLift => Mathf.Max(0f, fanFocusLift);
        public float FanFocusScale => Mathf.Clamp(fanFocusScale, 1f, 1.25f);
        public float FanSelectedLift => Mathf.Max(0f, fanSelectedLift);
        public float FanNeighborSpread => Mathf.Max(0f, fanNeighborSpread);
        public float FanTweenDuration => Mathf.Max(0.01f, fanTweenDuration);
        public Sprite RewardIcon => rewardIcon;
        public Sprite NpcPlaceholderIcon => npcPlaceholderIcon;
        public Sprite PanelSprite => panelSprite;
        public Sprite ReceiptSprite => receiptSprite;
        public Sprite CardSprite => cardSprite;
        public Sprite PrimaryButtonSprite => primaryButtonSprite;
        public Sprite SecondaryButtonSprite => secondaryButtonSprite;
        public Sprite LabelSprite => labelSprite;
        public Sprite NpcChatBubbleSprite => npcChatBubbleSprite;
        public Sprite PlayerChatBubbleSprite => playerChatBubbleSprite;
        public AudioClip DishRevealClip => dishRevealClip;
        public AudioClip QualityStampClip => qualityStampClip;
        public AudioClip RewardCountClip => rewardCountClip;

        public CookingQualityVisual GetQualityVisual(DishCraftGrade quality)
        {
            if (qualityVisuals != null)
            {
                for (int i = 0; i < qualityVisuals.Count; i++)
                {
                    CookingQualityVisual visual = qualityVisuals[i];
                    if (visual != null && visual.Quality == quality)
                        return visual;
                }
            }

            return CreateDefaultQualityVisual(quality);
        }

        public CookingReactionVisual GetReactionVisual(NpcConversationResult result)
        {
            if (reactionVisuals != null)
            {
                for (int i = 0; i < reactionVisuals.Count; i++)
                {
                    CookingReactionVisual visual = reactionVisuals[i];
                    if (visual != null && visual.Result == result)
                        return visual;
                }
            }

            return CreateDefaultReactionVisual(result);
        }

        public CookingTagVisual GetTagVisual(CookingTagPresentationKind kind)
        {
            if (tagVisuals != null)
            {
                for (int i = 0; i < tagVisuals.Count; i++)
                {
                    CookingTagVisual visual = tagVisuals[i];
                    if (visual != null && visual.Kind == kind)
                        return visual;
                }
            }

            return CreateDefaultTagVisual(kind);
        }

        public Color GetTagColor(CookingTagPresentationKind kind, CookingTagPresentationStatus status)
        {
            switch (status)
            {
                case CookingTagPresentationStatus.Missing:
                    return missingColor;
                case CookingTagPresentationStatus.Triggered:
                    return negativeColor;
                case CookingTagPresentationStatus.Matched:
                    return positiveColor;
                default:
                    return GetTagVisual(kind).Color;
            }
        }

        public void SetFontAsset(TMP_FontAsset value)
        {
            fontAsset = value;
        }

        public void SetQualityIcon(DishCraftGrade quality, Sprite value)
        {
            CookingQualityVisual visual = FindQualityVisual(quality);
            if (visual == null)
            {
                visual = CreateDefaultQualityVisual(quality);
                qualityVisuals.Add(visual);
            }
            visual.SetIcon(value);
        }

        public void SetReactionIcon(NpcConversationResult result, Sprite value)
        {
            CookingReactionVisual visual = FindReactionVisual(result);
            if (visual == null)
            {
                visual = CreateDefaultReactionVisual(result);
                reactionVisuals.Add(visual);
            }
            visual.SetIcon(value);
        }

        public void SetTagIcon(CookingTagPresentationKind kind, Sprite value)
        {
            CookingTagVisual visual = FindTagVisual(kind);
            if (visual == null)
            {
                visual = CreateDefaultTagVisual(kind);
                tagVisuals.Add(visual);
            }
            visual.SetIcon(value);
        }

        public void SetUtilityIcons(Sprite reward, Sprite npcPlaceholder)
        {
            rewardIcon = reward;
            npcPlaceholderIcon = npcPlaceholder;
        }

        public void SetSharedUiSkin(
            Sprite panel,
            Sprite receipt,
            Sprite card,
            Sprite primaryButton,
            Sprite secondaryButton,
            Sprite label,
            Sprite npcChatBubble,
            Sprite playerChatBubble)
        {
            panelSprite = panel;
            receiptSprite = receipt;
            cardSprite = card;
            primaryButtonSprite = primaryButton;
            secondaryButtonSprite = secondaryButton;
            labelSprite = label;
            npcChatBubbleSprite = npcChatBubble;
            playerChatBubbleSprite = playerChatBubble;
        }

        public void ResetDefaults()
        {
            qualityVisuals = new List<CookingQualityVisual>
            {
                CreateDefaultQualityVisual(DishCraftGrade.Perfect),
                CreateDefaultQualityVisual(DishCraftGrade.Good),
                CreateDefaultQualityVisual(DishCraftGrade.Normal),
                CreateDefaultQualityVisual(DishCraftGrade.Bad)
            };
            reactionVisuals = new List<CookingReactionVisual>
            {
                CreateDefaultReactionVisual(NpcConversationResult.Perfect),
                CreateDefaultReactionVisual(NpcConversationResult.Correct),
                CreateDefaultReactionVisual(NpcConversationResult.Similar),
                CreateDefaultReactionVisual(NpcConversationResult.Wrong),
                CreateDefaultReactionVisual(NpcConversationResult.Disgusting)
            };
            tagVisuals = new List<CookingTagVisual>
            {
                CreateDefaultTagVisual(CookingTagPresentationKind.Required),
                CreateDefaultTagVisual(CookingTagPresentationKind.Preferred),
                CreateDefaultTagVisual(CookingTagPresentationKind.Avoid),
                CreateDefaultTagVisual(CookingTagPresentationKind.Danger)
            };
        }

        private CookingQualityVisual FindQualityVisual(DishCraftGrade quality)
        {
            if (qualityVisuals == null)
                qualityVisuals = new List<CookingQualityVisual>();
            for (int i = 0; i < qualityVisuals.Count; i++)
            {
                if (qualityVisuals[i] != null && qualityVisuals[i].Quality == quality)
                    return qualityVisuals[i];
            }
            return null;
        }

        private CookingReactionVisual FindReactionVisual(NpcConversationResult result)
        {
            if (reactionVisuals == null)
                reactionVisuals = new List<CookingReactionVisual>();
            for (int i = 0; i < reactionVisuals.Count; i++)
            {
                if (reactionVisuals[i] != null && reactionVisuals[i].Result == result)
                    return reactionVisuals[i];
            }
            return null;
        }

        private CookingTagVisual FindTagVisual(CookingTagPresentationKind kind)
        {
            if (tagVisuals == null)
                tagVisuals = new List<CookingTagVisual>();
            for (int i = 0; i < tagVisuals.Count; i++)
            {
                if (tagVisuals[i] != null && tagVisuals[i].Kind == kind)
                    return tagVisuals[i];
            }
            return null;
        }

        private static CookingQualityVisual CreateDefaultQualityVisual(DishCraftGrade quality)
        {
            switch (quality)
            {
                case DishCraftGrade.Perfect:
                    return new CookingQualityVisual(quality, "완벽", new Color(1f, 0.77f, 0.24f, 1f));
                case DishCraftGrade.Good:
                    return new CookingQualityVisual(quality, "좋음", new Color(0.7f, 0.48f, 0.87f, 1f));
                case DishCraftGrade.Bad:
                    return new CookingQualityVisual(quality, "미흡", new Color(0.67f, 0.2f, 0.16f, 1f));
                default:
                    return new CookingQualityVisual(quality, "보통", new Color(0.83f, 0.75f, 0.58f, 1f));
            }
        }

        private static CookingReactionVisual CreateDefaultReactionVisual(NpcConversationResult result)
        {
            switch (result)
            {
                case NpcConversationResult.Perfect:
                    return new CookingReactionVisual(result, "황홀함", "손님의 취향을 정확히 사로잡았습니다.", new Color(1f, 0.76f, 0.24f, 1f));
                case NpcConversationResult.Correct:
                    return new CookingReactionVisual(result, "만족", "주문의 핵심을 충실히 채웠습니다.", new Color(0.46f, 0.78f, 0.42f, 1f));
                case NpcConversationResult.Similar:
                    return new CookingReactionVisual(result, "흥미", "조금 다르지만 손님이 관심을 보입니다.", new Color(0.5f, 0.67f, 0.86f, 1f));
                case NpcConversationResult.Disgusting:
                    return new CookingReactionVisual(result, "거부감", "위험한 재료나 조합이 감지되었습니다.", new Color(0.64f, 0.18f, 0.14f, 1f));
                default:
                    return new CookingReactionVisual(result, "아쉬움", "주문의 중요한 단서가 맞지 않습니다.", new Color(0.62f, 0.46f, 0.36f, 1f));
            }
        }

        private static CookingTagVisual CreateDefaultTagVisual(CookingTagPresentationKind kind)
        {
            switch (kind)
            {
                case CookingTagPresentationKind.Required:
                    return new CookingTagVisual(kind, "필수", new Color(0.78f, 0.62f, 0.3f, 1f));
                case CookingTagPresentationKind.Preferred:
                    return new CookingTagVisual(kind, "선호", new Color(0.48f, 0.72f, 0.42f, 1f));
                case CookingTagPresentationKind.Avoid:
                    return new CookingTagVisual(kind, "회피", new Color(0.76f, 0.4f, 0.28f, 1f));
                default:
                    return new CookingTagVisual(kind, "위험", new Color(0.68f, 0.18f, 0.14f, 1f));
            }
        }
    }
}
