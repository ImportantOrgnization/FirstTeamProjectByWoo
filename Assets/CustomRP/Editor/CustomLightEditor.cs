using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light),typeof(CustomRenderPipelineAsset))]
public class CustomLightEditor : LightEditor
{
    //重新灯光Inspector面板
    //如果光源的CullingMask不是Everything层，显示警告：CullingMask只影响阴影
    //如果不是定向光源，则提示除非开启逐对象光照，除了影响阴影还可以影响物体受光
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            settings.ApplyModifiedProperties();            
        }

        var light = target as Light;
        if (light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox(light.type == LightType.Directional?
                "CullingMask Only affects shadows":
                "CullingMask only affects shadows unless Use lights per Objects is On",
                MessageType.Warning);
        }
    }
}
