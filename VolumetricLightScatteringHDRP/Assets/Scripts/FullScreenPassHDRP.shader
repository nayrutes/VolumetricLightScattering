﻿Shader "FullScreen/FullScreenPass"
{
    properties
    {
        _ColorTest ("Color Test", Color) = (0.5,0.5,0.5,0.5)
        _Amount("Amount Froxels", Vector) = (10,10,10)
        VolumetricFogSampler("Fog",3D)="black"{}
    }

    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    // The PositionInputs struct allow you to retrieve a lot of useful information for your fullScreenShader:
    // struct PositionInputs
    // {
    //     float3 positionWS;  // World space position (could be camera-relative)
    //     float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    //     uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    //     uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    //     float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    //     float  linearDepth; // View space Z coordinate                              : [Near, Far]
    // };

    // To sample custom buffers, you have access to these functions:
    // But be careful, on most platforms you can't sample to the bound color buffer. It means that you
    // can't use the SampleCustomColor when the pass color buffer is set to custom (and same for camera the buffer).
    // float3 SampleCustomColor(float2 uv);
    // float3 LoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    float4 _ColorTest;
    float3 _Amount;
    sampler3D VolumetricFogSampler;
    sampler2D _CurveTexture;

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        float4 color = float4(0.0, 0.0, 0.0, 0.0);

        // Load the camera color buffer at the mip 0 if we're not at the before rendering injection point
        if (_CustomPassInjectionPoint != CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING)
            color = float4(CustomPassSampleCameraColor(posInput.positionNDC.xy, 0), 1);

        // Add your custom pass code here
        float depthLinear = Linear01Depth(depth, _ZBufferParams);
        float4 curveSample = tex2D(_CurveTexture, float2(depthLinear, 0.5));
        //curveSample = DelinearizeRGBA(curveSample);
        //color.rgb = 1 - color.rgb;
        float3 positionInVolume = float3(posInput.positionNDC.x,posInput.positionNDC.y,curveSample.r);
        //float3 positionInVolume = float3(posInput.positionNDC.x,posInput.positionNDC.y,depthLinear);
        float4 scatteringInformation = tex3D(VolumetricFogSampler, positionInVolume);
        
        
        // Fade value allow you to increase the strength of the effect while the camera gets closer to the custom pass volume
        float f = 1 - abs(_FadeValue * 2 - 1);
        //return float4(color.rgb, color.a);
        
        //float depthLinear = LinearEyeDepth(depth, _ZBufferParams);// * length(varyings.viewVector);
        
        
        //Maybe Ignore depth if transmittance (1) is correctly calculated?
#if 0
        float3 inScattering = scatteringInformation.rgb;
        float transmittance = scatteringInformation.a;
        
        //(1) transmittance always 0 ?
        float3 finalPixelColor = color.rgb * transmittance.xxx + inScattering;
        return float4(finalPixelColor.rgb, 1-transmittance);
#else
        float3 inScattering = scatteringInformation.rgb;
        float opticalDepth = scatteringInformation.a;
        float opacity = 1- exp(-opticalDepth);
        
        return float4(inScattering, opacity);
        //return float4(curveSample.rrr,1);
        //return float4(depthLinear.rrr,1);
#endif
        //return float4(color.rgb, color.a);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
