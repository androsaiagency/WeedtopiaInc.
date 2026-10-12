using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Actual rendered Game View captures, using an owned diagnostic save and normal scene objects.</summary>
    [InitializeOnLoad] public static class LayoutVisualEvidence
    {
        private const string Key="Weedtopia.LayoutCapture";
        private static string EvidenceRoot=>SessionState.GetString("Weedtopia.LayoutEvidenceRoot","Docs/LayoutRevision");
        public static string Folder=>EvidenceRoot+"/Screenshots";
        private static GameRoot root;
        private static ManualClock clock;
        private static List<Action> steps;
        private static readonly List<string> screenshots=new List<string>();
        private static int index,frames;
        private static string requested,pending;
        private static DateTime requestedUtc;
        private static double deadline,settleUntil;
        static LayoutVisualEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {BeginAt("Docs/LayoutRevision");}
        public static void BeginEnvironment()
        {BeginAt("Docs/EnvironmentRevision");}
        private static void BeginAt(string folder)
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            SessionState.SetString("Weedtopia.LayoutEvidenceRoot",folder);
            Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene(GameScaffold.GameScene);SetSize(1920,1080);EditorApplication.EnterPlaymode();
        }
        private static void SetSize(int width,int height)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizes);
            var instance=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var group=sizes.GetMethod("GetGroup").Invoke(instance,new object[]{0});
            var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new[]{Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),(object)width,height,"Layout QA "+width+"x"+height});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);view.Repaint();
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(deadline==0)deadline=EditorApplication.timeSinceStartup+600;
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Game View capture deadline");
                if(root==null)
                {
                    root=UnityEngine.Object.FindFirstObjectByType<GameRoot>();if(root==null)return;
                    root.enabled=false;clock=new ManualClock(new DateTime(2026,9,23,12,0,0,DateTimeKind.Utc));
                    root.UseTestStore(Path.GetFullPath(EvidenceRoot+"/capture_save.json"),clock);root.NewGame("Burning Hills");PrepareSteps();
                }
                if(++frames<8||EditorApplication.timeSinceStartup<settleUntil){EditorApplication.QueuePlayerLoopUpdate();return;}
                if(pending!=null)
                {
                    if(!File.Exists(pending)||File.GetLastWriteTimeUtc(pending)<requestedUtc){EditorApplication.QueuePlayerLoopUpdate();return;}
                    screenshots.Add(pending);pending=null;index++;frames=0;return;
                }
                if(requested!=null)
                {
                    pending=requested;requested=null;ProductionUiEvidence.Inspect(root.Ui,Path.GetFileName(pending));
                    requestedUtc=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(pending);frames=0;EditorApplication.QueuePlayerLoopUpdate();return;
                }
                if(index<steps.Count)steps[index]();
                else
                {
                    string fingerprint=ProductionVisualPreflight.Fingerprint();ProductionUiEvidence.Write(fingerprint);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
                    File.WriteAllText(EvidenceRoot+"/gameview_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fingerprint=fingerprint,screenshots=screenshots.ToArray()},true));
                    EditorApplication.Exit(0);return;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){SessionState.SetBool(Key,false);File.WriteAllText(EvidenceRoot+"/gameview_error.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        [Serializable] private sealed class Receipt {public string utc,fingerprint;public string mode="actual_playmode_gameview";public bool userAcceptance=false;public string[] screenshots;}
        private static void Capture(string name){requested=Path.GetFullPath(Folder+"/"+name+".png");frames=0;settleUntil=EditorApplication.timeSinceStartup+1.5;}
        private static void Drive(Func<bool> condition)
        {
            for(int i=0;i<1800&&!condition();i++){TutorialDriver.Act(root.Session,clock.UtcNow);clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Session.Advance(1);}
            if(!condition())throw new InvalidOperationException("Tutorial stalled: "+root.Session.Progression.Current?.id);
            root.Refresh();
        }
        private static void PrepareSteps()
        {
            steps=new List<Action>{()=>Capture("01_intro"),()=>{root.Session.CompleteIntro();root.Ui.Close();root.Session.Upgrades.BuyWithMoney("farm_grow_01");root.Session.Advance(12);root.Select("farm_grow_01",true);root.Refresh();Capture("02_farm_start");},
                ()=>{Drive(()=>root.Session.State.City.unlocked>=Zone.City);root.Select("farm_processing",true);root.Session.Advance(180);root.Refresh();Capture("02b_automated_farm_backlog");},
                ()=>{Drive(()=>root.Session.State.City.freeplay);root.Ui.Close();root.CameraRig.ShowOverview();Capture("03_connected_world");}};
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                var z=zone;steps.Add(()=>{root.FocusZone(z);root.CameraRig.ScrollTo(0);Capture("04_"+z+"_start");});
                steps.Add(()=>{root.CameraRig.ScrollTo(1);Capture("05_"+z+"_expansion");});
            }
            steps.Add(()=>{SetSize(1366,768);root.Session.State.uiScale=1.15f;root.FocusZone(Zone.City);root.Refresh();Capture("06_ui_1366_scale115");});
            steps.Add(()=>{SetSize(2560,1080);root.Session.State.uiScale=1;root.FocusZone(Zone.Farm);root.Refresh();Capture("07_ui_2560");});
            steps.Add(()=>{SetSize(1920,1080);root.Ui.ShowWorkers();root.Refresh();Capture("08_workers");});
            steps.Add(()=>{root.Ui.ShowTownHall();root.Refresh();Capture("09_townhall");});
            steps.Add(()=>{root.Ui.ShowJournal();root.Refresh();Capture("10_journal");});
            steps.Add(()=>{root.Ui.ShowPanel("settings");root.Refresh();Capture("11_settings");});
            steps.Add(()=>{root.Ui.Close();root.Select("farm_grow_01",true);root.Ui.SpeakToSelected();root.Refresh();Capture("12_dialog");});
            steps.Add(()=>{root.Ui.Close();root.Select("farm_grow_01",true);root.Session.State.money=0;root.Ui.Element<UnityEngine.UI.Button>("ui.upgrade").onClick.Invoke();root.Refresh();Capture("12b_premium_confirmation");});
            steps.Add(()=>{root.Ui.Element<UnityEngine.UI.Button>("ui.cancel").onClick.Invoke();root.Ui.ShowWorkers();root.Refresh();Capture("12c_worker_assignment");});
            steps.Add(()=>{root.Ui.Close();root.CameraRig.ShowOverview();var route=root.Presentation.Route("farm_city");root.CameraRig.SetDiagnosticZoom(35);root.CameraRig.Focus(route.Position(.5f));var driver=root.Session.State.workers.First(x=>x.buildingId=="farm_warehouse"&&x.slot>0);root.Session.State.deliveries.Clear();root.Session.State.deliveries.Add(new DeliveryState{route="farm_city",workerId=driver.id,cargo=10,totalSeconds=root.Definitions.routes.First(x=>x.id=="farm_city").travelSeconds,remainingSeconds=root.Definitions.routes.First(x=>x.id=="farm_city").travelSeconds/2});root.Refresh();Capture("13_farm_delivery");});
            if(EvidenceRoot.Contains("EnvironmentRevision"))
            {
                foreach(var spot in new[]{new Vector2(34,36),new Vector2(232,152),new Vector2(166,18),new Vector2(310,-118)})
                {
                    var center=spot;steps.Add(()=>{root.Ui.Close();root.Ui.Notify("");root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(42);root.CameraRig.Focus(DistrictMap.ToWorld(center));Capture("environment_"+(int)center.x+"_"+(int)center.y);});
                }
                steps.Add(()=>{root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(34);root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(166,18)));Capture("environment_motion_a");});
                steps.Add(()=>Capture("environment_motion_b"));
            }
            var samples=new Dictionary<string,string>{{"cultivation_plot","farm_grow_01"},{"farm_processing","farm_processing"},{"farm_warehouse","farm_warehouse"},{"city_warehouse","city_warehouse"},{"coffee_shop","coffee_01"},{"residential_house","settlement_01"},{"park","settlement_02"},{"butter_factory","butter_factory"},{"supermarket","supermarket"},{"bakery","bakery_01"},{"town_hall","town_hall"}};
            foreach(var entry in samples)foreach(int tier in entry.Key=="town_hall"?new[]{5000}:new[]{0,1,200,1000,5000})
            {
                string family=entry.Key,id=entry.Value;int level=tier;
                steps.Add(()=>{root.Ui.Close();var building=root.Session.State.Find(id);building.family=family;building.level=level;building.buildingSeconds=0;
                    root.Select(id);root.Refresh();root.CameraRig.ShowOverview();
                    var visual=root.Site(id).VisualRoot.GetComponentsInChildren<Weedtopia.Buildings.Presentation.BuildingSpriteVisual>().Last();
                    // Frame the actual sprite above the navigator, including the largest approved tiers.
                    float zoom=Mathf.Max(22,visual.AuthoredWorldHeight*.9f);
                    var center=visual.SpriteRenderer.bounds.center;var plan=DistrictMap.ToPlan(center);
                    var up=root.CameraRig.RenderCamera.transform.up;
                    float projectedGround=Vector3.Dot(up,new Vector3(DistrictMap.Diagonal,0,DistrictMap.Diagonal));
                    plan.y+=(center.y*up.y-zoom*.12f)/projectedGround;
                    root.Ui.Notify("");root.CameraRig.SetDiagnosticZoom(zoom);root.CameraRig.Focus(DistrictMap.ToWorld(plan));
                    Capture("building_"+family+"_"+VisualTiers.Id(level,family=="town_hall"));});
            }
        }
    }
}
