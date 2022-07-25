using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(Camera))]
public class CustomRenderPipelineCamera : MonoBehaviour
{
    [SerializeField] private CameraSettings settings = default;

    public CameraSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = new CameraSettings();
            }
            return settings;
        }
    }
}
