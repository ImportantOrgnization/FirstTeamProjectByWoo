using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

//////////////////////////////////////////////////////////////////////
// 绘制SRP不支持的着色器类型
//////////////////////////////////////////////////////////////////////
public partial class CameraRenderer
{

    partial void DrawUnsupportedShaders();        
    partial void DrawGizmos();                   
    partial void PrepareForSceneWindow();        //将UI发送到场景中
    partial void PrepareBuffer();                //设置命令缓冲区名字

#if UNITY_EDITOR

    
    private static Material errorMaterial;

    private string SampleName { get; set; }
    
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };

    partial void DrawUnsupportedShaders() {
        //材质球
        if (!errorMaterial)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        
        //传入第一个ShaderTagId进行DrawingSetting的构造
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0],new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial,
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i,legacyShaderTagIds[i]);
        }
        //使用默认设置即可，反正画出来的都是不支持的
        var filteringSettings = FilteringSettings.defaultValue;
        //开始绘制
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
    }

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera,GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);    //Emits UI geometry into the Scene view for rendering
        }
    }

    partial void PrepareBuffer()
    {
        //设置一下只有在编辑器模式下才会分配内存
        Profiler.BeginSample("Editor Only");
        SampleName = "SampleName_" + camera.name;
        buffer.name = "bufferName_" + camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = bufferName;
#endif
}
