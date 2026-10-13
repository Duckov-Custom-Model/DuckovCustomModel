using UnityEngine;

namespace DuckovCustomModel.Core.Ik
{
    public static class TwoBoneIk
    {
        private const float Epsilon = 0.000001f;

        public static bool TrySolve(in ArmIkPose pose, Vector3 target, Quaternion rotation, Vector3 elbowHint,
            out ArmIkPose solved)
        {
            solved = pose;
            if (!IsValid(pose) || !IsFinite(target) || !IsFinite(elbowHint) || !IsValid(rotation)) return false;
            var upper = pose.ElbowPosition - pose.UpperPosition;
            var lower = pose.HandPosition - pose.ElbowPosition;
            var upperLength = upper.magnitude;
            var lowerLength = lower.magnitude;
            if (!IsFinite(upperLength) || !IsFinite(lowerLength) || upperLength <= Epsilon || lowerLength <= Epsilon)
                return false;

            var direction = target - pose.UpperPosition;
            var distance = direction.magnitude;
            if (!IsFinite(distance)) return false;
            if (distance <= Epsilon)
            {
                direction = pose.HandPosition - pose.UpperPosition;
                if (direction.sqrMagnitude <= Epsilon * Epsilon) direction = upper;
                direction.Normalize();
            }
            else
            {
                direction /= distance;
            }

            var maximum = upperLength + lowerLength;
            var minimum = Mathf.Abs(upperLength - lowerLength);
            distance = Mathf.Clamp(distance, Mathf.Max(minimum, maximum * Epsilon), maximum);
            var bend = Vector3.ProjectOnPlane(elbowHint - pose.UpperPosition, direction);
            if (bend.sqrMagnitude <= Epsilon * Epsilon) bend = Vector3.ProjectOnPlane(upper, direction);
            if (bend.sqrMagnitude <= Epsilon * Epsilon)
                bend = Vector3.ProjectOnPlane(Mathf.Abs(direction.y) < 0.9f ? Vector3.up : Vector3.right, direction);
            bend.Normalize();

            var along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) /
                        (2 * distance);
            var height = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            solved.ElbowPosition = pose.UpperPosition + direction * along + bend * height;
            solved.HandPosition = pose.UpperPosition + direction * distance;
            solved.UpperRotation = Quaternion.FromToRotation(upper, solved.ElbowPosition - solved.UpperPosition) *
                                   pose.UpperRotation.normalized;
            solved.LowerRotation = Quaternion.FromToRotation(lower, solved.HandPosition - solved.ElbowPosition) *
                                   pose.LowerRotation.normalized;
            solved.HandRotation = rotation.normalized;
            if (IsValid(solved)) return true;
            solved = pose;
            return false;
        }

        public static bool IsValid(in ArmIkPose pose)
        {
            return IsFinite(pose.UpperPosition) && IsFinite(pose.ElbowPosition) && IsFinite(pose.HandPosition) &&
                   IsValid(pose.UpperRotation) && IsValid(pose.LowerRotation) && IsValid(pose.HandRotation);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsValid(Quaternion value)
        {
            var length = Quaternion.Dot(value, value);
            return IsFinite(length) && length > Epsilon * Epsilon;
        }
    }
}
