using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadows";
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    private const int maxCascades = 4;
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName,
    };
    
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];    //世界空间->阴影空间的矩阵集合
    private const int maxShadowedDirectionalLightCount = 4;
    private int shadowedDirectionalLightCount;    //已存储的可投射阴影的平行光的数量
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }
    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];    //储存所有产生阴影的可见光的索引

    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings settings;

    private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = shadowSettings;
        this.shadowedDirectionalLightCount = 0;
    }

    //储存可见光的阴影数据，目的是在阴影图集中为该光源的阴影贴图保留空间，并存储渲染他们的所需要的信息。
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //存储可见光源的索引，前提是光源开启了阴影投射并且阴影强度大于0
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
            && light.shadows!= LightShadows.None 
            && light.shadowStrength > 0f 
            && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))  //True if the light affects at least one shadow casting object in the Scene. 
        {
            shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight {visibleLightIndex = visibleLightIndex,};    //TODO
            return new Vector2(light.shadowStrength, settings.directional.cascadeCount * shadowedDirectionalLightCount++);    //阴影强度和图块偏移
        }
        return Vector2.zero;
    }

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
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);  //指定渲染数据存储到RT中
        buffer.ClearRenderTarget(true,false,Color.clear);  //清除深度缓冲区
        
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        int tiles = shadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderSingleDirectionalShadows(i,split, tileSize);    
        }
        
        buffer.SetGlobalInt(cascadeCountId,settings.directional.cascadeCount);    //级联数量
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId,cascadeCullingSpheres);    //级联包围球数据

        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);    //阴影转换矩阵
        buffer.EndSample(bufferName);
        
        ExecuteBuffer();
    }

    //渲染单个光源阴影
    void RenderSingleDirectionalShadows(int index, int split, int tileSize)
    {
        /*
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        var shadowDrawingSettings = new ShadowDrawingSettings(cullingResults,light.visibleLightIndex);
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1, Vector3.zero,
            tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData);
        shadowDrawingSettings.splitData = splitData;
        var offset = SetTileViewPort(index, split,tileSize);
        dirShadowMatrices[index] = ConvertToAtlasMatrix(projMatrix * viewMatrix,offset,split) ;    //世界空间->灯光空间的矩阵
        buffer.SetViewProjectionMatrices(viewMatrix,projMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowDrawingSettings);
        */
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        var shadowDrawingSettings = new ShadowDrawingSettings(cullingResults,light.visibleLightIndex); 
        
        //得到级联阴影贴图需要的参数
        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.cascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i,
                cascadeCount, ratios, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData);
            if (index == 0)
            {
                Vector4 cullingSphere = splitData.cullingSphere;
                cullingSphere.w *= cullingSphere.w;
                cascadeCullingSpheres[i] = cullingSphere;
            }
            shadowDrawingSettings.splitData = splitData;
            int tileIndex = tileOffset + i;    //调整土块索引，它等于光源的土块偏移加上级联的索引
            var offset = SetTileViewPort(tileIndex, split,tileSize);
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projMatrix * viewMatrix,offset,split) ;    //世界空间->灯光空间的矩阵
            buffer.SetViewProjectionMatrices(viewMatrix,projMatrix);
            ExecuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
        }
    }

    Vector2 SetTileViewPort(int index, int split , int tileSize)
    {
        //计算索引图块的偏移位置
        Vector2 offset = new Vector2(index % split,index / split);
        //设置渲染视口，拆分成多个图块
        buffer.SetViewport(new Rect(offset.x * tileSize,offset.y * tileSize , tileSize,tileSize));
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        float scale = 1 / (float) split;
        //如果使用了反向的ZBuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //设置矩阵坐标
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.x * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.x * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.x * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.x * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);

        return m;
    }
     
    public void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    public void Clearup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }
   
}
