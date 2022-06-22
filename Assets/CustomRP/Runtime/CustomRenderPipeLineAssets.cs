using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeLine")]
public class CustomRenderPipeLineAssets : RenderPipelineAsset
{
    [SerializeField] 
    private ShadowSettings shadowSettings = default;
    
    [SerializeField]
    private bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
        
    protected override RenderPipeline CreatePipeline() {
        return new CustomRenderPipeLine(useDynamicBatching,useGPUInstancing,useSRPBatcher,shadowSettings);
    }
}
