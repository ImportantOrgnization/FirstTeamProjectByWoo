using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    [RenderingLayerMaskField][Tooltip("物体遮罩")]
    public int renderingLayerMask = -1;
    
    [Tooltip("灯光遮罩")]
    public bool maskLights = false;

    public bool copyDepth = true;
    public bool copyColor = true;
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode source, destination;
    }

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        source = BlendMode.One,
        destination = BlendMode.Zero,
    };

    public bool overridePostFX = false;
    public PostFXSettings postFxSettings = default;

    public enum RenderScaleMode
    {
        Inherit,
        Multiply,
        Override,
    }

    public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;

    [Range(0.1f, 2f)] public float renderScale = 1f;

    public float GetRenderScale(float scale)
    {
        return renderScaleMode == RenderScaleMode.Inherit ? scale :
            renderScaleMode == RenderScaleMode.Override ? renderScale : scale * renderScale;
    }

    public bool allowFXAA = false;

    public bool keepAlpha = false;    // is false , calculate luma and store it in alpha channel
}
