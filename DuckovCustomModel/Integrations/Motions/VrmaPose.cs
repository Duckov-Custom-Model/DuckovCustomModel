using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Integrations.Vrm;
using UniGLTF;
using UniVRM10;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Motions
{
    internal sealed class VrmaPose : IMotionSampler
    {
        private static readonly Dictionary<string, Entry> Cache = new(StringComparer.Ordinal);
        private static long _access;
        private Entry? _entry;
        private GameObject? _root;
        private HumanPoseHandler? _sourcePose, _targetPose;
        private AnimationClip? _clip;
        private Vrm10AnimationInstance? _instance;
        private readonly MotionExpressions _expressions;
        private HumanPose _pose;
        private readonly List<(string name, Func<float> value)> _expressionTracks = new();
        private readonly Vector3 _initialBody;
        private readonly Animator _target;
        private readonly Vector3 _initialRoot;
        public bool ControlsBody => true;
        public double Duration => _clip != null ? Math.Max(_clip.length, 1.0 / 30) : 0;

        private VrmaPose(GameObject root, Animator target, MotionPlaybackSettings settings)
        {
            _root = root;
            _target = target;
            _initialRoot = root.transform.localPosition;
            var source = root.GetComponent<Animator>();
            var animation = root.GetComponent<Animation>();
            if (source == null || !source.isHuman || animation == null || animation.clip == null ||
                float.IsNaN(animation.clip.length) || float.IsInfinity(animation.clip.length) ||
                animation.clip.length > 86400)
                throw new InvalidDataException("VRMA has no playable humanoid animation.");
            animation.enabled = false;
            source.enabled = false;
            _clip = animation.clip;
            _instance = root.GetComponent<Vrm10AnimationInstance>();
            if (_instance != null)
                foreach (var pair in _instance.ExpressionMap)
                    _expressionTracks.Add((pair.Key.ToString(), pair.Value));
            _sourcePose = new HumanPoseHandler(source.avatar, source.transform);
            _targetPose = new HumanPoseHandler(target.avatar, target.transform);
            _expressions = new MotionExpressions(target, settings.ExpressionMappings);
            _clip.SampleAnimation(root, 0);
            _sourcePose.GetHumanPose(ref _pose);
            _initialBody = _pose.bodyPosition;
        }

        internal static async Task<IMotionSampler> Load(byte[] bytes, string name, Animator target,
            MotionPlaybackSettings settings, Func<bool> cancelled)
        {
            var hash = await Task.Run(() =>
            {
                using var digest = SHA256.Create();
                return Convert.ToBase64String(digest.ComputeHash(bytes));
            });
            if (cancelled()) throw new OperationCanceledException();
            var key = name + ":" + hash;
            if (!Cache.TryGetValue(key, out var entry) || entry.Task.IsFaulted ||
                entry.Task.IsCompleted && entry.Task.Result == null)
            {
                entry = new Entry { Task = Import(bytes, name), Length = bytes.Length };
                Cache[key] = entry;
            }

            entry.References++;
            entry.Access = ++_access;
            try
            {
                var root = await entry.Task;
                if (cancelled()) throw new OperationCanceledException();
                var pose = new VrmaPose(root, target, settings) { _entry = entry };
                Trim();
                return pose;
            }
            catch
            {
                Release(entry);
                throw;
            }
        }

        private static async Task<GameObject> Import(byte[] bytes, string name)
        {
            using var data = await Task.Run(() => new GlbBinaryParser(bytes, name).Parse());
            if (data.GLTF.meshes is { Count: > 0 } || data.GLTF.images is { Count: > 0 })
                throw new InvalidDataException("VRMA motion must not contain meshes or textures.");
            using var importer = new VrmAnimationImporter(new VrmAnimationData(data),
                materialGenerator: VrmModelSource.CreateAnimationMaterialGenerator());
            var instance = await importer.LoadAsync(new RuntimeOnlyAwaitCaller());
            var root = instance.gameObject;
            try
            {
                root.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(root);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                return root;
            }
            catch
            {
                Object.Destroy(root);
                throw;
            }
        }

        internal static void ClearCache()
        {
            foreach (var entry in Cache.Values)
            {
                entry.Retired = true;
                DestroyUnused(entry);
            }

            Cache.Clear();
        }

        private static void Trim()
        {
            while (Cache.Count > 4 || Cache.Values.Sum(entry => (long)entry.Length) > 64 * 1024 * 1024)
            {
                var oldest = Cache.OrderBy(pair => pair.Value.Access).First();
                Cache.Remove(oldest.Key);
                oldest.Value.Retired = true;
                DestroyUnused(oldest.Value);
            }
        }

        private static void Release(Entry entry)
        {
            entry.References--;
            if (entry.References == 0 && entry.Task.IsFaulted)
            {
                foreach (var key in Cache.Where(pair => ReferenceEquals(pair.Value, entry)).Select(pair => pair.Key)
                             .ToArray())
                    Cache.Remove(key);
            }

            DestroyUnused(entry);
        }

        private static void DestroyUnused(Entry entry)
        {
            if (entry.Retired && entry.References == 0 && entry.Task.Status == TaskStatus.RanToCompletion &&
                entry.Task.Result != null)
                Object.Destroy(entry.Task.Result);
        }

        private sealed class Entry
        {
            internal Task<GameObject> Task = null!;
            internal int References, Length;
            internal long Access;
            internal bool Retired;
        }

        public void Apply(double seconds, float weight, MotionPlaybackSettings settings)
        {
            if (_root == null || _clip == null || _sourcePose == null || _targetPose == null) return;
            _clip.SampleAnimation(_root, (float)seconds);
            if (settings.InPlace)
            {
                var position = _root.transform.localPosition;
                _root.transform.localPosition = new Vector3(_initialRoot.x, position.y, _initialRoot.z);
            }

            _sourcePose.GetHumanPose(ref _pose);
            _pose.bodyPosition = _initialBody + (_pose.bodyPosition - _initialBody) * settings.PositionScale;
            var rootPosition = _target.transform.localPosition;
            var rootRotation = _target.transform.localRotation;
            _targetPose.SetHumanPose(ref _pose);
            _target.transform.SetLocalPositionAndRotation(rootPosition, rootRotation);
            foreach (var (name, value) in _expressionTracks) _expressions.Set(name, value(), weight);
        }

        public void Restore() => _expressions.Restore();

        public void Dispose()
        {
            Restore();
            _sourcePose?.Dispose();
            _targetPose?.Dispose();
            _sourcePose = _targetPose = null;
            if (_entry != null) Release(_entry);
            _entry = null;
            _root = null;
        }
    }
}
