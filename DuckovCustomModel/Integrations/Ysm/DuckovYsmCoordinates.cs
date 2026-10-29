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
            return new(-value.X * UnitsPerPixel, value.Y * UnitsPerPixel, -value.Z * UnitsPerPixel);
        }

        public static Vector3 ToUnityDirection(NumericsVector value)
        {
            return new(-value.X, value.Y, -value.Z);
        }

        public static Quaternion ToUnityRotation(NumericsQuaternion value)
        {
            return new(-value.X, value.Y, -value.Z, value.W);
        }

        public static bool TryGetUnityTransform(NumericsMatrix matrix, out Vector3 position,
            out Quaternion rotation, out Vector3 scale)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            if (!NumericsMatrix.Decompose(matrix, out var sourceScale, out var sourceRotation,
                    out var sourcePosition)) return false;
            if (!Finite(sourceScale.X) || !Finite(sourceScale.Y) || !Finite(sourceScale.Z) ||
                !Finite(sourceRotation.X) || !Finite(sourceRotation.Y) || !Finite(sourceRotation.Z) ||
                !Finite(sourceRotation.W) || !Finite(sourcePosition.X) || !Finite(sourcePosition.Y) ||
                !Finite(sourcePosition.Z)) return false;
            position = ToUnityPosition(sourcePosition);
            rotation = ToUnityRotation(sourceRotation);
            scale = new(sourceScale.X, sourceScale.Y, sourceScale.Z);
            return true;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
