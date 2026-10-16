using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.MonoBehaviours
{
    internal sealed class ModelLoadingVisual : IDisposable
    {
        private readonly List<(Renderer renderer, bool hidden)> _renderers = new();
        private GameObject? _root;
        private Mesh? _mesh;
        private Material? _material;
        private Texture2D? _texture;

        internal ModelLoadingVisual(Transform original, GameObject? current, float height)
        {
            try
            {
                var sources = new HashSet<Renderer>(original.GetComponentsInChildren<Renderer>(true));
                if (current != null) sources.UnionWith(current.GetComponentsInChildren<Renderer>(true));
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null || !shader.isSupported) shader = Shader.Find("SodaCraft/SodaCharacter");
                if (shader == null || !shader.isSupported)
                    foreach (var source in sources)
                        if (source != null && source.sharedMaterial != null)
                        {
                            shader = source.sharedMaterial.shader;
                            break;
                        }

                if (shader == null) throw new InvalidOperationException("Loading model shader is unavailable.");
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                var color = Color.white;
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", color);
                if (_material.HasProperty("_Color")) _material.SetColor("_Color", color);
                _texture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
                {
                    name = "Loading palette", hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                };
                _texture.SetPixels(new[] { new Color(.1f, .55f, .9f, .12f), new Color(.28f, .8f, 1, .9f) });
                _texture.Apply(false, true);
                if (_material.HasProperty("_BaseMap")) _material.SetTexture("_BaseMap", _texture);
                if (_material.HasProperty("_MainTex")) _material.SetTexture("_MainTex", _texture);
                if (_material.HasProperty("_Surface"))
                {
                    _material.SetFloat("_Surface", 1);
                    _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    _material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    _material.SetFloat("_ZWrite", 0);
                    if (_material.HasProperty("_Cull")) _material.SetFloat("_Cull", (float)CullMode.Off);
                    _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    _material.SetOverrideTag("RenderType", "Transparent");
                    _material.renderQueue = (int)RenderQueue.Transparent;
                }
                _mesh = CreateMesh();
                _root = new GameObject("LoadingModel", typeof(MeshFilter), typeof(MeshRenderer));
                _root.transform.SetParent(original, false);
                var scale = original.lossyScale;
                height = Mathf.Clamp(height > 0 ? height : 1.8f, .1f, 10);
                _root.transform.localScale = new(height / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                    height / Mathf.Max(.001f, Mathf.Abs(scale.y)), height / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                _root.transform.localPosition = original.InverseTransformVector(Vector3.up * (height * .62f));
                _root.AddComponent<ModelLoadingIndicator>().Initialize(height);
                var layer = LayerMask.NameToLayer("Character");
                _root.layer = layer >= 0 ? layer : original.gameObject.layer;
                _root.GetComponent<MeshFilter>().sharedMesh = _mesh;
                var renderer = _root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                foreach (var source in sources)
                {
                    if (source == null) continue;
                    _renderers.Add((source, source.forceRenderingOff));
                    source.forceRenderingOff = true;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static Mesh CreateMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();
            var points = new[]
            {
                new Vector3(0, .17f, 0), new(.105f, 0, 0), new(0, 0, .105f),
                new(-.105f, 0, 0), new(0, 0, -.105f), new(0, -.17f, 0),
            };
            for (var i = 0; i < 4; i++)
            {
                var a = points[i + 1];
                var b = points[(i + 1) % 4 + 1];
                AddTriangle(points[0], b, a, false);
                AddTriangle(points[5], a, b, false);
                AddEdge(points[0], a);
                AddEdge(points[5], a);
                AddEdge(a, b);
            }

            const int segments = 40;
            for (var i = 0; i < segments; i++)
            {
                var a = i * Mathf.PI * 2 / segments;
                var b = (i + 1) * Mathf.PI * 2 / segments;
                var outerA = new Vector3(Mathf.Cos(a) * .145f, -.23f, Mathf.Sin(a) * .145f);
                var innerA = new Vector3(Mathf.Cos(a) * .135f, -.23f, Mathf.Sin(a) * .135f);
                var outerB = new Vector3(Mathf.Cos(b) * .145f, -.23f, Mathf.Sin(b) * .145f);
                var innerB = new Vector3(Mathf.Cos(b) * .135f, -.23f, Mathf.Sin(b) * .135f);
                AddTriangle(innerA, outerB, outerA, i < 10);
                AddTriangle(innerA, innerB, outerB, i < 10);
            }

            var mesh = new Mesh { name = "Loading indicator", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            void AddTriangle(Vector3 a, Vector3 b, Vector3 c, bool bright)
            {
                var first = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                var sample = new Vector2(bright ? .75f : .25f, .5f);
                uv.Add(sample);
                uv.Add(sample);
                uv.Add(sample);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
            }

            void AddEdge(Vector3 start, Vector3 end)
            {
                var direction = (end - start).normalized;
                var side = Vector3.Cross(direction, Vector3.up).normalized * .002f;
                var other = Vector3.Cross(direction, side);
                var offsets = new[] { side, other, -side, -other };
                for (var i = 0; i < offsets.Length; i++)
                {
                    var a = offsets[i];
                    var b = offsets[(i + 1) % offsets.Length];
                    AddTriangle(start + a, end + b, end + a, true);
                    AddTriangle(start + a, start + b, end + b, true);
                }
            }
        }

        public void Dispose()
        {
            foreach (var (renderer, hidden) in _renderers)
                if (renderer != null)
                    renderer.forceRenderingOff = hidden;
            _renderers.Clear();
            if (_root != null)
            {
                _root.SetActive(false);
                Release(_root);
            }

            if (_material != null) Release(_material);
            if (_mesh != null) Release(_mesh);
            if (_texture != null) Release(_texture);
            _root = null;
            _material = null;
            _mesh = null;
            _texture = null;
        }

        private static void Release(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
    [AddComponentMenu("")]
    internal sealed class ModelLoadingIndicator : MonoBehaviour
    {
        private Vector3 _position;
        private float _height;

        internal void Initialize(float height)
        {
            _position = transform.localPosition;
            _height = height;
        }

        private void Update()
        {
            var phase = Time.unscaledTime;
            transform.localRotation = Quaternion.Euler(0, phase * 45 % 360, 0);
            if (transform.parent != null)
                transform.localPosition = _position + transform.parent.InverseTransformVector(
                    Vector3.up * (Mathf.Sin(phase * 2) * _height * .012f));
        }
    }
}
