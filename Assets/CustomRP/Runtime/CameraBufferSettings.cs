﻿using System;
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
        
        // The minimum amount of local contrast required to apply algorithm.
        //   0.333 - too little (faster)
        //   0.250 - low quality
        //   0.166 - default
        //   0.125 - high quality 
        //   0.063 - overkill (slower)
        [Range(0.063f, 0.333f)]
        public float relativeThreshold;
    }

    public FXAA fxaa;

}
