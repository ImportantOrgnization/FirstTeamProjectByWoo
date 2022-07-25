using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light),typeof(CustomRenderPipelineAsset))]
public class CustomLightEditor : LightEditor
{
    
    static GUIContent renderingLayerMaskLabel = new GUIContent("Rendering Layer Mask" , "Functional version of above property");
    //重新灯光Inspector面板
    //如果光源的CullingMask不是Everything层，显示警告：CullingMask只影响阴影
    //如果不是定向光源，则提示除非开启逐对象光照，除了影响阴影还可以影响物体受光
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawRenderingLayerMask();
        if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
        }
        settings.ApplyModifiedProperties();            

        var light = target as Light;
        if (light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox(light.type == LightType.Directional?
                "CullingMask Only affects shadows":
                "CullingMask only affects shadows unless Use lights per Objects is On",
                MessageType.Warning);
        }
    }

    void DrawRenderingLayerMask()
    {
        //int.maxValue = Mixing , 0 = Nothing , -1 = EveryThing
        SerializedProperty property = settings.renderingLayerMask;    //renderingLayerMask 内部存储的是uint32类型，Eveything由 -1 标识，32层最高位代表比int.MaxValue大的数字，它们都Clamp为0 ，所以选择Everything和32层时，都会变成Nothing
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int mask = property.intValue;
        if (mask == int.MaxValue)
        {
            mask = -1;
        }
        

        mask = EditorGUILayout.MaskField(renderingLayerMaskLabel, mask, GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames);    //注意，这个renderingLayerMaskNames被override过了，在 .Editor.cs 文件中
        if (EditorGUI.EndChangeCheck())
        {
            property.intValue = mask == -1? int.MaxValue : mask;
        

        }
        EditorGUI.showMixedValue = false;

        
        
    }
}
