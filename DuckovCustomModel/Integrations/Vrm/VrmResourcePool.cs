using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniGLTF;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal static class VrmResourcePool
    {
        private static readonly Dictionary<string, Entry> Entries = new(StringComparer.Ordinal);

        internal static Entry Acquire(string key)
        {
            if (!Entries.TryGetValue(key, out var entry)) Entries.Add(key, entry = new(key));
            entry.References++;
            return entry;
        }

        internal static void Release(Entry entry)
        {
            if (--entry.References != 0) return;
            if (!entry.Retired && Entries.Count <= 2) return;
            if (Entries.TryGetValue(entry.Key, out var current) && ReferenceEquals(current, entry))
                Entries.Remove(entry.Key);
            foreach (var resource in entry.Textures.Values)
                if (resource != null)
                    Object.Destroy(resource);
            entry.Textures.Clear();
            entry.Gate.Dispose();
        }

        internal static void Clear()
        {
            var entries = Entries.Values.ToArray();
            Entries.Clear();
            foreach (var entry in entries)
            {
                entry.Retired = true;
                if (entry.References != 0) continue;
                entry.References++;
                Release(entry);
            }
        }

        internal sealed class Entry(string key)
        {
            internal readonly SemaphoreSlim Gate = new(1, 1);
            internal readonly string Key = key;
            internal readonly Dictionary<SubAssetKey, Object> Textures = [];
            internal int References;
            internal bool Retired;
        }
    }

    internal sealed class VrmResourceOwner : MonoBehaviour
    {
        internal readonly List<Object> Resources = [];
        internal VrmResourcePool.Entry? Entry;

        private void OnDestroy()
        {
            Release();
        }

        internal void Release()
        {
            foreach (var resource in Resources)
                if (resource != null)
                {
                    if (Application.isPlaying) Destroy(resource);
                    else DestroyImmediate(resource);
                }

            Resources.Clear();
            var entry = Entry;
            Entry = null;
            if (entry != null) VrmResourcePool.Release(entry);
        }
    }
}
