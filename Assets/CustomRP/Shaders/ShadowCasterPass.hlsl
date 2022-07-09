#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#include "../ShaderLibrary/Common.hlsl"

TEXTURE2D(_BaseMap);            //#define TEXTURE2D(textureName)                Texture2D textureName
SAMPLER(sampler_BaseMap);	    //#define SAMPLER(samplerName)                  SamplerState samplerName


//GPU Instancing
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//提供纹理的缩放和平移
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

//传入顶点着色器的数据结构
struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//传入片元着色器的数据结构
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//顶点函数
Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    //输出位置和索引，并复制索引
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);          //世界坐标
    
#if UNITY_REVERSED_Z
    output.positionCS.z = min(output.positionCS.z , output.positionCS.w * UNITY_NEAR_CLIP_VALUE);        
#else
    output.positionCS.z = max(output.positionCS.z , output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    
    return output;
}

//片元函数    
void ShadowCasterPassFragment(Varyings input)
{
    
    UNITY_SETUP_INSTANCE_ID(input)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap , input.baseUV);    //#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)    textureName.Sample(samplerName, coord2)
    float4 baseCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    float4 base = baseMap * baseCol;
#if defined (_SHADOWS_CLIP)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff));
#endif
} 

#endif
