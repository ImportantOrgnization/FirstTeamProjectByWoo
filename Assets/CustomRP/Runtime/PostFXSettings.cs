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
        //0表示散射只使用最低的Bloom金字塔级别，1散射量仅表示使用最高的Bloom金字塔级别，0.5时，4个级别的最终贡献为0.5，0.25，0.125，0.125
        //因为0和1的散射值只应用了一层金字塔级别，所以是没有意义的，我们这里对这个值做一些限制
        //大于1的Bloom强度不适合散射Bloom，因为那样会增加光线
        [Range(0.05f, 0.95f)] public float scatter;
        
        [Range(0f, 16f)] public int maxIterations;
        [Min(1f)] public int downscaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)] public float threshold;
        [Range(0f, 1f)] public float thresholdKnee;
        [Min(0f)] public float intensity;
        //淡化闪烁,类似于萤火虫闪烁，能够起点效果，但不能根治
        public bool fadeFireflies;    
    }

    [SerializeField] private BloomSettings bloom = new BloomSettings
    {
        scatter = 0.7f,
    };
    public BloomSettings Bloom => bloom;

    [Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode
        {
            None = -1,
            Reinhard ,
        };

        public Mode mode;
    }

    [SerializeField] private ToneMappingSettings toneMapping = default;
    public ToneMappingSettings ToneMapping => toneMapping;

}
