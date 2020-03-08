Shader "FullScreen/DebugFullScreenPassHDRP"
{
    properties
    {
        size ("Size", float) = 0.5
        debugImageSize ("Debug Image Size", Vector) = (256,256,0,0)
        debugTexture("Debug Texture",2D)="white"{}
    }

    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    
    // Instead #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassInjectionPoint.cs.hlsl"
    
    float _CustomPassInjectionPoint;
    float _FadeValue;
    
    float3 CustomPassSampleCameraColor(float2 uv, float lod, bool uvGuards = true)
    {
        if (uvGuards)
            uv = clamp(uv, 0, 1 - _ScreenSize.zw / 2.0);
    
        switch ((int)_CustomPassInjectionPoint)
        {
            case CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING: return float3(0, 0, 0);
            // there is no color pyramid yet for before transparent so we can't sample with mips.
            // Also, we don't use _RTHandleScaleHistory to sample because the color pyramid bound is the actual camera color buffer which is at the resolution of the camera
            case CUSTOMPASSINJECTIONPOINT_BEFORE_TRANSPARENT:
            case CUSTOMPASSINJECTIONPOINT_BEFORE_PRE_REFRACTION: return SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, uv * _RTHandleScaleHistory.xy, 0).rgb;
            default: return SampleCameraColor(uv, lod);
        }
    }
    
    float3 CustomPassLoadCameraColor(uint2 pixelCoords, float lod)
    {
        switch ((int)_CustomPassInjectionPoint)
        {
            case CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING: return float3(0, 0, 0);
            // there is no color pyramid yet for before transparent so we can't sample with mips.
            case CUSTOMPASSINJECTIONPOINT_BEFORE_TRANSPARENT:
            case CUSTOMPASSINJECTIONPOINT_BEFORE_PRE_REFRACTION: return LOAD_TEXTURE2D_X_LOD(_ColorPyramidTexture, pixelCoords, 0).rgb;
            default: return LoadCameraColor(pixelCoords, lod);
        }
    }
    
    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    
    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };
    
    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }
    
    
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
    // float4 SampleCustomColor(float2 uv);
    // float4 LoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    
    sampler2D debugTexture;
    float size;
    float2 debugImageSize;

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        float4 color = float4(0.0, 0.0, 0.0, 0.0);

        // Load the camera color buffer at the mip 0 if we're not at the before rendering injection point
        if (_CustomPassInjectionPoint != CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING)
            color = float4(CustomPassLoadCameraColor(varyings.positionCS.xy, 0), 1);

        // Add your custom pass code here
        
        float ratio = _ScreenSize.x / _ScreenSize.y;
        
        if(posInput.positionNDC.x*ratio < size && posInput.positionNDC.y < size)
        {
            float2 debugImageCoordiantes = posInput.positionNDC * debugImageSize;
            float2 displaySizeCoordinates = posInput.positionNDC / size;
            color = float4(tex2D(debugTexture, displaySizeCoordinates).rgb,1);
            //color.xyz = 1-color.xyz;
        }

        // Fade value allow you to increase the strength of the effect while the camera gets closer to the custom pass volume
        float f = 1 - abs(_FadeValue * 2 - 1);
        return float4(color.rgb + f, color.a);
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
