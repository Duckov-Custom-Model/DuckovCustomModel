using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniGLTF;
using UniGLTF.SpringBoneJobs.Blittables;
using UniVRM10;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal sealed class VrmSpringBoneRuntime : IVrm10SpringBoneRuntime
    {
        private readonly Dictionary<Transform, BlittableJointMutable> _jointSettings = new();
        private Vrm10Instance? _instance;
        private Vrm10FastSpringboneRuntimeStandalone? _runtime;
        private BlittableModelLevel _modelSettings = new(supportsScalingAtRuntime: true);
        private Vector3 _scale;

        public Task InitializeAsync(Vrm10Instance instance, IAwaitCaller awaitCaller)
        {
            _instance = instance;
            _ = instance.DefaultTransformStates;
            return Task.CompletedTask;
        }

        public void SetJointLevel(Transform joint, BlittableJointMutable settings)
        {
            _jointSettings[joint] = settings;
            _runtime?.SetJointLevel(joint, settings);
        }

        public void SetModelLevel(Transform modelRoot, BlittableModelLevel settings)
        {
            if (_instance == null || modelRoot != _instance.transform) return;
            _modelSettings = settings;
            _runtime?.SetModelLevel(modelRoot, settings);
        }

        public void Process(float deltaTime)
        {
            if (_instance == null || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            var scale = _instance.transform.lossyScale;
            if (float.IsNaN(scale.sqrMagnitude) || float.IsInfinity(scale.sqrMagnitude) ||
                Mathf.Abs(scale.x * scale.y * scale.z) < .000000001f)
            {
                RestoreInitialTransform();
                return;
            }

            if (deltaTime > .1f || (_runtime != null &&
                                    (scale - _scale).sqrMagnitude > .00000001f * Mathf.Max(1, _scale.sqrMagnitude)))
                RestoreInitialTransform();
            EnsureRuntime(scale);
            _runtime!.Process(Mathf.Min(deltaTime, 1f / 30));
        }

        private void EnsureRuntime(Vector3 scale)
        {
            if (_runtime != null || _instance == null) return;
            var runtime = new Vrm10FastSpringboneRuntimeStandalone();
            try
            {
                runtime.InitializeAsync(_instance, new ImmediateCaller()).GetAwaiter().GetResult();
                runtime.m_bufferCombiner.ReconstructIfDirty(default).Complete();
                runtime.SetModelLevel(_instance.transform, _modelSettings);
                foreach (var pair in _jointSettings)
                    if (pair.Key != null)
                        runtime.SetJointLevel(pair.Key, pair.Value);
                _runtime = runtime;
                _scale = scale;
            }
            catch
            {
                runtime.Dispose();
                throw;
            }
        }

        public void RestoreInitialTransform()
        {
            var runtime = _runtime;
            _runtime = null;
            if (runtime == null) return;
            try
            {
                runtime.RestoreInitialTransform();
            }
            finally
            {
                runtime.Dispose();
            }
        }

        public bool ReconstructSpringBone()
        {
            RestoreInitialTransform();
            if (_instance == null) return false;
            EnsureRuntime(_instance.transform.lossyScale);
            return true;
        }

        public void DrawGizmos() => _runtime?.DrawGizmos();

        public void Dispose()
        {
            _runtime?.Dispose();
            _runtime = null;
            _instance = null;
            _jointSettings.Clear();
        }
    }

    internal sealed class VrmSpringBoneRuntimeProvider : MonoBehaviour, IVrm10SpringBoneRuntimeProvider
    {
        public IVrm10SpringBoneRuntime CreateSpringBoneRuntime() => new VrmSpringBoneRuntime();
    }
}
