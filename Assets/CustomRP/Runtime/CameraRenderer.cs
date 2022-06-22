using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public partial class CameraRenderer {

    ScriptableRenderContext context;
    Camera camera;

    //CommendBuffer 是一个容器，它保存了一些需要执行的渲染命令
    const string bufferName = "My Render Camera";
    CommandBuffer buffer = new CommandBuffer() { name = bufferName };

    //CullingResults 是相机剔除后的数据
    CullingResults cullingResults;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    
    //Lighting
    Lighting lighting = new Lighting();
    
    public void Render(ScriptableRenderContext context , Camera camera,
        bool useDynamicBatching , bool useGPUInstancing , 
        ShadowSettings shadowSettings) {
        
        this.context = context;
        this.camera = camera;

        //命名缓冲区
        PrepareBuffer();
        
        //将UI几何物体发送到场景相机中，需要在剔除之前做
        PrepareForSceneWindow();
        
        //剔除
        if (!Cull(shadowSettings.maxDistance)) {
            return;
        }
        
        //设置灯光以及渲染阴影
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.SetUp(context,cullingResults,shadowSettings);
        buffer.EndSample(SampleName);
        
        SetUp();
        
        //绘制SRP支持的Shader类型
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);
        
        //绘制SRP不支持的Shader类型
        DrawUnsupportedShaders();
        //绘制Gizmos
        DrawGizmos();
        
        lighting.Cleanup();

        Submit();

    }


    void SetUp() {
        
        context.SetupCameraProperties(camera);
        
        CameraClearFlags clearFlags = camera.clearFlags;
        bool clearDepth = clearFlags <= CameraClearFlags.Depth;
        bool clearColor = clearFlags == CameraClearFlags.Color;
        buffer.ClearRenderTarget(clearDepth, clearColor, Color.clear);  //ClearRenderTarget(...)会自动包裹在一个使用命令缓冲去的样本条目中，所以我们将它移出BeginSample-EndSample
        
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching,bool useGPUInstancing) {
        
        //绘制不透明物体
        var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };  //设置指定的相机和绘制顺序
        var drawingSetting = new DrawingSettings(unlitShaderTagId, sortingSettings)                        //设置ShaderPass,排序模式
        {
            enableDynamicBatching = useDynamicBatching , enableInstancing = useGPUInstancing                //开启对应合批
        }; 
        drawingSetting.SetShaderPassName(1,litShaderTagId);                                          //渲染index = 1 的ShaderpassName，设置CustomLit表示的Pass块
        var filteringSetting = new FilteringSettings(RenderQueueRange.opaque);                             //设置哪些渲染队列可以被排序
        context.DrawRenderers(cullingResults, ref drawingSetting, ref filteringSetting);       

        //绘制天空盒
        context.DrawSkybox(camera); 

        //绘制透明物体
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSetting.sortingSettings = sortingSettings;
        filteringSetting.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSetting, ref filteringSetting);
    }

    void Submit() { 
        buffer.EndSample(SampleName); 
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer() {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance) {
        if (camera.TryGetCullingParameters( out ScriptableCullingParameters p)) {   //camera.TryGetCullingParameters 获取所有需要进行剔除检查的物体
            p.shadowDistance = Math.Min(maxShadowDistance, camera.farClipPlane);    //和远裁剪面进行比较而获得阴影的最远距离
            cullingResults = context.Cull(ref p);       //context.Cull 正式进行剔除操作
            return true;
        }
        return false;
    }

  

}
