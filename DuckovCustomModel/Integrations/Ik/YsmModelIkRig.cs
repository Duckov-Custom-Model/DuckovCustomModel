using System;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Ik;
using DuckovCustomModel.Integrations.Ysm;
using ModelRuntime;
using UnityEngine;
using NumericsVector = System.Numerics.Vector3;

namespace DuckovCustomModel.Integrations.Ik
{
    public sealed class YsmModelIkRig : IModelIkRig
    {
        private readonly ArmBinding? leftArm;
        private readonly ArmBinding? rightArm;
        private readonly ModelSimulation simulation;

        public YsmModelIkRig(Transform root, ModelSimulation simulation, ModelIkSettings settings)
        {
            Root = root;
            this.simulation = simulation;
            leftArm = Bind(settings.LeftArm);
            rightArm = Bind(settings.RightArm);
        }

        public bool HasBindings => leftArm != null || rightArm != null;

        public Transform Root { get; }

        public bool TryGetArm(bool left, out ArmIkPose pose)
        {
            pose = default;
            var arm = left ? leftArm : rightArm;
            if (!Root || arm == null) return false;
            pose = new()
            {
                UpperPosition = Position(arm.Upper), ElbowPosition = Position(arm.Lower),
                HandPosition = Position(arm.Hand), UpperRotation = Rotation(arm.Upper),
                LowerRotation = Rotation(arm.Lower), HandRotation = Rotation(arm.Hand),
            };
            return TwoBoneIk.IsValid(pose);
        }

        public void ApplyArm(bool left, in ArmIkPose solved, float weight)
        {
            var arm = left ? leftArm : rightArm;
            if (!Root || arm == null || !TwoBoneIk.IsValid(solved) || !TwoBoneIk.IsFinite(weight) ||
                weight <= 0 || !TryGetArm(left, out var current)) return;
            weight = Mathf.Clamp01(weight);
            var upper = Quaternion.Slerp(current.UpperRotation, solved.UpperRotation, weight);
            var lower = Quaternion.Slerp(current.LowerRotation, solved.LowerRotation, weight);
            var hand = Quaternion.Slerp(current.HandRotation, solved.HandRotation, weight);
            SetRotation(arm.Upper, upper);
            SetRotation(arm.Lower, lower);
            SetRotation(arm.Hand, hand);
            simulation.Pose.RebuildMatrices();
        }

        private Vector3 Position(int index)
        {
            var pivot = NumericsVector.Transform(simulation.Document.Bones[index].Pivot,
                simulation.Pose.Matrices[index]);
            return Root.TransformPoint(DuckovYsmCoordinates.ToUnityPosition(pivot));
        }

        private Quaternion Rotation(int index)
        {
            return Root.rotation * DuckovYsmCoordinates.ToUnityRotation(simulation.Pose.GetBoneModelRotation(index));
        }

        private void SetRotation(int index, Quaternion worldRotation)
        {
            var local = Quaternion.Inverse(Root.rotation) * worldRotation;
            simulation.Pose.TrySetBoneModelRotation(index,
                new(-local.x, -local.y, local.z, local.w));
        }

        private ArmBinding? Bind(ModelArmIkSettings settings)
        {
            if (settings == null) return null;
            int upper = Find(settings.UpperArmPath),
                lower = Find(settings.LowerArmPath),
                hand = Find(settings.HandPath);
            if (upper < 0 || lower < 0 || hand < 0 || upper == lower || lower == hand || upper == hand ||
                !DescendsFrom(lower, upper) || !DescendsFrom(hand, lower)) return null;
            return new(upper, lower, hand);
        }

        private int Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            var found = -1;
            for (var i = 0; i < simulation.Document.Bones.Count; i++)
            {
                var bone = simulation.Document.Bones[i];
                if (!string.Equals(bone.Name, name, StringComparison.Ordinal) &&
                    !string.Equals(bone.Id, name, StringComparison.Ordinal)) continue;
                if (found >= 0) return -1;
                found = i;
            }

            return found;
        }

        private bool DescendsFrom(int child, int ancestor)
        {
            for (var index = simulation.Document.Bones[child].Parent;
                 index >= 0;
                 index = simulation.Document.Bones[index].Parent)
                if (index == ancestor)
                    return true;
            return false;
        }

        private sealed class ArmBinding(int upper, int lower, int hand)
        {
            public int Upper { get; } = upper;
            public int Lower { get; } = lower;
            public int Hand { get; } = hand;
        }
    }
}
