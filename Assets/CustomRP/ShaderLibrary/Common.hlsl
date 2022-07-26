//公共方法库
#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"
//定义一些宏取代常用的转换矩阵
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
//获取值的平方
float Square (float v) {
	return v * v;
}

//计算两点间距离的平方
float DistanceSquared(float3 pA,float3 pB)
{
    return dot(pA - pB, pA - pB);
}

#if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK_ALWAYS)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

//根据unity_OrthographicCamera的w分量判断是否为正交相机
bool IsOrthographicCamera()
{
    return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear(float rawDepth)
{
#if UNITY_REVERSED_Z
    rawDepth = 1.0 - rawDepth;
#endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y; //yz 分量分别是近远裁剪面的值
}

#include "Fragment.hlsl"

void ClipLOD(Fragment fragment, float fade)
{
#if defined(LOD_FADE_CROSSFADE)
    float dither = InterleavedGradientNoise(fragment.positionSS,0);
    clip(fade + (fade < 0.0? dither : -dither));    //Lod过渡的时候，过渡的两个物体中有一个LOD Fade为负数
#endif
}

//解码法线数据，得到原来的法线向量
float3 DecodeNormal(float4 sample, float scale)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(sample,scale);
#else
    return UnpackNormalmapRGorAG(sample,scale);
#endif
}

//将法线从切线空间转换到世界空间
float3 NormalTangentToWorld(float3 normalTS , float3 normalWS , float4 tangentWS)
{
    //构建切线到世界空间的转换矩阵，需要世界空间法线、世界空间的切线的xyz 和 w 分量
    float3x3 tangentToWorld = CreateTangentToWorld(normalWS,tangentWS.xyz,tangentWS.w);
    return TransformTangentToWorld(normalTS,tangentToWorld);
}


#endif
