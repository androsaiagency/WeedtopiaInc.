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
    [InitializeOnLoad] public static class InteractionVisualEvidence
    {
        private const string Key="Weedtopia.InteractionCapture";
        private static string EvidenceRoot=>SessionState.GetString("Weedtopia.InteractionEvidenceRoot","Docs/InteractionRevision");
        public static string Folder=>EvidenceRoot+"/Screenshots";
        private static GameRoot root;
        private static ManualClock clock;
        private static List<Action> steps;
        private static readonly List<string> screenshots=new List<string>();
        private static int index,frames;
        private static string requested,pending;
        private static DateTime requestedUtc;
        private static double deadline,settleUntil;
        static InteractionVisualEvidence(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Begin()
        {BeginAt("Docs/InteractionRevision");}
        public static void BeginEnvironment()
        {BeginAt("Docs/InteractionRevision");}
        public static void BeginAlignment()
        {BeginAt("Docs/BuildingAlignment");}
        public static void BeginBakery()
        {BeginAt("Docs/BakeryDelivery");}
        public static void BeginLivingHills()
        {BeginAt("Docs/LivingHills");}
        public static void BeginDirectionalMotion()
        {BeginAt("Docs/DirectionalMotion");}
        public static void BeginFarmGardenBefore()
        {BeginAt("Docs/FarmGardenRevision/Before");}
        public static void BeginFarmGardenAfter()
        {BeginAt("Docs/FarmGardenRevision/After");}
        public static void BeginWorldNavigation()
        {BeginAt("Docs/WorldNavigationRevision");}
        public static void BeginTownVitality()
        {BeginAt("Docs/TownVitalityRevision");}
        public static void BeginWorldCorners()
        {BeginAt("Docs/WorldCornersRevision");}
        public static void BeginLuckyWheel()
        {BeginAt("Docs/LuckyWheel");}
        public static void BeginVipStories()
        {BeginAt("Docs/VipStories");}
        public static void BeginInventory()
        {BeginAt("Docs/InventoryRevision");}
        public static void BeginRewards()
        {BeginAt("Docs/RewardPresentation");}
        public static void BeginMenuLayout()
        {BeginAt("Docs/MenuLayoutRevision");}
        public static void BeginInventoryWheelDock()
        {BeginAt("Docs/InventoryWheelDock");}
        public static void BeginConveyors()
        {BeginAt("Docs/ConveyorRevision");}
        public static void BeginConveyorCargo()
        {BeginAt("Docs/ConveyorCargoFix");}
        public static void BeginConstructionFences()
        {BeginAt("Docs/ConstructionFences");}
        public static void BeginTownHall()
        {BeginAt(TownHall3DIntegration.ReportFolder);}
        public static void BeginTownHall2DRestore()
        {BeginAt(TownHall2DRestore.ReportFolder);}
        public static void BeginTownHallLightingCompare()
        {BeginAt(TownHallLightingAuthoring.ReportFolder+"/Compare");}
        public static void BeginTownHallLightingFinal()
        {BeginAt(TownHallLightingAuthoring.ReportFolder+"/Final");}
        private static void BeginAt(string folder)
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Owned batch editor required");
            SessionState.SetString("Weedtopia.InteractionEvidenceRoot",folder);
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
                if(++frames<8||EditorApplication.timeSinceStartup<settleUntil){if(EvidenceRoot=="Docs/RewardPresentation"||EvidenceRoot=="Docs/InventoryRevision"||EvidenceRoot=="Docs/LuckyWheel"||EvidenceRoot=="Docs/InventoryWheelDock"||EvidenceRoot=="Docs/VipStories")root.Refresh();EditorApplication.QueuePlayerLoopUpdate();return;}
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
                    string fingerprint=ProductionVisualPreflight.Fingerprint();
                    if(EvidenceRoot=="Docs/VipStories")ProductionUiEvidence.Write(fingerprint,EvidenceRoot+"/ui_overflow.json");else ProductionUiEvidence.Write(fingerprint);
                    SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
                    File.WriteAllText(EvidenceRoot+"/gameview_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fingerprint=fingerprint,screenshots=screenshots.ToArray()},true));
                    EditorApplication.Exit(0);return;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex){SessionState.SetBool(Key,false);File.WriteAllText(EvidenceRoot+"/gameview_error.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        [Serializable] private sealed class Receipt {public string utc,fingerprint;public string mode="actual_playmode_gameview";public bool userAcceptance=false;public string[] screenshots;}
        private static void Capture(string name){requested=Path.GetFullPath(Folder+"/"+name+".png");frames=0;settleUntil=EditorApplication.timeSinceStartup+(name.Contains("wheel_win")?5:name.StartsWith("motion_")?.02:1.5);}
        private static void Drive(Func<bool> condition)
        {
            for(int i=0;i<TutorialDriver.MaxSimulationSeconds&&!condition();i++){TutorialDriver.Act(root.Session,clock.UtcNow);clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Session.Advance(1);}
            if(!condition())throw new InvalidOperationException("Tutorial stalled: "+root.Session.Progression.Current?.id);
            root.Refresh();
        }
        private static void PrepareSteps()
        {
            if(EvidenceRoot=="Docs/InventoryWheelDock")
            {
                root.Session.CompleteIntro();root.Ui.Close();steps=new List<Action>();
                void ShotDock(string name,Action action){steps.Add(()=>{action();root.Refresh();root.Ui.Notify("");Capture(name);});}
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
                {
                    string prefix=size.x+"x"+size.y;
                    ShotDock(prefix+"_01_world_dock",()=>{SetSize(size.x,size.y);root.Session.State.uiScale=size.x==1366?1.15f:1;root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Farm);});
                    ShotDock(prefix+"_02_inventory",()=>root.Ui.Element<UnityEngine.UI.Button>("inventoryDock").onClick.Invoke());
                    ShotDock(prefix+"_03_wheel",()=>{root.Ui.Close();root.Ui.Element<UnityEngine.UI.Button>("wheelDock").onClick.Invoke();});
                }
                ShotDock("wheel_win_from_dock",()=>{SetSize(1920,1080);root.Session.State.uiScale=1;root.Ui.LuckyWheel.Spin();});
                ShotDock("world_free_spin_used",()=>{root.Ui.Rewards.DismissAll();root.Ui.Close();});
                return;
            }
            if(EvidenceRoot=="Docs/LuckyWheel"){steps=LuckyWheelEvidence.Prepare(root,clock,Capture,SetSize);return;}
            if(EvidenceRoot=="Docs/VipStories"){steps=VipStoriesEvidence.Prepare(root,clock,Capture,SetSize);return;}
            if(EvidenceRoot=="Docs/InventoryRevision"){steps=InventoryEvidence.Prepare(root,clock,Capture,SetSize);return;}
            if(EvidenceRoot=="Docs/RewardPresentation"){steps=RewardPresentationEvidence.Prepare(root,clock,Capture,SetSize);return;}
            root.Session.CompleteIntro();root.Ui.Close();root.Session.State.City.unlocked=Zone.Industry;
            root.Session.State.money=100000;root.Session.State.cards=50;
            foreach(var b in root.Session.State.buildings){if(b.family=="settlement_plot")b.family="residential_house";b.level=root.Definitions.Building(b.family).kind==BuildingKind.TownHall?1:10;b.buildingSeconds=0;}
            foreach(var role in root.Definitions.roles)root.Session.Workers.Grant(role.id,4);
            root.Refresh();var ui=root.Ui.Interaction;
            steps=new List<Action>();
            void Shot(string name,Action action){steps.Add(()=>{action();root.Refresh();root.Ui.Notify("");if(EvidenceRoot=="Docs/BakeryDelivery")File.WriteAllText(EvidenceRoot+"/"+name+"_state.json",JsonUtility.ToJson(root.Session.State,true));Capture(name);});}

            if(EvidenceRoot.StartsWith(TownHallLightingAuthoring.ReportFolder+"/",StringComparison.Ordinal))
            {
                TownHallLightingAuthoring.PrepareEvidence(root,Shot,EvidenceRoot.EndsWith("/Compare",StringComparison.Ordinal));
                return;
            }
            if(EvidenceRoot==TownHall2DRestore.ReportFolder)
            {
                TownHall2DRestore.PrepareEvidence(root,Shot);
                return;
            }
            if(EvidenceRoot==TownHall3DIntegration.ReportFolder)
            {
                TownHall3DIntegration.PrepareEvidence(root,Shot);
                return;
            }
            if(EvidenceRoot=="Docs/ConveyorCargoFix")
            {
                ConveyorVisualEvidence.Prepare(root, Shot, true);
                return;
            }
            if(EvidenceRoot=="Docs/ConstructionFences")
            {
                ConstructionFenceEvidence.Prepare(root, Shot);
                foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
                    Shot("fences_"+size.x+"x"+size.y,()=>{SetSize(size.x,size.y);root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(28);root.CameraRig.Focus(root.Site("farm_processing").transform.position+DistrictMap.ToWorld(new Vector2(0,12)));});
                return;
            }
            if(EvidenceRoot=="Docs/ConveyorRevision")
            {
                ConveyorVisualEvidence.Prepare(root, Shot);
                foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
                    Shot("conveyors_"+size.x+"x"+size.y,()=>{SetSize(size.x,size.y);root.CameraRig.SetDiagnosticZoom(47);root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(342,-24)));});
                return;
            }
            if(EvidenceRoot=="Docs/MenuLayoutRevision")
            {
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
                {
                    string prefix=size.x+"x"+size.y;
                    Shot(prefix+"_world",()=>{SetSize(size.x,size.y);root.Session.State.uiScale=size.x==1366?1.15f:1;root.Ui.Close();ui.CloseBuilding();root.CameraRig.EnterDistrict(Zone.Farm);});
                    Shot(prefix+"_team",()=>ui.inventory.onClick.Invoke());
                    Shot(prefix+"_settings",()=>{root.Ui.Close();root.Ui.Element<UnityEngine.UI.Button>("ui.pause").onClick.Invoke();});
                    Shot(prefix+"_orders",()=>{root.Ui.Close();root.Ui.ShowJournal();});
                }
                Shot("team_no_cards",()=>{SetSize(1920,1080);root.Session.State.uiScale=1;root.Session.State.cards=0;root.Ui.ShowWorkers();});
                Shot("team_locked",()=>{root.Session.State.City.unlocked=Zone.Farm;root.Session.State.cards=2;root.Ui.ShowWorkers();});
                return;
            }
            if(EvidenceRoot=="Docs/WorldCornersRevision")
            {
                foreach(var b in root.Session.State.buildings)b.level=1;
                var life=UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>();
                life.diagnosticFreeze=true;
                void View(float x,float y,float zoom)
                {
                    ui.CloseBuilding();root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(zoom);
                    root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(x,y)));life.PresentAt(12);
                }
                Shot("01_overview",()=>root.CameraRig.ShowOverview());
                Shot("02_orchard_context",()=>View(-143,333,82));
                Shot("03_orchard_close",()=>View(-178,364,43));
                Shot("04_botanical_context",()=>View(425,280,115));
                Shot("05_glasshouse_garden",()=>View(473,346,52));
                Shot("06_lookout",()=>View(529,357,30));
                Shot("07_meadow_picnic",()=>View(-185,-158,45));
                Shot("08_pond_connections",()=>View(265,100,105));
                Shot("09_garden_visitors",()=>View(473,346,52));
                Shot("10_garden_visitors_later",()=>life.PresentAt(54));
                Shot("11_ultrawide",()=>{SetSize(2560,1080);root.CameraRig.ShowOverview();});
                Shot("12_portrait",()=>{SetSize(1080,1920);View(476,327,55);});
                return;
            }
            if(EvidenceRoot=="Docs/TownVitalityRevision")
            {
                foreach(var b in root.Session.State.buildings)b.level=1;
                var life=UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>();
                life.diagnosticFreeze=true;
                void View(float x,float y,float zoom)
                {
                    ui.CloseBuilding();root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(zoom);
                    root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(x,y)));life.PresentAt(12);
                }
                Shot("01_overview",()=>root.CameraRig.ShowOverview());
                Shot("02_city_and_park",()=>View(-30,-147,95));
                Shot("03_neighborhood_park",()=>View(-95,-181,43));
                Shot("04_market_details",()=>View(-81,-139,23));
                Shot("05_civic_square",()=>View(34,22,48));
                Shot("06_settlement",()=>View(10,262,58));
                Shot("07_industry",()=>View(-145,48,60));
                Shot("08_orchard",()=>View(455,-126,65));
                Shot("09_neighbors_a",()=>View(31,-121,48));
                Shot("10_neighbors_b",()=>life.PresentAt(36));
                Shot("11_edge_southwest",()=>View(-900,-900,48));
                Shot("12_edge_northeast",()=>View(900,900,48));
                Shot("13_ultrawide",()=>{SetSize(2560,1080);root.CameraRig.ShowOverview();});
                Shot("14_portrait",()=>{SetSize(1080,1920);root.FocusZone(Zone.City);});
                return;
            }
            if(EvidenceRoot=="Docs/WorldNavigationRevision")
            {
                foreach(var b in root.Session.State.buildings)b.level=1;
                Shot("01_overview",()=>{root.CameraRig.ShowOverview();});
                Shot("02_farm",()=>{root.Ui.Element<UnityEngine.UI.Button>("zone.Farm").onClick.Invoke();});
                Shot("03_city",()=>{root.Ui.Element<UnityEngine.UI.Button>("zone.City").onClick.Invoke();});
                Shot("04_settlement",()=>{root.Ui.Element<UnityEngine.UI.Button>("zone.Settlement").onClick.Invoke();});
                Shot("05_industry",()=>{root.Ui.Element<UnityEngine.UI.Button>("zone.Industry").onClick.Invoke();});
                Shot("06_orders",()=>{root.Ui.Element<UnityEngine.UI.Button>("ui.journal").onClick.Invoke();});
                Shot("07_team",()=>{root.Ui.Close();root.Ui.ShowWorkers();});
                foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
                    Shot("navigation_"+size.x+"x"+size.y,()=>{SetSize(size.x,size.y);root.Session.State.uiScale=size.x==1366?1.15f:1;ui.townMenu.SetActive(false);root.CameraRig.ShowOverview();});
                Shot("08_inspector",()=>{SetSize(1920,1080);ui.OpenBuilding("farm_processing");});
                return;
            }
            if(EvidenceRoot.StartsWith("Docs/FarmGardenRevision/",StringComparison.Ordinal))
            {
                var life=UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>();life.diagnosticFreeze=true;
                foreach(var building in root.Session.State.buildings)building.level=1;
                void View(Vector2 point,float zoom){ui.CloseBuilding();root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(zoom);root.CameraRig.Focus(DistrictMap.ToWorld(point));life.PresentAt(12);}
                Shot("01_farm_overview",()=>View(new Vector2(363,-75),105));
                Shot("02_river_connection",()=>View(new Vector2(183,-49),38));
                Shot("03_garden",()=>View(new Vector2(291,-120),43));
                Shot("04_flower_meadow",()=>View(new Vector2(451,-121),62));
                Shot("05_garden_life",()=>{life.PresentAt(27);});
                return;
            }
            if(EvidenceRoot=="Docs/DirectionalMotion")
            {
                void MotionShot(string name,Action action){steps.Add(()=>{root.Refresh();root.Ui.Notify("");action();Capture(name);});}
                DirectionalMotionEvidence.Prepare(root,MotionShot);return;
            }
            if(EvidenceRoot=="Docs/LivingHills")
            {
                var life=UnityEngine.Object.FindFirstObjectByType<LivingTownPresentation>();life.diagnosticFreeze=true;life.PresentAt(12);
                void View(Vector2 point,float zoom){ui.CloseBuilding();root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(zoom);root.CameraRig.Focus(DistrictMap.ToWorld(point));}
                Shot("00_town_overview",()=>{root.CameraRig.ShowOverview();});
                foreach(Zone zone in Enum.GetValues(typeof(Zone)))
                {var z=zone;Shot("district_"+z,()=>{root.FocusZone(z);root.CameraRig.ScrollTo(0);});Shot("expansion_"+z,()=>{root.CameraRig.ScrollTo(1);});}
                Shot("01_civic_garden",()=>View(new Vector2(37,20),44));
                Shot("02_welcome",()=>View(new Vector2(89,37),25));
                Shot("03_cafe_life_a",()=>{life.PresentAt(12);View(new Vector2(18,-70),26);});
                Shot("04_cafe_life_b",()=>{life.PresentAt(19);});
                Shot("05_copper_hills",()=>View(new Vector2(-250,194),95));
                Shot("06_old_kiln",()=>View(new Vector2(-85,202),32));
                Shot("07_hill_trails",()=>View(new Vector2(350,168),88));
                Shot("08_swallow_pond_a",()=>{life.PresentAt(12);View(new Vector2(225,161),36);});
                Shot("09_swallow_pond_b",()=>{life.PresentAt(16);});
                Shot("10_settlement",()=>View(new Vector2(34,340),77));
                Shot("11_farm_walk",()=>View(new Vector2(280,-125),38));
                foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(2560,1080)})
                    Shot("menu_"+size.x+"x"+size.y,()=>{SetSize(size.x,size.y);root.Session.State.uiScale=size.x==1366?1.15f:1;ui.OpenBuilding("coffee_01");});
                Shot("12_maximum_buildings",()=>{SetSize(1920,1080);root.Session.State.uiScale=1;foreach(var b in root.Session.State.buildings)if(b.family!="town_hall")b.level=5000;root.Refresh();ui.CloseBuilding();root.FocusZone(Zone.City);});
                return;
            }
            if(EvidenceRoot=="Docs/BakeryDelivery")
            {
                var baker=root.Session.State.workers.Single(w=>w.role=="baker");
                root.Session.Workers.Assign(baker.id,"bakery_01",0);root.Session.State.Find("butter_factory").butter=32;
                DeliveryState Trip()=>root.Session.State.deliveries.Single(t=>t.workerId==baker.id);
                void RoadView(){ui.CloseBuilding();root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(28);root.CameraRig.Focus(root.Presentation.Route("butter_bakery1").Position(.5f));}
                Shot("01_industry_roads",()=>{root.FocusZone(Zone.Industry);});
                Shot("02_baker_ready",()=>{ui.OpenBuilding("bakery_01");});
                Shot("03_empty_departure",()=>{root.Session.Advance(1);RoadView();});
                Shot("04_empty_outbound",()=>{root.Session.Advance(Math.Max(1,Trip().remainingSeconds/2));});
                Shot("05_loading_at_factory",()=>{root.Session.Advance(Trip().remainingSeconds);});
                Shot("06_loaded_return",()=>{root.Session.Advance(Math.Max(1,Trip().remainingSeconds/2));});
                Shot("07_return_status",()=>{ui.OpenBuilding("bakery_01");});
                Shot("08_home_baking",()=>{root.Session.Advance(Trip().remainingSeconds);});
                Shot("09_western_roads",()=>{ui.CloseBuilding();root.FocusZone(Zone.Industry);root.CameraRig.ScrollTo(1);});
                foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(2560,1080)})
                    Shot("bakery_"+size.x+"x"+size.y,()=>{SetSize(size.x,size.y);root.Session.State.uiScale=size.x==1366?1.15f:1;ui.OpenBuilding("bakery_01");});
                return;
            }
            if(EvidenceRoot=="Docs/BuildingAlignment")
            {
                Shot("city_overview",()=>{ui.CloseBuilding();root.FocusZone(Zone.City);});
                foreach(string id in new[]{"farm_grow_01","farm_processing","farm_warehouse","city_warehouse","coffee_01","settlement_01","butter_factory","supermarket","bakery_01","town_hall"})
                    Shot(id,()=>{ui.OpenBuilding(id);});
                Shot("park",()=>{root.Session.State.Find("settlement_02").family="park";root.Refresh();ui.OpenBuilding("settlement_02");});
                foreach(int level in new[]{0,5000})Shot("coffee_level_"+level,()=>{ui.CloseBuilding();root.Session.State.Find("coffee_01").level=level;root.Refresh();ui.OpenBuilding("coffee_01");});
                foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(2560,1080)})
                    Shot("alignment_"+size.x+"x"+size.y,()=>{SetSize(size.x,size.y);root.Session.State.uiScale=size.x==1366?1.15f:1;ui.CloseBuilding();ui.OpenBuilding("city_warehouse");});
                return;
            }
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080)})
            {
                string prefix=size.x+"x"+size.y;
                Shot(prefix+"_01_free_world",()=>{SetSize(size.x,size.y);root.Session.State.uiScale=size.x==1366?1.15f:1;ui.CloseBuilding();ui.townMenu.SetActive(false);root.Ui.Close();root.FocusZone(Zone.City);});
                Shot(prefix+"_02_building",()=>{ui.OpenBuilding("farm_processing");});
                Shot(prefix+"_03_matching_workers",()=>{ui.OpenPicker(0);});
                Shot(prefix+"_04_assigned",()=>{ui.Assign(root.Session.State.workers.First(w=>w.role=="processing").id);ui.ClosePicker();});
                Shot(prefix+"_05_warehouse",()=>{ui.OpenBuilding("city_warehouse");});
                Shot(prefix+"_06_fleet",()=>{ui.OpenFleet();});
                Shot(prefix+"_07_drivers",()=>{ui.OpenPicker(1);});
                Shot(prefix+"_08_driver_assigned",()=>{ui.Assign(root.Session.State.workers.First(w=>w.role=="city_driver").id);});
                Shot(prefix+"_09_orders",()=>{ui.CloseBuilding();root.Ui.ShowJournal();});
            }
            Shot("1920_fleet_in_transit",()=>{SetSize(1920,1080);root.Session.State.uiScale=1;ui.townMenu.SetActive(false);ui.OpenBuilding("city_warehouse");
                var w=root.Session.State.workers.First(x=>x.role=="city_driver");root.Session.State.deliveries.Add(new DeliveryState{workerId=w.id,route=root.Definitions.routes.First(r=>r.from=="city_warehouse"&&r.vehicle).id,cargo=10,remainingSeconds=10,totalSeconds=15});ui.OpenFleet();});
            foreach(string id in new[]{"farm_grow_01","farm_warehouse","coffee_01","settlement_01","butter_factory","supermarket","bakery_01","town_hall"})
            {
                Shot("family_"+id,()=>{ui.ClosePicker();ui.OpenBuilding(id);});
            }
            foreach(int level in new[]{0,5000})Shot("coffee_level_"+level,()=>{ui.CloseBuilding();root.Session.State.Find("coffee_01").level=level;root.Refresh();ui.OpenBuilding("coffee_01");});
            Shot("settlement_choice",()=>{ui.CloseBuilding();root.Session.State.Find("settlement_02").family="settlement_plot";root.Session.State.Find("settlement_02").level=0;root.Refresh();ui.OpenBuilding("settlement_02");});
            Shot("park_details",()=>{ui.CloseBuilding();root.Session.State.Find("settlement_02").family="park";root.Session.State.Find("settlement_02").level=25;root.Refresh();ui.OpenBuilding("settlement_02");});
            Shot("empty_matching_workers",()=>{ui.OpenBuilding("farm_grow_01");root.Session.State.workers.RemoveAll(w=>w.role=="grow");ui.OpenPicker(0);});
            Shot("warehouse_24_drivers",()=>{ui.CloseBuilding();root.Session.State.Find("farm_warehouse").level=5000;root.Refresh();ui.OpenBuilding("farm_warehouse");ui.OpenFleet();ui.pickerScroll.verticalNormalizedPosition=0;});
            foreach(string panel in new[]{"workers","townhall","journal","settings"})Shot("menu_"+panel,()=>{ui.CloseBuilding();if(panel=="workers")root.Ui.ShowWorkers();else if(panel=="townhall")root.Ui.ShowTownHall();else if(panel=="journal")root.Ui.ShowJournal();else root.Ui.ShowPanel(panel);});
            Shot("premium_confirmation",()=>{root.Ui.Close();ui.OpenBuilding("farm_processing");root.Session.State.money=0;root.Ui.Element<UnityEngine.UI.Button>("ui.upgrade").onClick.Invoke();});
        }
    }
}
