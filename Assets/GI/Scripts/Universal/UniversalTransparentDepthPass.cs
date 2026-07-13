using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

namespace GI
{
    class UniversalTransparentDepthPass : RenderObjectsPass
    {
        private Transparents _transparents;

        public UniversalTransparentDepthPass(Transparents transparents, RenderPassEvent render_event)
            : base("Transparent Depth",
                   render_event,
                   null,
                   RenderQueueType.Transparent,
                   int.MaxValue,
                   new RenderObjects.CustomCameraSettings())
        {
            _transparents = transparents;

            overrideShader = Resources.Load<Shader>("Shaders/Render/TransparentDepth");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.Clear();
            cmd.SetRenderTarget(_transparents.Target);
            cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            base.Execute(context, ref renderingData);
        }
    }
}