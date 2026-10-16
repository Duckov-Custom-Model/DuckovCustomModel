using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.Utils;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Motions
{
    [DefaultExecutionOrder(10700)]
    internal sealed class ExternalMotionPlayer : MonoBehaviour
    {
        private ModelHandler? _handler;
        private Animator? _animator;
        private ModelBundleInfo? _bundle;
        private IMotionSampler? _sampler;
        private MotionAudio? _audio;
        private MotionPlaybackSettings _settings = new();
        private readonly List<(HumanBodyBones id, Transform bone)> _bones = [];
        private Vector3[] _positions = [];
        private Quaternion[] _rotations = [];
        private AnimatorCullingMode _cullingMode;
        private int _generation, _cycle;
        private double _time;
        private float _weight, _fade;
        private bool _stopping, _applied, _ownsState, _ownsAutoSync, _autoSync, _paused;
        private bool? _movementLockOverride;

        internal bool MovementLocked
        {
            get => _movementLockOverride ?? !_settings.InterruptOnMove;
            set => _movementLockOverride = value;
        }

        internal string? CurrentActionId { get; private set; }
        internal bool IsControllingPose => _sampler is { ControlsBody: true } && _weight > 0;

        internal void Initialize(ModelHandler handler, Animator animator, ModelBundleInfo bundle)
        {
            Stop(true);
            _movementLockOverride = null;
            _settings = new MotionPlaybackSettings();
            _handler = handler;
            _animator = animator;
            _bundle = bundle;
            _bones.Clear();
            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
                if (animator.GetBoneTransform((HumanBodyBones)i) is { } bone)
                    _bones.Add(((HumanBodyBones)i, bone));
            _positions = new Vector3[_bones.Count];
            _rotations = new Quaternion[_bones.Count];
        }

        internal bool TryPlay(RadialActionInfo action)
        {
            if (_animator == null || !_animator.isHuman || _animator.avatar == null || !_animator.avatar.isValid ||
                !isActiveAndEnabled) return false;
            Release();
            _paused = false;
            _settings = action.Playback ?? new MotionPlaybackSettings();
            CurrentActionId = action.Id;
            _ = Load(action, ++_generation);
            return true;
        }

        private async Task Load(RadialActionInfo action, int generation)
        {
            IMotionSampler? sampler = null;
            MotionAudio? audio = null;
            try
            {
                var settings = action.Playback ?? new MotionPlaybackSettings();
                var library = action.Id.StartsWith("dcm:motion/", StringComparison.Ordinal);
                var directory = library ? MotionLibrary.DirectoryPath : _handler!.CurrentModelDirectory!;
                if (!library && _bundle != null && ModelSourcePaths.IsFileSource(_bundle) &&
                    _handler!.CurrentModelInfo != null)
                    directory = Path.GetDirectoryName(ModelSourcePaths.SourcePath(_bundle, _handler.CurrentModelInfo))!;
                else if (!library && _bundle != null && !string.IsNullOrWhiteSpace(_bundle.BundlePath))
                    directory = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(_bundle.DirectoryPath,
                        _bundle.BundlePath)))!;
                var path = MotionLibrary.Resolve(directory, action.Motion);
                byte[]? embedded = null;
                if (!library && _bundle != null && !string.IsNullOrWhiteSpace(_bundle.BundlePath))
                {
                    var container = AssetBundleManager.GetOrLoadAssetBundle(_bundle);
                    var name = container != null && container.Contains(action.Motion) ? action.Motion :
                        container != null && container.Contains(action.Motion + ".bytes") ? action.Motion + ".bytes" :
                        null;
                    if (name != null)
                    {
                        var asset = AssetBundleManager.LoadAssetFromBundle<TextAsset>(_bundle, name);
                        if (asset == null) throw new InvalidDataException("Motion bundle resource is not a TextAsset.");
                        embedded = asset.bytes;
                    }
                }

                if (embedded == null && !File.Exists(path) && !library)
                {
                    var shared = MotionLibrary.Resolve(MotionLibrary.DirectoryPath, action.Motion);
                    if (File.Exists(shared))
                    {
                        path = shared;
                        directory = MotionLibrary.DirectoryPath;
                    }
                }

                if (Path.GetExtension(action.Motion).Equals(".vmd", StringComparison.OrdinalIgnoreCase))
                {
                    var motion = await MotionLibrary.LoadVmd(path, embedded);
                    if (Cancelled()) return;
                    sampler = new VmdPose(motion, _animator!, settings);
                }
                else
                {
                    var bytes = embedded ?? await Task.Run(() =>
                    {
                        var info = new FileInfo(path);
                        if (!info.Exists || info.Length > 64 * 1024 * 1024)
                            throw new InvalidDataException("VRMA is missing or too large.");
                        return File.ReadAllBytes(path);
                    });
                    if (Cancelled()) return;
                    sampler = await VrmaPose.Load(bytes, path, _animator!, settings, Cancelled);
                }

                if (Cancelled()) return;
                var audioPath = settings.Audio;
                var audioDirectory = library ? Path.GetDirectoryName(path)! : directory;
                if (string.IsNullOrWhiteSpace(audioPath))
                {
                    var folder = Path.GetDirectoryName(path)!;
                    if (Directory.Exists(folder))
                    {
                        var files = Directory.EnumerateFiles(folder).ToArray();
                        foreach (var extension in new[] { ".ogg", ".wav", ".mp3" })
                        {
                            var candidate = files.FirstOrDefault(file =>
                                Path.GetFileNameWithoutExtension(file).Equals(Path.GetFileNameWithoutExtension(path),
                                    StringComparison.Ordinal) &&
                                Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase));
                            if (candidate == null) continue;
                            audioPath = Path.GetRelativePath(audioDirectory, candidate);
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(audioPath))
                    audio = await MotionAudio.Load(MotionLibrary.Resolve(audioDirectory, audioPath),
                        settings.SpatialAudio, Cancelled);
                if (Cancelled()) return;
                _sampler = sampler;
                _audio = audio;
                sampler = null;
                audio = null;
                _settings = settings;
                _time = 0;
                _cycle = 0;
                _weight = 0;
                _stopping = false;
                _cullingMode = _animator!.cullingMode;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (_sampler.ControlsBody && _handler!.OriginalCharacterModel != null)
                {
                    _autoSync = _handler.OriginalCharacterModel.autoSyncRightHandRotation;
                    _handler.OriginalCharacterModel.autoSyncRightHandRotation = false;
                    _ownsAutoSync = true;
                }

                _ownsState = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (!Cancelled())
                {
                    CurrentActionId = null;
                    ModLogger.LogWarning($"Motion '{action.Name}' could not be played: {exception.Message}");
                }
            }
            finally
            {
                sampler?.Dispose();
                audio?.Dispose();
            }

            bool Cancelled() => this == null || !isActiveAndEnabled || generation != _generation || _animator == null;
        }

        internal void SetPaused(bool paused) => _paused = paused;

        private void Update() => RestorePose();

        private void LateUpdate()
        {
            RestorePose();
            if (_sampler == null || _handler?.CharacterMainControl == null) return;
            var character = _handler.CharacterMainControl;
            if (!_stopping && (character.Health is { IsDead: true } ||
                               character.CurrentAction is CA_Dash { Running: true } or CA_Attack { Running: true }
                                   or CA_UseItem { Running: true } ||
                               character.IsInAdsInput || _handler.CustomAnimatorControl is { Attacking: true } ||
                               !MovementLocked && RadialActionInput.MovementRequested(character))) Stop();
            if (_sampler == null) return;
            var delta = _paused ? 0 : Time.deltaTime;
            if (_stopping)
            {
                _time += delta * _settings.Speed;
                _fade += delta;
                _weight = _settings.BlendOut > 0 ? Mathf.Max(0, 1 - _fade / _settings.BlendOut) : 0;
                if (_weight <= 0)
                {
                    Release();
                    return;
                }
            }
            else
            {
                _weight = _settings.BlendIn > 0 ? Mathf.Min(1, _weight + delta / _settings.BlendIn) : 1;
                _time += delta * _settings.Speed;
                if (_time >= _sampler.Duration)
                {
                    if (_settings.Loop)
                    {
                        var cycles = (int)(_time / _sampler.Duration);
                        _time %= _sampler.Duration;
                        _cycle += cycles;
                    }
                    else
                    {
                        _time = _sampler.Duration;
                        Stop();
                    }
                }
            }

            if (_sampler.ControlsBody)
                for (var i = 0; i < _bones.Count; i++)
                {
                    _positions[i] = _bones[i].bone.localPosition;
                    _rotations[i] = _bones[i].bone.localRotation;
                }

            try
            {
                _sampler.Apply(Math.Min(_time, _sampler.Duration), _weight, _settings);
                if (_sampler.ControlsBody)
                    for (var i = 0; i < _bones.Count; i++)
                    {
                        var (id, bone) = _bones[i];
                        var weight = _settings.UpperBodyOnly && (id == HumanBodyBones.Hips || VmdPose.IsLeg(id))
                            ? 0
                            : _weight;
                        bone.localPosition = Vector3.Lerp(_positions[i], bone.localPosition, weight);
                        bone.localRotation = Quaternion.Slerp(_rotations[i], bone.localRotation, weight);
                    }

                _applied = _sampler.ControlsBody;
                _audio?.Sync(_time - _settings.AudioOffset, _cycle, _settings.Speed * Time.timeScale,
                    _handler.IsModelAudioEnabled ? _settings.Volume * _handler.ModelAudioVolume * _weight : 0,
                    transform.position, Time.timeScale == 0 || _paused);
            }
            catch (Exception exception)
            {
                _applied = _sampler?.ControlsBody == true;
                Release();
                ModLogger.LogWarning($"Motion playback stopped: {exception.Message}");
            }
        }

        internal bool Stop(bool immediate = false)
        {
            ++_generation;
            if (CurrentActionId == null && _sampler == null) return false;
            CurrentActionId = null;
            if (immediate || _sampler == null || Time.timeScale == 0) Release();
            else if (!_stopping)
            {
                _paused = false;
                _stopping = true;
                _fade = (1 - _weight) * _settings.BlendOut;
            }

            return true;
        }

        private void RestorePose()
        {
            _sampler?.Restore();
            if (!_applied) return;
            for (var i = 0; i < _bones.Count; i++)
                if (_bones[i].bone != null)
                {
                    _bones[i].bone.localPosition = _positions[i];
                    _bones[i].bone.localRotation = _rotations[i];
                }

            _applied = false;
        }

        private void Release()
        {
            RestorePose();
            _sampler?.Dispose();
            _sampler = null;
            _audio?.Dispose();
            _audio = null;
            if (_ownsState)
            {
                if (_animator != null) _animator.cullingMode = _cullingMode;
                if (_ownsAutoSync && _handler?.OriginalCharacterModel != null)
                    _handler.OriginalCharacterModel.autoSyncRightHandRotation = _autoSync;
            }

            _ownsState = false;
            _ownsAutoSync = false;
            _weight = 0;
            CurrentActionId = null;
        }

        private void OnDisable() => Stop(true);
        private void OnDestroy() => Stop(true);
    }
}
