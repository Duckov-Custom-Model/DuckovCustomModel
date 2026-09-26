using System;
using UnityEngine;
using NumericsMatrix = System.Numerics.Matrix4x4;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector = System.Numerics.Vector3;

namespace DuckovCustomModel.Integrations.Ysm
{
    public static class DuckovYsmCoordinates
    {
        public const float UnitsPerPixel = 1f / 16f;

        public static Vector3 ToUnityPosition(NumericsVector value)
        {
            return new(value.X * UnitsPerPixel, value.Y * UnitsPerPixel, -value.Z * UnitsPerPixel);
        }

        public static Vector3 ToUnityDirection(NumericsVector value)
        {
            return new(value.X, value.Y, -value.Z);
        }

        public static Quaternion ToUnityRotation(NumericsQuaternion value)
        {
            return new(-value.X, -value.Y, value.Z, value.W);
        }

        public static bool TryGetUnityTransform(NumericsMatrix matrix, out Vector3 position,
            out Quaternion rotation, out Vector3 scale)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            if (!NumericsMatrix.Decompose(matrix, out var sourceScale, out var sourceRotation,
                    out var sourcePosition)) return false;
            var reconstructed = NumericsMatrix.CreateScale(sourceScale) *
                                NumericsMatrix.CreateFromQuaternion(sourceRotation) *
                                NumericsMatrix.CreateTranslation(sourcePosition);
            if (!Near(matrix.M11, reconstructed.M11) || !Near(matrix.M12, reconstructed.M12) ||
                !Near(matrix.M13, reconstructed.M13) || !Near(matrix.M14, reconstructed.M14) ||
                !Near(matrix.M21, reconstructed.M21) || !Near(matrix.M22, reconstructed.M22) ||
                !Near(matrix.M23, reconstructed.M23) || !Near(matrix.M24, reconstructed.M24) ||
                !Near(matrix.M31, reconstructed.M31) || !Near(matrix.M32, reconstructed.M32) ||
                !Near(matrix.M33, reconstructed.M33) || !Near(matrix.M34, reconstructed.M34) ||
                !Near(matrix.M41, reconstructed.M41) || !Near(matrix.M42, reconstructed.M42) ||
                !Near(matrix.M43, reconstructed.M43) || !Near(matrix.M44, reconstructed.M44)) return false;
            position = ToUnityPosition(sourcePosition);
            rotation = ToUnityRotation(sourceRotation);
            scale = new(sourceScale.X, sourceScale.Y, sourceScale.Z);
            return true;
        }

        private static bool Near(float value, float expected)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) &&
                   !float.IsNaN(expected) && !float.IsInfinity(expected) &&
                   Math.Abs(value - expected) <= 0.0001f * Math.Max(1f, Math.Abs(value));
        }
    }
}
