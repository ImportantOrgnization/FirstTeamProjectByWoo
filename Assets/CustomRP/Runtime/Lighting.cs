using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using LightType = UnityEngine.LightType;

public class Lighting
{
    private const string bufferName = "Lihgting";
    private const int maxDirLightCount = 4;
    
    //private static int directionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    //private static int directionalLightDirection = Shader.PropertyToID("_DirectionalLightDirection");
    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName,
    };
    
    private CullingResults cullingResults;
    
    Shadows shadows = new Shadows();

    public void SetUp(ScriptableRenderContext context ,CullingResults cullingResults,ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        
        buffer.BeginSample(bufferName);
        shadows.Setup(context,cullingResults,shadowSettings);
        SetUpLights();    //设置光源信息，光源的阴影信息
        shadows.Render();      //渲染阴影图集      
        buffer.EndSample(bufferName);
        
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    // void SetUpDirectionalLight()
    // {
    //     Light light = RenderSettings.sun;
    //     buffer.SetGlobalVector(directionalLightColorId,light.color.linear * light.intensity);
    //     buffer.SetGlobalVector(directionalLightDirection, -light.transform.forward);
    // }

    void SetUpLights()
    {
        //得到影响相机可视空间的灯光
        NativeArray <VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                SetUpDirectionalLight(dirLightCount ++ , ref visibleLight);
                if (dirLightCount > maxDirLightCount)
                {
                    break;
                }
            }
        }
        //发送到GPU
        buffer.SetGlobalInt(dirLightCountId,dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId,dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId,dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId,dirShadowDatas);
    }

    private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    static Vector4[] dirShadowDatas = new Vector4[maxDirLightCount];
    void SetUpDirectionalLight(int index,ref  VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);    //第三列为z轴方向，即光源的前向向量，我们这里取它的负值
        dirShadowDatas[index] = shadows.ReserveDirectionalShadows(visibleLight.light,index);    //存储可见光源阴影数据
    }

    public void Cleanup()
    {
        shadows.Clearup();
    }
    
}
