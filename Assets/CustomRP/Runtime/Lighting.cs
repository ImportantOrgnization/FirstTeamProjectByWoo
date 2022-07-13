using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 灯光管理类
/// </summary>
public class Lighting
{

	const string bufferName = "Lighting";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};
    //设置最大可见定向光数量
    const int maxDirLightCount = 4;
    //定义其他类型光源的最大数量
    private const int maxOtherLightCount = 64;

    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    //存储定向光的颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
	static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

	private static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
	private static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
	private static int otherLightPositionId = Shader.PropertyToID("_OtherLightPositions");
	
	//存储其他类型光源的颜色和位置数据
	static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
	static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    
    //存储相机剔除后的结果
    CullingResults cullingResults;
	
    Shadows shadows = new Shadows();
    //初始化设置
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults,ShadowSettings shadowSettings)
	{
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //传递阴影数据
        shadows.Setup(context,cullingResults,shadowSettings);
        SetupLights();
        shadows.Render();
        buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
	/// <summary>
    /// 存储定向光的数据
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleIndex"></param>
    /// <param name="visibleLight"></param>
    /// <param name="light"></param>
	void SetupDirectionalLight(int index, ref VisibleLight visibleLight) {
        dirLightColors[index] = visibleLight.finalColor;
        //通过VisibleLight.localToWorldMatrix属性找到前向矢量,它在矩阵第三列，还要进行取反
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light,index);
    }
    /// <summary>
    /// 存储并发送所有光源数据
    /// </summary>
    /// <param name="useLightsPerObject"></param>
    /// <param name="renderingLayerMask"></param>
    void SetupLights() {
        //得到所有影响相机渲染物体的可见光数据
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        
        int dirLightCount = 0,otherLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];

            switch (visibleLight.lightType)
            {
	            case LightType.Spot:
		            break;
	            case LightType.Directional:
		            if (dirLightCount < maxDirLightCount)
		            {
			            //VisibleLight结构很大,我们改为传递引用不是传递值，这样不会生成副本
			            SetupDirectionalLight(dirLightCount ++ , ref visibleLight);
		            }
		            break;
	            case LightType.Point:
		            SetupPointLight(otherLightCount++ ,ref visibleLight);
		            break;
            }
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
	        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
	        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
	        buffer.SetGlobalVectorArray(dirLightShadowDataId,dirLightShadowData);    
        }
        
        buffer.SetGlobalInt(otherLightCountId,otherLightCount);
        if (otherLightCount > 0)
        {
	        buffer.SetGlobalVectorArray(otherLightColorsId,otherLightColors);
	        buffer.SetGlobalVectorArray(otherLightPositionId,otherLightPositions);
        }
        
    }
    
    //将点光源的颜色和位置信息存储到数组
    void SetupPointLight(int index, ref VisibleLight visibleLight)
    {
	    otherLightColors[index] = visibleLight.finalColor;	//颜色乘以强度
	    //位置信息在本地到世界的转换矩阵的最后一列
	    var position = visibleLight.localToWorldMatrix.GetColumn(3);
	    //将光照范围的平方倒数存在光源位置的W分量中
	    position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
	    otherLightPositions[index] = position;

    }
    
    //释放阴影贴图RT内存
    public void Cleanup()
    {
	    shadows.Clearup();
    }
}
