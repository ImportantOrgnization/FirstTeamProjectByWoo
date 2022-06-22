#ifndef CUSTOM_UNLIT_SHADER_INCLUDED
#define CUSTOM_UNLIT_SHADER_INCLUDED
#include "ShaderLibrary/Common.hlsl"

//所有的材质球属性都在这个常量缓冲区中 (SRP Batcher)
//CBUFFER_START(UnityPerMaterial)
//    float4 _BaseColor;
//CBUFFER_END
	 
TEXTURE2D(_BaseMap);         //#define SAMPLER(samplerName)                  SamplerState samplerName
SAMPLER(sampler_BaseMap);	 //#define TEXTURE2D(textureName)                Texture2D textureName

//GPU Instancing
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
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
Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS); 
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    
    return output;
}

//片元函数    
float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap , input.baseUV);    //#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)    textureName.Sample(samplerName, coord2)
    float4 baseCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    float4 base = baseMap * baseCol;
#if defined(_CLIPPING)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff));
#endif
    return base;
    
}
#endif