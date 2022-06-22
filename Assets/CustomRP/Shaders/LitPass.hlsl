 
#ifndef CUSTOM_LIT_SHADER_INCLUDED
#define CUSTOM_LIT_SHADER_INCLUDED
#include "ShaderLibrary/Common.hlsl"
#include "ShaderLibrary/Surface.hlsl"
#include "ShaderLibrary/Shadows.hlsl"
#include "ShaderLibrary/Lighting.hlsl"
#include "ShaderLibrary/BRDF.hlsl"
	 
TEXTURE2D(_BaseMap);         //#define SAMPLER(samplerName)                  SamplerState samplerName
SAMPLER(sampler_BaseMap);	 //#define TEXTURE2D(textureName)                Texture2D textureName

//GPU Instancing
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)   
UNITY_DEFINE_INSTANCED_PROP(float , _Metallic)
UNITY_DEFINE_INSTANCED_PROP(float , _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

//传入顶点着色器的数据结构
struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//传入片元着色器的数据结构
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAE_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float3 normalWS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//顶点函数
Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);          //世界坐标
    
    output.normalWS = TransformObjectToWorldNormal(input.normalOS); //世界法线
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    
    return output;
}

//片元函数    
float4 LitPassFragment(Varyings input) : SV_TARGET
{
    
    UNITY_SETUP_INSTANCE_ID(input)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap , input.baseUV);    //#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)    textureName.Sample(samplerName, coord2)
    float4 baseCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    float4 base = baseMap * baseCol;
#if defined(_CLIPPING)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff));
#endif
    
    Surface surface;
    surface.position = input.positionWS;
    surface.normal = normalize(input.normalWS);
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Metallic);
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Smoothness);
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface,true);
#else
    BRDF brdf = GetBRDF(surface);
#endif
    float3 color = GetLighting(surface,brdf);
    return float4(color  , surface.alpha);
}
#endif

