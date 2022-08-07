using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 自定义渲染管线资产
/// </summary>
//该标签会在你在Project下右键->Asset/Create菜单中添加一个新的子菜单
[CreateAssetMenu(menuName ="Rendering/CreateCustomRenderPipeline")]
public partial class CustomRenderPipelineAsset : RenderPipelineAsset
{
    public enum ColorLUTResolution
    {
        _16 = 16,_32 = 32 , _64= 64,
    }
    
    //设置批处理启用状态
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    [SerializeField]
    //是否使用逐对象光照
    bool useLightsPerObject = true;
    //阴影设置
    [SerializeField] 
    private ShadowSettings shadows = default;

    //后效资产配置
    [SerializeField] 
    PostFXSettings postFxSettings = default;

    [SerializeField]
    CameraBufferSettings cameraBuffer = new CameraBufferSettings
    {
        //如果没有HDR，unity就会把缓冲设置为sRGB格式，这种格式的缓冲就像一个普通纹理一样，在写入缓冲前需要进行伽马矫正，在读取缓冲时需要进行一次解码操作
        allowHDR = true,  
        renderScale = 1f,
        fxaa = new CameraBufferSettings.FXAA {
            fixedThreshold = 0.0833f,
            relativeThreshold = 0.166f,
        }
    };
    
    
    //LUT分辨率
    [SerializeField] private ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
    
    [SerializeField] private Shader cameraRendererShader = default;

    //重写抽象方法，需要返回一个RenderPipeline实例对象
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer, useDynamicBatching, useGPUInstancing, useSRPBatcher,useLightsPerObject,
            shadows,postFxSettings,(int)colorLUTResolution,cameraRendererShader);
    }
}
