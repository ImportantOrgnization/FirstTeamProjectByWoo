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
	
    //定向光
    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
	static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
	
	//点光 //聚光灯
	private static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
	private static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
	private static int otherLightPositionId = Shader.PropertyToID("_OtherLightPositions");
	static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
	static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
	private static int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
	static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
	private static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
	static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
	private static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");
	Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];
	private static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";
	
    //存储相机剔除后的结果
    CullingResults cullingResults;
	
    Shadows shadows = new Shadows();
    //初始化设置
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults,ShadowSettings shadowSettings,bool useLightsPerObject)
	{
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //传递阴影数据
        shadows.Setup(context,cullingResults,shadowSettings);
        SetupLights(useLightsPerObject);
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
	void SetupDirectionalLight(int index,int visibleIndex, ref VisibleLight visibleLight) {
        dirLightColors[index] = visibleLight.finalColor;
        //通过VisibleLight.localToWorldMatrix属性找到前向矢量,它在矩阵第三列，还要进行取反
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light,visibleIndex);
    }
    /// <summary>
    /// 存储并发送所有光源数据
    /// </summary>
    /// <param name="useLightsPerObject"></param>
    /// <param name="renderingLayerMask"></param>
    void SetupLights(bool useLightsPerObject) {
	    //拿到光源索引列表
	    NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
	    
        //得到所有影响相机渲染物体的可见光数据
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        
        int dirLightCount = 0,otherLightCount = 0;
        int i;
        for ( i = 0; i < visibleLights.Length; i++)
        {
	        int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];

            switch (visibleLight.lightType)
            {
	            case LightType.Spot:
		            if (otherLightCount < maxOtherLightCount)
		            {
			            newIndex = otherLightCount;
			            SetupSpotLight(otherLightCount++ ,i, ref visibleLight);
		            }
		            break;
	            case LightType.Directional:
		            if (dirLightCount < maxDirLightCount)
		            {
			            //VisibleLight结构很大,我们改为传递引用不是传递值，这样不会生成副本
			            SetupDirectionalLight(dirLightCount ++ ,i, ref visibleLight);
		            }
		            break;
	            case LightType.Point:
		            if (otherLightCount < maxOtherLightCount)
		            {
			            newIndex = otherLightCount;
			            SetupPointLight(otherLightCount++ ,i, ref visibleLight);    
		            }
		            break;
            }

            if (useLightsPerObject)
            {
	            indexMap[i] = newIndex;
            }
        }
        
        //消除所有不可见光的索引
        if (useLightsPerObject)
        {
	        for (; i < indexMap.Length; i++)
	        {
		        indexMap[i] = -1;
	        }
	        cullingResults.SetLightIndexMap(indexMap);
	        indexMap.Dispose();
	        Shader.EnableKeyword(lightsPerObjectKeyword);
        }
        else
        {
	        Shader.DisableKeyword(lightsPerObjectKeyword);
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
	        buffer.SetGlobalVectorArray(otherLightDirectionsId,otherLightDirections);
	        buffer.SetGlobalVectorArray(otherLightSpotAnglesId,otherLightSpotAngles);
	        buffer.SetGlobalVectorArray(otherLightShadowDataId,otherLightShadowData);
        }
        
    }
    
    //将点光源的颜色和位置信息存储到数组
    void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
	    otherLightColors[index] = visibleLight.finalColor;	//颜色乘以强度
	    //位置信息在本地到世界的转换矩阵的最后一列
	    var position = visibleLight.localToWorldMatrix.GetColumn(3);
	    //将光照范围的平方倒数存在光源位置的W分量中
	    position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
	    otherLightPositions[index] = position;
	    
	    otherLightSpotAngles[index] = new Vector4(0f,1f);	//xy的值在其他光照的计算中，会消除聚光灯计算过程对点光源光照计算的影响 //saturate( (d - cos(r0/2)) / (cos(ri/2) - cos(ro / 2)) ) ^2 		//spotAngleAttenuation = d * 0 + 1 = 1

	    Light light = visibleLight.light;
	    otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }

    //将聚光灯光源的颜色、位置、方向存储到数组
    void SetupSpotLight(int index,int visibleIndex, ref VisibleLight visibleLight)
    {
	    otherLightColors[index] = visibleLight.finalColor;
	    Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
	    position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
	    otherLightPositions[index] = position;
	    //本地到世界的转换矩阵的第三列在求反得到光照方向
	    otherLightDirections[index] =  -visibleLight.localToWorldMatrix.GetColumn(2);
	    Light light = visibleLight.light;
	    float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
	    float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
	    float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
	    otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
	    otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }
    
    //释放阴影贴图RT内存
    public void Cleanup()
    {
	    shadows.Clearup();
    }
}
