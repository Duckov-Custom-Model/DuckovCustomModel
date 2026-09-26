using System;
using System.Collections.Generic;
using System.IO;
using ModelRuntime;
using ModelRuntime.Adapters;
using UnityEngine;
using UnityEngine.Rendering;
using NVector = System.Numerics.Vector3;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed partial class DuckovYsmRenderBackend : IModelRenderBackend
    {
        public const string ShaderBundleRelativePath = "Resources/ysm-rendering.bundle";
        private static AssetBundle? shaderBundle;
        private static Shader? sharedShader;
        private static Shader? sharedPreviewShader;
        private static int shaderUsers;
        private readonly ITexturePixelSource pixels;
        private readonly List<Renderer> renderers = new();
        private bool disposed;
        private GraphicsState? graphics;
        private Vector3 lastAmbientProbePosition;
        private PoseEvaluator? lastPose;
        private double nextAmbientProbeSample;
        private bool renderStateLogged;
        private Shader? shader;
        private bool thumbnailMode;

        public DuckovYsmRenderBackend(Transform root, ModelPackage package, ITexturePixelSource pixels)
        {
            Root = root ? root : throw new ArgumentNullException(nameof(root));
            if (package == null) throw new ArgumentNullException(nameof(package));
            this.pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
            shader = AcquireShader();
            try
            {
                RegisterPipeline(this);
            }
            catch
            {
                ReleaseShader();
                shader = null;
                throw;
            }
        }

        public Transform Root { get; }
        public IReadOnlyList<Renderer> Renderers => renderers;
        public int RendererVersion { get; private set; }
        public double TimeSeconds { get; set; }
        public int TextureIndex { get; private set; }

        public void Load(ModelDocument document, MeshData geometry)
        {
            LoadCore(document, geometry);
        }

        public void Render(PoseEvaluator pose)
        {
            ThrowIfDisposed();
            if (graphics == null) return;
            UpdatePose(graphics, pose);
            lastPose = pose;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            UnregisterPipeline(this);
            graphics?.Dispose();
            graphics = null;
            lastPose = null;
            renderers.Clear();
            if (shader)
            {
                ReleaseShader();
                shader = null;
            }
        }

        internal void SetThumbnailMode()
        {
            if (graphics != null)
                throw new InvalidOperationException("Thumbnail mode must be selected before loading a model.");
            thumbnailMode = true;
            shader = sharedPreviewShader ?? throw new InvalidOperationException("YSM preview shader is unavailable.");
        }

        internal void LoadSelected(ModelDocument document)
        {
            LoadCore(document, null);
        }

        private void LoadCore(ModelDocument document, MeshData? geometry)
        {
            ThrowIfDisposed();
            var selected = Math.Max(0, Math.Min(document.DefaultTexture, document.Textures.Count - 1));
            if (document.Format.StartsWith("ysm", StringComparison.Ordinal) && document.Textures.Count > 0)
                geometry = MeshBaker.Bake(document, new SelectedPixels(pixels, document, selected));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            var next = BuildGraphics(document, geometry, selected);
            SwapGraphics(next, selected);
            lastPose = null;
        }

        public void SetTextureIndex(int index)
        {
            ThrowIfDisposed();
            var current = graphics ?? throw new InvalidOperationException("No model is loaded.");
            if (index < 0 || index >= current.Document.Textures.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index == TextureIndex) return;
            var source = new SelectedPixels(pixels, current.Document, index);
            var geometry = MeshBaker.Bake(current.Document, source);
            var next = BuildGraphics(current.Document, geometry, index);
            try
            {
                if (lastPose != null) UpdatePose(next, lastPose);
            }
            catch
            {
                next.Dispose();
                throw;
            }

            SwapGraphics(next, index);
        }

        private void SwapGraphics(GraphicsState next, int selected)
        {
            var previous = graphics;
            if (previous != null)
            {
                next.Object.layer = previous.Object.layer;
                next.TransparentObject.layer = previous.TransparentObject.layer;
                next.Renderer.renderingLayerMask = previous.Renderer.renderingLayerMask;
                next.Renderer.enabled = previous.Renderer.enabled;
                next.Renderer.forceRenderingOff = previous.Renderer.forceRenderingOff;
                next.TransparentRenderer.renderingLayerMask = previous.TransparentRenderer.renderingLayerMask;
                next.TransparentRenderer.forceRenderingOff = previous.TransparentRenderer.forceRenderingOff;
            }

            graphics = next;
            TextureIndex = selected;
            renderers.Clear();
            renderers.Add(next.Renderer);
            renderers.Add(next.TransparentRenderer);
            unchecked
            {
                ++RendererVersion;
            }

            next.Object.SetActive(true);
            previous?.Dispose();
        }

        private void UpdatePose(GraphicsState state, PoseEvaluator pose)
        {
            var baked = state.Geometry;
            for (var bone = 0; bone < state.Collapsed.Length; bone++)
            {
                var matrix = pose.Matrices[bone];
                var collapsed = CollapsedToLine(matrix);
                if (!collapsed)
                    for (var ancestor = bone; ancestor >= 0; ancestor = state.Document.Bones[ancestor].Parent)
                    {
                        var scale = pose.Scales[ancestor];
                        if ((scale.X == 0 ? 1 : 0) + (scale.Y == 0 ? 1 : 0) + (scale.Z == 0 ? 1 : 0) < 2)
                            continue;
                        collapsed = true;
                        break;
                    }

                state.Collapsed[bone] = collapsed;
                state.RequiresTriangleCheck[bone] = !collapsed && !HasVolume(matrix);
            }

            baked.Transform(pose, state.CpuVertices, state.CpuNormals, state.Collapsed);
            var colorsChanged = !state.ColorsUploaded;
            var glowChanged = !state.GlowUploaded;
            for (var i = 0; i < state.Vertices.Length; i++)
            {
                var bone = baked.BoneIndices[i];
                if (state.Collapsed[bone]) continue;
                state.Vertices[i] = DuckovYsmCoordinates.ToUnityPosition(state.CpuVertices[i]);
                state.Normals[i] = DuckovYsmCoordinates.ToUnityDirection(state.CpuNormals[i]);
                var color = pose.Colors[bone];
                var nextColor = new Color(color.X, color.Y, color.Z, color.W);
                var previousColor = state.Colors[i];
                if (previousColor.r != nextColor.r || previousColor.g != nextColor.g ||
                    previousColor.b != nextColor.b || previousColor.a != nextColor.a) colorsChanged = true;
                state.Colors[i] = nextColor;
                var nextGlow = new Vector2(pose.Glow[bone] >= 0 ? pose.Glow[bone] : pose.FullBright[bone] ? 15 : -1, 0);
                if (state.Glow[i].x != nextGlow.x) glowChanged = true;
                state.Glow[i] = nextGlow;
            }

            state.Mesh.vertices = state.Vertices;
            state.Mesh.normals = state.Normals;
            if (colorsChanged) state.Mesh.colors = state.Colors;
            if (glowChanged) state.Mesh.uv2 = state.Glow;
            state.Mesh.RecalculateBounds();
            state.TransparentMesh.vertices = state.Vertices;
            state.TransparentMesh.normals = state.Normals;
            if (colorsChanged) state.TransparentMesh.colors = state.Colors;
            if (glowChanged) state.TransparentMesh.uv2 = state.Glow;
            state.TransparentMesh.bounds = state.Mesh.bounds;
            state.ColorsUploaded = true;
            state.GlowUploaded = true;
            state.TransparentCount = 0;
            for (var partition = 0; partition < baked.Partitions.Length; partition++)
            {
                var source = baked.Partitions[partition];
                var indices = state.SolidIndices[partition];
                var solid = 0;
                var indicesChanged = state.SolidCounts[partition] < 0;
                for (var triangle = 0; triangle < source.Indices.Length; triangle += 3)
                {
                    int a = source.Indices[triangle],
                        b = source.Indices[triangle + 1],
                        c = source.Indices[triangle + 2];
                    var bone = baked.BoneIndices[a];
                    if (state.Collapsed[bone] || pose.Colors[bone].W <= 0 ||
                        (state.RequiresTriangleCheck[bone] || baked.BoneIndices[b] != bone ||
                         baked.BoneIndices[c] != bone) && !baked.IsTriangleRenderable(pose, a, b, c)) continue;
                    if (source.AlphaMode == MeshAlphaMode.Opaque && pose.Colors[bone].W >= 1)
                    {
                        if (indices[solid] != c || indices[solid + 1] != b || indices[solid + 2] != a)
                            indicesChanged = true;
                        indices[solid++] = c;
                        indices[solid++] = b;
                        indices[solid++] = a;
                    }
                    else
                    {
                        state.Order[state.TransparentCount++] = new() { Partition = partition, Triangle = triangle };
                    }
                }

                if (state.SolidCounts[partition] != solid) indicesChanged = true;
                state.SolidCounts[partition] = solid;
                if (indicesChanged) state.Mesh.SetTriangles(indices, 0, solid, partition, false);
            }

            BindTextureFrames(state);
            if (!thumbnailMode) BindAmbientProbe(state);
            if (!renderStateLogged && Root.gameObject.activeInHierarchy)
            {
                renderStateLogged = true;
                var solidTriangles = 0;
                for (var partition = 0; partition < state.Mesh.subMeshCount; partition++)
                    solidTriangles += (int)(state.Mesh.GetIndexCount(partition) / 3);
                var solidMaterial = state.SolidMaterials.Length > 0 ? state.SolidMaterials[0] : null;
                var blendMaterial = state.TransparentMaterials.Length > 0 ? state.TransparentMaterials[0] : null;
                var opaqueState = solidMaterial == null
                    ? "none"
                    : $"{solidMaterial.renderQueue}/{solidMaterial.GetFloat("_SrcBlend")}/{solidMaterial.GetFloat("_DstBlend")}/{solidMaterial.GetFloat("_ZWrite")}";
                var transparentState = blendMaterial == null
                    ? "none"
                    : $"{blendMaterial.renderQueue}/{blendMaterial.GetFloat("_SrcBlend")}/{blendMaterial.GetFloat("_DstBlend")}/{blendMaterial.GetFloat("_ZWrite")}";
                ModLogger.Log($"YSM render state: shader={shader?.name}, supported={shader?.isSupported}, " +
                              $"solid={solidTriangles}, transparent={state.TransparentCount}, layer={state.Object.layer}, " +
                              $"rendererEnabled={state.Renderer.enabled}, forceOff={state.Renderer.forceRenderingOff}, " +
                              $"opaqueQueue/Src/Dst/ZWrite={opaqueState}, transparentQueue/Src/Dst/ZWrite={transparentState}");
            }
        }

        private static bool CollapsedToLine(System.Numerics.Matrix4x4 matrix)
        {
            return !HasArea(matrix.M11, matrix.M12, matrix.M13, matrix.M21, matrix.M22, matrix.M23) &&
                   !HasArea(matrix.M11, matrix.M12, matrix.M13, matrix.M31, matrix.M32, matrix.M33) &&
                   !HasArea(matrix.M21, matrix.M22, matrix.M23, matrix.M31, matrix.M32, matrix.M33);
        }

        private static bool HasVolume(System.Numerics.Matrix4x4 matrix)
        {
            double translation = (double)matrix.M41 + matrix.M42 + matrix.M43;
            double determinant = matrix.M11 * ((double)matrix.M22 * matrix.M33 - (double)matrix.M23 * matrix.M32) -
                                 matrix.M12 * ((double)matrix.M21 * matrix.M33 - (double)matrix.M23 * matrix.M31) +
                                 matrix.M13 * ((double)matrix.M21 * matrix.M32 - (double)matrix.M22 * matrix.M31);
            return determinant != 0 && !double.IsNaN(determinant) && !double.IsInfinity(determinant) &&
                   !double.IsNaN(translation) && !double.IsInfinity(translation);
        }

        private static bool HasArea(double ax, double ay, double az, double bx, double by, double bz)
        {
            return ay * bz - az * by != 0 || az * bx - ax * bz != 0 || ax * by - ay * bx != 0;
        }

        private GraphicsState BuildGraphics(ModelDocument document, MeshData geometry, int selected)
        {
            var state = new GraphicsState(document, geometry);
            try
            {
                state.Object = new("YSM geometry");
                state.Object.SetActive(false);
                state.Object.layer = Root.gameObject.layer;
                state.Object.transform.SetParent(Root, false);
                state.Mesh = NewMesh(document.Name, geometry.Positions.Length);
                state.TransparentMesh = NewMesh(document.Name + " transparency", geometry.Positions.Length);
                var uv = new Vector2[state.Vertices.Length];
                var animation = new Vector2[uv.Length];
                for (var i = 0; i < uv.Length; i++)
                {
                    state.Vertices[i] = DuckovYsmCoordinates.ToUnityPosition(geometry.Positions[i]);
                    uv[i] = new(geometry.Uvs[i].X, geometry.Uvs[i].Y);
                    animation[i] =
                        new(geometry.AnimateTexture.Length == uv.Length && !geometry.AnimateTexture[i] ? 1 : 0, 0);
                }

                foreach (var mesh in new[] { state.Mesh, state.TransparentMesh })
                {
                    mesh.vertices = state.Vertices;
                    mesh.uv = uv;
                    mesh.uv3 = animation;
                    mesh.RecalculateBounds();
                }

                state.Mesh.subMeshCount = geometry.Partitions.Length;
                for (var i = 0; i < Math.Max(1, document.Textures.Count); i++)
                    if (document.Textures.Count == 0)
                    {
                        state.Textures.Add(CreateWhiteTexture());
                    }
                    else
                    {
                        if (!pixels.TryGetPixels(document.Textures[i], out var image))
                            throw new InvalidOperationException("Cannot decode model texture: " +
                                                                document.Textures[i].Name);
                        state.Textures.Add(UploadTexture(image, document.Textures[i].Name));
                    }

                for (var i = 0; i < geometry.Partitions.Length; i++)
                {
                    var draw = geometry.Partitions[i];
                    var texture = document.Format.StartsWith("ysm", StringComparison.Ordinal)
                        ? selected
                        : Math.Max(0, Math.Min(draw.TextureIndex, state.Textures.Count - 1));
                    state.TextureBindings[i] = texture;
                    state.SolidMaterials[i] = CreateMaterial(draw, state.Textures[texture], false);
                    state.TransparentMaterials[i] = CreateMaterial(draw, state.Textures[texture], true);
                }

                state.Object.AddComponent<MeshFilter>().sharedMesh = state.Mesh;
                state.Renderer = state.Object.AddComponent<MeshRenderer>();
                state.Renderer.sharedMaterials = state.SolidMaterials;
                state.Renderer.shadowCastingMode = ShadowCastingMode.On;
                state.Renderer.receiveShadows = true;
                state.TransparentObject = new("YSM transparency");
                state.TransparentObject.layer = state.Object.layer;
                state.TransparentObject.transform.SetParent(state.Object.transform, false);
                state.TransparentObject.AddComponent<MeshFilter>().sharedMesh = state.TransparentMesh;
                state.TransparentRenderer = state.TransparentObject.AddComponent<MeshRenderer>();
                if (state.TransparentMaterials.Length > 0)
                    state.TransparentRenderer.sharedMaterial = state.TransparentMaterials[0];
                state.TransparentRenderer.shadowCastingMode = ShadowCastingMode.Off;
                state.TransparentRenderer.receiveShadows = true;
                state.TransparentRenderer.enabled = false;
                BindTextureFrames(state);
                if (!thumbnailMode) BindAmbientProbe(state, true);
                return state;
            }
            catch
            {
                state.Dispose();
                throw;
            }
        }

        private Material CreateMaterial(MeshPartition partition, Texture2D texture, bool transparent)
        {
            var material = new Material(shader!)
            {
                name = "YSM " + (transparent ? "transparent" : "opaque"), mainTexture = texture,
                renderQueue = transparent ? 3000 : 2000,
            };
            material.SetFloat("_Cull", partition.CullBackFaces ? 2 : 0);
            material.SetFloat("_SrcBlend", transparent ? 5 : 1);
            material.SetFloat("_DstBlend", transparent ? 10 : 0);
            material.SetFloat("_ZWrite", transparent ? 0 : 1);
            material.SetFloat("_ForceOpaque", transparent ? 0 : 1);
            return material;
        }

        private void BindTextureFrames(GraphicsState state)
        {
            for (var i = 0; i < state.TextureBindings.Length; i++)
            {
                var index = state.TextureBindings[i];
                if (index >= state.Document.Textures.Count) continue;
                var texture = state.Textures[index];
                var frame = TextureAnimationSampler.Sample(state.Document.Textures[index], texture.width,
                    texture.height, TimeSeconds, TextureAnimationPlayback.DeclaredSequence);
                BindFrame(state.SolidMaterials[i], frame);
                BindFrame(state.TransparentMaterials[i], frame);
            }
        }

        private void BindAmbientProbe(GraphicsState state, bool force = false)
        {
            var position = Root.TransformPoint(state.Mesh.bounds.center);
            if (!force && TimeSeconds < nextAmbientProbeSample &&
                (position - lastAmbientProbePosition).sqrMagnitude < .25f) return;
            lastAmbientProbePosition = position;
            nextAmbientProbeSample = TimeSeconds + .25;
            var sh = RenderSettings.ambientProbe;
            if (LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.count > 0)
                LightProbes.GetInterpolatedProbe(position, state.Renderer, out sh);
            for (var i = 0; i < state.SolidMaterials.Length; i++)
            {
                BindAmbientProbe(state.SolidMaterials[i], sh);
                BindAmbientProbe(state.TransparentMaterials[i], sh);
            }
        }

        private static void BindAmbientProbe(Material material, SphericalHarmonicsL2 sh)
        {
            material.SetVector("_YsmSHAr", new(sh[0, 3], sh[0, 1], sh[0, 2], sh[0, 0] - sh[0, 6]));
            material.SetVector("_YsmSHAg", new(sh[1, 3], sh[1, 1], sh[1, 2], sh[1, 0] - sh[1, 6]));
            material.SetVector("_YsmSHAb", new(sh[2, 3], sh[2, 1], sh[2, 2], sh[2, 0] - sh[2, 6]));
            material.SetVector("_YsmSHBr", new(sh[0, 4], sh[0, 5], 3 * sh[0, 6], sh[0, 7]));
            material.SetVector("_YsmSHBg", new(sh[1, 4], sh[1, 5], 3 * sh[1, 6], sh[1, 7]));
            material.SetVector("_YsmSHBb", new(sh[2, 4], sh[2, 5], 3 * sh[2, 6], sh[2, 7]));
            material.SetVector("_YsmSHC", new(sh[0, 8], sh[1, 8], sh[2, 8], 1));
        }

        private static void BindFrame(Material material, TextureFrameSample frame)
        {
            material.SetVector("_FrameUv", new(frame.Scale.X, frame.Scale.Y, frame.Offset.X, frame.Offset.Y));
            material.SetVector("_NextFrameUv",
                new(frame.Scale.X, frame.Scale.Y, frame.NextOffset.X, frame.NextOffset.Y));
            material.SetFloat("_FrameBlend", frame.Blend);
        }

        private static Mesh NewMesh(string name, int vertices)
        {
            var mesh = new Mesh
                { name = name, indexFormat = vertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.MarkDynamic();
            return mesh;
        }

        private static Texture2D UploadTexture(TexturePixelData image, string name)
        {
            var texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false, false)
                { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            try
            {
                var data = image.Rgba.Span;
                var colors = new Color32[image.Width * image.Height];
                for (var y = 0; y < image.Height; y++)
                for (var x = 0; x < image.Width; x++)
                {
                    var offset = (y * image.Width + x) * 4;
                    colors[(image.Height - y - 1) * image.Width + x] = new(data[offset], data[offset + 1],
                        data[offset + 2], data[offset + 3]);
                }

                texture.SetPixels32(colors);
                texture.Apply(false, true);
                return texture;
            }
            catch
            {
                Release(texture);
                throw;
            }
        }

        private static Texture2D CreateWhiteTexture()
        {
            return UploadTexture(new(1, 1, new byte[] { 255, 255, 255, 255 }), "Untextured");
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(DuckovYsmRenderBackend));
        }

        internal static void Release(Object? resource)
        {
            if (!resource) return;
            if (Application.isPlaying) Object.Destroy(resource);
            else Object.DestroyImmediate(resource);
        }

        private static Shader AcquireShader()
        {
            if (sharedShader)
            {
                shaderUsers++;
                return sharedShader!;
            }

            var path = Path.Combine(
                ModEntry.ModDirectory ?? throw new InvalidOperationException("Mod directory is unavailable."),
                ShaderBundleRelativePath);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "YSM rendering requires the shader bundle matching the loaded game module.", path);
            var bundle = AssetBundle.LoadFromFile(path);
            if (!bundle) throw new InvalidOperationException("Cannot load YSM shader bundle: " + path);
            var loaded = bundle.LoadAsset<Shader>("Assets/DuckovYsm.shader");
            var preview = bundle.LoadAsset<Shader>("Assets/DuckovYsmPreview.shader");
            if (!loaded || !loaded.isSupported || !preview || !preview.isSupported)
            {
                bundle.Unload(true);
                throw new InvalidOperationException(
                    "YSM URP shaders are missing or unsupported on this graphics device.");
            }

            shaderBundle = bundle;
            sharedShader = loaded;
            sharedPreviewShader = preview;
            shaderUsers = 1;
            return loaded;
        }

        private static void ReleaseShader()
        {
            if (--shaderUsers > 0) return;
            shaderUsers = 0;
            sharedShader = null;
            sharedPreviewShader = null;
            if (shaderBundle) shaderBundle!.Unload(true);
            shaderBundle = null;
        }

        private sealed class SelectedPixels : ITexturePixelSource
        {
            private readonly ModelDocument document;
            private readonly int selected;
            private readonly ITexturePixelSource source;

            internal SelectedPixels(ITexturePixelSource source, ModelDocument document, int selected)
            {
                this.source = source;
                this.document = document;
                this.selected = selected;
            }

            public bool TryGetPixels(TextureAsset texture, out TexturePixelData data)
            {
                return source.TryGetPixels(
                    document.Format.StartsWith("ysm", StringComparison.Ordinal) ? document.Textures[selected] : texture,
                    out data);
            }
        }

        private sealed class GraphicsState : IDisposable
        {
            internal readonly bool[] Collapsed;
            internal readonly bool[] RequiresTriangleCheck;
            internal readonly Color[] Colors;
            internal readonly NVector[] CpuVertices, CpuNormals;
            internal readonly ModelDocument Document;
            internal readonly MeshData Geometry;
            internal readonly Vector2[] Glow;
            internal readonly TriangleDepth[] Order;
            internal readonly List<TransparentRun> Runs = new();
            internal readonly int[][] SolidIndices;
            internal readonly int[] SolidCounts;
            internal readonly Material[] SolidMaterials, TransparentMaterials;
            internal readonly int[] SortedIndices, TextureBindings;
            internal readonly List<Texture2D> Textures = new();
            internal readonly Vector3[] Vertices, Normals;
            internal Mesh Mesh = null!, TransparentMesh = null!;
            internal GameObject Object = null!, TransparentObject = null!;
            internal MeshRenderer Renderer = null!, TransparentRenderer = null!;
            internal Material[] RunMaterials = Array.Empty<Material>();
            internal int TransparentCount;
            internal bool TransparentIndicesInitialized;
            internal bool ColorsUploaded, GlowUploaded;

            internal GraphicsState(ModelDocument document, MeshData geometry)
            {
                Document = document;
                Geometry = geometry;
                var count = geometry.Positions.Length;
                CpuVertices = new NVector[count];
                CpuNormals = new NVector[count];
                Vertices = new Vector3[count];
                Normals = new Vector3[count];
                Colors = new Color[count];
                Glow = new Vector2[count];
                Collapsed = new bool[document.Bones.Count];
                RequiresTriangleCheck = new bool[document.Bones.Count];
                int partitions = geometry.Partitions.Length, triangles = 0;
                SolidIndices = new int[partitions][];
                SolidCounts = new int[partitions];
                for (var i = 0; i < partitions; i++)
                {
                    SolidCounts[i] = -1;
                    SolidIndices[i] = new int[geometry.Partitions[i].Indices.Length];
                    triangles += SolidIndices[i].Length / 3;
                }

                SortedIndices = new int[triangles * 3];
                Order = new TriangleDepth[triangles];
                TextureBindings = new int[partitions];
                SolidMaterials = new Material[partitions];
                TransparentMaterials = new Material[partitions];
            }

            public void Dispose()
            {
                if (Object) Object.SetActive(false);
                Release(Object);
                Release(Mesh);
                Release(TransparentMesh);
                foreach (var material in SolidMaterials) Release(material);
                foreach (var material in TransparentMaterials) Release(material);
                foreach (var texture in Textures) Release(texture);
            }
        }
    }
}
