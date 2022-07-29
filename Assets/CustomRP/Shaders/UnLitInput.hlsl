#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,name)

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(float, ZWrite)
UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesDistance)
UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesRange)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct InputConfig
{
    Fragment fragment;
    float2 baseUV;
    float2 detailUV;
    float4 color;
    float3 flipbookUVB;
    bool flipbookBlending;
    bool nearFade;
    bool softParticles;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV,float2 detailUV = 0.0)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.baseUV = baseUV;
    c.detailUV = detailUV;
    c.color = 1.0;
    c.flipbookUVB = 0.0;
    c.flipbookBlending = false;
    c.nearFade = false;
    c.softParticles = false;
    return c;
}

float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase(InputConfig c)
{
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,c.baseUV);
    if(c.flipbookBlending)
    {
        baseMap = lerp(baseMap,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,c.flipbookUVB.xy),c.flipbookUVB.z);
    }
    if(c.nearFade)
    {
        float nearAttenuation = (c.fragment.depth - INPUT_PROP(_NearFadeDistance)) / INPUT_PROP(_NearFadeRange);
        baseMap.a *= saturate(nearAttenuation);    
    }
    if(c.softParticles)
    {
        float depthDelta = c.fragment.bufferDepth - c.fragment.depth;
        float nearAttenuation = (depthDelta - INPUT_PROP(_SoftParticlesDistance))/INPUT_PROP(_SoftParticlesRange);
        baseMap.a *= saturate(nearAttenuation); //教程这里用的是 “ = ” ，是错的
    } 
    float4 baseColor = INPUT_PROP(_BaseColor);
    return baseMap * baseColor * c.color;
}

float GetCutoff(InputConfig c)
{
    return INPUT_PROP(_Cutoff);
}

float GetMetallic(InputConfig c)
{
    return INPUT_PROP(_Metallic);
}

float GetSmoothness(InputConfig c)
{
    return INPUT_PROP(_Smoothness);
}

float3 GetEmission(InputConfig c)
{
    return GetBase(c).rgb;
}

float GetFinalAlpha(float alpha)
{
    return INPUT_PROP(ZWrite) ? 1.0 : alpha;
}

#endif
