using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadows";
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    private static int cascadeCullingSphereId = Shader.PropertyToID("_CascadeCullingSpheres");
    //private static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
    private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    
    //存储阴影转换矩阵
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
        
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

    //最大级联数量
    private const int maxCascades = 4;

    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

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
    
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //存储可见光源的索引，前提是光源开启了阴影投射并且阴影强度大于0
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
            //还需加上一个判断，是否在阴影最大投射距离之内，有被该光源影响且需要投射阴影的物体存在，如果没有就不需要渲染光源的阴影贴图了
            && light.shadows!= LightShadows.None 
            && light.shadowStrength > 0f 
            && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))  //True if the light affects at least one shadow casting object in the Scene. 
        {
            shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight {visibleLightIndex = visibleLightIndex,};   
            //返回阴影强度和图块偏移
            return new Vector2(light.shadowStrength, settings.directional.cascadeCount * shadowedDirectionalLightCount++);
        }
        return Vector2.zero;
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
        //要分割的图块数量和大小
        int tiles = shadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i,split,tileSize);
        }
        //将级联数量和包围球数据发送到GPU
        buffer.SetGlobalInt(cascadeCountId,settings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSphereId,cascadeCullingSpheres);
        
        //阴影转换矩阵传入GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
        //buffer.SetGlobalFloat(shadowDistanceId,settings.maxDistance);
        buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(1f/settings.maxDistance,1f/settings.distanceFade));
        buffer.EndSample(bufferName);
        
        ExecuteBuffer();
    }
   
    //渲染单个光源阴影
    private void RenderDirectionalShadows(int index,int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //得到级联阴影贴图需要的参数
        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
            //计算视图和投影矩阵和裁剪空间的立方体
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i,
                cascadeCount, ratios, tileSize, 0f,
                out var viewMatrix, out var projectionMatrix, out var splitData);
            //得到第一个光源包围球数据
            if (index == 0)
            {
                Vector4 cullingSphere = splitData.cullingSphere;
                //得到半径的平方
                cullingSphere.w *= cullingSphere.w;
                cascadeCullingSpheres[i] = cullingSphere;
            }
            
            shadowSettings.splitData = splitData;
            //调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
        }
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
        //如果使用了反向ZBuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //设置矩阵
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

}
