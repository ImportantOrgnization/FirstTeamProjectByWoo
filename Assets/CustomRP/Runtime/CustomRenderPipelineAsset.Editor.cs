using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 自定义渲染管线资产
/// </summary>
//该标签会在你在Project下右键->Asset/Create菜单中添加一个新的子菜单
public partial class CustomRenderPipelineAsset
{
#if UNITY_EDITOR
    private static string[] renderingLayerNames;

    static CustomRenderPipelineAsset()
    {
        renderingLayerNames = new string[31];
        for (int i = 0; i < renderingLayerNames.Length; i++)
        {
            renderingLayerNames[i] = "Layer" + (i + 1);
        }
    }

    public override string[] renderingLayerMaskNames => renderingLayerNames;


#endif

}
