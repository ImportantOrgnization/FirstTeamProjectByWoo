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
        public enum Mode{Additive,Scattring,}
        public Mode mode;
        [Range(0f, 1f)] public float scatter;
        
        [Range(0f, 16f)] public int maxIterations;
        [Min(1f)] public int downscaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)] public float threshold;
        [Range(0f, 1f)] public float thresholdKnee;
        [Min(0f)] public float intensity;
        //淡化闪烁,类似于萤火虫闪烁，能够起点效果，但不能根治
        public bool fadeFireflies;    
    }

    [SerializeField] private BloomSettings bloom = default;
    public BloomSettings Bloom => bloom;

}
