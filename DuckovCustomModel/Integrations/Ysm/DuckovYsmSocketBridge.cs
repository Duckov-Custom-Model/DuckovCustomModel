using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using ModelRuntime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed class DuckovYsmLocatorOffset
    {
        public float[] Position { get; set; } = [0f, 0f, 0f];
        public float[] Rotation { get; set; } = [0f, 0f, 0f];
    }

    public sealed class DuckovYsmSocketBridge : IDisposable
    {
        private readonly List<Binding> _bindings = [];
        private readonly CharacterModel _characterModel;
        private readonly Dictionary<string, Transform> _locators = new(StringComparer.Ordinal);
        private readonly Transform _root;
        private bool _active;
        private bool _disposed;
        private bool _originalAutoSyncRightHandRotation;
        private bool _ownsRightHandRotation;

        public DuckovYsmSocketBridge(CharacterModel characterModel, Transform root,
            IReadOnlyDictionary<string, string>? locatorMappings = null,
            IReadOnlyDictionary<string, DuckovYsmLocatorOffset>? locatorOffsets = null)
        {
            if (characterModel == null) throw new ArgumentNullException(nameof(characterModel));
            if (root == null) throw new ArgumentNullException(nameof(root));
            _characterModel = characterModel;
            _root = root;
            var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var name in SocketNames.InternalSocketNames) mapping.Add(name, name);
            foreach (var name in SocketNames.ExternalSocketNames) mapping.Add(name, name);
            mapping.Add(SocketNames.Rifle, SocketNames.Rifle);
            mapping.Add(SocketNames.Pistol, SocketNames.Pistol);
            if (locatorMappings != null)
                foreach (var pair in locatorMappings)
                {
                    if (!mapping.ContainsKey(pair.Key))
                        throw new ArgumentException($"Unknown Duckov socket '{pair.Key}'.", nameof(locatorMappings));
                    mapping[pair.Key] = pair.Value;
                }

            if (locatorOffsets != null)
                foreach (var pair in locatorOffsets)
                    if (!mapping.ContainsKey(pair.Key))
                        throw new ArgumentException($"Unknown Duckov socket '{pair.Key}'.", nameof(locatorOffsets));
            foreach (var pair in mapping)
            {
                if (string.IsNullOrWhiteSpace(pair.Value)) continue;
                DuckovYsmLocatorOffset? offset = null;
                locatorOffsets?.TryGetValue(pair.Key, out offset);
                var rotation = offset == null && IsHeldItemSocket(pair.Key)
                    ? new(90f, 0f, 0f)
                    : ReadVector(offset?.Rotation, pair.Key);
                _bindings.Add(new(pair.Key, pair.Value,
                    ReadVector(offset?.Position, pair.Key), Quaternion.Euler(rotation)));
            }
        }

        public IReadOnlyDictionary<string, Transform> Locators => _locators;

        public bool OriginalAutoSyncRightHandRotation =>
            _ownsRightHandRotation || _characterModel is null || !_characterModel
                ? _originalAutoSyncRightHandRotation
                : _characterModel.autoSyncRightHandRotation;

        public void Dispose()
        {
            if (_disposed) return;
            Deactivate();
            foreach (var binding in _bindings)
                if (binding.PoseTransform != null)
                    Object.Destroy(binding.PoseTransform.gameObject);
            _locators.Clear();
            _disposed = true;
        }

        public void Activate()
        {
            ThrowIfDisposed();
            _active = true;
            UpdateRightHandRotationOwnership();
        }

        public void Deactivate()
        {
            _active = false;
            UpdateRightHandRotationOwnership();
        }

        public bool Update(PoseEvaluator pose)
        {
            ThrowIfDisposed();
            if (pose == null) throw new ArgumentNullException(nameof(pose));
            var changed = false;
            foreach (var binding in _bindings)
            {
                if (!(pose.TryGetLocatorTransform(binding.LocatorName, out var matrix) ||
                      pose.TryGetBonePivotTransform(binding.LocatorName, out matrix)) ||
                    !DuckovYsmCoordinates.TryGetUnityTransform(matrix, out var position, out var rotation,
                        out var scale))
                {
                    changed |= _locators.Remove(binding.SocketName);
                    continue;
                }

                if (binding.Transform == null)
                {
                    if (binding.PoseTransform == null)
                    {
                        binding.PoseTransform = new GameObject("YsmPose_" + binding.SocketName).transform;
                        binding.PoseTransform.SetParent(_root, false);
                    }

                    binding.Transform = new GameObject(binding.SocketName).transform;
                    binding.Transform.SetParent(binding.PoseTransform, false);
                    binding.Transform.localPosition = binding.Offset;
                    binding.Transform.localRotation = binding.Rotation;
                    changed = true;
                }

                binding.PoseTransform!.localPosition = position;
                binding.PoseTransform.localRotation = rotation;
                binding.PoseTransform.localScale = scale;
                if (!_locators.ContainsKey(binding.SocketName)) changed = true;
                _locators[binding.SocketName] = binding.Transform;
            }

            UpdateRightHandRotationOwnership();
            return changed;
        }

        private void UpdateRightHandRotationOwnership()
        {
            if (_characterModel is null || !_characterModel)
            {
                _ownsRightHandRotation = false;
                return;
            }

            var ownRotation = _active && _locators.ContainsKey(SocketNames.RightHand);
            if (ownRotation && !_ownsRightHandRotation)
            {
                _originalAutoSyncRightHandRotation = _characterModel.autoSyncRightHandRotation;
                _characterModel.autoSyncRightHandRotation = false;
                _ownsRightHandRotation = true;
            }
            else if (!ownRotation && _ownsRightHandRotation)
            {
                _characterModel.autoSyncRightHandRotation = _originalAutoSyncRightHandRotation;
                _ownsRightHandRotation = false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DuckovYsmSocketBridge));
        }

        private static Vector3 ReadVector(float[]? values, string socket)
        {
            if (values == null) return Vector3.zero;
            if (values.Length != 3)
                throw new ArgumentException($"Locator offset for '{socket}' must contain exactly three numbers.");
            foreach (var value in values)
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentException($"Locator offset for '{socket}' must contain finite numbers.");
            return new(values[0], values[1], values[2]);
        }

        private static bool IsHeldItemSocket(string socket)
        {
            return socket == SocketNames.RightHand || socket == SocketNames.LeftHand ||
                   socket == SocketNames.MeleeWeapon || socket == SocketNames.Rifle ||
                   socket == SocketNames.Pistol || socket == SocketNames.Carriable ||
                   socket == SocketNames.PaperBox;
        }

        private sealed class Binding(string socketName, string locatorName, Vector3 offset, Quaternion rotation)
        {
            internal readonly string LocatorName = locatorName;
            internal readonly Vector3 Offset = offset;
            internal readonly Quaternion Rotation = rotation;
            internal readonly string SocketName = socketName;
            internal Transform? PoseTransform;
            internal Transform? Transform;
        }
    }
}
