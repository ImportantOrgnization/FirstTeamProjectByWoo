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
    private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    private static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
    
    private static string[] directionalFilterKeyWords = {"_DIRECTIONAL_PCF3", "_DIRECTIONAL_PCF5", "_DIRECTIONAL_PCF7"};
    private static string[] cascadeBlendKeywords = {"_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER"};
    private static string[] shadowMaskKeywords = {"_SHADOW_MASK_ALWAYS", "_SHADOW_MASK_DISTANCE"};
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
        //斜度比例偏差值
        public float slopScaleBias;
        //阴影视锥近裁剪平面偏移
        public float nearPlaneOffset;
    }
    //存储可投射阴影的可见光源的索引
    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];    
    //已存储的可投射阴影的平行光的数量
    private int shadowedDirectionalLightCount;    
    
    //可投射阴影的非定向光源的最大数量
    private const int maxShadowedOtherLightCount = 16;
    //已存在的可投射的非定向光的数量
    private int shadowedOtherLightCount;
    

    //最大级联数量
    private const int maxCascades = 4;

    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    
    static Vector4[] cascadeData = new Vector4[maxCascades];

    private bool useShadowMask;
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        this.shadowedDirectionalLightCount = 0;
        useShadowMask = false;
        shadowedOtherLightCount = 0;
    }
    public void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //存储可见光源的索引，前提是光源开启了阴影投射并且阴影强度大于0
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
            //还需加上一个判断，是否在阴影最大投射距离之内，有被该光源影响且需要投射阴影的物体存在，如果没有就不需要渲染光源的阴影贴图了
            && light.shadows!= LightShadows.None 
            && light.shadowStrength > 0f )
        {
            float maskChannel = -1;
            //如果使用了ShadowMask
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel; 
            }

            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)) //True if the light affects at least one shadow casting object in the Scene. 
            {
                return new Vector4(-light.shadowStrength,0f,0f,maskChannel);    //没有阴影投射，就用阴影遮罩，但是阴影强度大于零时，会采样阴影贴图，所以我们给个负值，让它去采样阴影遮罩
            }
            
            shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopScaleBias =  light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane,
            };   
            //返回阴影强度和图块偏移
            return new Vector4(light.shadowStrength, settings.directional.cascadeCount * shadowedDirectionalLightCount++,light.shadowNormalBias,maskChannel);
        }
        return new Vector4(0,0,0,-1f);
    }
    
    //阴影渲染
    public void Render()
    {
        if (shadowedDirectionalLightCount > 0 )
        {
            RenderDirectionalShadows();    
        }
        //是否使用阴影蒙版
        buffer.BeginSample(bufferName);
        SetKeywords(shadowMaskKeywords,useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
        
        //将级联计数发送到GPU
        buffer.SetGlobalInt(cascadeCountId,shadowedDirectionalLightCount > 0 ? settings.directional.cascadeCount : 0);
        //阴影距离过渡相关数据发送GPU
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(1f/settings.maxDistance,1f/settings.distanceFade,1f/(1f - f*f)));
        
        buffer.EndSample(bufferName);
        ExecuteBuffer();
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
        //将级联包围球数据发送到GPU
        buffer.SetGlobalVectorArray(cascadeCullingSphereId,cascadeCullingSpheres);
        //级联数据发送GPU
        buffer.SetGlobalVectorArray(cascadeDataId,cascadeData);
        //阴影转换矩阵传入GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
        SetKeywords(directionalFilterKeyWords,(int) settings.directional.filter -1);
        SetKeywords(cascadeBlendKeywords,(int) settings.directional.cascadeBlend -1);
        //传递图集大小和纹素大小
        buffer.SetGlobalVector(shadowAtlasSizeId,new Vector4(atlasSize,1f/atlasSize));
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
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            //计算视图和投影矩阵和裁剪空间的立方体
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i,
                cascadeCount, ratios, tileSize, light.nearPlaneOffset,
                out var viewMatrix, out var projectionMatrix, out var splitData);
            //得到第一个光源包围球数据
            if (index == 0)
            {
                //设置级联数据
                SetCascadeData(i,splitData.cullingSphere,tileSize);
                
            }
            //剔除偏差
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            //调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
            //设置斜度比例偏差值
            buffer.SetGlobalDepthBias(0f,light.slopScaleBias);
            //绘制阴影
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f,0f);
        }
    }
    
    //存储其他类型光源的阴影
    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None && light.shadowStrength <= 0f)
        {
            return new Vector4(0f,0f,0f,-1f);
        }
        float maskChannel = -1f;
        LightBakingOutput lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && 
            lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
        }
        
        if (shadowedOtherLightCount >= maxShadowedOtherLightCount || !cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
        {
            return new Vector4(-light.shadowStrength , 0f,0f,maskChannel);
        }
        
        return new Vector4(light.shadowStrength,0f,0f,maskChannel);
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

    //设置级联数据
    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)    //index 级联索引
    {
        //包围球直径除以阴影图块尺寸 = 纹素大小
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float) settings.directional.filter + 1f);
        cullingSphere.w -= filterSize;    //有了过滤之后，会增加采样范围，边界区域会采样到外面去，这种情况应该避免 
        //得到半径的平方
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w , filterSize * 1.4142136f);   
    }

    //设置关键字开启哪种PCF滤波模式
    void SetKeywords(string[] keywords , int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
    
}
