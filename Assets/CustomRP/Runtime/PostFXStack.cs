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
        Copy,
    }

    public bool IsActive => settings != null;
    
    private ScriptableRenderContext context;
    private Camera camera;
    private PostFXSettings settings;

    private int fxSourceId = Shader.PropertyToID("_PostFXSource");

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;    //只渲染enum的前两个，即 GameView 和SceneView
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        Draw(sourceId,BuiltinRenderTextureType.CameraTarget,Pass.Copy);
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
    
}
