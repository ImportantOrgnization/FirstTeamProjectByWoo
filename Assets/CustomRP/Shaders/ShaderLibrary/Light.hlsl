#ifndef CUSTOM_LIHGT_INCLUDED
#define CUSTOM_LIHGT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomLight)
    //float3 _DirectionalLightColor;
    //float3 _DirectionalLightDirection;
    
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light{
    float3 color;
    float3 direction; // 光的来源方向
    float attenuation;
};

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData){
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    return data;
}

Light GetDirectionalLight(int index , Surface surfaceWS,ShadowData shadowData)
{
    Light light ;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    //得到阴影数据
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadowData);
    //得到阴影衰减
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData , surfaceWS);
    return light;
}


#endif