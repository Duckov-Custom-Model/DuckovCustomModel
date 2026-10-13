using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed partial class DuckovYsmRenderBackend
    {
        internal Bounds MeasurePoseBounds()
        {
            var state = graphics;
            if (state == null) return new();
            if (state.BoneBuffer == null) return state.Mesh.bounds;
            var bounds = new Bounds();
            var initialized = false;
            for (var vertex = 0; vertex < state.Vertices.Length; vertex++)
            {
                var bone = state.Geometry.BoneIndices[vertex];
                if (state.Collapsed[bone]) continue;
                var position = GpuPosition(state.GpuBones[bone], state.Vertices[vertex]);
                if (initialized) bounds.Encapsulate(position);
                else bounds = new(position, Vector3.zero);
                initialized = true;
            }

            return bounds;
        }

        private static void InitializeGpu(GraphicsState state)
        {
            if (SystemInfo.graphicsShaderLevel < 45 || state.GpuBones.Length == 0) return;
            try
            {
                state.BoneBuffer = new(state.GpuBones.Length, 128, ComputeBufferType.Structured);
                foreach (var material in state.SolidMaterials) BindBones(material, state.BoneBuffer);
                foreach (var material in state.TransparentMaterials) BindBones(material, state.BoneBuffer);
            }
            catch (Exception ex)
            {
                state.BoneBuffer?.Release();
                state.BoneBuffer = null;
                foreach (var material in state.SolidMaterials) material.DisableKeyword("YSM_GPU_POSE");
                foreach (var material in state.TransparentMaterials) material.DisableKeyword("YSM_GPU_POSE");
                ModLogger.LogWarning("YSM GPU pose unavailable: " + ex.Message);
            }
        }

        private static void BindBones(Material material, ComputeBuffer buffer)
        {
            material.SetBuffer("_YsmBones", buffer);
            material.EnableKeyword("YSM_GPU_POSE");
        }

        private static void UploadBones(GraphicsState state)
        {
            for (var bone = 0; bone < state.GpuBones.Length; bone++)
            {
                var m = state.BoneMatrices[bone];
                var xx = (double)m.M22 * m.M33 - (double)m.M23 * m.M32;
                var xy = (double)m.M32 * m.M13 - (double)m.M33 * m.M12;
                var xz = (double)m.M12 * m.M23 - (double)m.M13 * m.M22;
                var yx = (double)m.M23 * m.M31 - (double)m.M21 * m.M33;
                var yy = (double)m.M33 * m.M11 - (double)m.M31 * m.M13;
                var yz = (double)m.M13 * m.M21 - (double)m.M11 * m.M23;
                var zx = (double)m.M21 * m.M32 - (double)m.M22 * m.M31;
                var zy = (double)m.M31 * m.M12 - (double)m.M32 * m.M11;
                var zz = (double)m.M11 * m.M22 - (double)m.M12 * m.M21;
                var magnitude = Math.Max(Math.Max(Math.Max(Math.Abs(xx), Math.Abs(xy)), Math.Abs(xz)),
                    Math.Max(Math.Max(Math.Max(Math.Abs(yx), Math.Abs(yy)), Math.Abs(yz)),
                        Math.Max(Math.Max(Math.Abs(zx), Math.Abs(zy)), Math.Abs(zz))));
                var inverse = magnitude > 0 && !double.IsInfinity(magnitude) ? 1 / magnitude : 0;
                state.GpuBones[bone] = new()
                {
                    X = new(m.M11, m.M21, -m.M31, m.M41 / 16),
                    Y = new(m.M12, m.M22, -m.M32, m.M42 / 16),
                    Z = new(-m.M13, -m.M23, m.M33, -m.M43 / 16),
                    NormalX = new((float)(xx * inverse), (float)(xy * inverse), (float)(-xz * inverse), 0),
                    NormalY = new((float)(yx * inverse), (float)(yy * inverse), (float)(-yz * inverse), 0),
                    NormalZ = new((float)(-zx * inverse), (float)(-zy * inverse), (float)(zz * inverse), 0),
                    Color = state.BoneColors[bone],
                    Parameters = new(state.BoneGlow[bone].x, 0, 0, 0),
                };
            }

            state.BoneBuffer!.SetData(state.GpuBones);
        }

        private static Vector3 GpuPosition(GpuBone bone, Vector3 position)
        {
            return new(bone.X.x * position.x + bone.X.y * position.y + bone.X.z * position.z + bone.X.w,
                bone.Y.x * position.x + bone.Y.y * position.y + bone.Y.z * position.z + bone.Y.w,
                bone.Z.x * position.x + bone.Z.y * position.y + bone.Z.z * position.z + bone.Z.w);
        }

        private static void UpdateGpuBounds(GraphicsState state)
        {
            var bounds = new Bounds();
            var initialized = false;
            for (var bone = 0; bone < state.GpuBones.Length; bone++)
            {
                if (!state.BoneHasVertices[bone] || state.Collapsed[bone]) continue;
                var matrix = state.GpuBones[bone];
                var local = state.BoneBounds[bone];
                var extents = local.extents;
                var size = new Vector3(
                    Mathf.Abs(matrix.X.x) * extents.x + Mathf.Abs(matrix.X.y) * extents.y +
                    Mathf.Abs(matrix.X.z) * extents.z,
                    Mathf.Abs(matrix.Y.x) * extents.x + Mathf.Abs(matrix.Y.y) * extents.y +
                    Mathf.Abs(matrix.Y.z) * extents.z,
                    Mathf.Abs(matrix.Z.x) * extents.x + Mathf.Abs(matrix.Z.y) * extents.y +
                    Mathf.Abs(matrix.Z.z) * extents.z);
                var transformed = new Bounds(GpuPosition(matrix, local.center), size * 2);
                if (initialized) bounds.Encapsulate(transformed);
                else bounds = transformed;
                initialized = true;
            }

            state.Mesh.bounds = bounds;
        }

        private static Vector3 TriangleCenter(GraphicsState state, int a, int b, int c)
        {
            if (state.BoneBuffer == null) return (state.Vertices[a] + state.Vertices[b] + state.Vertices[c]) / 3;
            var bones = state.Geometry.BoneIndices;
            if (bones[a] == bones[b] && bones[a] == bones[c])
                return GpuPosition(state.GpuBones[bones[a]],
                    (state.Vertices[a] + state.Vertices[b] + state.Vertices[c]) / 3);
            return (GpuPosition(state.GpuBones[bones[a]], state.Vertices[a]) +
                    GpuPosition(state.GpuBones[bones[b]], state.Vertices[b]) +
                    GpuPosition(state.GpuBones[bones[c]], state.Vertices[c])) / 3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GpuBone
        {
            internal Vector4 X, Y, Z, NormalX, NormalY, NormalZ, Color, Parameters;
        }
    }
}
