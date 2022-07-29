using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 相机渲染管理类：单独控制每个相机的渲染
/// </summary>
public partial class CameraRenderer
{

    ScriptableRenderContext context;

    Camera camera;

    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    //存储相机剔除后的结果
    CullingResults cullingResults;
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    //光照实例
    Lighting lighting = new Lighting();
    
    PostFXStack postFxStack = new PostFXStack();
    private bool useHDR;
    
    static CameraSettings defaultCameraSettings = new CameraSettings();

    private static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    private bool useDepthTexture;
    //是否使用中间帧缓冲
    private bool useIntermediateBuffer;

    private Material material;

    private Texture2D missingTexture;
    
    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1,1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing",
        };
        missingTexture.SetPixel(0,0,Color.white * 0.5f);
        missingTexture.Apply(true,true);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }

    private static int sourceTextureId = Shader.PropertyToID("_SourceTexture");

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to)
    {
        buffer.SetGlobalTexture(sourceTextureId , from);
        buffer.SetRenderTarget(to,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material,0,MeshTopology.Triangles,3);
    }
    
    /// <summary>
    /// 相机渲染
    /// </summary>
    public void Render(ScriptableRenderContext context, Camera camera,CameraBufferSettings bufferSettings,
        bool useDynamicBatching, bool useGPUInstancing,bool useLightsPerObject,
        ShadowSettings shadowSettings,PostFXSettings postFxSettings,int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;
        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;

        //useDepthTexture = true;
        if (camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = bufferSettings.copyDepthReflection;
        }
        else
        {
            useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
        }
        
        //如果需要覆盖后处理配置，将渲染管线的后处理配置替换成该相机的后处理配置
        if (cameraSettings.overridePostFX)
        {
            postFxSettings = cameraSettings.postFxSettings;
        }
        
        //设置buffer缓冲区的名字
        PrepareBuffer();
        // 在Game视图绘制的几何体也绘制到Scene视图中
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        useHDR = bufferSettings.allowHDR && camera.allowHDR;
        
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults,shadowSettings,useLightsPerObject,cameraSettings.maskLights?cameraSettings.renderingLayerMask : -1 );
        postFxStack.Setup(context,camera,postFxSettings,useHDR,colorLUTResolution,cameraSettings.finalBlendMode);
        buffer.EndSample(SampleName);
        
        Setup();

        //绘制几何体
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing,useLightsPerObject,cameraSettings.renderingLayerMask);
        //绘制SRP不支持的内置shader类型
        DrawUnsupportedShaders();

        DrawGizmosBeforeFX();
        if (postFxStack.IsActive)
        {
            postFxStack.Render(colorAttachmentId);
        }else if (useIntermediateBuffer)
        {
            Draw(colorAttachmentId,BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }
        DrawGizmosAfterFX();
        
        Clearup();
        
        //提交命令缓冲区
        Submit(); 
    }

    /// <summary>
    /// 绘制几何体
    /// </summary>
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing,bool useLightsPerObject,int renderingLayerMask)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        //设置绘制顺序和指定渲染相机
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的shader pass和渲染排序
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //设置渲染时批处理的使用状态
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe 
                            | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes | lightsPerObjectFlags
        };
        //渲染CustomLit表示的pass块
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        ////只绘制RenderQueue为opaque不透明的物体
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque,renderingLayerMask:(uint) renderingLayerMask);
      
        //1.绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        //2.绘制天空盒
        context.DrawSkybox(camera);
        
        CopyAttachment();

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        //只绘制RenderQueue为transparent透明的物体
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.绘制透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

    }
    /// <summary>
    /// 提交命令缓冲区
    /// </summary>
    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    //private static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    private static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    private static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
    /// <summary>
    /// 设置相机的属性和矩阵
    /// </summary>
    void Setup()
    {
        context.SetupCameraProperties(camera);
        //得到相机的clear flags
        CameraClearFlags flags = camera.clearFlags;

        useIntermediateBuffer = useDepthTexture || postFxStack.IsActive;
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;    //unity 会确保每帧开始时清理帧缓冲区，但是如果是自定义的纹理，结果就不一定，所以当启用特效时，应当最终清除颜色和深度缓冲
            }

            buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            buffer.GetTemporaryRT(depthAttachmentId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(colorAttachmentId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store,
                depthAttachmentId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        }
        
        //设置相机清除状态
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, 
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);  
        //buffer.SetGlobalVector("_MyCamPos",this.camera.transform.position);
        buffer.SetGlobalTexture(depthTextureId,missingTexture);
        ExecuteBuffer();
      
    }
    /// <summary>
    /// 执行缓冲区命令
    /// </summary>
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    /// <summary>
    /// 剔除
    /// </summary>
    /// <returns></returns>
    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;

        if (camera.TryGetCullingParameters(out p))
        {
            //得到最大阴影距离，和相机远截面作比较，取最小的那个作为阴影距离
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    void CopyAttachment()
    {
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Point,RenderTextureFormat.Depth);
            buffer.CopyTexture(depthAttachmentId,depthTextureId);
            ExecuteBuffer();
        }
    }
    
    void Clearup()
    {
        lighting.Cleanup();
        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
        }
    }
}
