#ifndef CUSTOM_FXAA_PASS_INCLUDED
#define CUSTOM_FXAA_PASS_INCLUDED

float GetLuma(float2 uv)
{
#if defined(FXAA_ALPHA_CONTAINS_LUMA)
    //return sqrt( Luminance(GetSource(uv)));
    return GetSource(uv).a; 
#else
    return GetSource(uv).g; //由于人类眼睛对绿色敏感，所以这是一种更加高效的获取 luma 的方法
#endif
}

float4 FXAAPassFragment (Varyings input) : SV_TARGET {
	return GetLuma(input.screenUV);
}

#endif
