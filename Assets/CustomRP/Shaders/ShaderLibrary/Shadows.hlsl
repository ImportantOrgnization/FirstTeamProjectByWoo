#ifndef CUSTOM_SHADOW_INCLUDED
#define CUSTOM_SHADOW_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIHGT_COUNT 4

//声明阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas); 
//内联的采样器状态 ↓
#define SHADOW_SAMPLER sampler_linear_clamp_compare
//声明采样器     
SAMPLER_CMP(SHADOW_SAMPLER);

#define MAX_CASCADE_COUNT 4
CBUFFER_START(_CustomShadows)
//级联数量和包围球数据
int _CascadeCount;
float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
//阴影转换矩阵
float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIHGT_COUNT * MAX_CASCADE_COUNT];
CBUFFER_END

//阴影的数据信息
struct DirectionalShadowData{
    float strength;
    int tileIndex;
};

//采样阴影图集
float SampleDirctionalShadowAltas(float3 positionSTS){
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,positionSTS);
}

//计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData data , Surface surfaceWS){
    if(data.strength <= 0.0) {
        return 1.0;
    }
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex],float4 (surfaceWS.position,1.0)).xyz;
    
    float shadow = SampleDirctionalShadowAltas(positionSTS);
    
    return lerp(1.0 , shadow, data.strength);
}

//阴影数据
struct ShadowData{
    int cascadeIndex;
};
//得到世界空间的表面阴影数据
ShadowData GetShadowData(Surface surfaceWS){
    ShadowData data;
    int i;
    //如果物体表面到球心的距离平方小于球体半径的平方，就说明该物体再这层级联包围球中，得到合适的级联层级索引
    for(i = 0 ; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position,sphere.wyz);
        if(distanceSqr < sphere.w)
        {
            break;
        }
    }
    data.cascadeIndex = i;
    return data;
}




#endif