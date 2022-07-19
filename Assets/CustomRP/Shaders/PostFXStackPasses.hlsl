#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED

TEXTURE2D(_PostFXSource);
SAMPLER(sampler_linear_clamp);
float4 _ProjectionParams;

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

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.screenUV);
}


float4 _PostFXSource_TexelSize; // TexName_TexelSize => (1/width,1/height,width,height) 这种申明方式会自动计算这个向量

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

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
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x ;      // 这就是双线性滤波模式 ，offset 是 1，2，3... ,然后乘以纹素 ，这样子，九个样本的平局值为 2* 2 源像素
        color += GetSource(input.screenUV + float2(offset,0.0)).rgb * weights[i];
    }
    return float4(color,1.0);
}

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
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().y ;
        color += GetSource(input.screenUV + float2(0.0,offset)).rgb * weights[i];
    }
    return float4(color,1.0);
}

float4 BloomCombinePassFragment(Varyings input) : SV_TARGET
{
    float3 lowRes = GetSource(input.screenUV).rgb;
    float3 highRes = GetSource2(input.screenUV).rgb;
    return float4(lowRes + highRes , 1.0);
}


#endif
