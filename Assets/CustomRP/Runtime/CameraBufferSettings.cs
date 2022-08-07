using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    public bool copyDepth;
    public bool copyDepthReflection;
    public bool copyColor;
    public bool copyColorReflection;
    [Range(0.1f, 2f)] public float renderScale;
    public BicubicRescalingMode bicubicRescaling;

    public enum BicubicRescalingMode
    {
        Off,UpOnly,UpAndDown,
    }

    [Serializable]
    public struct FXAA
    {
        public bool enabled;
        [Range(0.0312f, 0.0833f)]
        public float fixedThreshold;
    }

    public FXAA fxaa;

}
