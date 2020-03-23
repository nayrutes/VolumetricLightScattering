Shader "FullScreen/GaussianBlur"
{
    properties
    {
        //VolumetricFogSampler("Fog",2D)="black"{}
        blurFactor("blurFactor",float) = 0.1
        _MainTex("Tex",2D) = "black"
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
    // float4 SampleCustomColor(float2 uv);
    // float4 LoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    float blurFactor;
    //TEXTURE2D_X(_Source);
    sampler2D _MainTex;
     //normpdf function gives us a Guassian distribution for each blur iteration; 
     //this is equivalent of multiplying by hard #s 0.16,0.15,0.12,0.09, etc. in code above
     float normpdf(float x, float sigma)
     {
         return 0.39894*exp(-0.5*x*x / (sigma*sigma)) / sigma;
     }
     //this is the blur function... pass in standard col derived from tex2d(_MainTex,i.uv)
     half4 blur(sampler2D tex, float2 uv,float blurAmount) {
         //get our base color...
         half4 col = tex2D(tex, uv);
         //total width/height of our blur "grid":
         const int mSize = 3; //11
         //this gives the number of times we'll iterate our blur on each side 
         //(up,down,left,right) of our uv coordinate;
         //NOTE that this needs to be a const or you'll get errors about unrolling for loops
         const int iter = (mSize - 1) / 2;
         //run loops to do the equivalent of what's written out line by line above
         //(number of blur iterations can be easily sized up and down this way)
         for (int i = -iter; i <= iter; ++i) {
             for (int j = -iter; j <= iter; ++j) {
                 col += tex2D(tex, float2(uv.x + i * blurAmount, uv.y + j * blurAmount)) * normpdf(float(i), 7);
             }
         }
         //return blurred color
         return col/mSize;
     }

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
        color = blur(_MainTex, posInput.positionNDC,blurFactor);
        // Fade value allow you to increase the strength of the effect while the camera gets closer to the custom pass volume
        //float f = 1 - abs(_FadeValue * 2 - 1);
        //return float4(color.rgb + f, color.a);
        return color;
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
