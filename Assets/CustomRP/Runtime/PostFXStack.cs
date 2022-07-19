using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    private const string bufferName = "Post FX";
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    enum Pass
    {
        BloomHorizontal,
        BloomVertical,
        Copy,
    }

    public bool IsActive => settings != null;
    
    private ScriptableRenderContext context;
    private Camera camera;
    private PostFXSettings settings;

    private int fxSourceId = Shader.PropertyToID("_PostFXSource");

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

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;    //只渲染enum的前两个，即 GameView 和SceneView
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        DoBloom(sourceId);
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

    void DoBloom(int sourceId)
    {
        buffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        RenderTextureFormat format = RenderTextureFormat.Default;
        int fromId = sourceId;
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
        Draw(fromId,BuiltinRenderTextureType.CameraTarget,Pass.BloomHorizontal);
        for (i -= 1; i >= 0; i--)
        {
            buffer.ReleaseTemporaryRT(fromId);
            buffer.ReleaseTemporaryRT(fromId -1 );
            fromId -= 2;
        }
        
        buffer.EndSample("Bloom");
    }
    
}
