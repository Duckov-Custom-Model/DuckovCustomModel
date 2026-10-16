using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal sealed class VrmTransparentDepthFeature : ScriptableRendererFeature
    {
        private const RenderPassEvent DepthEvent =
            (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingTransparents - 1);

        private static readonly int CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        private static readonly int ZWrite = Shader.PropertyToID("_M_ZWrite");
        private static readonly Dictionary<Renderer[], List<Surface>> Models = [];
        private readonly List<Surface> _visible = [];
        private readonly Plane[] _planes = new Plane[6];
        private readonly object[] _copyTargets = new object[2];
        private ScriptableRenderPass? _copy;
        private MethodInfo? _setup;
        private FieldInfo? _originalDepth;
        private RTHandle? _depth;
        private DepthPass? _pass;

        internal static void Register(Renderer[] renderers)
        {
            var surfaces = new List<Surface>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null || material.shader.name != "VRM10/Universal Render Pipeline/MToon10" ||
                        material.renderQueue <= 2500 || !material.HasProperty(ZWrite) ||
                        material.GetFloat(ZWrite) < .5f) continue;
                    var pass = material.FindPass("DepthOnly");
                    if (pass >= 0) surfaces.Add(new Surface(renderer, material, index, pass));
                }
            }

            Models[renderers] = surfaces;
        }

        internal static void Unregister(Renderer[] renderers) => Models.Remove(renderers);

        public override void Create()
        {
            _pass = new DepthPass(_visible) { renderPassEvent = DepthEvent };
            _pass.ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        internal void Initialize(ScriptableRenderer renderer)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var type = renderer.GetType();
            _originalDepth = type.GetField("m_DepthTexture", flags) ??
                             throw new NotSupportedException("The URP camera depth texture is unavailable.");
            var material = type.GetField("m_CopyDepthMaterial", flags)?.GetValue(renderer) as Material ??
                           throw new NotSupportedException("The URP depth copy material is unavailable.");
            var copyType = typeof(ScriptableRenderer).Assembly.GetType(
                "UnityEngine.Rendering.Universal.Internal.CopyDepthPass", true)!;
            foreach (var constructor in copyType.GetConstructors())
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length < 2 || parameters[0].ParameterType != typeof(RenderPassEvent) ||
                    parameters[1].ParameterType != typeof(Material)) continue;
                var arguments = new object[parameters.Length];
                arguments[0] = DepthEvent;
                arguments[1] = material;
                for (var index = 2; index < parameters.Length; index++)
                    arguments[index] = parameters[index].Name == "copyToDepth" ||
                                       (parameters[index].HasDefaultValue &&
                                        parameters[index].DefaultValue is true);
                _copy = (ScriptableRenderPass)constructor.Invoke(arguments);
                _setup = copyType.GetMethod("Setup", [typeof(RTHandle), typeof(RTHandle)]);
                if (_setup != null) return;
            }

            throw new NotSupportedException("The URP depth copy pass is unavailable.");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _visible.Clear();
            var camera = renderingData.cameraData.camera;
            if (_copy == null || _pass == null || camera.cameraType == CameraType.Preview) return;
            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            foreach (var surfaces in Models.Values)
            foreach (var surface in surfaces)
            {
                var target = surface.Renderer;
                if (target == null || !target.enabled || target.forceRenderingOff ||
                    target.shadowCastingMode == ShadowCastingMode.ShadowsOnly ||
                    !target.gameObject.activeInHierarchy ||
                    (camera.cullingMask & (1 << target.gameObject.layer)) == 0 ||
                    !GeometryUtility.TestPlanesAABB(_planes, target.bounds)) continue;
                _visible.Add(surface);
            }

            if (_visible.Count == 0) return;
            renderer.EnqueuePass(_copy);
            renderer.EnqueuePass(_pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (_visible.Count == 0 || _copy == null || _pass == null) return;
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthBufferBits = 32;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            RenderingUtils.ReAllocateIfNeeded(ref _depth, descriptor, FilterMode.Point,
                TextureWrapMode.Clamp, name: "_DcmVrmDepthTexture");
            _copyTargets[0] = renderer.cameraDepthTargetHandle;
            _copyTargets[1] = _depth!;
            _setup!.Invoke(_copy, _copyTargets);
            _pass.Setup(_depth!, _originalDepth!.GetValue(renderer) as RTHandle);
        }

        protected override void Dispose(bool disposing)
        {
            _depth?.Release();
            _depth = null;
            _visible.Clear();
        }

        private readonly struct Surface(Renderer renderer, Material material, int submesh, int pass)
        {
            internal readonly Renderer Renderer = renderer;
            internal readonly Material Material = material;
            internal readonly int Submesh = submesh;
            internal readonly int Pass = pass;
        }

        private sealed class DepthPass(List<Surface> surfaces) : ScriptableRenderPass
        {
            private RTHandle? _target;
            private RTHandle? _original;

            internal void Setup(RTHandle target, RTHandle? original)
            {
                _target = target;
                _original = original;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ConfigureTarget(_target!);
                ConfigureClear(ClearFlag.None, Color.clear);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("DCM VRM Transparent Depth");
                foreach (var surface in surfaces)
                    cmd.DrawRenderer(surface.Renderer, surface.Material, surface.Submesh, surface.Pass);
                cmd.SetGlobalTexture(CameraDepthTexture, _target!.nameID);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (_original != null) cmd.SetGlobalTexture(CameraDepthTexture, _original.nameID);
            }
        }
    }
}
