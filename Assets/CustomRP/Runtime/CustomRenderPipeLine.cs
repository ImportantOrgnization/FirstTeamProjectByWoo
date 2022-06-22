using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class CustomRenderPipeLine : RenderPipeline {
    public CameraRenderer camRender = new CameraRenderer();

    private bool useDynamicBatching;

    private bool useGPUInstancing;

    private ShadowSettings shadowSettings;
    //启用SRP合批
    public CustomRenderPipeLine(bool useDynamicBatching,bool useGPUInstancing, bool useSRPBatcher,ShadowSettings shadowSettings)
    {
        this.shadowSettings = shadowSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        GraphicsSettings.lightsUseLinearIntensity = true;    //灯光使用线性强度
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        foreach (var cam in cameras) {
            camRender.Render(context, cam,useDynamicBatching,useGPUInstancing,shadowSettings);
        }
    }
}
