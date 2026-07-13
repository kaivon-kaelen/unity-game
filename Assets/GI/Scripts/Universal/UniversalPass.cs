using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GI
{
    public abstract class UniversalPass : ScriptableRenderPass
    {
        public abstract void Release();

        public static void Fill(GISettings settings, ref PassData data, ref RenderingData rendering_data)
        {
            var renderer = rendering_data.cameraData.renderer;

            data.StereoMode = rendering_data.cameraData.xr.enabled;
            data.Camera = rendering_data.cameraData.camera;
            data.Atlas = GameObject.FindObjectOfType<SDFAtlas>();
            data.Settings = settings;
            data.ColorRenderHandle = rendering_data.cameraData.renderer.cameraColorTargetHandle;
            data.DepthRenderHandle = rendering_data.cameraData.renderer.cameraDepthTargetHandle;
        }

        public static void ApplyRequirements(ScriptableRenderPass pass, int requirements)
        {
            if ((requirements & Requirements.Depth) == Requirements.Depth)
                pass.ConfigureInput(ScriptableRenderPassInput.Depth);

            if ((requirements & Requirements.Normals) == Requirements.Normals)
                pass.ConfigureInput(ScriptableRenderPassInput.Normal);

            if ((requirements & Requirements.Motion) == Requirements.Motion)
                pass.ConfigureInput(ScriptableRenderPassInput.Motion);
        }
    }

    public class UniversalPassWrapper : UniversalPass
    {
        private Pass _pass;
        private GISettings _settings;
        private ScriptableExecutor _executor;

        public UniversalPassWrapper(GISettings settings, Pass pass)
        {
            _pass = pass;
            _settings = settings;
            _executor = new ScriptableExecutor();

            ApplyRequirements(this, pass.InputRequirements(settings));
        }

        public override void Release()
        {
            if (_pass != null)
            {
                _pass.Release();
                _pass = null;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData rendering_data)
        {
            var data = new PassData();
            UniversalPass.Fill(_settings, ref data, ref rendering_data);

            ApplyRequirements(this, _pass.InputRequirements(_settings));

            _executor.Context = context;
            _pass.Execute(_executor, data);
        }
    }

    public class UniversalPassManager : PassManager
    {
        public GISettings Settings;
        public RenderPassEvent RenderEvent = RenderPassEvent.AfterRenderingGbuffer;
        public ScriptableRenderer ScriptableRenderer;

        private List<UniversalPass> _list = new List<UniversalPass>();

        protected override void ReleaseAdditional()
        {
            foreach (var pass in _list)
                pass.Release();

            _list.Clear();
        }

        public override void Run()
        {
            foreach (var pass in _list)
                ScriptableRenderer.EnqueuePass(pass);
        }

        public override void Add(Pass pass, Queue queue)
        {
            var wrapper = new UniversalPassWrapper(Settings, pass);

            switch (queue)
            {
                case Queue.BeforeRender: wrapper.renderPassEvent = RenderPassEvent.BeforeRendering; break;
                case Queue.Render: wrapper.renderPassEvent = RenderEvent; break;
                case Queue.BeforePostProcess: wrapper.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; break;
                case Queue.AfterRender: wrapper.renderPassEvent = RenderPassEvent.AfterRendering; break;
            }

            _list.Add(wrapper);
        }
    }
}