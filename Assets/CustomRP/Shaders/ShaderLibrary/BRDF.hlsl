#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

//给非金属定制最低反射率
#define MIN_REFLECTIVITY 0.04   

struct BRDF{
    float3 diffuse;
    float3 specular;
    float roughness;
};

float OneMinusReflectivity(float metallic){
    float range = 1.0 - MIN_REFLECTIVITY;
    return range * ( 1 - metallic);
}

BRDF GetBRDF(Surface surface,bool alphaToDiffuse = false){
    BRDF brdf;

    ////////////////////////////////////////////////////////////////////////////
    //物体的metallic越大，其自身的反照率（Albedo）就越不明显，对周围环境景象的反射就越清晰
    ////////////////////////////////////////////////////////////////////////////
    
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;    //物体自身的反照率Albedo
    if(alphaToDiffuse) {
        brdf.diffuse *= surface.alpha;                          //Premultipled Alpha 预乘alpha
    }      
    brdf.specular = lerp(MIN_REFLECTIVITY,surface.color,surface.metallic);
    
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness); 
    return brdf;
}

float SpecularStrength(Surface surface, BRDF brdf , Light light) {
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal,h)));
    float lh2 = Square(saturate(dot(light.direction,h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1,lh2) * normalization);
}

float3 DirectBRDF(Surface surface , BRDF brdf , Light light) 
{
    return SpecularStrength(surface,brdf , light) * brdf.specular + brdf.diffuse;
}



#endif