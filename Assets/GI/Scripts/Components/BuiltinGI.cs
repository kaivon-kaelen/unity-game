using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class BuiltinGI : MonoBehaviour
    {
        public GISettings Settings = new GISettings();

        private Camera _camera;

        private CommandBuffer _pre_render_buffer;
        private CommandBuffer _render_buffer;
        private CommandBuffer _apply_buffer;
        private CommandBuffer _before_post_process_buffer;
        private CommandBuffer _after_render_buffer;

        private BuiltinPassManager _passes;

        private FarProbes _far_probes;
        private Frame _frame;
        private GDF _gdf;
        private Lights _lights;
        private Probes _probes;
        private ScreenProbes _screen_probes;
        private SDFGrid _sdf_grid;
        private Surfels _surfels;
        private Temporal _temporal;
        private Traces _traces;
        private Transparents _transparents;

        public void Recreate()
        {
            destructGI();
            createGI();
        }

        private void OnValidate()
        {
            Recreate();
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            createGI();
        }

        private void createBuffers()
        {
            if (_pre_render_buffer == null)
            {
                _pre_render_buffer = new CommandBuffer();
                _pre_render_buffer.name = "GI Pre Render";
            }

            if (_render_buffer == null)
            {
                _render_buffer = new CommandBuffer();
                _render_buffer.name = "GI Render";
            }

            if (_apply_buffer == null)
            {
                _apply_buffer = new CommandBuffer();
                _apply_buffer.name = "GI Apply";
            }

            if (_before_post_process_buffer == null)
            {
                _before_post_process_buffer = new CommandBuffer();
                _before_post_process_buffer.name = "GI Debug";
            }

            if (_after_render_buffer == null)
            {
                _after_render_buffer = new CommandBuffer();
                _after_render_buffer.name = "GI Debug";
            }
        }

        private void createGI()
        {
            Shader.SetKeyword(GlobalKeyword.Create("PROBES_SH3"), Settings.Probes == ProbeChoice.SH3);
            Shader.SetKeyword(GlobalKeyword.Create("PROBES_SIMPLE"), Settings.Probes == ProbeChoice.Simple);
            Shader.SetKeyword(GlobalKeyword.Create("PROBES_OCCLUSION"), Settings.ProbeOcclusion);

            _frame = new Frame();
            _gdf = new GDF(Settings.GDFCascades);
            _lights = new Lights();
            _probes = new Probes(Settings.Cell, Settings.Cascades, Settings.Probes == ProbeChoice.Simple, Settings.ProbeOcclusion, Settings.ProbeFiltering, Settings.Probes == ProbeChoice.SH3, Settings.Experimental.SurfelMapping);
            _sdf_grid = new SDFGrid();
            _surfels = new Surfels();
            _traces = new Traces(Settings.Experimental.ScreenProbes);

            var temporal_accumulation = Settings.TemporalAccumulation != TemporalChoice.None;

            if (temporal_accumulation || Settings.Experimental.ScreenProbes)
                _temporal = new Temporal();

            if (Settings.FarProbes)
                _far_probes = new FarProbes(Settings.Cell, Settings.Cascades, Settings.Experimental.SurfelMapping);

            if (Settings.Experimental.ScreenProbes)
                _screen_probes = new ScreenProbes(Settings.Experimental.MaxAdaptiveProbes);

            //

            _passes = new BuiltinPassManager();
            _passes.PreRender = _pre_render_buffer;
            _passes.Render = _render_buffer;
            _passes.Apply = _apply_buffer;
            _passes.BeforePostProcess = _before_post_process_buffer;
            _passes.AfterRender = _after_render_buffer;

            //

            var before_render = new PassList();

            before_render.Passes.Add(new SDFGridUpdatePass(_sdf_grid));
            before_render.Passes.Add(new GDFUpdatePass(_gdf, _sdf_grid));
            before_render.Passes.Add(new ProbeMaintainPass(_probes));
            before_render.Passes.Add(new SurfelMaintainPass(_surfels));

            if (Settings.FarProbes)
                before_render.Passes.Add(new FarProbeMaintainPass(_far_probes));

            _passes.Add(before_render, Queue.BeforeRender);

            //

            var render = new PassList();

            if (Settings.Experimental.ScreenProbes)
                render.Passes.Add(new ScreenProbeCreatePass(_frame, _screen_probes, _probes, _traces, _gdf, _sdf_grid));

            render.Passes.Add(new ProbeCreatePass(_frame, _gdf, _sdf_grid, _probes, _surfels, 2, _transparents));

            if (Settings.FarProbes)
                render.Passes.Add(new FarProbeCreatePass(_far_probes, _probes, _screen_probes));

            render.Passes.Add(new ProbeTracePass(_frame, _traces, _gdf, _probes, _far_probes, _surfels, _sdf_grid));

            if (Settings.FarProbes)
                render.Passes.Add(new FarProbeTracePass(_frame, _traces, _gdf, _far_probes, _surfels));

            if (Settings.Experimental.ScreenProbes)
                render.Passes.Add(new ScreenProbeTracePass(_frame, _traces, _gdf, _screen_probes, _far_probes, _surfels, _sdf_grid));

            //

            // order matters, Probe pass must be first before other light passes
            render.Passes.Add(new SurfelCreatePass(_probes, _screen_probes, _far_probes, _surfels, _traces));
            render.Passes.Add(new SurfelLightProbePass(_probes, _surfels));
            render.Passes.Add(new SurfelLightSunPass(_gdf, _probes, _surfels, _lights));
            render.Passes.Add(new SurfelLightPass(_gdf, _probes, _surfels, _lights));

            if (Settings.FarProbes)
            {
                render.Passes.Add(new FarProbeRadiancePass(_far_probes, _surfels, _traces));
                render.Passes.Add(new FarProbeAtlasPass(_far_probes));
            }

            render.Passes.Add(new ProbeRadiancePass(_traces, _probes, _far_probes, _surfels));

            if (Settings.Experimental.ScreenProbes)
                render.Passes.Add(new ScreenProbeRadiancePass(_screen_probes, _far_probes, _traces, _surfels));

            //

            if (Settings.ProbeFiltering)
                render.Passes.Add(new ProbeFilterPass(_probes));

            render.Passes.Add(new ProbeAtlasPass(_probes));
            render.Passes.Add(new ProbeGatherPass(_probes, Settings.Probes));

            //

            if (Settings.Experimental.ScreenProbes)
            {
                render.Passes.Add(new ScreenProbeFilterPass(_screen_probes));
                render.Passes.Add(new ScreenProbeGatherPass(_screen_probes));
                render.Passes.Add(new ScreenProbeIntegratePass(_screen_probes));
            }

            //

            if (temporal_accumulation || Settings.Experimental.ScreenProbes)
            {
                if (!Settings.Experimental.ScreenProbes)
                    render.Passes.Add(new ProbeIntegratePass(_probes));

                render.Passes.Add(new TemporalPass(_temporal, _screen_probes, _probes));
            }

            //

            _passes.Add(render, Queue.Render);

            //

            // if (Settings.DeferredMode)
            {
                Pass apply;

                if (temporal_accumulation || Settings.Experimental.ScreenProbes)
                    apply = new ApplyTemporalDeferredPass(_screen_probes, _probes, _temporal);
                else
                    apply = new ApplyProbesDeferredPass(_probes);

                _passes.Add(apply, Queue.Apply);
            }

            //

            _passes.Add(new BeforePostProcessDebugPass(_gdf, _probes, _surfels, _far_probes), Queue.BeforePostProcess);
            _passes.Add(new AfterRenderDebugPass(_screen_probes), Queue.AfterRender);
        }

        private void destructBuffers()
        {
            if (_pre_render_buffer != null)
            {
                _pre_render_buffer.Release();
                _pre_render_buffer = null;
            }

            if (_render_buffer != null)
            {
                _render_buffer.Release();
                _render_buffer = null;
            }

            if (_apply_buffer != null)
            {
                _apply_buffer.Release();
                _apply_buffer = null;
            }

            if (_before_post_process_buffer != null)
            {
                _before_post_process_buffer.Release();
                _before_post_process_buffer = null;
            }

            if (_after_render_buffer != null)
            {
                _after_render_buffer.Release();
                _after_render_buffer = null;
            }
        }

        private void destructGI()
        {
            if (_far_probes != null)
            {
                _far_probes.Release();
                _far_probes = null;
            }

            // if (_frame != null)
            // {
            //     _frame.Release();
            //     _frame = null;
            // }

            if (_gdf != null)
            {
                _gdf.Release();
                _gdf = null;
            }

            // if (_lights != null)
            // {
            //     _lights.Release();
            //     _lights = null;
            // }

            if (_probes != null)
            {
                _probes.Release();
                _probes = null;
            }

            if (_screen_probes != null)
            {
                _screen_probes.Release();
                _screen_probes = null;
            }

            if (_sdf_grid != null)
            {
                _sdf_grid.Release();
                _sdf_grid = null;
            }

            if (_surfels != null)
            {
                _surfels.Release();
                _surfels = null;
            }

            if (_temporal != null)
            {
                _temporal.Release();
                _temporal = null;
            }

            if (_transparents != null)
            {
                _transparents.Release();
                _transparents = null;
            }

            if (_traces != null)
            {
                _traces.Release();
                _traces = null;
            }

            //

            if (_passes != null)
            {
                _passes.Release();
                _passes = null;
            }
        }

        private void OnPreRender()
        {
            _pre_render_buffer.Clear();
            _render_buffer.Clear();
            _apply_buffer.Clear();
            _before_post_process_buffer.Clear();
            _after_render_buffer.Clear();

            var atlas = GameObject.FindObjectOfType<SDFAtlas>();

            if (atlas != null && atlas.IsValid)
            {
                Shader.SetKeyword(GlobalKeyword.Create("PROBES_DIFFUSE_ONLY"), atlas.Debug == DebugChoice.Diffuse);

                if (_frame == null)
                    createGI();

                if (_probes.Choice != Settings.Probes)
                {
                    _probes.Choice = Settings.Probes;
                    _probes.NeedReset = true;
                }

                _frame.Update();
                _lights.Update();

                if (_transparents != null)
                    _transparents.EnsureTarget(_camera.pixelWidth, _camera.pixelHeight);

                if (atlas.Terrain != null)
                    atlas.Terrain.AutoUpdate(_camera.transform.position);

                _probes.Update(_camera.transform.position);
                _surfels.Update(_camera.transform.position);
                _gdf.Update(_camera.transform.position);
                _sdf_grid.Update(_camera.transform.position);

                if (_far_probes != null)
                    _far_probes.Update(_camera.transform.position);

                //

                var data = new PassData();
                data.Camera = _camera;
                data.Atlas = atlas;
                data.Settings = Settings;

                _passes.PassData = data;
                _passes.Run();
            }
        }

        private void OnDestroy()
        {
            destructGI();
            destructBuffers();
        }

        private void OnEnable()
        {
            createBuffers();

            _camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, _pre_render_buffer);
            _camera.AddCommandBuffer(CameraEvent.BeforeLighting, _render_buffer);
            _camera.AddCommandBuffer(CameraEvent.AfterLighting, _apply_buffer);
            _camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _before_post_process_buffer);
            _camera.AddCommandBuffer(CameraEvent.AfterImageEffects, _after_render_buffer);
        }

        private void OnDisable()
        {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, _pre_render_buffer);
            _camera.RemoveCommandBuffer(CameraEvent.BeforeLighting, _render_buffer);
            _camera.RemoveCommandBuffer(CameraEvent.AfterLighting, _apply_buffer);
            _camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _before_post_process_buffer);
            _camera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _after_render_buffer);

            destructBuffers();
        }
    }
}
