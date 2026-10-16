using System;
using System.Collections.Generic;
using UniGLTF.SpringBoneJobs;
using UniGLTF.SpringBoneJobs.Blittables;
using UniGLTF.SpringBoneJobs.InputPorts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Core.MonoBehaviours.Physics
{
    internal sealed class ModelSpringSimulation : IDisposable
    {
        private readonly ModelSpringBone _owner;
        private readonly Dictionary<Transform, Quaternion> _pose = new();
        private readonly List<GameObject> _tips = new();
        private FastSpringBoneBuffer? _buffer;
        private FastSpringBoneBufferCombiner? _combiner;
        private FastSpringBoneScheduler? _scheduler;
        private Vector3 _scale, _position;

        public ModelSpringSimulation(ModelSpringBone owner)
        {
            _owner = owner;
            try
            {
                Build();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Build()
        {
            var used = new HashSet<Transform>();
            var paths = new List<List<Transform>>();
            var center = _owner.Center;
            if (center != null && center != _owner.transform && !center.IsChildOf(_owner.transform))
                throw new InvalidOperationException("The center must belong to this model.");
            foreach (var chain in _owner.Chains)
            {
                var bones = new List<Transform>();
                if (!ModelSpringBone.TryGetBones(chain, _owner.transform, bones, out var error))
                    throw new InvalidOperationException(error);
                foreach (var bone in bones)
                {
                    if (!used.Add(bone))
                        throw new InvalidOperationException($"Bone {bone.name} is used by multiple chains.");
                    if (bone.GetComponentsInParent<ModelSpringBone>(true).Length > 1)
                        throw new InvalidOperationException(
                            $"Bone {bone.name} is managed by nested Spring Bones components.");
                    _pose.Add(bone, _owner.DefaultRotation(bone));
                }

                paths.Add(bones);
            }

            foreach (var path in paths)
            foreach (var bone in path)
                for (var parent = bone.parent; parent != null && parent != _owner.transform; parent = parent.parent)
                    if (used.Contains(parent) && !path.Contains(parent))
                        throw new InvalidOperationException("Connected simulated bones must belong to the same chain.");

            if (center != null)
                foreach (var bone in used)
                    if (center == bone || center.IsChildOf(bone))
                        throw new InvalidOperationException("The center cannot be a simulated bone or its descendant.");
            var colliders = new List<FastSpringBoneCollider>();
            foreach (var collider in _owner.Colliders)
            {
                if (collider == null || !collider.isActiveAndEnabled) continue;
                if (!collider.transform.IsChildOf(_owner.transform) && collider.transform != _owner.transform)
                    throw new InvalidOperationException("Colliders must belong to this model.");
                colliders.Add(new FastSpringBoneCollider
                {
                    Transform = collider.transform,
                    Collider = new BlittableCollider(collider.Offset, Mathf.Max(0, collider.Radius), collider.Tail,
                        collider.Shape == ModelSpringCollider.ColliderShape.Capsule
                            ? BlittableColliderType.Capsule
                            : BlittableColliderType.Sphere),
                });
            }

            var springs = new FastSpringBoneSpring[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                var chain = _owner.Chains[i];
                var bones = paths[i];
                var last = bones[bones.Count - 1];
                var tip = new GameObject("DCM Spring Tail") { hideFlags = HideFlags.HideAndDontSave };
                _tips.Add(tip);
                tip.transform.SetParent(last, false);
                var offset = chain.EndOffset;
                if (offset.sqrMagnitude < .00000001f)
                    offset = bones.Count > 1
                        ? last.InverseTransformVector(last.position - bones[bones.Count - 2].position)
                        : Vector3.down * .07f;
                tip.transform.localPosition = offset;
                var joints = new FastSpringBoneJoint[bones.Count + 1];
                for (var j = 0; j < joints.Length; j++)
                {
                    var bone = j < bones.Count ? bones[j] : tip.transform;
                    joints[j] = new FastSpringBoneJoint
                    {
                        Transform = bone,
                        DefaultLocalRotation = j < bones.Count ? _owner.DefaultRotation(bone) : Quaternion.identity,
                        Joint = new BlittableJointMutable(Mathf.Max(0, chain.Stiffness), Mathf.Max(0, chain.Gravity),
                            chain.GravityDirection.normalized, Mathf.Clamp01(chain.Drag), Mathf.Max(0, chain.Radius)),
                    };
                }

                springs[i] = new FastSpringBoneSpring
                    { center = center, joints = joints, colliders = colliders.ToArray() };
            }

            _buffer = new FastSpringBoneBuffer(_owner.transform, springs);
            _combiner = new FastSpringBoneBufferCombiner();
            _scheduler = new FastSpringBoneScheduler(_combiner);
            _combiner.Register(_buffer, null);
            _combiner.ReconstructIfDirty(default).Complete();
            _combiner.Combined.SetModelLevel(_owner.transform, new BlittableModelLevel(supportsScalingAtRuntime: true));
            _scale = _owner.transform.lossyScale;
            _position = _owner.transform.position;
        }

        public void Process(float deltaTime)
        {
            var scale = _owner.transform.lossyScale;
            var position = _owner.transform.position;
            if (float.IsNaN(scale.sqrMagnitude) || float.IsInfinity(scale.sqrMagnitude) ||
                Mathf.Abs(scale.x * scale.y * scale.z) < .000000001f) return;
            if (deltaTime > .1f || (scale - _scale).sqrMagnitude > .00000001f * Mathf.Max(1, _scale.sqrMagnitude) ||
                (position - _position).sqrMagnitude > Mathf.Pow(Mathf.Max(.01f, _owner.TeleportDistance), 2))
            {
                Dispose();
                Build();
            }

            _position = position;
            _scheduler!.Schedule(Mathf.Min(deltaTime, 1f / 30)).Complete();
        }

        public void Dispose()
        {
            if (_scheduler != null) _scheduler.Dispose();
            else _combiner?.Dispose();
            _scheduler = null;
            _combiner = null;
            _buffer?.Dispose();
            _buffer = null;
            foreach (var pair in _pose)
                if (pair.Key != null)
                    pair.Key.localRotation = pair.Value;
            _pose.Clear();
            foreach (var tip in _tips)
                if (tip != null)
                {
                    Object.DestroyImmediate(tip);
                }

            _tips.Clear();
        }
    }
}
