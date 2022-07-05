using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadows";
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    //存储阴影转换矩阵
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];
        
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName,
    };
    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings settings;
    
    //可投射阴影的定向光数量
    private const int maxShadowedDirectionalLightCount = 4;
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }
    //存储可投射阴影的可见光源的索引
    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];    
    //已存储的可投射阴影的平行光的数量
    private int shadowedDirectionalLightCount;    

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        this.shadowedDirectionalLightCount = 0;

    }
    public void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //存储可见光源的索引，前提是光源开启了阴影投射并且阴影强度大于0
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
            //还需加上一个判断，是否在阴影最大投射距离之内，有被该光源影响且需要投射阴影的物体存在，如果没有就不需要渲染光源的阴影贴图了
            && light.shadows!= LightShadows.None 
            && light.shadowStrength > 0f 
            && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))  //True if the light affects at least one shadow casting object in the Scene. 
        {
            shadowedDirectionalLights[shadowedDirectionalLightCount++] = new ShadowedDirectionalLight {visibleLightIndex = visibleLightIndex,};    //TODO ???
        }
    }
    
    //阴影渲染
    public void Render()
    {
        if (shadowedDirectionalLightCount > 0 )
        {
            RenderDirectionalShadows();    
        }
    }
    //渲染定向光阴影至阴影贴图
    private void RenderDirectionalShadows()
    {
        // 创建renderTexture，并指定该类型是阴影贴图
        int atlasSize = (int) settings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        //指定渲染数据存储到RT中而不是帧缓冲中
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store); 
        //清除深度缓冲区
        buffer.ClearRenderTarget(true,false,Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        int split = shadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i,split,tileSize);
        }
        buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
        buffer.EndSample(bufferName);
        
        ExecuteBuffer();
    }
   
    //渲染单个光源阴影
    private void RenderDirectionalShadows(int index,int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 
            0, 1, Vector3.zero, tileSize, 0f,
            out var viewMatrix, out var projectionMatrix, out var splitData);
        
        shadowSettings.splitData = splitData;
        dirShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(index, split, tileSize), split);
        buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
        
    }
    
    public void Clearup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }
    
    //设置渲染视口来渲染单个图块
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        //计算索引图块的偏移位置
        Vector2 offset = new Vector2(index % split,index / split);
        //设置渲染视口，拆分成多个图块
        buffer.SetViewport(new Rect(offset.x * tileSize,offset.y * tileSize , tileSize,tileSize));
        return offset;
    }

    //返回一个从世界空间到阴影图块空间的转换矩阵
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        
        return m;
    }

}
