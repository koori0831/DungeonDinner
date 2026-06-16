using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace DungeonDinner.Rendering
{
    public sealed class ShortHikePixelationRendererFeature : ScriptableRendererFeature
    {
        private const string DefaultShaderName = "Hidden/DungeonDinner/ShortHikePixelation";

        public enum ResolutionMode
        {
            PixelSize,
            TargetResolution
        }

        public enum PaletteSource
        {
            Texture,
            ColorArray
        }

        [Serializable]
        public sealed class Settings
        {
            public bool enabled = true;

            [Tooltip("PixelSize divides the current camera resolution. TargetResolution locks the pixel grid to a fixed virtual resolution.")]
            public ResolutionMode resolutionMode = ResolutionMode.TargetResolution;

            [Min(1)]
            public int pixelSize = 4;

            [Tooltip("PixelSize mode uses this virtual output height, so the look stays consistent across different screen resolutions.")]
            [Min(1)]
            public int referenceHeight = 1080;

            [Min(1)]
            public int targetWidth = 426;

            [Min(1)]
            public int targetHeight = 240;

            [Tooltip("Floor gives a stable hard grid. Round can feel slightly more centered on some resolutions.")]
            public bool useFloorSnapping = true;

            [Tooltip("Texture is best for imported/shared palettes. ColorArray is easiest for quick palettes made in the inspector.")]
            public PaletteSource paletteSource = PaletteSource.Texture;

            [Tooltip("Optional palette texture. Leave empty to preserve all colors. Use a small PNG containing one palette color per pixel.")]
            public Texture2D paletteTexture;

            [Tooltip("Optional hand-authored palette. Used when Palette Source is ColorArray.")]
            [ColorUsage(false, false)]
            public Color[] paletteColors = Array.Empty<Color>();

            [Range(1, 256)]
            public int maxPaletteColors = 64;

            [Tooltip("Skip overlay cameras so UI cameras can remain sharp.")]
            public bool skipOverlayCameras = true;

            public bool affectSceneView = true;

            public Shader shader;
        }

        [SerializeField]
        private Settings settings = new();

        private Material material;
        private PixelationPass pass;

        public override void Create()
        {
            pass = new PixelationPass();
            pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.enabled)
                return;

            ref CameraData cameraData = ref renderingData.cameraData;

            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
                return;

            if (!settings.affectSceneView && cameraData.cameraType == CameraType.SceneView)
                return;

            if (settings.skipOverlayCameras && cameraData.renderType == CameraRenderType.Overlay)
                return;

            Shader shader = settings.shader != null ? settings.shader : Shader.Find(DefaultShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(ShortHikePixelationRendererFeature)} skipped. Shader '{DefaultShaderName}' was not found.");
                return;
            }

            if (material == null || material.shader != shader)
            {
                CoreUtils.Destroy(material);
                material = CoreUtils.CreateEngineMaterial(shader);
            }

            pass.Setup(material, settings);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        private sealed class PixelationPass : ScriptableRenderPass
        {
            private static readonly int SourceResolutionId = Shader.PropertyToID("_SourceResolution");
            private static readonly int TargetResolutionId = Shader.PropertyToID("_TargetResolution");
            private static readonly int UseFloorSnappingId = Shader.PropertyToID("_UseFloorSnapping");
            private static readonly int PaletteTextureId = Shader.PropertyToID("_PaletteTex");
            private static readonly int PaletteInfoId = Shader.PropertyToID("_PaletteInfo");
            private static readonly int PaletteColorsId = Shader.PropertyToID("_PaletteColors");

            private const string PassName = "Short Hike Pixelation";
            private const string PaletteKeyword = "_PALETTE_QUANTIZE";

            private readonly Vector4[] paletteColorBuffer = new Vector4[256];

            private Material material;
            private Settings settings;

            public PixelationPass()
            {
                profilingSampler = new ProfilingSampler(PassName);
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material, Settings settings)
            {
                this.material = material;
                this.settings = settings;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null || settings == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer)
                {
                    Debug.LogWarning($"{PassName} skipped. The active target is the back buffer, so it cannot be sampled as a texture.");
                    return;
                }

                TextureHandle source = resourceData.activeColorTexture;
                TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "_ShortHikePixelationColor";
                destinationDesc.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

                Vector2 sourceResolution = GetSourceResolution(cameraData);
                Vector2 targetResolution = GetTargetResolution(sourceResolution, settings);

                material.SetVector(SourceResolutionId, new Vector4(sourceResolution.x, sourceResolution.y, 0f, 0f));
                material.SetVector(TargetResolutionId, new Vector4(targetResolution.x, targetResolution.y, 0f, 0f));
                material.SetFloat(UseFloorSnappingId, settings.useFloorSnapping ? 1f : 0f);

                bool useTexturePalette = settings.paletteSource == PaletteSource.Texture && settings.paletteTexture != null;
                bool useColorArrayPalette = settings.paletteSource == PaletteSource.ColorArray
                    && settings.paletteColors != null
                    && settings.paletteColors.Length > 0;
                bool hasPalette = useTexturePalette || useColorArrayPalette;

                CoreUtils.SetKeyword(material, PaletteKeyword, hasPalette);

                if (useTexturePalette)
                {
                    Texture2D palette = settings.paletteTexture;
                    int colorCount = Mathf.Min(settings.maxPaletteColors, palette.width * palette.height);
                    material.SetTexture(PaletteTextureId, palette);
                    material.SetVector(PaletteInfoId, new Vector4(palette.width, palette.height, colorCount, 1f));
                }
                else if (useColorArrayPalette)
                {
                    int colorCount = Mathf.Min(settings.maxPaletteColors, settings.paletteColors.Length, paletteColorBuffer.Length);
                    for (int i = 0; i < colorCount; i++)
                    {
                        Color color = settings.paletteColors[i];
                        paletteColorBuffer[i] = new Vector4(color.r, color.g, color.b, color.a);
                    }

                    material.SetVectorArray(PaletteColorsId, paletteColorBuffer);
                    material.SetVector(PaletteInfoId, new Vector4(1f, 1f, colorCount, 2f));
                }

                RenderGraphUtils.BlitMaterialParameters parameters = new(source, destination, material, 0);
                renderGraph.AddBlitPass(parameters, PassName);

                resourceData.cameraColor = destination;
            }

            private static Vector2 GetSourceResolution(UniversalCameraData cameraData)
            {
                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                return new Vector2(Mathf.Max(1, descriptor.width), Mathf.Max(1, descriptor.height));
            }

            private static Vector2 GetTargetResolution(Vector2 sourceResolution, Settings settings)
            {
                if (settings.resolutionMode == ResolutionMode.TargetResolution)
                    return new Vector2(Mathf.Max(1, settings.targetWidth), Mathf.Max(1, settings.targetHeight));

                int pixelSize = Mathf.Max(1, settings.pixelSize);
                float aspect = sourceResolution.x / Mathf.Max(1f, sourceResolution.y);
                float targetHeight = Mathf.Max(1f, settings.referenceHeight / (float)pixelSize);
                float targetWidth = Mathf.Max(1f, targetHeight * aspect);

                return new Vector2(
                    Mathf.RoundToInt(targetWidth),
                    Mathf.RoundToInt(targetHeight));
            }
        }
    }
}
