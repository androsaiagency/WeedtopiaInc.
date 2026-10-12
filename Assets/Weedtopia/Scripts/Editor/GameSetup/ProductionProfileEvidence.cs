using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Diagnostic editor sampling only. Never creates a target-device acceptance receipt.</summary>
    public static class ProductionProfileEvidence
    {
        [Serializable] public sealed class Sample
        {
            public string view;
            public int frames, maxDrawCalls, maxBatches, gcCollections, framesOver16Ms, framesOver33Ms;
            public double medianFrameMs, p95FrameMs, maxFrameMs;
            public long maxGcBytes, allocatedBytes, textureRuntimeBytes;
        }
        [Serializable] private sealed class Report
        {
            public int schemaVersion = 2;
            public string utc, fingerprint, scope, device, graphics, unity, buildContext;
            public bool targetAccepted, releaseAccepted;
            public Sample[] samples;
        }
        private sealed class Window
        {
            public readonly List<double> times = new List<double>();
            public int draws, batches, firstGc, lastGc;
            public long gc, allocated;
        }
        private static readonly Dictionary<string, Window> windows = new Dictionary<string, Window>();
        private static ProfilerRecorder allocations;
        private static int lastFrame = -1;

        public static void Reset()
        {
            if (allocations.Valid) allocations.Dispose();
            allocations = default;
            windows.Clear();
            lastFrame = -1;
        }

        public static void Observe(string view)
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (!allocations.Valid) allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            if (!windows.TryGetValue(view, out var window))
            {
                window = new Window { firstGc = GC.CollectionCount(0) };
                windows.Add(view, window);
            }
            window.lastGc = GC.CollectionCount(0);
            window.times.Add(Time.unscaledDeltaTime * 1000);
            window.draws = Math.Max(window.draws, UnityStats.drawCalls);
            window.batches = Math.Max(window.batches, UnityStats.batches);
            window.allocated = Math.Max(window.allocated, Profiler.GetTotalAllocatedMemoryLong());
            if (allocations.Valid) window.gc = Math.Max(window.gc, allocations.LastValue);
        }

        public static void Write()
        {
            try
            {
                long textures = Resources.FindObjectsOfTypeAll<Texture>().Sum(texture => Profiler.GetRuntimeMemorySizeLong(texture));
                var samples = windows.Select(pair =>
                {
                    var window = pair.Value;
                    var sorted = window.times.OrderBy(time => time).ToArray();
                    return new Sample
                    {
                        view = pair.Key, frames = sorted.Length, maxDrawCalls = window.draws, maxBatches = window.batches,
                        gcCollections = window.lastGc - window.firstGc, maxGcBytes = window.gc,
                        allocatedBytes = window.allocated, textureRuntimeBytes = textures,
                        medianFrameMs = sorted[sorted.Length / 2],
                        p95FrameMs = sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * .95))],
                        maxFrameMs = sorted[sorted.Length - 1],
                        framesOver16Ms = sorted.Count(time => time > 1000d / 60),
                        framesOver33Ms = sorted.Count(time => time > 1000d / 30)
                    };
                }).ToArray();
                var target = EditorUserBuildSettings.activeBuildTarget;
                var report = new Report
                {
                    utc = DateTime.UtcNow.ToString("O"), fingerprint = ProductionVisualPreflight.Fingerprint(),
                    buildContext = ProductionVisualPreflight.BuildContext(target, BuildOptions.None),
                    scope = "Unity Editor Game View, uncapped; includes editor and capture overhead. Frame data is unscaled frame duration, not GPU time. Allocated bytes are the per-view peak Unity allocation. Texture bytes are one final snapshot across all loaded editor textures, not measured VRAM. GC deltas cover each view's sampled interval. This is not player or target-device acceptance.",
                    device = SystemInfo.graphicsDeviceName, graphics = SystemInfo.graphicsDeviceType.ToString(),
                    unity = UnityEngine.Application.unityVersion, targetAccepted = false, releaseAccepted = false, samples = samples
                };
                string path = "Docs/ProductionFinish/Evidence/editor_profile.json";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
            }
            finally { Reset(); }
        }
    }
}
