using System;
using System.Collections.Generic;
using System.Linq;
using UniGLTF;
using UnityEngine;
using UniVRM10;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal static class VrmTemplateCache
    {
        private static readonly Dictionary<string, Entry> Entries = new(StringComparer.Ordinal);
        private static readonly HashSet<string> PinnedKeys = new(StringComparer.Ordinal);
        private static long _sequence;

        internal static bool Contains(string key)
        {
            return Entries.TryGetValue(key, out var entry) && entry.Template != null && !entry.Retired;
        }

        internal static void SetPinnedKeys(IEnumerable<string> keys)
        {
            var pinned = keys.ToArray();
            PinnedKeys.Clear();
            foreach (var key in pinned)
                if (!string.IsNullOrEmpty(key))
                    PinnedKeys.Add(key);
            TrimIdle();
        }

        internal static void Store(string key, GameObject pristineRoot)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Missing template key.", nameof(key));
            if (pristineRoot == null) throw new ArgumentNullException(nameof(pristineRoot));
            var instance = pristineRoot.GetComponent<Vrm10Instance>();
            if (instance == null || pristineRoot.GetComponent<VrmResourceOwner>() == null ||
                pristineRoot.transform.parent != null)
                throw new ArgumentException("A pristine, owned VRM root is required.", nameof(pristineRoot));
            if (Entries.TryGetValue(key, out var previous) && previous.Template == pristineRoot) return;
            pristineRoot.SetActive(false);
            instance.UpdateType = Vrm10Instance.UpdateTypes.None;
            instance.enabled = false;
            instance.DisposeRuntime();
            if (Application.isPlaying) Object.DontDestroyOnLoad(pristineRoot);
            var entry = new Entry(key, pristineRoot) { LastUsed = ++_sequence };
            Entries[key] = entry;
            if (previous != null) Retire(previous);
            TrimIdle();
        }

        internal static bool TryCreate(string key, out GameObject root)
        {
            root = null!;
            if (!Entries.TryGetValue(key, out var entry) || entry.Template == null || entry.Retired) return false;
            entry.References++;
            entry.LastUsed = ++_sequence;
            GameObject? clone = null;
            VrmTemplateInstance? lease = null;
            try
            {
                clone = Object.Instantiate(entry.Template);
                clone.SetActive(false);
                clone.hideFlags = HideFlags.None;
                var imported = clone.GetComponent<RuntimeGltfInstance>();
                if (imported != null) Object.DestroyImmediate(imported);
                var importedOwner = clone.GetComponent<VrmResourceOwner>();
                if (importedOwner != null) Object.DestroyImmediate(importedOwner);
                var instance = clone.GetComponent<Vrm10Instance>();
                if (instance == null) throw new InvalidOperationException("Cloned VRM has no runtime component.");
                lease = clone.AddComponent<VrmTemplateInstance>();
                lease.Entry = entry;
                lease.Instance = instance;
                instance.enabled = false;
                instance.UpdateType = Vrm10Instance.UpdateTypes.None;
                var sharedSpringProvider = clone.GetComponent<Vrm10FastSpringboneRuntimeProvider>();
                if (sharedSpringProvider != null) Object.DestroyImmediate(sharedSpringProvider);
                var standaloneSpringProvider = clone.GetComponent<Vrm10FastSpringboneRuntimeStandaloneProvider>();
                if (standaloneSpringProvider != null) Object.DestroyImmediate(standaloneSpringProvider);
                if (clone.GetComponent<VrmSpringBoneRuntimeProvider>() == null)
                    clone.AddComponent<VrmSpringBoneRuntimeProvider>();
                var animator = clone.GetComponent<Animator>();
                var animatorEnabled = animator != null && animator.enabled;
                if (animator != null) animator.enabled = false;
                var renderers = clone.GetComponentsInChildren<Renderer>(true);
                var rendererEnabled = new bool[renderers.Length];
                for (var i = 0; i < renderers.Length; i++)
                {
                    rendererEnabled[i] = renderers[i].enabled;
                    renderers[i].enabled = false;
                }

                var initialTransforms =
                    VrmSpringBoneCloner.Copy(entry.Template.GetComponent<Vrm10Instance>(), instance);
                var originalActive = new bool[initialTransforms.Length];
                for (var i = 0; i < initialTransforms.Length; i++)
                {
                    originalActive[i] = initialTransforms[i].gameObject.activeSelf;
                    if (initialTransforms[i] != clone.transform) initialTransforms[i].gameObject.SetActive(true);
                }

                clone.SetActive(true);
                var runtime = instance.Runtime;
                if (runtime.SpringBone is not VrmSpringBoneRuntime ||
                    !entry.SpringRuntimes.Add(runtime.SpringBone))
                    throw new InvalidOperationException("Cloned VRM did not create an independent spring runtime.");
                lease.SpringRuntime = runtime.SpringBone;
                foreach (var transform in initialTransforms)
                    if (!runtime.InitPose.ContainsKey(transform))
                        throw new InvalidOperationException("Cloned VRM has no initial pose for " + transform.name);
                foreach (var constraint in runtime.Constraints)
                    if (constraint.ConstraintSource != null &&
                        (!runtime.InitPose.ContainsKey(constraint.ConstraintSource) ||
                         constraint.ConstraintTarget == null ||
                         !runtime.InitPose.ContainsKey(constraint.ConstraintTarget)))
                        throw new InvalidOperationException("Cloned VRM constraint has an incomplete initial pose.");
                foreach (var spring in instance.SpringBone.Springs)
                {
                    if (spring.Center != null && !spring.Center.IsChildOf(clone.transform))
                        throw new InvalidOperationException("Cloned VRM spring center references another instance.");
                    foreach (var joint in spring.Joints)
                        if (joint != null && !joint.transform.IsChildOf(clone.transform))
                            throw new InvalidOperationException("Cloned VRM spring joint references another instance.");
                    foreach (var collider in spring.ColliderGroups)
                        if (collider != null && !collider.transform.IsChildOf(clone.transform))
                            throw new InvalidOperationException(
                                "Cloned VRM spring collider references another instance.");
                }

                foreach (var renderer in renderers)
                {
                    if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                    foreach (var material in renderer.sharedMaterials)
                        if (material != null && entry.Materials.Contains(material))
                            throw new InvalidOperationException("Cloned VRM retained a shared mutable material.");
                }

                clone.SetActive(false);
                for (var i = 0; i < initialTransforms.Length; i++)
                    if (initialTransforms[i] != clone.transform)
                        initialTransforms[i].gameObject.SetActive(originalActive[i]);
                for (var i = 0; i < renderers.Length; i++) renderers[i].enabled = rendererEnabled[i];
                if (animator != null) animator.enabled = animatorEnabled;
                root = clone;
                return true;
            }
            catch
            {
                if (lease != null) lease.Release();
                else Release(entry, null);
                if (clone != null) Object.DestroyImmediate(clone);
                throw;
            }
        }

        internal static void Clear()
        {
            var entries = Entries.Values.ToArray();
            Entries.Clear();
            PinnedKeys.Clear();
            foreach (var entry in entries) Retire(entry);
        }

        internal static void Release(Entry entry, IVrm10SpringBoneRuntime? springRuntime)
        {
            if (springRuntime != null) entry.SpringRuntimes.Remove(springRuntime);
            entry.References--;
            entry.LastUsed = ++_sequence;
            if (entry.Retired)
            {
                if (entry.References == 0) DestroyTemplate(entry);
                return;
            }

            TrimIdle();
        }

        private static void TrimIdle()
        {
            while (true)
            {
                Entry? oldest = null;
                var idleCount = 0;
                foreach (var entry in Entries.Values)
                {
                    if (entry.References != 0 || PinnedKeys.Contains(entry.Key)) continue;
                    idleCount++;
                    if (oldest == null || entry.LastUsed < oldest.LastUsed) oldest = entry;
                }

                if (idleCount <= 2 || oldest == null) return;
                Entries.Remove(oldest.Key);
                Retire(oldest);
            }
        }

        private static void Retire(Entry entry)
        {
            entry.Retired = true;
            if (entry.References == 0) DestroyTemplate(entry);
        }

        private static void DestroyTemplate(Entry entry)
        {
            if (entry.Template != null)
            {
                var owner = entry.Template.GetComponent<VrmResourceOwner>();
                if (owner != null) owner.Release();
                Object.DestroyImmediate(entry.Template);
            }

            entry.Materials.Clear();
        }

        internal sealed class Entry
        {
            internal readonly string Key;
            internal readonly HashSet<Material> Materials = new();
            internal readonly HashSet<IVrm10SpringBoneRuntime> SpringRuntimes = new();
            internal readonly GameObject Template;
            internal long LastUsed;
            internal int References;
            internal bool Retired;

            internal Entry(string key, GameObject template)
            {
                Key = key;
                Template = template;
                foreach (var renderer in template.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                    foreach (var material in renderer.sharedMaterials)
                        if (material != null)
                            Materials.Add(material);
                }
            }
        }
    }

    internal sealed class VrmTemplateInstance : MonoBehaviour
    {
        internal VrmTemplateCache.Entry? Entry;
        internal Vrm10Instance? Instance;
        internal IVrm10SpringBoneRuntime? SpringRuntime;

        private void OnDestroy()
        {
            Release();
        }

        internal void Release()
        {
            var entry = Entry;
            if (entry == null) return;
            Entry = null;
            try
            {
                if (Instance != null) Instance.DisposeRuntime();
            }
            finally
            {
                VrmTemplateCache.Release(entry, SpringRuntime);
                SpringRuntime = null;
            }
        }
    }
}
