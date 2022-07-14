//灯光数据相关库
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	//定向光源颜色、方向、阴影等数据
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    //阴影强度和图块偏移
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
    
    //非定向光源的属性
    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];    //这是一个颜色乘以强度的值的队列
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
    
    float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
    
CBUFFER_END

//灯光的属性
struct Light {
	//颜色
	float3 color;
	//方向
	float3 direction;
	//灯光衰减
	float attenuation;
};
//获取方向光源的数量
int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}

//获取方向光阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex,ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}

//获取目标索引定向光的属性
Light GetDirectionalLight (int index,Surface surfaceWS,ShadowData shadowData) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	//得到阴影数据
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadowData);
	//得到阴影衰减
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData,shadowData,surfaceWS);
	//light.attenuation =shadowData.cascadeIndex /4.0;  //这句代码可以更加清晰地查看级联球的范围
	return light;
}

//获取非定向光源的数量
int GetOtherLightCount()
{
    return _OtherLightCount;
}

//获取其他类型光源的阴影数据
OtherShadowData GetOtherShadowData(int lightIndex)
{
    OtherShadowData data;
    data.strength = _OtherLightShadowData[lightIndex].x;
    data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
    return data;
}

//获取指定索引的非定向光源数据
Light GetOtherLight(int index , Surface surfaceWS,ShadowData shadowData)
{
    Light light;
    light.color = _OtherLightColors[index].rgb;
    float3 ray = _OtherLightPositions[index].xyz - surfaceWS.position;
    light.direction = normalize(ray);
    //光照强度随距离衰减
    float distanceSqr = max(dot(ray,ray),0.00001);
    //套用公式计算随光照范围衰减 ->  max(0, 1 - (d^2 / r^2)^2)
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));
    light.attenuation = rangeAttenuation / distanceSqr;
    
    //得到聚光灯衰减
    float4 spotAngles = _OtherLightSpotAngles[index];
    float spotAttenuation = Square(saturate(dot(_OtherLightDirections[index].xyz,light.direction) * spotAngles.x + spotAngles.y)); //saturate( (d - cos(r0/2)) / (cos(ri/2) - cos(ro / 2)) ) ^2
    light.attenuation = spotAttenuation * light.attenuation;
   
    OtherShadowData otherShadowData = GetOtherShadowData(index);
    light.attenuation = GetOtherShadowAttenuation(otherShadowData,shadowData,surfaceWS) * light.attenuation;
   
    return light;
}

#endif