using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using ModelRuntime;
using ModelRuntime.Adapters;
using ModelRuntime.Media;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vector2 = System.Numerics.Vector2;

namespace DuckovCustomModel.Integrations.Ysm
{
    internal sealed class YsmAnimatedThumbnail : MonoBehaviour
    {
        private const double FrameInterval = .1;
        private const int Width = 128, Height = 172;
        private const float ViewWidth = 52f / 30f;
        private const float ViewHeight = Height * ViewWidth / Width;
        private static readonly Vector3 Origin = new(0, -10000, 0);
        private static readonly Stack<int> FreePositions = new();
        private static int nextPosition, lastCreatedFrame = -1;
        private ModelBundleInfo? bundle;
        private RawImage? image;
        private ModelInfo? model;
        private double nextRenderAt;
        private YsmThumbnailSession? session;
        private RectTransform? viewport;

        private void LateUpdate()
        {
            if (bundle == null || model == null || image == null || !Visible())
            {
                ReleaseSession();
                return;
            }

            var now = Time.unscaledTimeAsDouble;
            if (now < nextRenderAt) return;
            nextRenderAt = now + FrameInterval;
            try
            {
                if (session == null)
                {
                    if (lastCreatedFrame == Time.frameCount) return;
                    lastCreatedFrame = Time.frameCount;
                    session = new(bundle, model);
                    image.texture = session.Texture;
                    image.uvRect = new(0, 0, 1, 1);
                }

                session.Sample(now);
                session.Render();
            }
            catch (Exception ex)
            {
                ModLogger.LogWarning("YSM animated thumbnail could not be rendered: " + ex.Message);
                ReleaseSession();
                enabled = false;
            }
        }

        private void OnDisable()
        {
            ReleaseSession();
        }

        private void OnDestroy()
        {
            ReleaseSession();
        }

        internal void Initialize(ModelBundleInfo sourceBundle, ModelInfo sourceModel, RawImage target,
            RectTransform? scrollViewport)
        {
            ReleaseSession();
            bundle = sourceBundle;
            model = sourceModel;
            image = target;
            viewport = scrollViewport;
            target.raycastTarget = false;
            target.color = Color.white;
        }

        private bool Visible()
        {
            if (viewport == null) return true;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, transform);
            var rect = viewport.rect;
            return bounds.max.x > rect.xMin && bounds.min.x < rect.xMax
                                            && bounds.max.y > rect.yMin && bounds.min.y < rect.yMax;
        }

        private void ReleaseSession()
        {
            if (image != null) image.texture = null;
            session?.Dispose();
            session = null;
            nextRenderAt = 0;
        }

        private sealed class YsmThumbnailSession : IDisposable
        {
            private ModelAdapter? adapter;
            private DuckovYsmRenderBackend? backend;
            private Camera? camera;
            private GameObject? cameraObject;
            private CommandBuffer? commands;
            private double lastSampleAt;
            private YsmModelLease? lease;
            private int position = -1;
            private GameObject? root;
            private bool sampled;
            private ModelSimulation? simulation;
            private RenderTexture? texture;

            internal YsmThumbnailSession(ModelBundleInfo bundle, ModelInfo model)
            {
                try
                {
                    position = FreePositions.Count > 0 ? FreePositions.Pop() : nextPosition++;
                    var center = Origin + Vector3.right * (position * 1024f);
                    lease = YsmModelSource.Acquire(bundle, model);
                    var document = lease.Package.Models[lease.Profile.ModelTarget];
                    root = new("YSM animated thumbnail") { layer = 30 };
                    var scale = document.DisplayScale;
                    root.transform.localScale = new Vector3(scale.X, scale.Y, scale.Z) * lease.Profile.Scale;
                    var presentation = YsmPresentationSettings.From(document);
                    var direction = presentation.DisablePreviewRotation
                        ? Vector3.forward
                        : new(0f, Mathf.Sin(10f * Mathf.Deg2Rad), Mathf.Cos(10f * Mathf.Deg2Rad));
                    var viewRotation = Quaternion.Inverse(Quaternion.LookRotation(-direction, Vector3.up));
                    if (!presentation.DisablePreviewRotation)
                        viewRotation *= Quaternion.AngleAxis(-20f, Vector3.up);
                    var targetOffset = Vector3.up * ((30f + (presentation.DisablePreviewRotation ? 5.5f : 0f)) / 30f);
                    root.transform.SetPositionAndRotation(center - targetOffset, viewRotation);
                    backend = new(root.transform, lease.Package, new StandardTexturePixelSource());
                    backend.SetThumbnailMode();
                    backend.LoadSelected(document);
                    simulation = new(lease.Package, lease.Profile.ModelTarget,
                            new()
                            {
                                Dialect = MolangDialect.Ysm, ValueCompatibility = lease.Profile.ValueCompatibility,
                            })
                        { UseFallbackMainController = true };
                    var stateProvider = new ThumbnailStateProvider(document, presentation.DisablePreviewRotation);
                    adapter = new(simulation, lease.Package, stateProvider, renderer: null);
                    adapter.AnimationPolicy.HandCompatibility = lease.Profile.HandCompatibility;
                    stateProvider.PreviewEnabled = true;
                    texture = new(Width, Height, 16, RenderTextureFormat.ARGB32)
                        { name = "YSM thumbnail", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    if (!texture.Create())
                        throw new InvalidOperationException("Cannot allocate the YSM thumbnail texture.");
                    cameraObject = new("YSM thumbnail camera");
                    camera = cameraObject.AddComponent<Camera>();
                    camera.enabled = false;
                    commands = new() { name = "YSM thumbnail" };
                    camera.cullingMask = 1 << 30;
                    camera.orthographic = true;
                    camera.orthographicSize = ViewHeight * .5f;
                    camera.aspect = (float)Width / Height;
                    camera.nearClipPlane = .01f;
                    camera.farClipPlane = 100f;
                    camera.backgroundColor = new(.11f, .14f, .18f, 1);
                    camera.transform.SetPositionAndRotation(center - Vector3.forward * 10f, Quaternion.identity);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal RenderTexture Texture => texture!;

            public void Dispose()
            {
                commands?.Dispose();
                commands = null;
                if (texture != null)
                {
                    texture.Release();
                    Destroy(texture);
                }

                if (cameraObject) Destroy(cameraObject);
                adapter?.Dispose();
                adapter = null;
                backend?.Dispose();
                backend = null;
                lease?.Dispose();
                lease = null;
                if (root != null)
                {
                    root.SetActive(false);
                    Destroy(root);
                }

                root = null;
                camera = null;
                cameraObject = null;
                texture = null;
                simulation = null;
                if (position >= 0)
                {
                    FreePositions.Push(position);
                    position = -1;
                }
            }

            internal void Sample(double now)
            {
                var delta = sampled ? Math.Max(0, now - lastSampleAt) : 0;
                lastSampleAt = now;
                sampled = true;
                backend!.TimeSeconds = simulation!.Time + delta;
                adapter!.Advance(delta);
                backend.Render(simulation!.Pose);
            }

            internal void Render()
            {
                backend!.RenderThumbnail(camera!, texture!, commands!);
            }

            private sealed class ThumbnailStateProvider : IEntityStateProvider
            {
                private readonly float bodyYaw;
                private readonly string previewAnimation;
                private readonly string textureName;

                internal ThumbnailStateProvider(ModelDocument document, bool disablePreviewRotation)
                {
                    bodyYaw = disablePreviewRotation ? 180f : 200f;
                    previewAnimation = document.Animations.ContainsKey(document.PreferredAnimation)
                        ? document.PreferredAnimation
                        : document.Animations.ContainsKey("gui")
                            ? "gui"
                            : string.Empty;
                    textureName = document.Textures.Count > 0
                        ? document.Textures[Mathf.Clamp(document.DefaultTexture, 0, document.Textures.Count - 1)].Name
                        : string.Empty;
                }

                internal bool PreviewEnabled { get; set; }

                public void Capture(EntityState state, double deltaSeconds)
                {
                    state.Id = "thumbnail";
                    state.Flags = EntityFlags.OnGround;
                    state.TextureName = textureName;
                    state.WorldDayTime = 6000;
                    state.RenderingInPaperdoll = true;
                    state.BodyRotation = new(0, bodyYaw, 0);
                    state.ViewRotation = new(0, bodyYaw);
                    state.HeadRotation = Vector2.Zero;
                    state.Preview = PreviewEnabled && previewAnimation.Length > 0;
                    state.PreviewAnimation = previewAnimation;
                }
            }
        }
    }
}
