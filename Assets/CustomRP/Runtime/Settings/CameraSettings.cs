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

}
