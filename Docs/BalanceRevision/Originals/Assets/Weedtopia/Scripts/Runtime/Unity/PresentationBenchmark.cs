#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Opt-in diagnostic player. Absent from release builds and inert without the exact CLI flag.</summary>
    public sealed class PresentationBenchmark : MonoBehaviour
    {
        [Serializable] private sealed class Measurement
        {
            public string scenario;public int frames,gcCollections,loadedBuildingSprites;public double medianMs,p95Ms,maxMs;
            public long maxGcBytes,maxDrawCalls,maxBatches,textureRuntimeBytes,totalAllocatedBytes;
            public bool gcCounterAvailable,drawCounterAvailable,batchCounterAvailable;
        }
        [Serializable] private sealed class Report
        {
            public string utc,unity,device,graphics,scope;public int width,height;public bool completed,targetAccepted;public Measurement[] measurements;
        }
        private string output;private GameRoot root;private int stage,frames,warmup=90,startGc;private float deadline;
        private readonly List<Measurement> results=new List<Measurement>();private readonly List<double> times=new List<double>();
        private ProfilerRecorder gc,draws,batches;private long maxGc,maxDraw,maxBatch;
        private RenderTexture target;private RenderPipeline.StandardRequest renderRequest;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BeginIfRequested()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--weedtopia-diagnostic");
            if(index<0||index+1>=args.Length)return;
            var obj=new GameObject("ExplicitDiagnosticBenchmark");DontDestroyOnLoad(obj);var runner=obj.AddComponent<PresentationBenchmark>();runner.output=Path.GetFullPath(args[index+1]);runner.deadline=Time.realtimeSinceStartup+120;
            QualitySettings.vSyncCount=0;UnityEngine.Application.targetFrameRate=-1;
        }
        private void Update()
        {
            try
            {
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Diagnostic player exceeded 120 seconds");
                if(root==null)
                {
                    root=FindFirstObjectByType<GameRoot>();if(root==null)return;
                    Directory.CreateDirectory(Path.GetDirectoryName(output));root.UseTestStore(output+".save",new SystemClock());root.NewGame("Presentation benchmark");root.Session.CompleteIntro();root.Ui.Close();
                    target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){name="DiagnosticOffscreenFrame"};target.Create();renderRequest=new RenderPipeline.StandardRequest{destination=target};
                    if(!RenderPipeline.SupportsRenderRequest(root.CameraRig.RenderCamera,renderRequest))throw new InvalidOperationException("Pipeline cannot render the diagnostic request");
                    var canvas=root.Ui.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=root.CameraRig.RenderCamera;canvas.planeDistance=1;canvas.overrideSorting=true;canvas.sortingOrder=32760;
                    gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);Prepare();return;
                }
                // Hidden windows can skip presentation. Explicit URP requests exercise the real camera,
                // scene and UI at a fixed output size without taking focus from the user.
                RenderPipeline.SubmitRenderRequest(root.CameraRig.RenderCamera,renderRequest);
                if(warmup-->0)return;
                if(stage==2&&frames==60){root.Session.Upgrades.BuyWithMoney("farm_grow_01");root.Session.Advance(10);root.Refresh();}
                times.Add(Time.unscaledDeltaTime*1000);frames++;
                if(gc.Valid)maxGc=Math.Max(maxGc,gc.LastValue);if(draws.Valid)maxDraw=Math.Max(maxDraw,draws.LastValue);if(batches.Valid)maxBatch=Math.Max(maxBatch,batches.LastValue);
                if(frames<300)return;
                SaveFrame(new[]{"farm","overview","upgrade"}[stage]);
                var sorted=times.OrderBy(x=>x).ToArray();results.Add(new Measurement{scenario=new[]{"automated_farm","freeplay_overview","upgrade_completion"}[stage],frames=frames,gcCollections=GC.CollectionCount(0)-startGc,loadedBuildingSprites=Weedtopia.Buildings.Presentation.BuildingSpriteCache.LoadedCount,medianMs=sorted[sorted.Length/2],p95Ms=sorted[(int)(sorted.Length*.95)],maxMs=sorted.Last(),maxGcBytes=maxGc,maxDrawCalls=maxDraw,maxBatches=maxBatch,gcCounterAvailable=gc.Valid,drawCounterAvailable=draws.Valid,batchCounterAvailable=batches.Valid,textureRuntimeBytes=Resources.FindObjectsOfTypeAll<Texture>().Sum(x=>Profiler.GetRuntimeMemorySizeLong(x)),totalAllocatedBytes=Profiler.GetTotalAllocatedMemoryLong()});
                stage++;if(stage<3){Prepare();return;}
                var report=new Report{utc=DateTime.UtcNow.ToString("O"),unity=UnityEngine.Application.unityVersion,device=SystemInfo.graphicsDeviceName,graphics=SystemInfo.graphicsDeviceType.ToString(),width=target.width,height=target.height,completed=true,targetAccepted=false,scope="Windows x64 Development player, uncapped Direct3D11, explicit offscreen URP StandardRequest, 1920x1080; diagnostic canvas uses ScreenSpaceCamera. PNG readbacks prove rendered content. Approved building sprites and 56-site district layout. Upgrade crosses the T07/T08 boundary. Texture runtime bytes are not a GPU VRAM measurement. Human playthrough, touch and final target acceptance remain separate.",measurements=results.ToArray()};
                File.WriteAllText(output,JsonUtility.ToJson(report,true));Dispose();UnityEngine.Application.Quit(0);enabled=false;
            }
            catch(Exception ex){File.WriteAllText(output+".error.txt",ex.ToString());Debug.LogException(ex);Dispose();UnityEngine.Application.Quit(1);enabled=false;}
        }
        private void Prepare()
        {
            for(int i=0;i<300&&(stage==0?root.Session.State.City.unlocked<Zone.City:!root.Session.State.City.freeplay);i++){TutorialDriver.Act(root.Session,DateTime.UtcNow);root.Session.Advance(1);}
            if(stage==0&&root.Session.State.City.unlocked<Zone.City||stage>0&&!root.Session.State.City.freeplay)throw new InvalidOperationException("Benchmark progression stalled");
            root.Ui.Close();root.Select(stage==1?"town_hall":"farm_grow_01",true);root.Refresh();
            if(stage==1)root.CameraRig.ShowOverview();else root.CameraRig.SetDiagnosticZoom(28);
            if(stage==2){root.Session.State.Find("farm_grow_01").level=199;root.Session.State.Find("farm_grow_01").buildingSeconds=0;root.Session.State.money=1e18;root.Refresh();}
            times.Clear();frames=0;maxGc=maxDraw=maxBatch=0;startGc=GC.CollectionCount(0);warmup=90;
        }
        private void SaveFrame(string name)
        {
            var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(output+"."+name+".png",pixels.EncodeToPNG());RenderTexture.active=previous;Destroy(pixels);
        }
        private void Dispose(){if(gc.Valid)gc.Dispose();if(draws.Valid)draws.Dispose();if(batches.Valid)batches.Dispose();if(target!=null){target.Release();Destroy(target);}}
    }
}
#endif
