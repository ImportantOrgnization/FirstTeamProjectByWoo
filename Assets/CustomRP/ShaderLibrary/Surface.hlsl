#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface {
    float3 position;
	float3 normal;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	float3 viewDirection;
	//表面深度
	float depth;
	float dither;
	float fresnelStrength;
	//顶点着色器传过来的插值法线，未正则化的
	float3 interpolatedNormal;
	uint renderingLayerMask;
	//遮挡数据
	float occlusion;
};

#endif
