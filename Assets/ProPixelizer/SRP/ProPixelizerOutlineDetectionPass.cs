// Copyright Elliot Bentine, 2018-
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace ProPixelizer
{
    /// <summary>
    /// Performs the outline rendering and detection pass.
    /// </summary>
    public class OutlineDetectionPass : ProPixelizerPass
    {
        public OutlineDetectionPass(ShaderResources resources)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            Materials = new MaterialLibrary(resources);
        }

        private MaterialLibrary Materials;

        /// <summary>
        /// Shader resources used by the OutlineDetectionPass.
        /// </summary>
        [Serializable]
        public sealed class ShaderResources
        {
            public Shader OutlineDetection;

            public ShaderResources Load()
            {
                OutlineDetection = Shader.Find(OutlineDetectionShader);
                return this;
            }
        }

        /// <summary>
        /// Materials used by the OutlineDetectionPass.
        /// </summary>
        public sealed class MaterialLibrary
        {
            private ShaderResources Resources;
            public Material OutlineDetection
            {
                get
                {
                    if (_OutlineDetection == null)
                        _OutlineDetection = new Material(Resources.OutlineDetection);
                    return _OutlineDetection;
                }
            }
            private Material _OutlineDetection;

            public MaterialLibrary(ShaderResources resources)
            {
                Resources = resources;
            }
        }


        public bool DepthTestOutlines;
        public float DepthTestThreshold;
        public bool UseNormalsForEdgeDetection = true;
        public float NormalEdgeDetectionSensitivity = 1f;

#if URP_13
        /// <summary>
        /// Buffer into which objects are rendered using the Outline pass.
        /// </summary>
        public RTHandle _OutlineObjectBuffer;
        public RTHandle _OutlineObjectBuffer_Depth;

        /// <summary>
        /// Buffer to store the results from outline analysis.
        /// </summary>
        public RTHandle _OutlineBuffer;
#else
        /// <summary>
        /// Buffer into which objects are rendered using the Outline pass.
        /// </summary>
        private int _OutlineObjectBuffer;
        public int _OutlineObjectBuffer_Depth;

        /// <summary>
        /// Buffer to store the results from outline analysis.
        /// </summary>
        private int _OutlineBuffer;
#endif

        static ShaderTagId ProPixelizerShaderTagID = new ShaderTagId(PROPIXELIZER_SHADER_TAG);

        private const string OutlineDetectionShader = "Hidden/ProPixelizer/SRP/OutlineDetection";

        private Vector4 TexelSize;
        public const string PROPIXELIZER_OBJECT_BUFFER = "ProPixelizerMetadata";
        public const string OUTLINE_BUFFER = "_ProPixelizerOutlines";
        private const string PROPIXELIZER_SHADER_TAG = "ProPixelizer";

#if UNITY_6000_0_OR_NEWER
        private static readonly GlobalKeyword NormalEdgeDetectionKeyword = GlobalKeyword.Create("NORMAL_EDGE_DETECTION_ON");

        internal TextureHandle OutlineObjectTexture { get; private set; }
        internal TextureHandle OutlineObjectDepthTexture { get; private set; }
        internal TextureHandle OutlineTexture { get; private set; }

        private sealed class RenderObjectsPassData
        {
            internal RendererListHandle RendererList;
            internal bool IsPreviewCamera;
            internal bool UseNormals;
            internal Vector4 RenderTargetInfo;
        }

        private sealed class DetectOutlinesPassData
        {
            internal TextureHandle Metadata;
            internal TextureHandle Depth;
            internal Material Material;
            internal Vector4 TexelSize;
            internal bool DepthTestOutlines;
            internal float DepthTestThreshold;
            internal bool UseNormals;
            internal float NormalSensitivity;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            var colorDescriptor = cameraData.cameraTargetDescriptor;
            colorDescriptor.useMipMap = false;
            colorDescriptor.depthBufferBits = 0;
            colorDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            colorDescriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

            var depthDescriptor = cameraData.cameraTargetDescriptor;
            depthDescriptor.useMipMap = false;
            depthDescriptor.colorFormat = RenderTextureFormat.Depth;

            OutlineObjectTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, colorDescriptor, PROPIXELIZER_OBJECT_BUFFER, true, FilterMode.Point);
            OutlineObjectDepthTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, depthDescriptor, PROPIXELIZER_OBJECT_BUFFER + "_Depth", true, FilterMode.Point);
            OutlineTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, colorDescriptor, OUTLINE_BUFFER, true, FilterMode.Point);

            TexelSize = new Vector4(
                1f / cameraData.cameraTargetDescriptor.width,
                1f / cameraData.cameraTargetDescriptor.height,
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height);

            var sortingSettings = new SortingSettings(cameraData.camera);
            var drawingSettings = new DrawingSettings(ProPixelizerShaderTagID, sortingSettings);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            var rendererList = renderGraph.CreateRendererList(rendererListParams);

            using (var builder = renderGraph.AddRasterRenderPass<RenderObjectsPassData>(
                "ProPixelizer Outline Pass (Render Objects)", out var passData))
            {
                passData.RendererList = rendererList;
                passData.IsPreviewCamera = cameraData.cameraType == CameraType.Preview;
                passData.UseNormals = UseNormalsForEdgeDetection;
                var handleProps = RTHandles.rtHandleProperties;
                passData.RenderTargetInfo = new Vector4(
                    cameraData.cameraTargetDescriptor.width,
                    cameraData.cameraTargetDescriptor.height,
                    handleProps.rtHandleScale.x,
                    handleProps.rtHandleScale.y);

                builder.UseRendererList(rendererList);
                builder.SetRenderAttachment(OutlineObjectTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(OutlineObjectDepthTexture, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (RenderObjectsPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector("_ProPixelizer_RenderTargetInfo", data.RenderTargetInfo);
                    context.cmd.SetGlobalFloat("_ProPixelizer_Pixel_Scale", data.IsPreviewCamera ? 0.01f : 1f);
                    context.cmd.SetKeyword(NormalEdgeDetectionKeyword, data.UseNormals);
                    context.cmd.ClearRenderTarget(RTClearFlags.All, Color.white, 1f, 0);
                    context.cmd.DrawRendererList(data.RendererList);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DetectOutlinesPassData>(
                "ProPixelizer Outline Detection", out var passData))
            {
                passData.Metadata = OutlineObjectTexture;
                passData.Depth = OutlineObjectDepthTexture;
                passData.Material = Materials.OutlineDetection;
                passData.TexelSize = TexelSize;
                passData.DepthTestOutlines = DepthTestOutlines;
                passData.DepthTestThreshold = DepthTestThreshold;
                passData.UseNormals = UseNormalsForEdgeDetection;
                passData.NormalSensitivity = NormalEdgeDetectionSensitivity;

                builder.UseTexture(OutlineObjectTexture, AccessFlags.Read);
                builder.UseTexture(OutlineObjectDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(OutlineTexture, 0, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(OutlineTexture, Shader.PropertyToID(OUTLINE_BUFFER));
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (DetectOutlinesPassData data, RasterGraphContext context) =>
                {
                    CoreUtils.SetKeyword(data.Material, "DEPTH_TEST_OUTLINES_ON", data.DepthTestOutlines);
                    data.Material.SetFloat("_OutlineDepthTestThreshold", data.DepthTestThreshold);
                    data.Material.SetFloat("_NormalEdgeDetectionSensitivity", data.NormalSensitivity);
                    context.cmd.SetKeyword(NormalEdgeDetectionKeyword, data.UseNormals);
                    context.cmd.SetGlobalTexture("_MainTex", data.Metadata);
                    context.cmd.SetGlobalTexture("_MainTex_Depth", data.Depth);
                    context.cmd.SetGlobalVector("_TexelSize", data.TexelSize);
                    Blitter.BlitTexture(context.cmd, data.Metadata, new Vector4(1f, 1f, 0f, 0f), data.Material, 0);
                });
            }
        }
#endif

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var outlineDescriptor = cameraTextureDescriptor;
            outlineDescriptor.useMipMap = false;
            outlineDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            outlineDescriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
#if URP_13
            var depthDescriptor = outlineDescriptor;
            depthDescriptor.colorFormat = RenderTextureFormat.Depth;
            outlineDescriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _OutlineObjectBuffer, outlineDescriptor, name: PROPIXELIZER_OBJECT_BUFFER);
            RenderingUtils.ReAllocateIfNeeded(ref _OutlineObjectBuffer_Depth, depthDescriptor, name: PROPIXELIZER_OBJECT_BUFFER);
            RenderingUtils.ReAllocateIfNeeded(ref _OutlineBuffer, outlineDescriptor, name: OUTLINE_BUFFER);
#else
            _OutlineObjectBuffer = Shader.PropertyToID(PROPIXELIZER_OBJECT_BUFFER);
            _OutlineObjectBuffer_Depth = _OutlineObjectBuffer;
            _OutlineBuffer = Shader.PropertyToID(OUTLINE_BUFFER);
            cmd.GetTemporaryRT(_OutlineObjectBuffer, outlineDescriptor, FilterMode.Point);
            cmd.GetTemporaryRT(_OutlineBuffer, outlineDescriptor, FilterMode.Point);
#endif

            TexelSize = new Vector4(
                1f / cameraTextureDescriptor.width,
                1f / cameraTextureDescriptor.height,
                cameraTextureDescriptor.width,
                cameraTextureDescriptor.height
            );

#if UNITY_2023_1_OR_NEWER
            // Required for 2023.2 to get Unity to correctly set target buffers after a ProPixelizer draw call.
            ConfigureTarget(_OutlineObjectBuffer, _OutlineObjectBuffer_Depth);
#endif
        }
        public override void FrameCleanup(CommandBuffer cmd)
        {
#if URP_13
#else
            cmd.ReleaseTemporaryRT(_OutlineObjectBuffer);
            cmd.ReleaseTemporaryRT(_OutlineBuffer);
#endif
        }

#if URP_13
        public void Dispose()
        {
            _OutlineObjectBuffer?.Release();
            _OutlineObjectBuffer_Depth?.Release();
            _OutlineBuffer?.Release();
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // set externally managed RTHandle references to null.
        }
#endif

        public const string PROFILER_TAG = "ProPixelizerOutlines";

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Prepare(cmd, ref renderingData);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (DepthTestOutlines)
            {
                Materials.OutlineDetection.EnableKeyword("DEPTH_TEST_OUTLINES_ON");
                Materials.OutlineDetection.SetFloat("_OutlineDepthTestThreshold", DepthTestThreshold);
            }
            else
                Materials.OutlineDetection.DisableKeyword("DEPTH_TEST_OUTLINES_ON");

            if (UseNormalsForEdgeDetection)
            {
                Materials.OutlineDetection.SetFloat("_NormalEdgeDetectionSensitivity", NormalEdgeDetectionSensitivity);
            }

            CommandBuffer buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Pass";

            // Preview cameras must unfortunately disable dither expansion
            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                buffer.SetGlobalFloat("_ProPixelizer_Pixel_Scale", 0.01f);
            else 
                buffer.SetGlobalFloat("_ProPixelizer_Pixel_Scale", 1f);

            if (UseNormalsForEdgeDetection)
                buffer.EnableShaderKeyword("NORMAL_EDGE_DETECTION_ON");
            else
                buffer.DisableShaderKeyword("NORMAL_EDGE_DETECTION_ON");


            // Set up matrices for rendering outlines.
#if CAMERADATA_MATRICES
            buffer.SetViewMatrix(renderingData.cameraData.GetViewMatrix());
            buffer.SetProjectionMatrix(renderingData.cameraData.GetProjectionMatrix());
#else
            buffer.SetViewMatrix(renderingData.cameraData.camera.worldToCameraMatrix);
            buffer.SetProjectionMatrix(renderingData.cameraData.camera.projectionMatrix);
#endif

            // Render outlines into a render target.
#if URP_13
            buffer.SetRenderTarget(_OutlineObjectBuffer, _OutlineObjectBuffer_Depth);
#else
            buffer.SetRenderTarget(_OutlineObjectBuffer);
#endif
            buffer.ClearRenderTarget(true, true, Color.white);
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);

            var sort = new SortingSettings(renderingData.cameraData.camera);
            var drawingSettings = new DrawingSettings(ProPixelizerShaderTagID, sort);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);
#if UNITY_2023_2_OR_NEWER
            buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Pass (Render Objects)";
            var renderListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            var renderList = context.CreateRendererList(ref renderListParams);
            buffer.DrawRendererList(renderList);
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);
#else
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
#endif

            // Perform outline detection
            buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Detection";
            buffer.SetGlobalTexture("_MainTex", _OutlineObjectBuffer);
#if URP_13
            buffer.SetGlobalTexture("_MainTex_Depth", _OutlineObjectBuffer_Depth);//, RenderTextureSubElement.Depth);
#else
            buffer.SetGlobalTexture("_MainTex_Depth", _OutlineObjectBuffer_Depth, RenderTextureSubElement.Depth);
#endif
            buffer.SetGlobalVector("_TexelSize", TexelSize);

#if BLIT_API
            Blitter.BlitCameraTexture(buffer, _OutlineObjectBuffer, _OutlineBuffer, Materials.OutlineDetection, 0);
#else
            Blit(buffer, _OutlineObjectBuffer, _OutlineBuffer, Materials.OutlineDetection);
#endif

            buffer.SetGlobalTexture(OUTLINE_BUFFER, _OutlineBuffer);

            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);
        }
    }
}
