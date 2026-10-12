using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weedtopia.Buildings.Presentation
{
    /// <summary>Only displayed tiers own textures. Catalog-prefab references never preload all 251 images.</summary>
    public static class BuildingSpriteCache
    {
        private sealed class Entry { public Sprite sprite; public int references; }
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        public static int LoadedCount => Entries.Count;
        public static Sprite Acquire(string path)
        {
            if (!Entries.TryGetValue(path, out var entry))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Required production building sprite is missing: " + path);
                entry = new Entry { sprite = sprite }; Entries.Add(path, entry);
            }
            entry.references++;
            return entry.sprite;
        }
        public static void Release(string path)
        {
            if (!Entries.TryGetValue(path, out var entry)) return;
            if (--entry.references > 0) return;
            Entries.Remove(path);
            var texture = entry.sprite.texture;
            Resources.UnloadAsset(entry.sprite);
            Resources.UnloadAsset(texture);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Entries.Clear(); }
    }
}
