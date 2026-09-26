using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using ModelRuntime;
using ModelRuntime.Adapters;
using ModelRuntime.Media;
using UnityEngine;
using NumericsMatrix = System.Numerics.Matrix4x4;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed class YsmCharacterRuntime : IDisposable, IEntityStateProvider
    {
        private readonly ModelAdapter? _adapter;
        private readonly DuckovYsmEffects? _effects;
        private readonly ModelHandler _handler;
        private readonly YsmModelLease _lease;
        private readonly ITexturePixelSource _pixels = CreatePixelSource();
        private readonly DuckovYsmRenderBackend? _renderer;
        private readonly DuckovYsmSocketBridge? _sockets;
        private readonly DuckovEntityStateProvider? _source;
        private bool _active;
        private bool _cameraLayerChecked;
        private bool _disposed;
        private string _extra = string.Empty;
        private long _extraSequence;

        public YsmCharacterRuntime(ModelHandler handler, ModelBundleInfo bundle, ModelInfo model)
        {
            if (handler.CharacterMainControl == null || handler.OriginalCharacterModel == null)
                throw new InvalidOperationException(
                    "The character must be initialized before creating a YSM instance.");
            _handler = handler;
            _lease = YsmModelSource.Acquire(bundle, model);
            Root = new(ModelHandler.CustomModelInstanceName);
            Root.SetActive(false);
            var characterLayer = LayerMask.NameToLayer("Character");
            if (characterLayer >= 0) Root.layer = characterLayer;
            else
                ModLogger.LogWarning(
                    "YSM cannot find the named Character layer; check this game's layer configuration.");
            Root.transform.SetParent(handler.OriginalCharacterModel.transform, false);
            try
            {
                var profile = _lease.Profile;
                var context = new MolangContext
                    { Dialect = MolangDialect.Ysm, ValueCompatibility = profile.ValueCompatibility };
                Simulation = new(_lease.Package, profile.ModelTarget, context);
                Simulation.UseFallbackMainController = true;
                var scale = Simulation.Document.DisplayScale;
                InitialRootScale = new Vector3(scale.X, scale.Y, scale.Z) * profile.Scale;
                Root.transform.localScale = InitialRootScale;
                _source = new(handler.CharacterMainControl,
                    handler.OriginalCharacterModel, profile, Simulation.Document,
                    handler.GetOriginalSocketTransform(SocketNames.RightHand));
                _renderer = new(Root.transform, _lease.Package, _pixels);
                _effects = new(Root.transform, handler, GetModelToWorld);
                _effects.Bind(Simulation);
                _adapter = new(Simulation, _lease.Package, this,
                    new DuckovWorldQueryProvider(), _effects, _renderer, _pixels);
                _adapter.AnimationPolicy.HandCompatibility = profile.HandCompatibility;
                _adapter.ExtraAnimationStopped += OnExtraStopped;
                _sockets = new(handler.OriginalCharacterModel, Root.transform,
                    profile.LocatorMappings, profile.LocatorOffsets);
                _adapter.Advance(0);
                _sockets.Update(Simulation.Pose);
                MeasureNaturalHeight();
                Simulation.Reset();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public GameObject Root { get; }
        public ModelAdapter Adapter => _adapter ?? throw new ObjectDisposedException(nameof(YsmCharacterRuntime));
        public ModelSimulation Simulation { get; }
        public MolangContext Context => Simulation.Context;

        public bool IsAvailable => !_disposed && _active && Root != null &&
                                   _handler.CharacterMainControl != null &&
                                   (_handler.CharacterMainControl.Health == null ||
                                    !_handler.CharacterMainControl.Health.IsDead);

        public YsmPresentationSettings Presentation => Adapter.Presentation;
        public IReadOnlyList<YsmExtraAnimationEntry> ExtraActions => Presentation.ExtraAnimations;
        public bool ExtraAnimationLocked { get; set; }
        public IReadOnlyDictionary<string, Transform> Locators => _sockets!.Locators;
        public Vector3 InitialRootScale { get; }
        public float NaturalHeight { get; private set; }
        public bool OriginalAutoSyncRightHandRotation => _sockets!.OriginalAutoSyncRightHandRotation;
        public int RendererVersion => _renderer?.RendererVersion ?? 0;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Release(Deactivate);
            Release(() => Simulation?.Reset());
            if (_adapter != null) _adapter.ExtraAnimationStopped -= OnExtraStopped;
            Release(() => _adapter?.Dispose());
            Release(() => _effects?.Dispose());
            Release(() => _source?.Dispose());
            Release(() => _sockets?.Dispose());
            Release(() => _renderer?.Dispose());
            Release(() => (_pixels as StandardTexturePixelSource)?.Clear());
            Release(_lease.Dispose);
            Release(() =>
            {
                if (Root != null) Object.DestroyImmediate(Root);
            });
        }

        void IEntityStateProvider.Capture(EntityState destination, double deltaSeconds)
        {
            _source!.Capture(destination, deltaSeconds);
            var textures = Simulation.Document.Textures;
            destination.TextureName = textures.Count > 0
                ? textures[Mathf.Clamp(_renderer!.TextureIndex, 0, textures.Count - 1)].Name
                : string.Empty;
            var camera = GameCamera.Instance != null ? GameCamera.Instance.renderCamera : null;
            destination.CameraDistance = camera != null && _handler.CharacterMainControl != null
                ? Vector3.Distance(camera.transform.position, _handler.CharacterMainControl.transform.position)
                : null;
            destination.ExtraAnimation = _extra;
            destination.ExtraAnimationSequence = _extraSequence;
            destination.ExtraAnimationLocked = ExtraAnimationLocked;
        }

        public Transform? GetGunLocator(int itemTypeId)
        {
            var category = _lease.Profile.ItemCategories.TryGetValue(itemTypeId, out var configured)
                ? configured
                : string.Empty;
            var name = string.Equals(category, "pistol", StringComparison.OrdinalIgnoreCase)
                ? SocketNames.Pistol
                : SocketNames.Rifle;
            return _lease.Profile.LocatorMappings.ContainsKey(name) &&
                   _sockets!.Locators.TryGetValue(name, out var locator)
                ? locator
                : null;
        }

        private static ITexturePixelSource CreatePixelSource()
        {
            return new StandardTexturePixelSource();
        }

        public void Activate()
        {
            if (_disposed || _active) return;
            _sockets!.Activate();
            _effects!.Enabled = true;
            _active = true;
            CheckCameraLayer();
        }

        public void Deactivate()
        {
            if (!_active) return;
            _active = false;
            Release(() => _effects!.Enabled = false);
            Release(() => _sockets!.Deactivate());
        }

        public bool Tick(double deltaSeconds)
        {
            if (_disposed || !_active || !Root.activeInHierarchy) return false;
            if (!_cameraLayerChecked) CheckCameraLayer();
            _renderer!.TimeSeconds = Simulation.Time + Math.Max(0, deltaSeconds);
            Adapter.Advance(Math.Max(0, deltaSeconds));
            var socketsChanged = _sockets!.Update(Simulation.Pose);
            _effects!.Tick(Math.Max(0, deltaSeconds));
            return socketsChanged;
        }

        private void CheckCameraLayer()
        {
            var camera = GameCamera.Instance != null ? GameCamera.Instance.renderCamera : null;
            if (camera == null) return;
            _cameraLayerChecked = true;
            var layer = LayerMask.NameToLayer("Character");
            if (layer >= 0 && (camera.cullingMask & (1 << layer)) == 0)
                ModLogger.LogWarning(
                    "The active game camera excludes the named Character layer; the YSM model may not be visible.");
            if (LayerMask.NameToLayer("SpecialCamera") < 0)
                ModLogger.LogWarning(
                    "YSM cannot find the named SpecialCamera layer used by character visibility controls.");
        }

        public bool PlayExtra(string animation)
        {
            if (_disposed || string.IsNullOrWhiteSpace(animation) ||
                !Simulation.Document.Animations.ContainsKey(animation)) return false;
            _extra = animation;
            unchecked
            {
                ++_extraSequence;
            }

            return true;
        }

        public void StopExtra()
        {
            _extra = string.Empty;
            unchecked
            {
                ++_extraSequence;
            }
        }

        private void OnExtraStopped(string animation, long sequence)
        {
            if (_extra == animation && _extraSequence == sequence) _extra = string.Empty;
        }

        private void MeasureNaturalHeight()
        {
            if (Locators.TryGetValue(SocketNames.Helmet, out var helmet))
                NaturalHeight = Root.transform.InverseTransformPoint(helmet.position).y * InitialRootScale.y;
            if (NaturalHeight > 0) return;
            var maximum = 0f;
            var minimum = 0f;
            foreach (var renderer in _renderer!.Renderers)
            {
                var bounds = renderer.localBounds;
                maximum = Mathf.Max(maximum, bounds.max.y);
                minimum = Mathf.Min(minimum, bounds.min.y);
            }

            NaturalHeight = (maximum - minimum) * Mathf.Abs(InitialRootScale.y);
        }

        private NumericsMatrix GetModelToWorld()
        {
            var m = Root.transform.localToWorldMatrix;
            var world = new NumericsMatrix(m.m00, m.m10, m.m20, m.m30,
                m.m01, m.m11, m.m21, m.m31, m.m02, m.m12, m.m22, m.m32, m.m03, m.m13, m.m23, m.m33);
            return NumericsMatrix.CreateScale(1f / 16f, 1f / 16f, -1f / 16f) * world;
        }

        private static void Release(Action release)
        {
            try
            {
                release();
            }
            catch (Exception exception)
            {
                ModLogger.LogError($"YSM resource cleanup failed: {exception.Message}");
            }
        }
    }
}
