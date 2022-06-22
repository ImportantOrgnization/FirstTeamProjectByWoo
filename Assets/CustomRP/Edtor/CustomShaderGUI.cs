using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    private bool showPresets = true;
    
    private MaterialEditor editor;
    private Object[] materials;
    private MaterialProperty[] properties;
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        this.editor = materialEditor;
        this.properties = properties;
        materials = materialEditor.targets;
        
        EditorGUILayout.Space();
        showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if (showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }

    }

    bool HasProperty(string name) => FindProperty(name, properties, false) != null;
    private bool HasPremultyAlpha => HasProperty("_PremulAlpha");

    bool SetProperty(string name, float value)
    {
        MaterialProperty property = FindProperty(name, properties);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }

        return false;
    }

    void SetKeyword(string key, bool enabled)
    {
        foreach (Material m in materials)
        {
            if (enabled)
            {
                m.EnableKeyword(key);
            }
            else
            {
                m.DisableKeyword(key);
            }
            
        }
        
    }

    void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name,value?1f:0f))
        {
            SetKeyword(keyword,value);            
        }
    }

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    private bool PremultyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float) value);
    }

    private BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float) value);
    }

    private bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material m in materials)
            {
                m.renderQueue = (int) value;
            }
        }
    }

    /// <summary>
    /// 预置按钮
    /// </summary>
    bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }

    void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }

      
    void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

      
    void TransparentPreset()
    {
        if (HasPremultyAlpha && PresetButton("Transparent"))
        {
            Clipping = false;
            PremultyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

      

}
