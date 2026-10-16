using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomModel.Core.MonoBehaviours.Physics
{
    [Serializable]
    public sealed class ModelSpringChain
    {
        public Transform? Root;

        [Tooltip(
            "Optional. Follows single-child bones automatically. Specify an end bone for branching hierarchies. The end bone also moves.")]
        public Transform? End;

        [Min(0)] public float Stiffness = 1;
        [Range(0, 1)] public float Drag = .6f;
        [Min(0)] public float Gravity;
        public Vector3 GravityDirection = Vector3.down;
        [Min(0)] public float Radius = .01f;

        [Tooltip("Tail offset in the end bone local space. Zero extends automatically along the preceding bone.")]
        public Vector3 EndOffset;
    }

    [DefaultExecutionOrder(10950)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Duckov Custom Model/Spring Bones")]
    public sealed class ModelSpringBone : MonoBehaviour
    {
        [Tooltip("Each chain extends from root to end. Do not include the same bone in multiple chains.")]
        public List<ModelSpringChain> Chains = new();

        public List<ModelSpringCollider> Colliders = new();

        [Tooltip("Optional inertia reference. Leave empty for world-space inertia.")]
        public Transform? Center;

        [Min(.01f)] public float TeleportDistance = 8;
        [NonSerialized] private ModelSpringSimulation? _simulation;
        [NonSerialized] private bool _dirty;
        private readonly Dictionary<Transform, Quaternion> _defaultRotations = new();
        public string? SimulationError { get; private set; }

        private void OnEnable() => CapturePose();

        public void PrepareSimulation()
        {
            ResetSimulation();
            CapturePose();
        }

        private void CapturePose()
        {
            _defaultRotations.Clear();
            var bones = new List<Transform>();
            foreach (var chain in Chains)
                if (TryGetBones(chain, transform, bones, out _))
                    foreach (var bone in bones)
                        _defaultRotations[bone] = bone.localRotation;
        }

        internal Quaternion DefaultRotation(Transform bone)
        {
            if (_defaultRotations.TryGetValue(bone, out var rotation)) return rotation;
            return _defaultRotations[bone] = bone.localRotation;
        }

        private void LateUpdate()
        {
            if (Application.isPlaying) Simulate(Time.deltaTime);
        }

        public void Simulate(float deltaTime)
        {
            if (!isActiveAndEnabled || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            if (_dirty) ResetSimulation();
            if (SimulationError != null) return;
            if (Chains.Count == 0) return;
            var scale = transform.lossyScale;
            if (float.IsNaN(scale.sqrMagnitude) || float.IsInfinity(scale.sqrMagnitude) ||
                Mathf.Abs(scale.x * scale.y * scale.z) < .000000001f)
            {
                ResetSimulation();
                return;
            }

            try
            {
                _simulation ??= new ModelSpringSimulation(this);
                _simulation.Process(deltaTime);
            }
            catch (Exception exception)
            {
                ResetSimulation();
                SimulationError = exception.Message;
                Debug.LogError($"[Duckov Custom Model] Invalid spring-bone configuration：{exception.Message}", this);
            }
        }

        public void ResetSimulation()
        {
            _simulation?.Dispose();
            _simulation = null;
            _dirty = false;
            SimulationError = null;
        }

        private void OnDisable() => ResetSimulation();
        private void OnDestroy() => ResetSimulation();
        private void OnValidate() => _dirty = true;

        public static bool TryGetBones(ModelSpringChain chain, Transform owner, List<Transform> bones, out string error)
        {
            bones.Clear();
            error = string.Empty;
            if (chain == null || chain.Root == null)
            {
                error = "Assign a root bone.";
                return false;
            }

            if (chain.Root == owner || !chain.Root.IsChildOf(owner))
            {
                error = "The root bone must be a child of the Spring Bones component.";
                return false;
            }

            if (chain.End != null)
            {
                var current = chain.End;
                while (current != null && current != chain.Root)
                {
                    bones.Add(current);
                    current = current.parent;
                }

                if (current == null)
                {
                    error = "The end bone must be the root bone or one of its descendants.";
                    bones.Clear();
                    return false;
                }

                bones.Add(current);
                bones.Reverse();
            }
            else
            {
                var current = chain.Root;
                while (current != null)
                {
                    bones.Add(current);
                    Transform? child = null;
                    for (var i = 0; i < current.childCount; i++)
                    {
                        var candidate = current.GetChild(i);
                        if ((candidate.gameObject.hideFlags & HideFlags.DontSave) != 0) continue;
                        if (child != null)
                        {
                            error = $"{current.name} has multiple children. Specify an end bone.";
                            bones.Clear();
                            return false;
                        }

                        child = candidate;
                    }

                    current = child;
                }
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            var color = Gizmos.color;
            Gizmos.color = new Color(.4f, 1, .5f);
            var bones = new List<Transform>();
            foreach (var chain in Chains)
            {
                if (!TryGetBones(chain, transform, bones, out _)) continue;
                var scale = transform.lossyScale;
                var radius = Mathf.Max(0, chain.Radius) *
                             Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                for (var i = 0; i < bones.Count; i++)
                {
                    if (i > 0) Gizmos.DrawLine(bones[i - 1].position, bones[i].position);
                    if (radius > 0) Gizmos.DrawWireSphere(bones[i].position, radius);
                }
            }

            Gizmos.color = color;
        }
    }
}
