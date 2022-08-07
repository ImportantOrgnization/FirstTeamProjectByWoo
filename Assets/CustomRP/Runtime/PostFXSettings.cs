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
        //是否忽略渲染缩放
        public bool ignoreRenderScale;
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
            None,
            Reinhard ,    //比较暗
            Neutral,    //中性色调
            ACES,    //Academy Color Encoding System 标准的电影效果
        };

        public Mode mode;
    }

    [SerializeField] private ToneMappingSettings toneMapping = default;
    public ToneMappingSettings ToneMapping => toneMapping;

    [Serializable]
    public struct ColorAdjustmentsSettings
    {
        //后曝光，调整场景的整体曝光度
        public float postExposure;
        //对比度，扩大或缩小色调值的总体范围
        [Range(-100f, 100f)] public float contrast;
        //颜色滤镜，通过乘以颜色来给渲染器着色
        [ColorUsage(false, true)] public Color colorFilter;
        //色调偏移,改变所有颜色的色调
        [Range(-180f, 180f)] public float hueShift;
        //饱和度,推动所有颜色的强度
        [Range(-100f, 100f)] public float saturation;
    }

    [SerializeField] private ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings()
    {
        colorFilter = Color.white
    };

    public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;

    [Serializable]
    public struct WhiteBalanceSettings
    {
        //色温，调整白平衡的冷暖偏向
        [Range(-100f, 100f)] public float temperature;
        //色调，调整温度变化后的颜色
        [Range(-100f, 100f)] public float tint;
    }

    [SerializeField]
    private WhiteBalanceSettings whiteBalance = default;
    public WhiteBalanceSettings WhiteBalance => whiteBalance;

    [Serializable]
    public struct SplitToningSettings
    {
        //用于对阴影和高光着色
        [ColorUsage(false)] public Color shadows, highlights;
        //设置阴影和高光之间的平衡滑块
        [Range(-100f, 100f)] public float balance;
    }

    [SerializeField]
    private SplitToningSettings splitToning = new SplitToningSettings
    {
        shadows = Color.gray,
        highlights = Color.gray,
    };

    public SplitToningSettings SplitToning => splitToning;

    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }

    [SerializeField] private ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward,
    };

    public ChannelMixerSettings ChannelMixer => channelMixer;

    [Serializable]
    public struct ShadowMidtonesHighlightsSettings
    {
        [ColorUsage(false, true)] public Color shadows, midtones, highLights;
        [Range(0f, 2f)] public float shadowStart, shadowEnd, highlightsStart, highlightsEnd;
    }

    [SerializeField] private ShadowMidtonesHighlightsSettings shadowsMidtonesHighlights =
        new ShadowMidtonesHighlightsSettings
        {
            shadows = Color.white,
            midtones = Color.white,
            highLights = Color.white,
            shadowEnd = 0.3f,
            highlightsStart = 0.55f,
            highlightsEnd = 1f,
        };

    public ShadowMidtonesHighlightsSettings ShadowMidtonesHighlights => shadowsMidtonesHighlights;
}
