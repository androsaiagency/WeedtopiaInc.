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
            public string view; public int frames,maxDrawCalls,maxBatches,gcCollections;
            public double medianFrameMs,p95FrameMs;public long maxGcBytes,allocatedBytes,textureRuntimeBytes;
        }
        [Serializable] private sealed class Report
        {
            public string utc,fingerprint,scope,device,graphics,unity;public bool targetAccepted;public Sample[] samples;
        }
        private sealed class Window
        {
            public readonly List<double> times=new List<double>();public int draws,batches,firstGc;public long gc;
        }
        private static readonly Dictionary<string,Window> windows=new Dictionary<string,Window>();
        private static ProfilerRecorder allocations;
        private static int lastFrame=-1;
        public static void Observe(string view)
        {
            if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            if(!allocations.Valid)allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            if(!windows.TryGetValue(view,out var w)){w=new Window{firstGc=GC.CollectionCount(0)};windows.Add(view,w);}
            w.times.Add(Time.unscaledDeltaTime*1000);w.draws=Math.Max(w.draws,UnityStats.drawCalls);w.batches=Math.Max(w.batches,UnityStats.batches);
            if(allocations.Valid)w.gc=Math.Max(w.gc,allocations.LastValue);
        }
        public static void Write()
        {
            long textures=Resources.FindObjectsOfTypeAll<Texture>().Sum(x=>Profiler.GetRuntimeMemorySizeLong(x));
            var samples=windows.Select(pair=>{var w=pair.Value;var sorted=w.times.OrderBy(x=>x).ToArray();return new Sample{view=pair.Key,frames=sorted.Length,maxDrawCalls=w.draws,maxBatches=w.batches,gcCollections=GC.CollectionCount(0)-w.firstGc,maxGcBytes=w.gc,allocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),textureRuntimeBytes=textures,medianFrameMs=sorted[sorted.Length/2],p95FrameMs=sorted[Math.Min(sorted.Length-1,(int)(sorted.Length*.95))]};}).ToArray();
            var report=new Report{utc=DateTime.UtcNow.ToString("O"),fingerprint=ProductionVisualPreflight.Fingerprint(),scope="Windows Unity Editor graphics batch Game View, uncapped; includes editor/capture overhead. Texture bytes cover loaded editor textures, not measured GPU VRAM. GC collection deltas cover the remaining capture run. Not a player build or target acceptance.",device=SystemInfo.graphicsDeviceName,graphics=SystemInfo.graphicsDeviceType.ToString(),unity=UnityEngine.Application.unityVersion,targetAccepted=false,samples=samples};
            File.WriteAllText("Docs/ProductionFinish/Evidence/editor_profile.json",JsonUtility.ToJson(report,true));if(allocations.Valid)allocations.Dispose();
        }
    }
}
