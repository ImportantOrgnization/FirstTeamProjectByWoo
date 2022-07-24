using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack
{
    private const string bufferName = "Post FX";
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    enum Pass
    {
        Copy,
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomScatter,
        BloomPrefilter,
        BloomPrefilterflies,
        ToneMappingNone,
        ToneMappingReinhard,
        ToneMappingNeutral,
        ToneMappingACES,
    }

    public bool IsActive => settings != null;
    
    private ScriptableRenderContext context;
    private Camera camera;
    private PostFXSettings settings;

    private int fxSourceId = Shader.PropertyToID("_PostFXSource");         //用于降采样和高斯模糊以及后面的combine
    private int fxSource2Id = Shader.PropertyToID("_PostFXSource2");       //用于bloom combine
    private int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"); //预滤波纹理
    private int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    private int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    
    private const int maxBloomPyramidLevels = 16;
    //纹理标识符
    private int bloomPyramidId;

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0"); //只跟踪第一个标识符
        for (int i = 0; i < maxBloomPyramidLevels * 2; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);    //确保占用接下来的标识符的位置,它们是连续的
        }
    }

    private bool useHDR;
    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings,bool useHDR)
    {
        this.useHDR = useHDR;
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;    //只渲染enum的前两个，即 GameView 和SceneView
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        
        if (DoBloom(sourceId))
        {
            DoColorGradingAndToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoColorGradingAndToneMapping(sourceId);
        }
        
        //Draw(sourceId,BuiltinRenderTextureType.CameraTarget,Pass.Copy);
        //buffer.Blit(sourceId,BuiltinRenderTextureType.CameraTarget);    //目标设置为当前渲染相机的帧缓冲区
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId,from);
        buffer.SetRenderTarget(to,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material,(int) pass,MeshTopology.Triangles,3);
    }

    private int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    private int bloomResultId = Shader.PropertyToID("_BloomResult");
    bool DoBloom(int sourceId)
    {
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        
        //如果跳过bloom，则用CopyPass作为替代
        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2)
        {
            return false;
        }

        buffer.BeginSample("Bloom");
        Vector4 threshold; //( t , -t + tk ,2tk , 1 / (4tk + 0.00001))
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId,threshold);
        
        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        buffer.GetTemporaryRT(bloomPrefilterId,width,height,0,FilterMode.Bilinear,format);
        Draw(sourceId,bloomPrefilterId, bloom.fadeFireflies? Pass.BloomPrefilterflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
      
        int fromId = bloomPrefilterId;
        int toId = bloomPyramidId + 1;
        
        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
            {
                break;
            }

            int midId = toId - 1;
            buffer.GetTemporaryRT(midId,width,height,0,FilterMode.Bilinear,format);    
            buffer.GetTemporaryRT(toId,width,height,0,FilterMode.Bilinear,format);    //生成一个尺寸 1/2 大小的纹理
            Draw(fromId,midId,Pass.BloomHorizontal);
            Draw(midId,toId,Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        
        //将最后一级纹理图像数据拷贝到相机的渲染目标中
        //Draw(fromId,BuiltinRenderTextureType.CameraTarget,Pass.BloomHorizontal);
        buffer.SetGlobalFloat(bloomBicubicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);
        Pass combinePass;
        float finalIntensity;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = Pass.BloomAdd;
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            buffer.SetGlobalFloat(bloomIntensityId,bloom.scatter);
        }
        
        if (i > 1 )
        {
            
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
        
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id,toId +1);
                Draw(fromId,toId,combinePass);
            
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId +1 );
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }
        buffer.SetGlobalFloat(bloomIntensityId,bloom.intensity);
        buffer.SetGlobalTexture(fxSource2Id,sourceId);    //sourceId = 自定义缓冲纹理 , formId = 经历了降采样模糊 以及 叠加操作后的间接纹理
        buffer.GetTemporaryRT(bloomResultId,camera.pixelWidth,camera.pixelHeight,0,FilterMode.Bilinear,format);        
        Draw(fromId,bloomResultId,Pass.BloomAdd);    //教程这里是错的，它是 combinePass
        buffer.ReleaseTemporaryRT(fromId);
        buffer.ReleaseTemporaryRT(bloomPrefilterId);

        buffer.EndSample("Bloom");
        return true;
    }

    private int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    private int colorFilterId = Shader.PropertyToID("_ColorFilter");
    
    //获取颜色调整配置
    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId,new Vector4(
            Mathf.Pow(2f,colorAdjustments.postExposure),    //2 ^ postExpoure
            colorAdjustments.contrast * 0.01f + 1f,    //[0,2]
            colorAdjustments.hueShift * (1f / 360f),   //[-0.5,0.5]
            colorAdjustments.saturation * 0.01f + 1f    //[0,2]
            ));
        buffer.SetGlobalColor(colorFilterId,colorAdjustments.colorFilter.linear);
    }

    private int whiteBalanceId = Shader.PropertyToID("_WhiteBalance");

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
        buffer.SetGlobalVector(whiteBalanceId,ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature,whiteBalance.tint));
    }

    private int splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
    private int splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");

    void ConfigureSplitToning()
    {
        SplitToningSettings splitToning = settings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalColor(splitToningShadowsId,splitColor);
        buffer.SetGlobalColor(splitToningHighlightsId,splitToning.highlights);
    }

    private int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
    private int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
    private int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");

    void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId,channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId,channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId,channelMixer.blue);
    }
    
    void DoColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = mode < 0 ? Pass.Copy : Pass.ToneMappingNone + (int) mode;
        Draw(sourceId,BuiltinRenderTextureType.CameraTarget,pass);
    }
    
    
}
