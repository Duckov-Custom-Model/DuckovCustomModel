using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed partial class DuckovYsmRenderBackend
    {
        private static readonly List<DuckovYsmRenderBackend> Instances = new();

        private static readonly FieldInfo? RendererFeatures = typeof(ScriptableRenderer).GetField(
            "m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool lightingLogged;

        private static void RegisterPipeline(DuckovYsmRenderBackend backend)
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("YSM rendering requires the game's Universal Render Pipeline.");
            if (Instances.Count == 0) RenderPipelineManager.beginCameraRendering += PrepareCamera;
            Instances.Add(backend);
        }

        private static void UnregisterPipeline(DuckovYsmRenderBackend backend)
        {
            Instances.Remove(backend);
            if (Instances.Count == 0) RenderPipelineManager.beginCameraRendering -= PrepareCamera;
        }

        private static void PrepareCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)) return;
            float mode = 0;
            var renderShadows = !camera.TryGetComponent<UniversalAdditionalCameraData>(out var data) ||
                                data.renderShadows;
            if (pipeline.supportsMainLightShadows && renderShadows && camera.name != "YSM thumbnail camera")
            {
                mode = pipeline.shadowCascadeCount > 1 ? 2 : 1;
                var renderer = data ? data.scriptableRenderer : pipeline.scriptableRenderer;
                if (RendererFeatures?.GetValue(renderer) is List<ScriptableRendererFeature> features)
                    foreach (var feature in features)
                        if (feature && feature.isActive && feature.GetType().Name == "ScreenSpaceShadows")
                        {
                            mode = 3;
                            break;
                        }
            }

            Shader.SetGlobalFloat("_YsmShadowMode", mode);
            var sun = RenderSettings.sun;
            if (sun && sun.isActiveAndEnabled && sun.type == LightType.Directional)
            {
                var color = sun.color;
                Shader.SetGlobalVector("_YsmSunColor", new(color.r * sun.intensity, color.g * sun.intensity,
                    color.b * sun.intensity, 1));
                var direction = -sun.transform.forward;
                Shader.SetGlobalVector("_YsmSunDirection", new(direction.x, direction.y, direction.z, 0));
            }
            else
            {
                Shader.SetGlobalVector("_YsmSunColor", Vector4.zero);
            }

            if (!lightingLogged && camera.name == "Main Camera")
            {
                lightingLogged = true;
                ModLogger.Log($"YSM lighting: shadowMode={mode}, cascades={pipeline.shadowCascadeCount}, " +
                              $"mainShadows={pipeline.supportsMainLightShadows}, cameraShadows={renderShadows}, " +
                              $"ambientMode={RenderSettings.ambientMode}, sun={(sun ? sun.name : "none")}");
            }

            var frustum = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var instance in Instances) instance.SortTransparency(camera, frustum);
        }

        private void SortTransparency(Camera camera, Plane[] frustum)
        {
            var state = graphics;
            if (disposed || state == null || !Root || !Root.gameObject.activeInHierarchy ||
                !state.Object.activeInHierarchy || !state.Renderer || !state.Renderer.enabled ||
                state.Renderer.forceRenderingOff || (camera.cullingMask & (1 << state.Object.layer)) == 0) return;
            var mesh = state.TransparentMesh;
            if (state.TransparentCount == 0)
            {
                state.TransparentRenderer.enabled = false;
                return;
            }

            if (!GeometryUtility.TestPlanesAABB(frustum, state.Renderer.bounds)) return;
            var world = state.Object.transform.localToWorldMatrix;
            var position = camera.transform.position;
            var forward = camera.transform.forward;
            var count = state.TransparentCount;
            for (var i = 0; i < count; i++)
            {
                var entry = state.Order[i];
                var indices = state.Geometry.Partitions[entry.Partition].Indices;
                var center = world.MultiplyPoint3x4((state.Vertices[indices[entry.Triangle]] +
                                                     state.Vertices[indices[entry.Triangle + 1]] +
                                                     state.Vertices[indices[entry.Triangle + 2]]) / 3);
                entry.Depth = ((double)center.x - position.x) * forward.x +
                              ((double)center.y - position.y) * forward.y + ((double)center.z - position.z) * forward.z;
                state.Order[i] = entry;
            }

            Array.Sort(state.Order, 0, count, FarFirst.Instance);
            state.Runs.Clear();
            for (var i = 0; i < count; i++)
            {
                var entry = state.Order[i];
                var indices = state.Geometry.Partitions[entry.Partition].Indices;
                for (var v = 0; v < 3; v++) state.SortedIndices[i * 3 + v] = indices[entry.Triangle + 2 - v];
                if (state.Runs.Count == 0 || state.Runs[state.Runs.Count - 1].Partition != entry.Partition)
                {
                    state.Runs.Add(new() { Start = i * 3, Count = 3, Partition = entry.Partition });
                }
                else
                {
                    var last = state.Runs.Count - 1;
                    var run = state.Runs[last];
                    run.Count += 3;
                    state.Runs[last] = run;
                }
            }

            var flags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
            if (!state.TransparentIndicesInitialized)
            {
                mesh.SetIndexBufferParams(state.SortedIndices.Length, IndexFormat.UInt32);
                state.TransparentIndicesInitialized = true;
            }

            mesh.SetIndexBufferData(state.SortedIndices, 0, 0, count * 3, flags);
            mesh.subMeshCount = state.Runs.Count;
            var materials = state.RunMaterials;
            var materialsChanged = materials.Length != state.Runs.Count;
            if (materialsChanged) materials = new Material[state.Runs.Count];
            for (var i = 0; i < state.Runs.Count; i++)
            {
                var run = state.Runs[i];
                mesh.SetSubMesh(i, new(run.Start, run.Count)
                    { bounds = mesh.bounds, firstVertex = 0, vertexCount = mesh.vertexCount }, flags);
                if (materials[i] != state.TransparentMaterials[run.Partition]) materialsChanged = true;
                materials[i] = state.TransparentMaterials[run.Partition];
            }

            if (materialsChanged)
            {
                state.RunMaterials = materials;
                state.TransparentRenderer.sharedMaterials = materials;
            }

            state.TransparentRenderer.enabled = true;
        }

        internal void RenderThumbnail(Camera camera, RenderTexture target, CommandBuffer commands)
        {
            var state = graphics;
            if (state == null) return;
            SortTransparency(camera, GeometryUtility.CalculateFrustumPlanes(camera));
            commands.Clear();
            commands.SetRenderTarget(target);
            commands.ClearRenderTarget(true, true, camera.backgroundColor);
            commands.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            var matrix = state.Object.transform.localToWorldMatrix;
            for (var i = 0; i < state.SolidMaterials.Length; i++)
                commands.DrawMesh(state.Mesh, matrix, state.SolidMaterials[i], i, 0);
            if (state.TransparentCount > 0 && state.TransparentRenderer.enabled)
                for (var i = 0; i < state.RunMaterials.Length; i++)
                    commands.DrawMesh(state.TransparentMesh, matrix, state.RunMaterials[i], i, 0);
            Graphics.ExecuteCommandBuffer(commands);
        }

        private struct TriangleDepth
        {
            internal double Depth;
            internal int Triangle, Partition;
        }

        private struct TransparentRun
        {
            internal int Start, Count, Partition;
        }

        private sealed class FarFirst : IComparer<TriangleDepth>
        {
            internal static readonly FarFirst Instance = new();

            public int Compare(TriangleDepth a, TriangleDepth b)
            {
                var result = b.Depth.CompareTo(a.Depth);
                if (result != 0) return result;
                result = a.Partition.CompareTo(b.Partition);
                return result != 0 ? result : a.Triangle.CompareTo(b.Triangle);
            }
        }
    }
}
