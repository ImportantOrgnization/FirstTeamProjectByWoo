using UnityEngine;
using UnityEngine.Rendering;

public class PostFXStack
{
    private const string bufferName = "Post FX";
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    public bool IsActive => settings != null;
    
    private ScriptableRenderContext context;
    private Camera camera;
    private PostFXSettings settings;

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
    {
        this.context = context;
        this.camera = camera;
        this.settings = settings;
    }

    public void Render(int sourceId)
    {
        buffer.Blit(sourceId,BuiltinRenderTextureType.CameraTarget);    //目标设置为当前渲染相机的帧缓冲区
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    
}
