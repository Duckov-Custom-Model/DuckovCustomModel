using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal static class VrmRenderPipeline
    {
        private static readonly FieldInfo? Features =
            typeof(ScriptableRenderer).GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Dictionary<ScriptableRenderer, List<ScriptableRendererFeature>> Installed = [];
        private static int _instances;

        internal static void Register()
        {
            if (_instances++ == 0) RenderPipelineManager.beginCameraRendering += PrepareCamera;
        }

        internal static void Unregister()
        {
            if (--_instances != 0) return;
            RenderPipelineManager.beginCameraRendering -= PrepareCamera;
            foreach (var pair in Installed)
            {
                var list = Features?.GetValue(pair.Key) as List<ScriptableRendererFeature>;
                foreach (var feature in pair.Value)
                {
                    list?.Remove(feature);
                    feature.Dispose();
                    Object.Destroy(feature);
                }
            }

            Installed.Clear();
        }

        private static void PrepareCamera(ScriptableRenderContext context, Camera camera)
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline) return;
            var renderer = camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)
                ? data.scriptableRenderer
                : pipeline.scriptableRenderer;
            if (Installed.ContainsKey(renderer)) return;
            if (Features?.GetValue(renderer) is not List<ScriptableRendererFeature> features) return;
            var owned = new List<ScriptableRendererFeature>();
            Installed.Add(renderer, owned);
            try
            {
                var assembly = typeof(ScriptableRenderer).Assembly;
                var type = assembly.GetType("UnityEngine.Rendering.Universal.RenderObjects") ??
                           assembly.GetType("UnityEngine.Experimental.Rendering.Universal.RenderObjects") ??
                           throw new NotSupportedException("The native URP RenderObjects feature is unavailable.");
                for (var queue = 0; queue < 2; queue++)
                {
                    var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
                    owned.Add(feature);
                    feature.name = "DCM VRM Outline " + queue;
                    var settings = type.GetField("settings")!.GetValue(feature);
                    Set(settings, "passTag", feature.name);
                    Set(settings, "Event",
                        queue == 0
                            ? RenderPassEvent.AfterRenderingOpaques
                            : RenderPassEvent.AfterRenderingTransparents);
                    var filter = settings.GetType().GetField("filterSettings")!.GetValue(settings);
                    Set(filter, "PassNames", new[] { "MToonOutline" });
                    Set(filter, "LayerMask", (LayerMask)(-1));
                    var queueField = filter.GetType().GetField("RenderQueueType")!;
                    queueField.SetValue(filter, Enum.ToObject(queueField.FieldType, queue));
                    feature.Create();
                    features.Add(feature);
                }

            }
            catch (Exception ex)
            {
                foreach (var feature in owned)
                {
                    features.Remove(feature);
                    feature.Dispose();
                    Object.Destroy(feature);
                }

                owned.Clear();
                ModLogger.LogWarning("VRM outline pass could not be initialized: " + ex.Message);
            }

            var depth = ScriptableObject.CreateInstance<VrmTransparentDepthFeature>();
            try
            {
                depth.name = "DCM VRM Transparent Depth";
                depth.Create();
                depth.Initialize(renderer);
                features.Add(depth);
                owned.Add(depth);
            }
            catch (Exception ex)
            {
                depth.Dispose();
                Object.Destroy(depth);
                ModLogger.LogWarning("VRM transparent depth pass could not be initialized: " + ex.Message);
            }
        }

        private static void Set(object instance, string name, object value)
        {
            instance.GetType().GetField(name)!.SetValue(instance, value);
        }
    }
}
