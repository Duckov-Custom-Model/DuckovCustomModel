using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Ik;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Ik
{
    public sealed class TransformModelIkRig : IModelIkRig
    {
        private readonly ArmBinding? leftArm;
        private readonly ArmBinding? rightArm;

        public TransformModelIkRig(Transform root, Animator? animator, ModelIkSettings settings)
        {
            Root = root;
            leftArm = Bind(animator, settings.LeftArm, true);
            rightArm = Bind(animator, settings.RightArm, false);
        }

        public bool HasBindings => leftArm != null || rightArm != null;

        public Transform Root { get; }

        public bool TryGetArm(bool left, out ArmIkPose pose)
        {
            pose = default;
            var arm = left ? leftArm : rightArm;
            if (!Root || arm == null || !arm.IsValid) return false;
            pose = new()
            {
                UpperPosition = arm.Upper.position, ElbowPosition = arm.Lower.position,
                HandPosition = arm.Hand.position, UpperRotation = arm.Upper.rotation,
                LowerRotation = arm.Lower.rotation, HandRotation = arm.Hand.rotation,
            };
            return TwoBoneIk.IsValid(pose);
        }

        public void ApplyArm(bool left, in ArmIkPose solved, float weight)
        {
            var arm = left ? leftArm : rightArm;
            if (!Root || arm == null || !arm.IsValid || !TwoBoneIk.IsValid(solved) ||
                !TwoBoneIk.IsFinite(weight) || weight <= 0) return;
            weight = Mathf.Clamp01(weight);
            var upperParent = arm.Upper.parent ? arm.Upper.parent.rotation : Quaternion.identity;
            var lowerParent = solved.UpperRotation * Quaternion.Inverse(arm.Upper.rotation) * arm.Lower.parent.rotation;
            var handParent = solved.LowerRotation * Quaternion.Inverse(arm.Lower.rotation) * arm.Hand.parent.rotation;
            var upperLocal = Quaternion.Inverse(upperParent) * solved.UpperRotation;
            var lowerLocal = Quaternion.Inverse(lowerParent) * solved.LowerRotation;
            var handLocal = Quaternion.Inverse(handParent) * solved.HandRotation;
            arm.Upper.localRotation = Quaternion.Slerp(arm.Upper.localRotation, upperLocal, weight);
            arm.Lower.localRotation = Quaternion.Slerp(arm.Lower.localRotation, lowerLocal, weight);
            arm.Hand.localRotation = Quaternion.Slerp(arm.Hand.localRotation, handLocal, weight);
        }

        private ArmBinding? Bind(Animator? animator, ModelArmIkSettings settings, bool left)
        {
            if (!Root || settings == null) return null;
            var upper = FindBone(animator, settings.UpperArmPath,
                left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            var lower = FindBone(animator, settings.LowerArmPath,
                left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var hand = FindBone(animator, settings.HandPath,
                left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (upper == null || lower == null || hand == null || upper == lower || lower == hand || upper == hand ||
                !upper.IsChildOf(Root) || !lower.IsChildOf(upper) || !hand.IsChildOf(lower)) return null;
            if ((lower.position - upper.position).sqrMagnitude <= 0.000000000001f ||
                (hand.position - lower.position).sqrMagnitude <= 0.000000000001f) return null;
            return new(upper, lower, hand);
        }

        private Transform? FindBone(Animator? animator, string path, HumanBodyBones bone)
        {
            if (!string.IsNullOrEmpty(path)) return Root.Find(path);
            return animator != null && animator.isHuman && animator.avatar && animator.avatar.isValid
                ? animator.GetBoneTransform(bone)
                : null;
        }

        private sealed class ArmBinding(Transform upper, Transform lower, Transform hand)
        {
            public Transform Upper { get; } = upper;
            public Transform Lower { get; } = lower;
            public Transform Hand { get; } = hand;
            public bool IsValid => Upper && Lower && Hand && Lower.IsChildOf(Upper) && Hand.IsChildOf(Lower);
        }
    }
}
