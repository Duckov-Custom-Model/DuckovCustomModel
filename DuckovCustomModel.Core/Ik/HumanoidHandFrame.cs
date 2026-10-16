using UnityEngine;

namespace DuckovCustomModel.Core.Ik
{
    public static class HumanoidHandFrame
    {
        public static bool TryGetLocalRotation(Animator animator, bool left, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (animator == null || !animator.isHuman) return false;
            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (hand == null) return false;
            var middle =
                animator.GetBoneTransform(left
                    ? HumanBodyBones.LeftMiddleProximal
                    : HumanBodyBones.RightMiddleProximal);
            var index = animator.GetBoneTransform(left
                ? HumanBodyBones.LeftIndexProximal
                : HumanBodyBones.RightIndexProximal);
            var little =
                animator.GetBoneTransform(left
                    ? HumanBodyBones.LeftLittleProximal
                    : HumanBodyBones.RightLittleProximal);
            var forearm = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var finger = middle != null ? middle : index != null ? index : little;
            var forward = finger != null ? finger.position - hand.position
                : forearm != null ? hand.position - forearm.position : Vector3.zero;
            if (!TwoBoneIk.IsFinite(forward) || forward.sqrMagnitude < .00000001f) return false;
            var up = index != null && little != null
                ? Vector3.Cross(forward, index.position - little.position) * (left ? -1 : 1)
                : animator.transform.up;
            if (Vector3.Cross(forward, up).sqrMagnitude < .00000001f) up = animator.transform.forward;
            if (Vector3.Cross(forward, up).sqrMagnitude < .00000001f) return false;
            rotation = Quaternion.Inverse(hand.rotation) * Quaternion.LookRotation(forward, up);
            return true;
        }
    }
}
