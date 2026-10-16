using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomModel.Core.Animations;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Ik;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Motions
{
    internal sealed class VmdPose : IMotionSampler
    {
        private readonly VmdMotion _motion;
        private readonly Animator _animator;

        private readonly Dictionary<string, HumanBodyBones> _map = new(StringComparer.Ordinal)
        {
            ["下半身"] = HumanBodyBones.Hips, ["上半身"] = HumanBodyBones.Spine,
            ["上半身2"] = HumanBodyBones.Chest, ["上半身３"] = HumanBodyBones.UpperChest,
            ["首"] = HumanBodyBones.Neck, ["頭"] = HumanBodyBones.Head,
            ["左肩"] = HumanBodyBones.LeftShoulder, ["右肩"] = HumanBodyBones.RightShoulder,
            ["左腕"] = HumanBodyBones.LeftUpperArm, ["右腕"] = HumanBodyBones.RightUpperArm,
            ["左ひじ"] = HumanBodyBones.LeftLowerArm, ["右ひじ"] = HumanBodyBones.RightLowerArm,
            ["左手首"] = HumanBodyBones.LeftHand, ["右手首"] = HumanBodyBones.RightHand,
            ["左足"] = HumanBodyBones.LeftUpperLeg, ["右足"] = HumanBodyBones.RightUpperLeg,
            ["左ひざ"] = HumanBodyBones.LeftLowerLeg, ["右ひざ"] = HumanBodyBones.RightLowerLeg,
            ["左足首"] = HumanBodyBones.LeftFoot, ["右足首"] = HumanBodyBones.RightFoot,
            ["左つま先"] = HumanBodyBones.LeftToes, ["右つま先"] = HumanBodyBones.RightToes,
            ["左目"] = HumanBodyBones.LeftEye, ["右目"] = HumanBodyBones.RightEye
        };

        private readonly Dictionary<Transform, Quaternion> _worldRest = new();
        private readonly Dictionary<Transform, Matrix4x4> _restMatrices = new();
        private readonly Dictionary<string, SkeletonBone> _rest = new(StringComparer.Ordinal);
        private readonly List<(VmdMotion.BoneKey[] track, HumanBodyBones id, Transform bone)> _tracks = [];
        private readonly MotionExpressions _expressions;
        private readonly float _scale;
        private readonly bool _hasRootMotion;
        public bool ControlsBody { get; }
        private readonly Dictionary<HumanBodyBones, Vector3> _feet = new();
        public double Duration => Math.Max(_motion.Duration, 1.0 / 30);

        internal VmdPose(VmdMotion motion, Animator animator, MotionPlaybackSettings settings)
        {
            _motion = motion;
            _hasRootMotion = motion.Bones.ContainsKey("センター") || motion.Bones.ContainsKey("グルーブ") ||
                             motion.Bones.ContainsKey("全ての親") || motion.Bones.ContainsKey("下半身");
            _animator = animator;
            _scale = animator.humanScale / 12.5f;
            _expressions = new MotionExpressions(animator, settings.ExpressionMappings);
            foreach (var bone in animator.avatar.humanDescription.skeleton) _rest[bone.name] = bone;
            foreach (var side in new[] { "左", "右" })
            foreach (var finger in new[]
                         { ("親指", "Thumb"), ("人指", "Index"), ("中指", "Middle"), ("薬指", "Ring"), ("小指", "Little") })
                for (var i = 0; i < 3; i++)
                    if (Enum.TryParse((side == "左" ? "Left" : "Right") + finger.Item2 +
                                      new[] { "Proximal", "Intermediate", "Distal" }[i], out HumanBodyBones bone))
                    {
                        var number = finger.Item2 == "Thumb" ? i : i + 1;
                        _map[side + finger.Item1 + (char)('０' + number)] = bone;
                    }

            foreach (var pair in settings.BoneMappings)
                if (Enum.TryParse(pair.Value, true, out HumanBodyBones bone) && bone < HumanBodyBones.LastBone)
                    _map[pair.Key] = bone;
            foreach (var pair in motion.Bones)
                if (_map.TryGetValue(pair.Key, out var id) && animator.GetBoneTransform(id) is { } bone)
                {
                    RestWorld(bone);
                    _tracks.Add((pair.Value, id, bone));
                }

            if (_hasRootMotion && !_tracks.Exists(entry => entry.id == HumanBodyBones.Hips) &&
                animator.GetBoneTransform(HumanBodyBones.Hips) is { } hips && hips != animator.transform)
                _tracks.Add((new[]
                {
                    new VmdMotion.BoneKey(0, System.Numerics.Vector3.Zero,
                        System.Numerics.Quaternion.Identity, new byte[16])
                }, HumanBodyBones.Hips, hips));
            foreach (var id in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
                if (animator.GetBoneTransform(id) is { } foot)
                    _feet[id] = RestPosition(foot);
            ControlsBody = _hasRootMotion || _tracks.Count > 0 || _motion.Bones.ContainsKey("左足ＩＫ") ||
                           _motion.Bones.ContainsKey("右足ＩＫ") || _motion.Bones.ContainsKey("左足IK") ||
                           _motion.Bones.ContainsKey("右足IK");
            if (_tracks.Count == 0 && motion.Morphs.Count == 0)
                throw new InvalidDataException("VMD has no compatible humanoid tracks.");
        }

        public void Apply(double seconds, float weight, MotionPlaybackSettings settings)
        {
            var expressionWeight = weight;
            weight = 1;
            var axis = Quaternion.Euler(0, settings.RotationY, 0);
            var rootRotation = Rotation("全ての親", seconds, axis) * Rotation("センター", seconds, axis) *
                               Rotation("グルーブ", seconds, axis);
            var lowerRotation = Rotation("下半身", seconds, axis);
            foreach (var (track, id, bone) in _tracks)
            {
                if (settings.UpperBodyOnly && (id == HumanBodyBones.Hips || IsLeg(id))) continue;
                VmdMotion.Sample(track, seconds, out _, out var sample);
                var rotation = axis * new Quaternion(sample.X, sample.Y, sample.Z, sample.W) * Quaternion.Inverse(axis);
                if (id == HumanBodyBones.Hips) rotation = rootRotation * rotation;
                if (id == HumanBodyBones.Spine) rotation = Quaternion.Inverse(lowerRotation) * rotation;
                if (id == HumanBodyBones.LeftUpperArm)
                    rotation *= Quaternion.AngleAxis(settings.ArmAngle, Vector3.forward);
                if (id == HumanBodyBones.RightUpperArm)
                    rotation *= Quaternion.AngleAxis(-settings.ArmAngle, Vector3.forward);
                var parent = RestWorld(bone.parent);
                var local = _rest.TryGetValue(bone.name, out var rest) ? rest.rotation : bone.localRotation;
                var target = Quaternion.Inverse(parent) * rotation * parent * local;
                bone.localRotation = Quaternion.Slerp(bone.localRotation, target, weight);
            }

            if (_hasRootMotion && !settings.UpperBodyOnly &&
                _animator.GetBoneTransform(HumanBodyBones.Hips) is { } hips &&
                hips != _animator.transform)
            {
                var position = Position("センター", seconds, axis) + Position("グルーブ", seconds, axis);
                var rootPosition = Position("全ての親", seconds, axis);
                if (settings.InPlace) rootPosition = Vector3.up * rootPosition.y;
                position = (position + rootPosition) * (_scale * settings.PositionScale);
                var baseline = _rest.TryGetValue(hips.name, out var rest) ? rest.position : hips.localPosition;
                var offset = hips.parent.InverseTransformVector(_animator.transform.TransformVector(position));
                if (!TwoBoneIk.IsFinite(baseline + offset))
                    throw new InvalidDataException("VMD position is outside the supported range.");
                hips.localPosition = Vector3.Lerp(hips.localPosition, baseline + offset, weight);
            }

            if (!settings.UpperBodyOnly)
            {
                ApplyLeg(true, seconds, weight, settings, axis);
                ApplyLeg(false, seconds, weight, settings, axis);
            }

            foreach (var pair in _motion.Morphs)
                _expressions.Set(pair.Key, VmdMotion.Sample(pair.Value, seconds), expressionWeight);
        }

        private void ApplyLeg(bool left, double seconds, float weight, MotionPlaybackSettings settings, Quaternion axis)
        {
            var name = left ? "左足ＩＫ" : "右足ＩＫ";
            if (!_motion.Bones.TryGetValue(name, out var track))
            {
                name = left ? "左足IK" : "右足IK";
                if (!_motion.Bones.TryGetValue(name, out track)) return;
            }

            if (_motion.Ik.TryGetValue(name, out var enabled) && !VmdMotion.Sample(enabled, seconds)) return;
            var upper = _animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            var lower = _animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            var footId = left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;
            var foot = _animator.GetBoneTransform(footId);
            if (upper == null || lower == null || foot == null || !_feet.TryGetValue(footId, out var rest)) return;
            VmdMotion.Sample(track, seconds, out var position, out var rotation);
            var delta = axis * new Vector3(position.X, position.Y, position.Z) * (_scale * settings.PositionScale);
            var rootPosition = Position("全ての親", seconds, axis);
            if (settings.InPlace) rootPosition = Vector3.up * rootPosition.y;
            var target = _animator.transform.TransformPoint(Rotation("全ての親", seconds, axis) * (rest + delta) +
                                                            rootPosition * (_scale * settings.PositionScale));
            var turn = axis * new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W) * Quaternion.Inverse(axis);
            var pose = new ArmIkPose
            {
                UpperPosition = upper.position, ElbowPosition = lower.position, HandPosition = foot.position,
                UpperRotation = upper.rotation, LowerRotation = lower.rotation, HandRotation = foot.rotation
            };
            var hint = upper.position + _animator.transform.forward * (upper.position - lower.position).magnitude;
            if (!TwoBoneIk.TrySolve(pose, target, _animator.transform.rotation * turn * RestWorld(foot), hint,
                    out var solved)) return;
            upper.rotation = Quaternion.Slerp(upper.rotation, solved.UpperRotation, weight);
            lower.rotation = Quaternion.Slerp(lower.rotation, solved.LowerRotation, weight);
            foot.rotation = Quaternion.Slerp(foot.rotation, solved.HandRotation, weight);
        }

        private Quaternion Rotation(string name, double seconds, Quaternion axis)
        {
            if (!_motion.Bones.TryGetValue(name, out var track)) return Quaternion.identity;
            VmdMotion.Sample(track, seconds, out _, out var rotation);
            return axis * new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W) * Quaternion.Inverse(axis);
        }

        private Vector3 Position(string name, double seconds, Quaternion axis)
        {
            if (!_motion.Bones.TryGetValue(name, out var track)) return Vector3.zero;
            VmdMotion.Sample(track, seconds, out var position, out _);
            return axis * new Vector3(position.X, position.Y, position.Z);
        }

        private Quaternion RestWorld(Transform? bone)
        {
            if (bone == null || bone == _animator.transform) return Quaternion.identity;
            if (_worldRest.TryGetValue(bone, out var value)) return value;
            var local = _rest.TryGetValue(bone.name, out var rest) ? rest.rotation : bone.localRotation;
            return _worldRest[bone] = RestWorld(bone.parent) * local;
        }

        private Matrix4x4 RestMatrix(Transform? bone)
        {
            if (bone == null || bone == _animator.transform) return Matrix4x4.identity;
            if (_restMatrices.TryGetValue(bone, out var matrix)) return matrix;
            var local = _rest.TryGetValue(bone.name, out var rest)
                ? Matrix4x4.TRS(rest.position, rest.rotation, rest.scale)
                : Matrix4x4.TRS(bone.localPosition, bone.localRotation, bone.localScale);
            return _restMatrices[bone] = RestMatrix(bone.parent) * local;
        }

        private Vector3 RestPosition(Transform bone) => RestMatrix(bone).MultiplyPoint3x4(Vector3.zero);

        internal static bool IsLeg(HumanBodyBones bone) => bone is HumanBodyBones.LeftUpperLeg
            or HumanBodyBones.RightUpperLeg
            or HumanBodyBones.LeftLowerLeg or HumanBodyBones.RightLowerLeg or HumanBodyBones.LeftFoot
            or HumanBodyBones.RightFoot or HumanBodyBones.LeftToes or HumanBodyBones.RightToes;

        public void Restore() => _expressions.Restore();
        public void Dispose() => Restore();
    }
}
