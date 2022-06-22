#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
#include "Light.hlsl"
#include "BRDF.hlsl"
//兰伯特光照函数
float3 IncomingLight(Surface surface , Light light)
{
    return saturate(dot(surface.normal , light.direction) * light.attenuation) * light.color;
}

//单盏灯
//float3 GetLighting(Surface surface,Light light)
//{
 //   return IncomingLight(surface,light);
//}

//加入BRDF
float3 GetLighting(Surface surface,BRDF brdf, Light light){
    return IncomingLight(surface,light) * DirectBRDF(surface,brdf,light);
}

//多盏灯光累加，以及BRDF
float3 GetLighting(Surface surfaceWS , BRDF brdf){
    //得到表面阴影数据
    ShadowData shadowData = GetShadowData(surfaceWS);
    //可见光的光照结果进行累加得到的最终光照
    float3 color = 0.0;
    for(int i=0; i < GetDirectionalLightCount(); i++){
        Light light = GetDirectionalLight(i,surfaceWS,shadowData);
        color += GetLighting(surfaceWS,brdf,light);
    }
    return color;
}


#endif