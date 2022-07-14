//阴影采样
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
//如果使用的是 PCF 3x3
#if defined(_DIRECTIONAL_PCF3)
//需要4个滤波样本
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWD_DIRECTIONAL_LIGHT_COUNT 4
//阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

//烘焙阴影数据
struct ShadowMask
{
    bool always;
    bool distance;
    float4 shadows;
};

//阴影数据
struct ShadowData
{
    int cascadeIndex; 
    //是否采样阴影标识
    float strength;
    //混合级联
    float cascadeBlend;
    ShadowMask shadowMask;
};

#define MAX_CASCADE_COUNT 4
CBUFFER_START(_CustomShadows)
//级联数量和包围球数据
int _CascadeCount;
float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
//级联数据
float4 _CascadeData[MAX_CASCADE_COUNT];
//阴影转换矩阵
float4x4 _DirectionalShadowMatrices[MAX_SHADOWD_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
//float _ShadowDistance;
//阴影过渡
float4 _ShadowDistanceFade;
float4 _ShadowAtlasSize;
CBUFFER_END

//阴影的数据信息
struct DirectionalShadowData{
    float strength;
    int tileIndex;
    //法线偏差
    float normalBias;
    int shadowMaskChannel;
};

//采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)  //阴影纹理空间中的表面位置
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,positionSTS);
}

//通过 DIRECTIONAL_FILTER_SETUP 方法获取多个采样权重和位置，然后根据这些信息采样
float FilterDirectionalShadow(float3 positionSTS)
{
#if defined(DIRECTIONAL_FILTER_SETUP)
    //样本权重
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    //样本位置
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;    //xy分量是图集纹素大小，zw分量是图集尺寸
    DIRECTIONAL_FILTER_SETUP(size,positionSTS.xy,weights,positions);
    float shadow = 0;
    for(int i = 0 ; i < DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        //遍历所有样本得到权重和
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy,positionSTS.z));
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

//得到烘焙阴影的衰减值
float GetBakedShadow(ShadowMask mask,int channel)
{
    float shadow = 1.0;
    if(mask.distance || mask.always)
    {
        if(channel >= 0) 
        {
            shadow = mask.shadows[channel];
        }
    }
    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    if(mask.distance || mask.always)
    {
        return lerp(1.0,GetBakedShadow(mask,channel),strength);
    }
    return 1.0;
}

//混合烘焙和实时阴影
float MixBakedAndRealtimeShadows(ShadowData global, float shadow, int shadowMaskChannel , float strength)
{
    float baked = GetBakedShadow(global.shadowMask,shadowMaskChannel);
    if(global.shadowMask.always)
    {
        shadow = lerp(1.0,shadow,global.strength);
        shadow = min(baked , shadow);
        return lerp(1.0,shadow,strength);
    }
    if(global.shadowMask.distance)
    {
        shadow = lerp(baked,shadow,global.strength);
        return lerp(1.0,shadow,strength);
    }
    return lerp(1.0 ,shadow, strength * global.strength);
}

float GetCascadedShadow(DirectionalShadowData directional,ShadowData global,Surface surfaceWS)
{
    //计算法线偏差
    float3 normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.cascadeIndex].y);
     //通过阴影转换矩阵和表面位置得到阴影纹理（图块）空间的位置，然后对图集进行采样
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],float4(surfaceWS.position + normalBias,1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    
    //如果级联混合小于1，代表在级联层过渡区域中，必须从下一个级联中采样并在两个值之间进行插值
    if(global.cascadeBlend <1.0)
    {
        normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1],float4(surfaceWS.position + normalBias,1.0)).xyz;  
        shadow = lerp(FilterDirectionalShadow(positionSTS) , shadow, global.cascadeBlend);
    }
    return shadow;
}

//计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData directional,ShadowData global, Surface surfaceWS)
{
    //如果不接受阴影，阴影衰减为1
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
    float shadow;
    if(directional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask,directional.shadowMaskChannel,abs(directional.strength));
    }
    else
    {
        shadow = GetCascadedShadow(directional,global,surfaceWS);
        shadow = MixBakedAndRealtimeShadows(global,shadow,directional.shadowMaskChannel,directional.strength);
    }
    //最终阴影衰减值是阴影强度和衰减因子的插值
    return shadow;
}

//公式计算阴影过渡时的强度
float FadeShadowStrength(float distance,float scale,float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

//得到世界空间的表面阴影数据
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.shadowMask.always = false;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;
    data.cascadeBlend = 1.0;
    //阴影最大距离的过渡阴影强度
    data.strength = FadeShadowStrength(surfaceWS.depth,_ShadowDistanceFade.x , _ShadowDistanceFade.y);
    int i;
    //如果物体表面到球心的平方距离小于球体半径的平方，就说明该物体在这层级联包围球中，得到合适的级联层级索引
    for(i = 0; i<_CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position,sphere.xyz);
        if(distanceSqr < sphere.w)
        {
            //计算级联阴影的过渡强度
            float fade = FadeShadowStrength(distanceSqr , _CascadeData[i].x,_ShadowDistanceFade.z);
            //如果绘制的对象在最后一个级联的范围内，计算级联的过渡阴影强度，和阴影最大距离的过渡阴影强度相乘得到最终阴影强度
            if(i == _CascadeCount -1)
            {
                data.strength *= fade;
            }
            else
            {
                data.cascadeBlend = fade;
            }
            break;
        }
    }
    //如果超出级联范围，不进行阴影采样
    if(i == _CascadeCount) 
    {
        data.strength = 0.0;
    }
    //当混合模式为抖动模式时，如果我们不在最后一个级联中，且当级联混合值小于抖动值时，则跳到下一个级联
#if defined(_CASCADE_BLEND_DITHER)
    else if(data.cascadeBlend < surfaceWS.dither)
    {
        i += 1;
    }
#endif
#if !defined(_CASCADE_BLEND_SOFT)
    data.cascadeBlend = 1.0;
#endif
    
    
    data.cascadeIndex = i;
    return data;
}


struct OtherShadowData
{
    float strength;
    int shadowMaskChannel;
};

//得到其他类型光源的阴影衰减
float GetOtherShadowAttenuation(OtherShadowData other,ShadowData global, Surface surfaceWS)
{
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
    float shadow;
    if(other.strength > 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask,other.shadowMaskChannel,other.strength);
    }
    else
    {
        shadow = 1.0;
    }
    return shadow;
}

#endif