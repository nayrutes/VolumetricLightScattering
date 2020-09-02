Shader"CustomShader/BlitShader"
 {
 Properties {
 _MainTex ("Texture", 2D) = "white" {}
 _LeftTex ("Texture Left", 2D) = "white" {}
 _RightTex ("Texture Right", 2D) = "white" {}
 _TopTex ("Texture Top", 2D) = "white" {}
 _BottomTex ("Texture Bottom", 2D) = "white" {}
 _FrontTex ("Texture Front", 2D) = "white" {}
 _BackTex ("Texture Back", 2D) = "white" {}
 _Scaler ("Scaler", Range (0,3.0)) = 1.0
 }
 
 
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 
 sampler2D _LeftTex;
 sampler2D _RightTex;
 sampler2D _TopTex;
 sampler2D _BottomTex;
 sampler2D _FrontTex;
 sampler2D _BackTex;
 
 float _Scaler;
 
 struct VertexData
    {
        float4 vertex : POSITION;float2 uv : TEXCOORD0;
    };
  
    struct Interpolators
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };
   
    Interpolators VertexProgram (VertexData v)
    {
        Interpolators i;
        i.pos = UnityObjectToClipPos(v.vertex);
        i.uv = v.uv;
        return i;
    }
ENDCG
 
 
 SubShader {
 Cull Off ZTest Always ZWrite Off 
 Pass {
 CGPROGRAM
 #pragma vertex VertexProgram
 #pragma fragment FragmentProgram
 
 half4 FragmentProgram (Interpolators i) : SV_Target 
 {
    //return half4(i.uv.x,i.uv.y, 0, 1);
float o3 = 1.0/3.0;
    
 bool vbottomthird = i.uv.y >0.0 && i.uv.y < o3;
 bool vmidthird = i.uv.y >o3 && i.uv.y < (2.0/3.0);
 bool vtopthird = i.uv.y >(2.0/3.0) && i.uv.y < (3.0/3.0);
 
 bool hleftthird = i.uv.x >0.0 && i.uv.x < o3;
 bool hmidthird = i.uv.x >o3 && i.uv.x < (2.0/3.0);
 bool hrightthird = i.uv.x >(2.0/3.0) && i.uv.x < (3.0/3.0);
 
 
 //Bottom
    if(hleftthird && vmidthird){
    //return half4(i.uv.x,i.uv.y, 0, 1);
        return tex2D(_BottomTex, (i.uv-float2(0,o3))*3);
    }
    //Left
    else if(hmidthird && vmidthird){
    //return half4(i.uv.x,i.uv.y, 0, 1);
        return tex2D(_LeftTex, (i.uv-float2(o3,o3))*3);
    }
    //Right
    else if(hrightthird && vmidthird){
    //return half4(i.uv.x,i.uv.y, 0, 1);
        return tex2D(_RightTex, (i.uv-float2(2*o3,o3))*3);
    }
    //Front
    else if(hleftthird && vbottomthird){
        return tex2D(_FrontTex, (i.uv-float2(0,0))*3) * half4(1, 1, 1, 0);
    }
    //Top
    else if(hmidthird && vbottomthird){
        return tex2D(_TopTex, (i.uv-float2(o3,0))*3);
    }
    //Back
    else if(hrightthird && vbottomthird){
        return tex2D(_BackTex, (i.uv-float2(2*o3,0))*3);
    }
    
    else
        return half4(0,0,0,1);
 }
 
 ENDCG
 }
 }
 }

//Shader "CustomShader/BlitShader"
//{
//    HLSLINCLUDE
//
//    #pragma target 4.5
//    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
//
//    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
//    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
//    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
//    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
//    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
//
//    struct Attributes
//    {
//        uint vertexID : SV_VertexID;
//        UNITY_VERTEX_INPUT_INSTANCE_ID
//    };
//
//    struct Varyings
//    {
//        float4 positionCS : SV_POSITION;
//        float2 texcoord   : TEXCOORD0;
//        UNITY_VERTEX_OUTPUT_STEREO
//    };
//
//    Varyings Vert(Attributes input)
//    {
//        Varyings output;
//        UNITY_SETUP_INSTANCE_ID(input);
//        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
//        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
//        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
//        return output;
//    }
//
//    // List of properties to control your post process effect
//    float _Intensity;
//    TEXTURE2D_X(_InputTexture);
//
//    float4 CustomPostProcess(Varyings input) : SV_Target
//    {
//        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//
//        uint2 positionSS = input.texcoord * _ScreenSize.xy;
//        float3 outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz;
//
//        return float4(outColor, 1);
//    }
//
//    ENDHLSL
//
//    SubShader
//    {
//        Pass
//        {
//            Name "BlitShader"
//
//            ZWrite Off
//            ZTest Always
//            Blend Off
//            Cull Off
//
//            HLSLPROGRAM
//                #pragma fragment CustomPostProcess
//                #pragma vertex Vert
//            ENDHLSL
//        }
//    }
//    Fallback Off
//}
