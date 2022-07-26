using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 自定义渲染管线实例
/// </summary>
public partial class CustomRenderPipeline : RenderPipeline
{
    //CameraRenderer renderer = new CameraRenderer();
    private CameraRenderer renderer;
    bool useDynamicBatching, useGPUInstancing;
    bool useLightsPerObject;
    private ShadowSettings shadowSettings;
    private PostFXSettings postFxSettings;
    private bool allowHDR;
    private int colorLUTResolution;
    public CustomRenderPipeline(bool allowHDR, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher , bool useLightsPerObject , 
        ShadowSettings shadowSettings,PostFXSettings postFxSettings,int colorLutResolution,Shader cameraRendererShader)
    {
        this.shadowSettings = shadowSettings;
        this.postFxSettings = postFxSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        this.allowHDR = allowHDR;
        this.colorLUTResolution = colorLutResolution;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        //灯光使用线性强度
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
        renderer = new CameraRenderer(cameraRendererShader);
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //遍历所有相机单独渲染
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera,allowHDR, useDynamicBatching, useGPUInstancing,useLightsPerObject,shadowSettings,postFxSettings,colorLUTResolution);
        }
    }
    
    //清理和重置委托
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        renderer.Dispose();
    }
}
