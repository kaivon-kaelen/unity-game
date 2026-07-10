using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelateFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material pixelateMaterial;
        public float pixelSize = 4f;
    }

    public Settings settings = new Settings();
    private PixelatePass pixelatePass;

    public override void Create()
    {
        pixelatePass = new PixelatePass(settings.pixelateMaterial, settings.pixelSize);
        pixelatePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pixelatePass);
    }
}

class PixelatePass : ScriptableRenderPass
{
    private Material material;
    private float pixelSize;
    private RTHandle tempTextureHandle;
    private const string tempTextureName = "_TempPixelateTex";

    public PixelatePass(Material mat, float size)
    {
        material = mat;
        pixelSize = size;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
    if (material == null) return;

    var desc = renderingData.cameraData.cameraTargetDescriptor;
    if (desc.width <= 0 || desc.height <= 0) return; // sécurité

    CommandBuffer cmd = CommandBufferPool.Get("Pixelate Pass");

    var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

    material.SetFloat("_PixelSize", pixelSize);

    desc.depthBufferBits = 0;
    desc.msaaSamples = 1;

    RenderingUtils.ReAllocateIfNeeded(ref tempTextureHandle, desc, name: tempTextureName);

    cmd.Blit(source, tempTextureHandle, material);
    cmd.Blit(tempTextureHandle, source);

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();
    CommandBufferPool.Release(cmd);
    }
}