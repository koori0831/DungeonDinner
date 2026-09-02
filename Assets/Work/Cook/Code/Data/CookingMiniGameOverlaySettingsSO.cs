using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.Cook.Code.Data
{
    [Serializable]
    public sealed class CookingMiniGameOverlayProfile
    {
        [SerializeField] private CookingMiniGameType miniGameType;
        [SerializeField] private float duration = 8f;
        [SerializeField] private float maximumDuration;
        [SerializeField, Range(0f, 1f)] private float targetMin = 0.55f;
        [SerializeField, Range(0f, 1f)] private float targetMax = 0.75f;
        [SerializeField] private int requiredCount = 1;
        [SerializeField, Range(0.01f, 0.5f)] private float primaryTolerance = 0.1f;
        [SerializeField, Range(0.01f, 0.5f)] private float secondaryTolerance = 0.08f;

        public CookingMiniGameType MiniGameType => miniGameType;
        public float Duration => Mathf.Max(0.1f, duration);
        public float MaximumDuration => Mathf.Max(0f, maximumDuration);
        public float TargetMin => Mathf.Clamp01(Mathf.Min(targetMin, targetMax));
        public float TargetMax => Mathf.Clamp01(Mathf.Max(targetMin, targetMax));
        public int RequiredCount => Mathf.Max(1, requiredCount);
        public float PrimaryTolerance => Mathf.Clamp(primaryTolerance, 0.01f, 0.5f);
        public float SecondaryTolerance => Mathf.Clamp(secondaryTolerance, 0.01f, 0.5f);

        public CookingMiniGameOverlayProfile(
            CookingMiniGameType miniGameType,
            float duration,
            float maximumDuration,
            float targetMin,
            float targetMax,
            int requiredCount,
            float primaryTolerance,
            float secondaryTolerance)
        {
            this.miniGameType = miniGameType;
            this.duration = duration;
            this.maximumDuration = maximumDuration;
            this.targetMin = targetMin;
            this.targetMax = targetMax;
            this.requiredCount = requiredCount;
            this.primaryTolerance = primaryTolerance;
            this.secondaryTolerance = secondaryTolerance;
        }

        public static CookingMiniGameOverlayProfile CreateDefault(CookingMiniGameType type)
        {
            switch (type)
            {
                case CookingMiniGameType.Slicing:
                    return new CookingMiniGameOverlayProfile(type, 10f, 0f, 0f, 1f, 3, 0.12f, 0.08f);
                case CookingMiniGameType.Roasting:
                    return new CookingMiniGameOverlayProfile(type, 6f, 6f, 0.58f, 0.76f, 1, 0.12f, 0.08f);
                case CookingMiniGameType.Cleansing:
                    return new CookingMiniGameOverlayProfile(type, 10f, 0f, 0f, 1f, 4, 0.18f, 0.08f);
                case CookingMiniGameType.Chopping:
                    return new CookingMiniGameOverlayProfile(type, 8f, 0f, 0f, 1f, 5, 0.16f, 0.08f);
                case CookingMiniGameType.Burning:
                    return new CookingMiniGameOverlayProfile(type, 5f, 5f, 0.78f, 0.92f, 1, 0.1f, 0.06f);
                case CookingMiniGameType.Boiling:
                    return new CookingMiniGameOverlayProfile(type, 6f, 6f, 0.52f, 0.7f, 1, 0.16f, 0.1f);
                case CookingMiniGameType.Stewing:
                    return new CookingMiniGameOverlayProfile(type, 9f, 0f, 0f, 1f, 3, 0.18f, 0.1f);
                case CookingMiniGameType.Freezing:
                    return new CookingMiniGameOverlayProfile(type, 5.5f, 15f, 0.6f, 0.78f, 9, 0.18f, 0.12f);
                case CookingMiniGameType.Grinding:
                    return new CookingMiniGameOverlayProfile(type, 10f, 0f, 0f, 1f, 4, 0.18f, 0.1f);
                case CookingMiniGameType.Diluting:
                    return new CookingMiniGameOverlayProfile(type, 5f, 12f, 0.48f, 0.66f, 1, 0.18f, 0.1f);
                default:
                    return new CookingMiniGameOverlayProfile(type, 8f, 0f, 0f, 1f, 1, 0.1f, 0.08f);
            }
        }
    }

    [CreateAssetMenu(fileName = "CookingMiniGameOverlaySettings", menuName = "Dungeon Dinner/Cooking/Mini Game Overlay Settings")]
    public sealed class CookingMiniGameOverlaySettingsSO : ScriptableObject
    {
        [SerializeField] private List<CookingMiniGameOverlayProfile> profiles = new List<CookingMiniGameOverlayProfile>();
        [SerializeField, Min(0f)] private float resultDisplayDuration = 2f;
        [SerializeField] private Color focusDimColor = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField] private Color guideColor = new Color(1f, 0.86f, 0.35f, 0.62f);
        [SerializeField] private Color successColor = new Color(0.38f, 0.9f, 0.45f, 0.95f);
        [SerializeField] private Color mistakeColor = new Color(1f, 0.3f, 0.2f, 0.95f);

        [Header("Optional Tool Sprites")]
        [SerializeField] private Sprite knifeSprite;
        [SerializeField] private Sprite brushSprite;
        [SerializeField] private Sprite panSprite;
        [SerializeField] private Sprite plateSprite;
        [SerializeField] private Sprite pestleSprite;
        [SerializeField] private Sprite pitcherSprite;

        [Header("Optional Feedback")]
        [SerializeField] private AudioClip actionClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip mistakeClip;
        [SerializeField] private bool enableHaptics;

        public float ResultDisplayDuration => Mathf.Max(0f, resultDisplayDuration);
        public Color FocusDimColor => focusDimColor;
        public Color GuideColor => guideColor;
        public Color SuccessColor => successColor;
        public Color MistakeColor => mistakeColor;
        public Sprite KnifeSprite => knifeSprite;
        public Sprite BrushSprite => brushSprite;
        public Sprite PanSprite => panSprite;
        public Sprite PlateSprite => plateSprite;
        public Sprite PestleSprite => pestleSprite;
        public Sprite PitcherSprite => pitcherSprite;
        public AudioClip ActionClip => actionClip;
        public AudioClip SuccessClip => successClip;
        public AudioClip MistakeClip => mistakeClip;
        public bool EnableHaptics => enableHaptics;

        public CookingMiniGameOverlayProfile GetProfile(CookingMiniGameType type)
        {
            if (profiles != null)
            {
                for (int i = 0; i < profiles.Count; i++)
                {
                    CookingMiniGameOverlayProfile profile = profiles[i];
                    if (profile != null && profile.MiniGameType == type)
                        return profile;
                }
            }

            return CookingMiniGameOverlayProfile.CreateDefault(type);
        }

        public void ResetDefaults()
        {
            profiles = new List<CookingMiniGameOverlayProfile>();
            for (int value = (int)CookingMiniGameType.Slicing;
                 value <= (int)CookingMiniGameType.Diluting;
                 value++)
            {
                CookingMiniGameType type = (CookingMiniGameType)value;
                profiles.Add(CookingMiniGameOverlayProfile.CreateDefault(type));
            }
        }
    }
}
