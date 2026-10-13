using UnityEngine;

namespace DuckovCustomModel.Core.Ik
{
    public interface IModelIkRig
    {
        Transform Root { get; }
        bool TryGetArm(bool left, out ArmIkPose pose);
        void ApplyArm(bool left, in ArmIkPose solved, float weight);
    }

    public struct ArmIkPose
    {
        public Vector3 UpperPosition;
        public Vector3 ElbowPosition;
        public Vector3 HandPosition;
        public Quaternion UpperRotation;
        public Quaternion LowerRotation;
        public Quaternion HandRotation;
    }
}
