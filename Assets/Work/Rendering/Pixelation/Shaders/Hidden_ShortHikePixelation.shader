Shader "Hidden/DungeonDinner/ShortHikePixelation"
{
    Properties
    {
        _PaletteTex("Palette Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Short Hike Pixelation"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local_fragment _ _PALETTE_QUANTIZE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_PaletteTex);

            float2 _SourceResolution;
            float2 _TargetResolution;
            float _UseFloorSnapping;
            float4 _PaletteInfo;
            float4 _PaletteColors[256];

            float2 SnapUvToPixelGrid(float2 uv)
            {
                float2 targetResolution = max(_TargetResolution, float2(1.0, 1.0));
                float2 gridPosition = uv * targetResolution;
                float2 snapped = lerp(round(gridPosition), floor(gridPosition), saturate(_UseFloorSnapping));
                return (snapped + float2(0.5, 0.5)) / targetResolution;
            }

            float3 FindNearestPaletteColor(float3 color)
            {
                int paletteWidth = max(1, (int)_PaletteInfo.x);
                int paletteHeight = max(1, (int)_PaletteInfo.y);
                int colorCount = max(1, min(256, (int)_PaletteInfo.z));

                float3 bestColor = color;
                float bestDistance = 1.0e20;

                UNITY_LOOP
                for (int i = 0; i < 256; i++)
                {
                    if (i >= colorCount)
                        break;

                    float3 paletteColor;
                    if (_PaletteInfo.w < 1.5)
                    {
                        int x = i % paletteWidth;
                        int y = i / paletteWidth;
                        if (y >= paletteHeight)
                            break;

                        float2 paletteUv = (float2(x, y) + float2(0.5, 0.5)) / float2(paletteWidth, paletteHeight);
                        paletteColor = SAMPLE_TEXTURE2D_LOD(_PaletteTex, sampler_PointClamp, paletteUv, 0).rgb;
                    }
                    else
                    {
                        paletteColor = _PaletteColors[i].rgb;
                    }

                    float3 delta = color - paletteColor;
                    float distance = dot(delta, delta);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestColor = paletteColor;
                    }
                }

                return bestColor;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 snappedUv = SnapUvToPixelGrid(saturate(input.texcoord.xy));
                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, snappedUv, 0);

                #if defined(_PALETTE_QUANTIZE)
                color.rgb = FindNearestPaletteColor(color.rgb);
                #endif

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
