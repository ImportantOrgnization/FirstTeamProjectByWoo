#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#include "../ShaderLibrary/Common.hlsl"

bool _ShadowPancaking;

//传入顶点着色器的数据结构
struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//传入片元着色器的数据结构
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float2 detailUV : VAR_DETAIL_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//顶点函数
Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    //输出位置和索引，并复制索引
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);          //世界坐标
    
    if(_ShadowPancaking)
    {
         
#if UNITY_REVERSED_Z
    output.positionCS.z = min(output.positionCS.z , output.positionCS.w * UNITY_NEAR_CLIP_VALUE);        
#else
    output.positionCS.z = max(output.positionCS.z , output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
    
    }
    
    output.baseUV = TransformBaseUV(input.baseUV);
    output.detailUV = TransformDetailUV(input.baseUV);
    return output;
}

//片元函数    
void ShadowCasterPassFragment(Varyings input)
{
    
    UNITY_SETUP_INSTANCE_ID(input)
    //ClipLOD(input.positionCS.xy,unity_LODFade.x);
    InputConfig config = GetInputConfig(input.positionCS,input.baseUV,input.detailUV);
    ClipLOD(config.fragment,unity_LODFade.x);
    float4 base = GetBase(config);
#if defined (_SHADOWS_CLIP)
    //透明度低于阈值的片元进行舍弃
    clip(base.a - GetCutoff(input.baseUV));
#elif defined(_SHADOWS_DITHER)
    float dither = InterleavedGradientNoise(input.positionCS.xy,0);
    clip(base.a - dither);
#endif
} 

#endif
