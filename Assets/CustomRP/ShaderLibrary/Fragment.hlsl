#ifndef FRAGMENT_INCLUDED
#define FRAGMENT_INCLUDED

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraColorTexture);
float4 _CameraBufferSize;

struct Fragment
{
    float2 positionSS;
    float depth;        //当前片元的实际深度
    float2 screenUV;
    //深度缓冲
    float bufferDepth;  //存在buffer中老的深度
};

float4 GetBufferColor(Fragment fragment, float2 uvOffset = float2(0.0,0.0))
{
    float2 uv = fragment.screenUV + uvOffset;
    return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture,sampler_CameraColorTexture,uv,0);
}

Fragment GetFragment(float4 positionSS)
{
    Fragment f;
    f.positionSS = positionSS.xy;
    f.screenUV = f.positionSS * _CameraBufferSize.xy;   //  /_ScreenParams.xy
    f.depth = IsOrthographicCamera()? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w; //positionSS.z is rawDepth, 新frag的深度
    f.bufferDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture,sampler_point_clamp,f.screenUV,0);
    f.bufferDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(f.bufferDepth) : LinearEyeDepth(f.bufferDepth,_ZBufferParams); //老frag的深度
    return f;
}

#endif
