using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField] private Shader shader = default;
    [System.NonSerialized] private Material material;

    public Material Material
    {
        get
        {
            if (material == null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }

    [Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)] public int maxIterations;
        [Min(1f)] public int downscaleLimit;
    }

    [SerializeField] private BloomSettings bloom = default;
    public BloomSettings Bloom => bloom;

}
