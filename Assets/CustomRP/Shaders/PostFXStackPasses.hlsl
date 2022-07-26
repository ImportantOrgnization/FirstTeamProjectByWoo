#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

bool _BloomBicubicUpsampling;   //将双三次滤波上采样选项作为可选项
float _BloomIntensity;
TEXTURE2D(_PostFXSource);

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

//三个顶点分别为(-1,-1) (-1,3) (3,-1) , uv分别为(0,0),(0,2), (2,0)
Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
    Varyings output;
    output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0 , vertexID == 1 ? 3.0 : -1.0 , 0.0, 1.0);
    output.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0 , vertexID == 1 ? 2.0 : 0.0);
    if(_ProjectionParams.x < 0.0)
    {
        output.screenUV.y = 1.0 - output.screenUV.y;
    }
    return output;
}

float4 GetSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource,sampler_linear_clamp,screenUV,0);
}

TEXTURE2D(_PostFXSource2);
float4 GetSource2(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2,sampler_linear_clamp,screenUV,0);
}

float4 _PostFXSource_TexelSize; // TexName_TexelSize => (1/width,1/height,width,height) 这种申明方式会自动计算这个向量

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 GetSourceBiCubic(float2 screenUV)
{
    return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostFXSource,sampler_linear_clamp),screenUV,_PostFXSource_TexelSize.zwxy,1.0,0.0);
}

float4 _BloomThreshold;  //( t , -t + tk ,2tk , 1 / (4tk + 0.00001))

float3 ApplyBloomThreshold(float3 color)
{
    float brightness = Max3(color.r, color.g,color.b);
    float soft = brightness + _BloomThreshold.y;        //s = min(max(0,b - t + tk),2tk)^2 / (4tk + 0.00001)
    soft = clamp(soft,0.0,_BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    
    float contribution = max(soft,brightness - _BloomThreshold.x);  //weight = max (s , b - t ) / max(b , 0.00001)
    contribution /= max(brightness ,0.00001);
    return color * contribution;
}

//Copy Pass
float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.screenUV);
}

//Bloom Horizontal Pass
float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {-4.0 , -3.0,-2.0 ,-1.0,0.0,1.0,2.0,3.0,4.0};
    float weights[] =   //权重来自于杨辉三角第13行中间9个数字
    {
        0.01621622,0.05405405,0.12162162,0.19459459,0.22702703,
        0.19459459,0.12162162,0.05405405,0.01621622,
    };
    for(int i = 0 ; i < 9 ; i ++ )
    {
        //下面公式中 2.0 的由来：下采样
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x ;      // 这就是双线性滤波模式 ，offset 是 1，2，3... ,然后乘以纹素 ，这样子，九个样本的平局值为 2* 2 源像素
        color += GetSource(input.screenUV + float2(offset,0.0)).rgb * weights[i];
    }
    return float4(color,1.0);
}

//Bloom Vertical Pass
float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {-3.23076923 , -1.38461538,0.0 ,1.38461538,3.23076923};
    float weights[] =   //权重来自于杨辉三角第13行中间9个数字
    {
        0.07027027,0.31621622,0.22702703,0.31621622,0.07027027,
    };
    for(int i = 0 ; i < 5 ; i ++ )
    {
        float offset = offsets[i] * GetSourceTexelSize().y ;
        color += GetSource(input.screenUV + float2(0.0,offset)).rgb * weights[i];
    }
    return float4(color,1.0);
}

//Bloom Add Pass
float4 BloomAddPassFragment(Varyings input) : SV_TARGET
{
    float3 lowRes;
    if(_BloomBicubicUpsampling)
    {
        lowRes = GetSourceBiCubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    
    float4 highRes = GetSource2(input.screenUV);
    return float4(lowRes * _BloomIntensity + highRes , highRes.a);
}

float4 BloomScatterPassFragment(Varyings input) : SV_TARGET
{
    float3 lowRes;
    if(_BloomBicubicUpsampling)
    {
        lowRes = GetSourceBiCubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    
    float4 highRes = GetSource2(input.screenUV);
    lowRes += highRes.rgb - ApplyBloomThreshold(highRes.rgb);
    return float4(lerp(highRes.rgb, lowRes , _BloomIntensity) , highRes.a);
}

//Bloom Prefilter Pass  提取高亮
float4 BloomPrefilterPassFragment(Varyings input) :SV_TARGET
{
    float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
    return float4(color ,1.0);
}

float4 BloomPrefilterFireFliesPassFragment(Varyings input) :SV_TARGET
{
    float3 color = 0.0;
    float weightSum = 0.0;
    float2 offsets[] = 
    {
        float2(0.0,0.0),
        float2(-1.0,-1.0),float2(-1.0,1.0),float2(1.0,-1.0),float2(1.0,1.0),
    };
    for(int i = 0 ; i < 5 ; i ++ ) 
    {
        float3 c = GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
        c = ApplyBloomThreshold(c);
        float w = 1.0 / (Luminance(c) + 1.0);   
        color += c * w;
        weightSum += w;
    }
    
    color /= weightSum;     //完全根治萤火虫现象，原理 Sum( color * weight) / weightSum , 其中 weight = 1 / ( Luminance(color) + 1 ) 
    return float4 (color ,1.0);
}

float4 _ColorAdjustments;
float4 _ColorFilter;

float3 ColorGradePostExposure (float3 color)
{
    return color * _ColorAdjustments.x;
}

float Luminance(float3 color , bool useACES)
{
    return useACES ? AcesLuminance(color) : Luminance(color);
}

float3 ColorGradingContrast(float3 color,bool useACES)
{
    color = useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
    //color = LinearToLogC(color);    //从线性空间转换到LogC空间
    color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY; // ACEScc_MIDGRAY = 0.4135884
    return useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
    //return LogCToLinear(color);
}

float3 ColorGradingFilter(float3 color)
{
    return color * _ColorFilter.rgb;
}

float3 ColorGradingHueShift(float3 color)
{
    color = RgbToHsv(color);
    float hue = color.x + _ColorAdjustments.z;
    color.x = RotateHue(hue,0.0,1.0);
    return HsvToRgb(color);
}

float3 ColorGradingSaturation(float3 color,bool useACES)
{   
    float luminace = Luminance(color,useACES);
    return (color - luminace)* _ColorAdjustments.w + luminace; 
}

float4 _WhiteBalance;
float3 ColorGradeWhiteBalance(float3 color)
{
    color = LinearToLMS(color);
    color *= _WhiteBalance.rgb;
    return LMSToLinear(color);
}

float4 _SplitToningShadows;
float4 _SplitToningHighlights;
//色调分离
float3 ColorGradeSplitToning(float3 color,bool useACES)
{
    color = PositivePow(color,1.0/2.2);
    float t = saturate(Luminance(saturate(color),useACES) + _SplitToningShadows.w);
    float3 shadows = lerp(0.5,_SplitToningShadows.rgb , 1.0 - t);
    float3 highlights = lerp(0.5,_SplitToningHighlights.rgb,t);
    color = SoftLight(color, shadows);
	color = SoftLight(color, highlights);
    return PositivePow(color,2.2);
}

float4 _ChannelMixerRed;
float4 _ChannelMixerGreen;
float4 _ChannelMixerBlue;
float3 ColorGradingChannelMixer(float3 color)
{
    return mul(float3x3(_ChannelMixerRed.rgb , _ChannelMixerGreen.rgb , _ChannelMixerBlue.rgb) , color);
}

float4 _SMHShadows;
float4 _SMHMidtones;
float4 _SMHHighlights;
float4 _SMHRange;
float3 ColorGradingShadowsMidtonesHighlights(float3 color,bool useACES)
{
    float luminace = Luminance(color,useACES);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x , _SMHRange.y , luminace);
    float highlightsWeight = smoothstep(_SMHRange.z,_SMHRange.w ,luminace);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
    return color * _SMHShadows.rgb * shadowsWeight + 
            color * _SMHMidtones.rgb * midtonesWeight + 
            color * _SMHHighlights.rgb * highlightsWeight;
}

//颜色分级
float3 ColorGrade(float3 color,bool useACES = false)
{
    //color = min(color,60.0);
    color = ColorGradePostExposure(color); 
    color = ColorGradeWhiteBalance(color); 
    color = ColorGradingContrast(color,useACES);
    color = ColorGradingFilter(color);
    //消除对比度调整带来的负值o
    color = max(color,0.0);
    color = ColorGradeSplitToning(color,useACES);
    color = ColorGradingChannelMixer(color);
    color = max(color,0.0);
    color = ColorGradingShadowsMidtonesHighlights(color,useACES);
    color = ColorGradingHueShift(color); //必须在消除负值后进行色调调整
    color = ColorGradingSaturation(color,useACES);
    color = max(useACES ? ACEScg_to_ACES(color) : color , 0.0);
    return color;
}

float4 _ColorGradingLUTParameters;
float _ColorGradingLUTInLogC;
float3 GetColorGradeLUT(float2 uv , bool useACES = false)
{
    float3 color = GetLutStripValue(uv , _ColorGradingLUTParameters);
    return ColorGrade(_ColorGradingLUTInLogC ?  LogCToLinear(color) : color ,useACES);
}

//不进行色调映射
float4 ToneMappingNonePassFragment(Varyings input) : SV_TARGET
{
    float3 color = GetColorGradeLUT(input.screenUV);
    return float4(color,1.0);
}

//Reinhard 色调映射
float4 ToneMappingReinhardPassFragment(Varyings input) : SV_TARGET{
	float3 color = GetColorGradeLUT(input.screenUV);
	color /= color + 1.0;
	return float4(color,1.0);
}

//Neutral 色调映射
float4 ToneMappingNeutralPassFragment(Varyings input) : SV_TARGET{
	float3 color = GetColorGradeLUT(input.screenUV);
	color = NeutralTonemap(color);
    return float4(color,1.0);
}

//ACES 色调映射
float4 ToneMappingACESPassFragment(Varyings input) : SV_TARGET{
	float3 color = GetColorGradeLUT(input.screenUV);
	color = AcesTonemap(unity_to_ACES(color));
	return float4( color,1.0);
}

TEXTURE2D(_ColorGradingLUT);
//SAMPLER(sampler_point_clamp);

float3 ApplyColorGradingLUT(float3 color)
{
//    return ApplyLut2D(TEXTURE2D_ARGS(_ColorGradingLUT, sampler_point_clamp),  //通过point采样可以更明显地观察到LUT的颜色条带
    return ApplyLut2D(TEXTURE2D_ARGS(_ColorGradingLUT, sampler_linear_clamp),
        saturate(_ColorGradingLUTInLogC ? LinearToLogC(color) : color),
        _ColorGradingLUTParameters.xyz);
}

float4 FinalPassFragment(Varyings input) : SV_TARGET
{
    float4 color = GetSource(input.screenUV);
    color.rgb = ApplyColorGradingLUT(color.rgb);
    return color;
}

#endif
