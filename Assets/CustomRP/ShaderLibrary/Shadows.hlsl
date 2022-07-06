//阴影采样
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWD_DIRECTIONAL_LIGHT_COUNT 4
//阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

//阴影数据
struct ShadowData
{
    int cascadeIndex; 
    //是否采样阴影标识
    float strength;
};

#define MAX_CASCADE_COUNT 4
CBUFFER_START(_CustomShadows)
//级联数量和包围球数据
int _CascadeCount;
float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
//阴影转换矩阵
float4x4 _DirectionalShadowMatrices[MAX_SHADOWD_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
float _ShadowDistance;
CBUFFER_END

//阴影的数据信息
struct DirectionalShadowData{
    float strength;
    int tileIndex;
};

//采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)  //阴影纹理空间中的表面位置
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,positionSTS);
}

//计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
    if(data.strength <= 0.0)
    {
        return 1.0;
    }
    //通过阴影转换矩阵和表面位置得到阴影纹理（图块）空间的位置，然后对图集进行采样
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex],float4(surfaceWS.position,1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    //最终阴影衰减值是阴影强度和衰减因子的插值
    return lerp(1.0,shadow,data.strength);
}

//得到世界空间的表面阴影数据
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.strength = surfaceWS.depth < _ShadowDistance ? 1.0f : 0.0;
    int i;
    //如果物体表面到球心的平方距离小于球体半径的平方，就说明该物体在这层级联包围球中，得到合适的级联层级索引
    for(i = 0; i<_CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position,sphere.xyz);
        if(distanceSqr < sphere.w)
        {
            break;
        }
    }
    if(i == _CascadeCount) 
    {
        data.strength = 0.0;
    }
    data.cascadeIndex = i;
    return data;
}

#endif